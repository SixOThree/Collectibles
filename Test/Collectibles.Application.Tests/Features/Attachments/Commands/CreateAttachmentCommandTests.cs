using Collectibles.Application.Features.Attachments.Commands;
using Collectibles.Application.Tests.Common;
using Collectibles.Domain.Configuration.Storage;
using Collectibles.Domain.Enums;
using Collectibles.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Collectibles.Application.Tests.Features.Attachments.Commands;

public class CreateAttachmentCommandTests : CommandTestBase<CreateAttachmentCommand, long>
{
    private readonly Mock<IFileProcessingService> _fileProcessingServiceMock;
    private readonly Mock<IApplicationDbContextFactory> _contextFactoryMock;
    private readonly Mock<IFileStorage> _fileStorageMock;
    private readonly Mock<IEventLogService> _eventLogServiceMock;
    private readonly Mock<IAttachmentHashService> _hashServiceMock;

    public CreateAttachmentCommandTests()
    {
        _fileProcessingServiceMock = new Mock<IFileProcessingService>();
        _contextFactoryMock = new Mock<IApplicationDbContextFactory>();
        _fileStorageMock = new Mock<IFileStorage>();
        _eventLogServiceMock = new Mock<IEventLogService>();
        _hashServiceMock = new Mock<IAttachmentHashService>();

        // Setup default behavior for the hash service
        _hashServiceMock
            .Setup(x => x.ComputeHash(It.IsAny<byte[]>()))
            .Returns("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef");

        // Setup context factory to return the test context
        // We need to wrap the context to prevent disposal by the handler
        _contextFactoryMock
            .Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new NonDisposableDbContextWrapper(Context as IApplicationDbContext));

        // Setup default behavior for the file processing service
        _fileProcessingServiceMock
            .Setup(x => x.GeneratePreviewAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[] content, string contentType, CancellationToken ct) =>
                "Generated thumbnail"u8.ToArray());

        // Setup default behavior for file storage
        _fileStorageMock
            .Setup(x => x.SaveFileAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[] content, string fileName, string contentType, long? showcaseId, CancellationToken ct) => $"files/{Guid.NewGuid()}/{fileName}");
    }

    protected override IRequestHandler<CreateAttachmentCommand, long> CreateHandler()
    {
        var storageSettings = Options.Create(new StorageSettings { Provider = StorageProvider.Database });
        return new CreateAttachmentCommandHandler(_contextFactoryMock.Object, _fileProcessingServiceMock.Object, _fileStorageMock.Object, _eventLogServiceMock.Object, _hashServiceMock.Object, storageSettings);
    }

    [Fact]
    public async Task HandleValidCommandShouldCreateAttachment()
    {
        var command = new CreateAttachmentCommand
        {
            Name = "Test Attachment",
            OriginalFilename = "test.pdf",
            FileType = "application/pdf",
            AttachmentType = AttachmentType.Document,
            Base64Content = Convert.ToBase64String("Test content"u8.ToArray()),
            Base64PreviewThumbnail = Convert.ToBase64String("Thumbnail content"u8.ToArray()),
        };

        var result = await Act(command);

        result.Should().BeGreaterThan(0);

        var attachment = await Context.Attachments
            .Include(a => a.AttachmentContent)
            .Include(a => a.AttachmentPreview)
            .FirstOrDefaultAsync(a => a.Id == result);
        attachment.Should().NotBeNull();
        attachment!.Name.Should().Be(command.Name);
        attachment.OriginalFilename.Should().Be(command.OriginalFilename);
        attachment.FileType.Should().Be(command.FileType);
        attachment.AttachmentType.Should().Be(command.AttachmentType);
        attachment.AttachmentContent.Should().NotBeNull();
        attachment.AttachmentContent!.Content.Should().NotBeNull();
        attachment.AttachmentPreview.Should().NotBeNull();
        attachment.AttachmentPreview!.PreviewThumbnail.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleCommandWithoutOptionalFieldsShouldCreateAttachment()
    {
        var command = new CreateAttachmentCommand
        {
            Name = "Simple Attachment",
        };

        var result = await Act(command);

        result.Should().BeGreaterThan(0);

        var attachment = await Context.Attachments
            .Include(a => a.AttachmentContent)
            .Include(a => a.AttachmentPreview)
            .FirstOrDefaultAsync(a => a.Id == result);
        attachment.Should().NotBeNull();
        attachment!.Name.Should().Be(command.Name);
        attachment.OriginalFilename.Should().BeNull();
        attachment.FileType.Should().BeNull();
        attachment.AttachmentType.Should().BeNull();
        attachment.AttachmentContent.Should().BeNull();
        attachment.AttachmentPreview.Should().BeNull();
    }

    [Fact]
    public async Task HandleCommandWithEmptyBase64ShouldCreateAttachmentWithNullContent()
    {
        var command = new CreateAttachmentCommand
        {
            Name = "Test Attachment",
            Base64Content = string.Empty,
            Base64PreviewThumbnail = string.Empty,
        };

        var result = await Act(command);

        result.Should().BeGreaterThan(0);

        var attachment = await Context.Attachments
            .Include(a => a.AttachmentContent)
            .Include(a => a.AttachmentPreview)
            .FirstOrDefaultAsync(a => a.Id == result);
        attachment.Should().NotBeNull();
        attachment!.AttachmentContent.Should().BeNull();
        attachment.AttachmentPreview.Should().BeNull();
    }

    [Fact]
    public async Task HandleValidCommandShouldIncrementAttachmentCount()
    {
        var initialCount = await CountAsync<Attachment>();

        var command = new CreateAttachmentCommand
        {
            Name = "Test Attachment",
        };

        await Act(command);

        var finalCount = await CountAsync<Attachment>();
        finalCount.Should().Be(initialCount + 1);
    }

    [Theory]
    [InlineData("SGVsbG8gV29ybGQ=")]
    [InlineData("VGVzdCBkYXRh")]
    [InlineData("")]
    public async Task HandleValidBase64ContentShouldCreateAttachment(string base64Content)
    {
        var command = new CreateAttachmentCommand
        {
            Name = "Test Attachment",
            Base64Content = base64Content,
        };

        var result = await Act(command);

        result.Should().BeGreaterThan(0);

        var attachment = await Context.Attachments
            .Include(a => a.AttachmentContent)
            .Include(a => a.AttachmentPreview)
            .FirstOrDefaultAsync(a => a.Id == result);
        attachment.Should().NotBeNull();

        if (string.IsNullOrEmpty(base64Content))
        {
            attachment!.AttachmentContent.Should().BeNull();
        }
        else
        {
            attachment!.AttachmentContent.Should().NotBeNull();
            attachment.AttachmentContent!.Content.Should().Equal(Convert.FromBase64String(base64Content));
        }
    }

    [Fact]
    public async Task HandleCommandWithAllAttachmentTypesShouldCreateCorrectly()
    {
        var attachmentTypes = Enum.GetValues<AttachmentType>();
        var results = new List<long>();

        foreach (var type in attachmentTypes)
        {
            var command = new CreateAttachmentCommand
            {
                Name = $"Test {type} Attachment",
                AttachmentType = type,
            };

            var result = await Act(command);
            results.Add(result);
        }

        results.Should().HaveCount(attachmentTypes.Length);
        results.Should().OnlyContain(id => id > 0);

        foreach (var (type, index) in attachmentTypes.Select((t, i) => (t, i)))
        {
            var attachment = await Context.Attachments
                .FirstOrDefaultAsync(a => a.Id == results[index]);
            attachment.Should().NotBeNull();
            attachment!.AttachmentType.Should().Be(type);
        }
    }

    [Fact]
    public async Task HandleCommandWithContentButNoThumbnailShouldGenerateThumbnail()
    {
        var command = new CreateAttachmentCommand
        {
            Name = "Test Image",
            FileType = "image/jpeg",
            AttachmentType = AttachmentType.Image,
            Base64Content = Convert.ToBase64String("Test image content"u8.ToArray()),

            // No Base64PreviewThumbnail provided
        };

        var result = await Act(command);

        result.Should().BeGreaterThan(0);

        var attachment = await Context.Attachments
            .Include(a => a.AttachmentContent)
            .Include(a => a.AttachmentPreview)
            .FirstOrDefaultAsync(a => a.Id == result);
        attachment.Should().NotBeNull();
        attachment!.AttachmentPreview.Should().NotBeNull();
        attachment.AttachmentPreview!.PreviewThumbnail.Should().Equal("Generated thumbnail"u8.ToArray());

        // Verify the file processing service was called
        _fileProcessingServiceMock.Verify(
            x => x.GeneratePreviewAsync(
                It.IsAny<byte[]>(),
                "image/jpeg",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleCommandWithProvidedThumbnailShouldNotGenerateThumbnail()
    {
        var providedThumbnail = "Provided thumbnail"u8.ToArray();
        var command = new CreateAttachmentCommand
        {
            Name = "Test Image",
            FileType = "image/jpeg",
            AttachmentType = AttachmentType.Image,
            Base64Content = Convert.ToBase64String("Test image content"u8.ToArray()),
            Base64PreviewThumbnail = Convert.ToBase64String(providedThumbnail),
        };

        var result = await Act(command);

        result.Should().BeGreaterThan(0);

        var attachment = await Context.Attachments
            .Include(a => a.AttachmentContent)
            .Include(a => a.AttachmentPreview)
            .FirstOrDefaultAsync(a => a.Id == result);
        attachment.Should().NotBeNull();
        attachment!.AttachmentPreview.Should().NotBeNull();
        attachment.AttachmentPreview!.PreviewThumbnail.Should().Equal(providedThumbnail);

        // Verify the file processing service was NOT called
        _fileProcessingServiceMock.Verify(
            x => x.GeneratePreviewAsync(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
