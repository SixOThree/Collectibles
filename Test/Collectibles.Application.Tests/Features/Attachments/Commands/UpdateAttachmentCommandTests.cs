using Collectibles.Application.Features.Attachments.Commands;
using Collectibles.Application.Tests.Common;
using Collectibles.Application.Tests.Common.TestDataBuilders;
using Collectibles.Domain.Configuration.Storage;
using Collectibles.Domain.Enums;
using Collectibles.Domain.Interfaces;

using Microsoft.Extensions.Options;

namespace Collectibles.Application.Tests.Features.Attachments.Commands;

public class UpdateAttachmentCommandTests : BaseTestFixture
{
    private readonly Mock<IApplicationDbContextFactory> _contextFactoryMock;
    private readonly Mock<IFileStorage> _fileStorageMock;
    private readonly Mock<IFileProcessingService> _fileProcessingServiceMock;
    private readonly Mock<IEventLogService> _eventLogServiceMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;

    public UpdateAttachmentCommandTests()
    {
        _contextFactoryMock = new Mock<IApplicationDbContextFactory>();
        _fileStorageMock = new Mock<IFileStorage>();
        _fileProcessingServiceMock = new Mock<IFileProcessingService>();
        _eventLogServiceMock = new Mock<IEventLogService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.UserId).Returns("test-user-id");

        // Setup context factory to return the test context
        // We need to wrap the context to prevent disposal by the handler
        _contextFactoryMock
            .Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new NonDisposableDbContextWrapper(Context as IApplicationDbContext));

        // Setup default behavior for file storage
        _fileStorageMock
            .Setup(x => x.SaveFileAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[] content, string fileName, string contentType, long? showcaseId, CancellationToken ct) => $"files/{Guid.NewGuid()}/{fileName}");

        _fileStorageMock
            .Setup(x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup default behavior for file processing service
        _fileProcessingServiceMock
            .Setup(x => x.GeneratePreviewAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[] content, string contentType, CancellationToken ct) =>
                "Generated thumbnail"u8.ToArray());
    }

    private UpdateAttachmentCommandHandler CreateHandler()
    {
        var storageSettings = Options.Create(new StorageSettings { Provider = StorageProvider.Database });
        return new UpdateAttachmentCommandHandler(_contextFactoryMock.Object, _fileStorageMock.Object, _fileProcessingServiceMock.Object, _eventLogServiceMock.Object, storageSettings, _currentUserServiceMock.Object);
    }

    private async Task Act(UpdateAttachmentCommand command)
    {
        var handler = CreateHandler();
        await handler.Handle(command, CancellationToken);
    }

    [Fact]
    public async Task HandleValidCommandShouldUpdateAttachment()
    {
        var attachment = new AttachmentBuilder()
            .WithName("Original Name")
            .WithOriginalFilename("original.pdf")
            .WithFileType("application/pdf")
            .WithAttachmentType(AttachmentType.Document)
            .WithContent("Original content"u8.ToArray())
            .WithPreviewThumbnail("Original thumbnail"u8.ToArray())
            .Build();
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        var command = new UpdateAttachmentCommand
        {
            Id = attachment.Id,
            Name = "Updated Name",
            OriginalFilename = "updated.pdf",
            FileType = "application/octet-stream",
            AttachmentType = AttachmentType.Image,
            Base64Content = Convert.ToBase64String("Updated content"u8.ToArray()),
            Base64PreviewThumbnail = Convert.ToBase64String("Updated thumbnail"u8.ToArray()),
        };

        await Act(command);

        var updatedAttachment = await Context.Attachments
            .Include(a => a.AttachmentContent)
            .Include(a => a.AttachmentPreview)
            .FirstOrDefaultAsync(a => a.Id == attachment.Id);
        updatedAttachment.Should().NotBeNull();
        updatedAttachment!.Name.Should().Be(command.Name);
        updatedAttachment.OriginalFilename.Should().Be(command.OriginalFilename);
        updatedAttachment.FileType.Should().Be(command.FileType);
        updatedAttachment.AttachmentType.Should().Be(command.AttachmentType);
        updatedAttachment.AttachmentContent.Should().NotBeNull();
        updatedAttachment.AttachmentContent!.Content.Should().Equal(Convert.FromBase64String(command.Base64Content!));
        updatedAttachment.AttachmentPreview.Should().NotBeNull();
        updatedAttachment.AttachmentPreview!.PreviewThumbnail.Should().Equal(Convert.FromBase64String(command.Base64PreviewThumbnail!));
    }

    [Fact]
    public async Task HandleCommandWithoutBase64ContentShouldNotUpdateContent()
    {
        var originalContent = "Original content"u8.ToArray();
        var originalThumbnail = "Original thumbnail"u8.ToArray();

        var attachment = new AttachmentBuilder()
            .WithName("Original Name")
            .WithContent(originalContent)
            .WithPreviewThumbnail(originalThumbnail)
            .Build();
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        var command = new UpdateAttachmentCommand
        {
            Id = attachment.Id,
            Name = "Updated Name",
            Base64Content = null,
            Base64PreviewThumbnail = null,
        };

        await Act(command);

        var updatedAttachment = await Context.Attachments
            .Include(a => a.AttachmentContent)
            .Include(a => a.AttachmentPreview)
            .FirstOrDefaultAsync(a => a.Id == attachment.Id);
        updatedAttachment.Should().NotBeNull();
        updatedAttachment!.Name.Should().Be(command.Name);
        updatedAttachment.AttachmentContent.Should().NotBeNull();
        updatedAttachment.AttachmentContent!.Content.Should().Equal(originalContent);
        updatedAttachment.AttachmentPreview.Should().NotBeNull();
        updatedAttachment.AttachmentPreview!.PreviewThumbnail.Should().Equal(originalThumbnail);
    }

    [Fact]
    public async Task HandleCommandWithEmptyBase64ContentShouldNotUpdateContent()
    {
        var originalContent = "Original content"u8.ToArray();
        var originalThumbnail = "Original thumbnail"u8.ToArray();

        var attachment = new AttachmentBuilder()
            .WithName("Original Name")
            .WithContent(originalContent)
            .WithPreviewThumbnail(originalThumbnail)
            .Build();
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        var command = new UpdateAttachmentCommand
        {
            Id = attachment.Id,
            Name = "Updated Name",
            Base64Content = string.Empty,
            Base64PreviewThumbnail = string.Empty,
        };

        await Act(command);

        var updatedAttachment = await Context.Attachments
            .Include(a => a.AttachmentContent)
            .Include(a => a.AttachmentPreview)
            .FirstOrDefaultAsync(a => a.Id == attachment.Id);
        updatedAttachment.Should().NotBeNull();
        updatedAttachment!.Name.Should().Be(command.Name);
        updatedAttachment.AttachmentContent.Should().NotBeNull();
        updatedAttachment.AttachmentContent!.Content.Should().Equal(originalContent);
        updatedAttachment.AttachmentPreview.Should().NotBeNull();
        updatedAttachment.AttachmentPreview!.PreviewThumbnail.Should().Equal(originalThumbnail);
    }

    [Fact]
    public async Task HandleNonExistentIdShouldThrowArgumentException()
    {
        var command = new UpdateAttachmentCommand
        {
            Id = 999,
            Name = "Test Name",
        };

        var act = async () => await Act(command);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Attachment with ID 999 not found.*");
    }

    [Fact]
    public async Task HandleUpdateToNullValuesShouldSetFieldsToNull()
    {
        var attachment = new AttachmentBuilder()
            .WithName("Original Name")
            .WithOriginalFilename("original.pdf")
            .WithFileType("application/pdf")
            .WithAttachmentType(AttachmentType.Document)
            .Build();
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        var command = new UpdateAttachmentCommand
        {
            Id = attachment.Id,
            Name = "Updated Name",
            OriginalFilename = null,
            FileType = null,
            AttachmentType = null,
        };

        await Act(command);

        var updatedAttachment = await Context.Attachments
            .FirstOrDefaultAsync(a => a.Id == attachment.Id);
        updatedAttachment.Should().NotBeNull();
        updatedAttachment!.Name.Should().Be(command.Name);
        updatedAttachment.OriginalFilename.Should().BeNull();
        updatedAttachment.FileType.Should().BeNull();
        updatedAttachment.AttachmentType.Should().BeNull();
    }

    [Theory]
    [InlineData("SGVsbG8gV29ybGQ=")]
    [InlineData("VGVzdCBkYXRh")]
    public async Task HandleValidBase64ContentShouldUpdateContent(string base64Content)
    {
        var attachment = new AttachmentBuilder()
            .WithName("Original Name")
            .WithContent("Original content"u8.ToArray())
            .Build();
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        var command = new UpdateAttachmentCommand
        {
            Id = attachment.Id,
            Name = "Updated Name",
            Base64Content = base64Content,
        };

        await Act(command);

        var updatedAttachment = await Context.Attachments
            .Include(a => a.AttachmentContent)
            .Include(a => a.AttachmentPreview)
            .FirstOrDefaultAsync(a => a.Id == attachment.Id);
        updatedAttachment.Should().NotBeNull();
        updatedAttachment!.AttachmentContent.Should().NotBeNull();
        updatedAttachment.AttachmentContent!.Content.Should().Equal(Convert.FromBase64String(base64Content));
    }

    [Fact]
    public async Task HandleUpdateAllAttachmentTypesShouldUpdateCorrectly()
    {
        var attachmentTypes = Enum.GetValues<AttachmentType>();

        foreach (var originalType in attachmentTypes)
        {
            var attachment = new AttachmentBuilder()
                .WithName($"Original {originalType}")
                .WithAttachmentType(originalType)
                .Build();
            Context.Attachments.Add(attachment);
            await Context.SaveChangesAsync();

            foreach (var newType in attachmentTypes)
            {
                var command = new UpdateAttachmentCommand
                {
                    Id = attachment.Id,
                    Name = $"Updated {newType}",
                    AttachmentType = newType,
                };

                await Act(command);

                var updatedAttachment = await Context.Attachments
                    .FirstOrDefaultAsync(a => a.Id == attachment.Id);
                updatedAttachment.Should().NotBeNull();
                updatedAttachment!.AttachmentType.Should().Be(newType);
            }

            Context.Attachments.Remove(attachment);
            await Context.SaveChangesAsync();
        }
    }
}
