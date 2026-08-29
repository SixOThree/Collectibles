using Collectibles.Application.Features.Attachments.Commands;
using Collectibles.Application.Features.Attachments.Dtos;
using Collectibles.Application.Tests.Common;
using Collectibles.Domain.Configuration.Storage;
using Collectibles.Domain.Interfaces;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Collectibles.Application.Tests.Features.Attachments.Commands;

/// <summary>
/// Pins the invariant that at least one durable copy of an attachment's content exists at
/// every point in these flows: irreversible storage deletes must follow a committed write,
/// and rollback must refuse when the database copy is gone.
/// </summary>
public class AttachmentContentLifecycleTests : BaseTestFixture
{
    private readonly Mock<IApplicationDbContextFactory> _contextFactoryMock = new();
    private readonly Mock<IFileStorage> _fileStorageMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IEventLogService> _eventLogServiceMock = new();

    public AttachmentContentLifecycleTests()
    {
        _contextFactoryMock
            .Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new NonDisposableDbContextWrapper(Context));

        _currentUserServiceMock.Setup(x => x.UserId).Returns("test-user-id");
        _currentUserServiceMock.Setup(x => x.IsAdministrator).Returns(true);
    }

    [Fact]
    public async Task RollbackRefusesWhenTheDatabaseCopyHasAlreadyBeenCleanedUp()
    {
        // migrate -> cleanup (database copy nulled) -> rollback(DeleteFromStorage) used to
        // delete the blob and null FilePath, leaving content in neither place.
        var attachment = new Attachment
        {
            Name = "Migrated",
            FilePath = "blobs/migrated.bin",
            IsMigrated = true,
            MigrationDate = DateTime.UtcNow,
            AttachmentContent = new AttachmentContent { Content = null },
        };
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        var handler = new RollbackAttachmentMigrationCommandHandler(
            Context,
            _fileStorageMock.Object,
            Mock.Of<ILogger<RollbackAttachmentMigrationCommandHandler>>(),
            _currentUserServiceMock.Object);

        var result = await handler.Handle(
            new RollbackAttachmentMigrationCommand { DeleteFromStorage = true },
            CancellationToken);

        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(1);
        result.Errors.Should().ContainSingle()
            .Which.ErrorType.Should().Be(RollbackErrorType.MissingDatabaseCopy);

        _fileStorageMock.Verify(
            x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        var reloaded = await Context.Attachments.FirstAsync(a => a.Id == attachment.Id);
        reloaded.FilePath.Should().Be("blobs/migrated.bin");
        reloaded.IsMigrated.Should().BeTrue();
    }

    [Fact]
    public async Task RollbackCommitsTheDatabaseChangeBeforeDeletingTheBlob()
    {
        var attachment = new Attachment
        {
            Name = "Migrated",
            FilePath = "blobs/migrated.bin",
            PreviewPath = "blobs/migrated_preview.jpg",
            IsMigrated = true,
            AttachmentContent = new AttachmentContent { Content = "content"u8.ToArray() },
        };
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        var filePathWhenBlobDeleted = "not captured";
        _fileStorageMock
            .Setup(x => x.DeleteFileAsync("blobs/migrated.bin", It.IsAny<CancellationToken>()))
            .Callback(() => filePathWhenBlobDeleted = Context.Attachments
                .IgnoreQueryFilters()
                .First(a => a.Id == attachment.Id).FilePath ?? "<null>")
            .Returns(Task.CompletedTask);

        var handler = new RollbackAttachmentMigrationCommandHandler(
            Context,
            _fileStorageMock.Object,
            Mock.Of<ILogger<RollbackAttachmentMigrationCommandHandler>>(),
            _currentUserServiceMock.Object);

        var result = await handler.Handle(
            new RollbackAttachmentMigrationCommand { DeleteFromStorage = true },
            CancellationToken);

        result.SuccessCount.Should().Be(1);
        filePathWhenBlobDeleted.Should().Be("<null>", "the row must be updated and committed before the blob is destroyed");
    }

    [Fact]
    public async Task UpdateKeepsTheOldBlobWhenSavingTheNewContentFails()
    {
        var attachment = new Attachment
        {
            Name = "Original",
            FilePath = "blobs/original.bin",
            FileType = "application/pdf",
            CreatedBy = "test-user-id",
        };
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        _fileStorageMock
            .Setup(x => x.SaveFileAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("storage outage"));

        var handler = new UpdateAttachmentCommandHandler(
            _contextFactoryMock.Object,
            _fileStorageMock.Object,
            Mock.Of<IFileProcessingService>(),
            _eventLogServiceMock.Object,
            Options.Create(new StorageSettings()),
            _currentUserServiceMock.Object);

        var act = async () => await handler.Handle(
            new UpdateAttachmentCommand
            {
                Id = attachment.Id,
                Name = "Original",
                FileType = "application/pdf",
                Base64Content = Convert.ToBase64String("new content"u8.ToArray()),
            },
            CancellationToken);

        await act.Should().ThrowAsync<IOException>();

        // The old blob is still referenced and was never deleted, so the content survives.
        _fileStorageMock.Verify(
            x => x.DeleteFileAsync("blobs/original.bin", It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
