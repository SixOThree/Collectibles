using Collectibles.Application.Features.Attachments.Commands;
using Collectibles.Application.Tests.Common;
using Collectibles.Domain.Interfaces;

namespace Collectibles.Application.Tests.Features.Attachments.Commands;

public class DeleteAttachmentCommandTests : BaseTestFixture
{
    private readonly Mock<IApplicationDbContextFactory> _contextFactoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IFileStorage> _fileStorageMock;
    private readonly Mock<IEventLogService> _eventLogServiceMock;

    public DeleteAttachmentCommandTests()
    {
        _contextFactoryMock = new Mock<IApplicationDbContextFactory>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _fileStorageMock = new Mock<IFileStorage>();
        _eventLogServiceMock = new Mock<IEventLogService>();

        // Setup context factory to return the test context
        // We need to wrap the context to prevent disposal by the handler
        _contextFactoryMock
            .Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new NonDisposableDbContextWrapper(Context as IApplicationDbContext));

        // Setup current user service with a default user ID
        _currentUserServiceMock
            .Setup(x => x.UserId)
            .Returns("test-user-id");

        // Setup default behavior for file storage
        _fileStorageMock
            .Setup(x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private DeleteAttachmentCommandHandler CreateHandler()
    {
        return new DeleteAttachmentCommandHandler(_contextFactoryMock.Object, _currentUserServiceMock.Object, _fileStorageMock.Object, _eventLogServiceMock.Object);
    }

    private async Task Act(DeleteAttachmentCommand command)
    {
        var handler = CreateHandler();
        await handler.Handle(command, CancellationToken);
    }

    /// <summary>
    /// Attachments are soft-deleted: the row survives (so the purge job can reclaim it and
    /// its storage files later) but the global query filter hides it from every read.
    /// </summary>
    private async Task AssertSoftDeletedAsync(long attachmentId)
    {
        var visible = await Context.Attachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId);
        visible.Should().BeNull();

        var row = await Context.Attachments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == attachmentId);
        row.Should().NotBeNull();
        row!.Deleted.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleValidIdShouldDeleteAttachment()
    {
        var attachment = new Attachment
        {
            Name = "Test Attachment",
            OriginalFilename = "test.pdf",
            FileType = "application/pdf",
            AttachmentType = AttachmentType.Document,
            CreatedBy = "test-user-id",
        };
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        var command = new DeleteAttachmentCommand(attachment.Id);

        await Act(command);

        await AssertSoftDeletedAsync(attachment.Id);
    }

    [Fact]
    public async Task HandleValidIdShouldDecrementAttachmentCount()
    {
        var attachment = new Attachment
        {
            Name = "Test Attachment",
            CreatedBy = "test-user-id",
        };
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        var initialCount = await Context.Attachments.CountAsync();

        var command = new DeleteAttachmentCommand(attachment.Id);
        await Act(command);

        // The soft-delete query filter hides the row from every ordinary read.
        var finalCount = await Context.Attachments.CountAsync();
        finalCount.Should().Be(initialCount - 1);
    }

    [Fact]
    public async Task HandleNonExistentIdShouldThrowArgumentException()
    {
        var command = new DeleteAttachmentCommand(999);

        var act = async () => await Act(command);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Attachment with ID 999 not found.*");
    }

    [Fact]
    public async Task HandleDeleteAttachmentWithContentShouldDeleteCompletely()
    {
        var attachment = new Attachment
        {
            Name = "Test Attachment",
            CreatedBy = "test-user-id",
        };

        attachment.AttachmentContent = new AttachmentContent
        {
            Id = attachment.Id,
            Content = "Test content"u8.ToArray(),
            Attachment = attachment,
        };

        attachment.AttachmentPreview = new AttachmentPreview
        {
            Id = attachment.Id,
            PreviewThumbnail = "Test thumbnail"u8.ToArray(),
            Attachment = attachment,
        };

        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        var command = new DeleteAttachmentCommand(attachment.Id);

        await Act(command);

        await AssertSoftDeletedAsync(attachment.Id);
    }

    [Fact]
    public async Task HandleDeleteMultipleAttachmentsShouldDeleteAllCorrectly()
    {
        var attachments = new[]
        {
            new Attachment { Name = "Attachment 1", CreatedBy = "test-user-id" },
            new Attachment { Name = "Attachment 2", CreatedBy = "test-user-id" },
            new Attachment { Name = "Attachment 3", CreatedBy = "test-user-id" },
        };

        Context.Attachments.AddRange(attachments);
        await Context.SaveChangesAsync();

        var initialCount = await Context.Attachments.CountAsync();

        foreach (var attachment in attachments)
        {
            var command = new DeleteAttachmentCommand(attachment.Id);
            await Act(command);
        }

        var finalCount = await Context.Attachments.CountAsync();
        finalCount.Should().Be(initialCount - attachments.Length);

        foreach (var attachment in attachments)
        {
            await AssertSoftDeletedAsync(attachment.Id);
        }
    }

    [Fact]
    public async Task HandleDeleteAllAttachmentTypesShouldDeleteCorrectly()
    {
        var attachmentTypes = Enum.GetValues<AttachmentType>();
        var attachments = new List<Attachment>();

        foreach (var type in attachmentTypes)
        {
            var attachment = new Attachment
            {
                Name = $"Test {type} Attachment",
                AttachmentType = type,
                CreatedBy = "test-user-id",
            };
            attachments.Add(attachment);
        }

        Context.Attachments.AddRange(attachments);
        await Context.SaveChangesAsync();

        foreach (var attachment in attachments)
        {
            var command = new DeleteAttachmentCommand(attachment.Id);
            await Act(command);

            await AssertSoftDeletedAsync(attachment.Id);
        }
    }

    [Fact]
    public async Task HandleDeleteNonExistentAfterValidDeleteShouldThrowArgumentException()
    {
        var attachment = new Attachment
        {
            Name = "Test Attachment",
            CreatedBy = "test-user-id",
        };
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        var command = new DeleteAttachmentCommand(attachment.Id);
        await Act(command);

        var secondDeleteAct = async () => await Act(command);

        await secondDeleteAct.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Attachment with ID {attachment.Id} not found.*");
    }
}
