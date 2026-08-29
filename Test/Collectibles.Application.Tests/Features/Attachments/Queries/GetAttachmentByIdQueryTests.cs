using Collectibles.Application.Common.Authorization.Requirements;
using Collectibles.Application.Features.Attachments;
using Collectibles.Application.Features.Attachments.Queries;
using Collectibles.Application.Mappings.Explicit;
using Collectibles.Application.Tests.Common;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Tests.Features.Attachments.Queries;

public class GetAttachmentByIdQueryTests : QueryTestBase<GetAttachmentByIdQuery, AttachmentDto>
{
    private readonly Mock<IAttachmentMappingService> _attachmentMappingServiceMock = new();
    private readonly Mock<IAuthorizationService> _authorizationServiceMock = new();
    private readonly Mock<ILogger<GetAttachmentByIdQueryHandler>> _loggerMock = new();

    public GetAttachmentByIdQueryTests()
    {
        // Setup default behavior for the mapping service in constructor
        // so that test-specific setups (called after constructor) take precedence
        _attachmentMappingServiceMock.Setup(x => x.MapWithContentAsync(
                It.IsAny<Domain.Entities.Attachment>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Attachment a, CancellationToken ct) =>
                new AttachmentDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    OriginalFilename = a.OriginalFilename,
                    FileType = a.FileType,
                    AttachmentType = a.AttachmentType,
                    Created = a.Created ?? DateTime.MinValue,
                    CreatedBy = a.CreatedBy,
                    LastModified = a.LastModified,
                    LastModifiedBy = a.LastModifiedBy,
                    Base64Content = a.AttachmentContent?.Content != null ? Convert.ToBase64String(a.AttachmentContent.Content) :
                                   a.FilePath != null ? Convert.ToBase64String("External file content"u8.ToArray()) : null,
                    Base64PreviewThumbnail = a.AttachmentPreview?.PreviewThumbnail != null ? Convert.ToBase64String(a.AttachmentPreview.PreviewThumbnail) :
                                             a.PreviewPath != null ? Convert.ToBase64String("External preview content"u8.ToArray()) : null,
                });

        _authorizationServiceMock.Setup(x => x.AuthorizeAsync(
                It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                It.IsAny<object>(),
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());
    }

    protected override IRequestHandler<GetAttachmentByIdQuery, AttachmentDto> CreateHandler()
    {
        return new GetAttachmentByIdQueryHandler(
            Context,
            _attachmentMappingServiceMock.Object,
            _authorizationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleExistingAttachmentShouldReturnAttachmentDto()
    {
        var attachment = CreateTestAttachment("Test Attachment", "test.pdf", "application/pdf", AttachmentType.Document);
        await Context.Attachments.AddAsync(attachment);
        await Context.SaveChangesAsync();

        var query = new GetAttachmentByIdQuery(attachment.Id);

        var result = await Act(query);

        result.Should().NotBeNull();
        result.Id.Should().Be(attachment.Id);
        result.Name.Should().Be(attachment.Name);
        result.OriginalFilename.Should().Be(attachment.OriginalFilename);
        result.FileType.Should().Be(attachment.FileType);
        result.AttachmentType.Should().Be(attachment.AttachmentType);
    }

    [Fact]
    public async Task HandleExistingAttachmentWithContentShouldReturnWithBase64Content()
    {
        var content = "Test content"u8.ToArray();
        var attachment = CreateTestAttachment("Test Attachment", content: content);
        await Context.Attachments.AddAsync(attachment);
        await Context.SaveChangesAsync();

        var query = new GetAttachmentByIdQuery(attachment.Id);

        var result = await Act(query);

        result.Should().NotBeNull();
        result.Base64Content.Should().Be(Convert.ToBase64String(content));
    }

    [Fact]
    public async Task HandleExistingAttachmentWithThumbnailShouldReturnWithBase64Thumbnail()
    {
        var thumbnail = "Thumbnail content"u8.ToArray();
        var attachment = CreateTestAttachment("Test Attachment", previewThumbnail: thumbnail);
        await Context.Attachments.AddAsync(attachment);
        await Context.SaveChangesAsync();

        var query = new GetAttachmentByIdQuery(attachment.Id);

        var result = await Act(query);

        result.Should().NotBeNull();
        result.Base64PreviewThumbnail.Should().Be(Convert.ToBase64String(thumbnail));
    }

    [Fact]
    public async Task HandleExistingAttachmentWithoutOptionalFieldsShouldReturnWithNullValues()
    {
        var attachment = CreateTestAttachment("Simple Attachment");
        await Context.Attachments.AddAsync(attachment);
        await Context.SaveChangesAsync();

        var query = new GetAttachmentByIdQuery(attachment.Id);

        var result = await Act(query);

        result.Should().NotBeNull();
        result.Id.Should().Be(attachment.Id);
        result.Name.Should().Be(attachment.Name);
        result.OriginalFilename.Should().BeNull();
        result.FileType.Should().BeNull();
        result.AttachmentType.Should().BeNull();
        result.Base64Content.Should().BeNull();
        result.Base64PreviewThumbnail.Should().BeNull();
    }

    [Fact]
    public async Task HandleNonExistentAttachmentShouldThrowArgumentException()
    {
        var query = new GetAttachmentByIdQuery(999L);

        var act = async () => await Act(query);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Attachment with ID 999 not found. (Parameter 'request')");
    }

    [Fact]
    public async Task HandleAttachmentWithAuditFieldsShouldReturnAuditInformation()
    {
        var attachment = CreateTestAttachment("Test Attachment");
        attachment.Created = DateTime.UtcNow.AddDays(-1);
        attachment.CreatedBy = "testuser";
        attachment.LastModified = DateTime.UtcNow;
        attachment.LastModifiedBy = "modifyuser";

        await Context.Attachments.AddAsync(attachment);
        await Context.SaveChangesAsync();

        var query = new GetAttachmentByIdQuery(attachment.Id);

        var result = await Act(query);

        result.Should().NotBeNull();
        result.Created.Should().Be(attachment.Created.Value);
        result.CreatedBy.Should().Be(attachment.CreatedBy);
        result.LastModified.Should().Be(attachment.LastModified.Value);
        result.LastModifiedBy.Should().Be(attachment.LastModifiedBy);
    }

    [Theory]
    [InlineData(AttachmentType.Document)]
    [InlineData(AttachmentType.Image)]
    [InlineData(AttachmentType.Audio)]
    [InlineData(AttachmentType.Video)]
    [InlineData(AttachmentType.Other)]
    public async Task HandleAttachmentWithDifferentTypesShouldReturnCorrectType(AttachmentType attachmentType)
    {
        var attachment = CreateTestAttachment("Test Attachment", attachmentType: attachmentType);
        await Context.Attachments.AddAsync(attachment);
        await Context.SaveChangesAsync();

        var query = new GetAttachmentByIdQuery(attachment.Id);

        var result = await Act(query);

        result.Should().NotBeNull();
        result.AttachmentType.Should().Be(attachmentType);
    }

    [Fact]
    public async Task HandleMultipleAttachmentsShouldReturnCorrectOne()
    {
        var attachment1 = CreateTestAttachment("First Attachment");
        var attachment2 = CreateTestAttachment("Second Attachment");
        var attachment3 = CreateTestAttachment("Third Attachment");

        await Context.Attachments.AddRangeAsync(attachment1, attachment2, attachment3);
        await Context.SaveChangesAsync();

        var query = new GetAttachmentByIdQuery(attachment2.Id);

        var result = await Act(query);

        result.Should().NotBeNull();
        result.Id.Should().Be(attachment2.Id);
        result.Name.Should().Be("Second Attachment");
    }

    [Fact]
    public async Task HandleAttachmentWithExternalStorageShouldReturnContentFromMappingService()
    {
        // Arrange
        var externalContent = "External file content"u8.ToArray();
        var attachment = CreateTestAttachment("External Attachment");
        attachment.FilePath = "/storage/attachments/test-file.pdf";

        await Context.Attachments.AddAsync(attachment);
        await Context.SaveChangesAsync();

        _attachmentMappingServiceMock.Setup(x => x.MapWithContentAsync(
                It.Is<Domain.Entities.Attachment>(a => a.FilePath == attachment.FilePath),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AttachmentDto
            {
                Id = attachment.Id,
                Name = attachment.Name,
                Base64Content = Convert.ToBase64String(externalContent),
            });

        var query = new GetAttachmentByIdQuery(attachment.Id);

        // Act
        var result = await Act(query);

        // Assert
        result.Should().NotBeNull();
        result.Base64Content.Should().Be(Convert.ToBase64String(externalContent));
        _attachmentMappingServiceMock.Verify(
            x => x.MapWithContentAsync(
            It.Is<Domain.Entities.Attachment>(a => a.FilePath == attachment.FilePath),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAttachmentWithExternalPreviewShouldReturnPreviewFromMappingService()
    {
        // Arrange
        var externalPreview = "External preview content"u8.ToArray();
        var attachment = CreateTestAttachment("External Attachment");
        attachment.PreviewPath = "/storage/previews/test-preview.jpg";

        await Context.Attachments.AddAsync(attachment);
        await Context.SaveChangesAsync();

        _attachmentMappingServiceMock.Setup(x => x.MapWithContentAsync(
                It.Is<Domain.Entities.Attachment>(a => a.PreviewPath == attachment.PreviewPath),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AttachmentDto
            {
                Id = attachment.Id,
                Name = attachment.Name,
                Base64PreviewThumbnail = Convert.ToBase64String(externalPreview),
            });

        var query = new GetAttachmentByIdQuery(attachment.Id);

        // Act
        var result = await Act(query);

        // Assert
        result.Should().NotBeNull();
        result.Base64PreviewThumbnail.Should().Be(Convert.ToBase64String(externalPreview));
        _attachmentMappingServiceMock.Verify(
            x => x.MapWithContentAsync(
            It.Is<Domain.Entities.Attachment>(a => a.PreviewPath == attachment.PreviewPath),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAttachmentWithBothDatabaseAndExternalStorageShouldUseMapping()
    {
        // Arrange
        var databaseContent = "Database content"u8.ToArray();
        var externalContent = "External content"u8.ToArray();
        var attachment = CreateTestAttachment("Mixed Storage Attachment", content: databaseContent);
        attachment.FilePath = "/storage/attachments/test-file.pdf";

        await Context.Attachments.AddAsync(attachment);
        await Context.SaveChangesAsync();

        _attachmentMappingServiceMock.Setup(x => x.MapWithContentAsync(
                It.Is<Domain.Entities.Attachment>(a => a.FilePath == attachment.FilePath),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AttachmentDto
            {
                Id = attachment.Id,
                Name = attachment.Name,
                Base64Content = Convert.ToBase64String(externalContent), // Mapping service should prefer external
            });

        var query = new GetAttachmentByIdQuery(attachment.Id);

        // Act
        var result = await Act(query);

        // Assert
        result.Should().NotBeNull();
        result.Base64Content.Should().Be(Convert.ToBase64String(externalContent));
        _attachmentMappingServiceMock.Verify(
            x => x.MapWithContentAsync(
            It.Is<Domain.Entities.Attachment>(a => a.FilePath == attachment.FilePath),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAttachmentInOtherUsersPrivateShowcaseShouldThrowUnauthorizedAccessException()
    {
        var showcase = new Showcase
        {
            Name = "Other User Private Showcase",
            UserId = "other-user-id",
            IsPrivate = true,
        };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();

        var collectibleItem = new CollectibleItem
        {
            Name = "Other User Item",
        };
        collectibleItem.Showcases.Add(showcase);
        Context.CollectibleItems.Add(collectibleItem);
        await Context.SaveChangesAsync();

        var attachment = CreateTestAttachment("Private Attachment");
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        Context.CollectibleItemAttachments.Add(new CollectibleItemAttachment
        {
            CollectibleItemId = collectibleItem.Id,
            AttachmentId = attachment.Id,
        });
        await Context.SaveChangesAsync();

        _authorizationServiceMock
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                It.IsAny<object>(),
                It.Is<IEnumerable<IAuthorizationRequirement>>(requirements =>
                    requirements.OfType<ViewAttachmentRequirement>().Any())))
            .ReturnsAsync(AuthorizationResult.Failed());

        var query = new GetAttachmentByIdQuery(attachment.Id);
        var act = async () => await Act(query);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You do not have permission to view this attachment.");

        _attachmentMappingServiceMock.Verify(
            x => x.MapWithContentAsync(It.IsAny<Domain.Entities.Attachment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAttachmentInPublicShowcaseShouldReturnAttachmentDto()
    {
        var showcase = new Showcase
        {
            Name = "Public Showcase",
            UserId = "other-user-id",
            IsPrivate = false,
        };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();

        var collectibleItem = new CollectibleItem
        {
            Name = "Public Item",
        };
        collectibleItem.Showcases.Add(showcase);
        Context.CollectibleItems.Add(collectibleItem);
        await Context.SaveChangesAsync();

        var attachment = CreateTestAttachment("Public Attachment");
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        Context.CollectibleItemAttachments.Add(new CollectibleItemAttachment
        {
            CollectibleItemId = collectibleItem.Id,
            AttachmentId = attachment.Id,
        });
        await Context.SaveChangesAsync();

        _authorizationServiceMock
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                It.IsAny<object>(),
                It.Is<IEnumerable<IAuthorizationRequirement>>(requirements =>
                    requirements.OfType<ViewAttachmentRequirement>().Any())))
            .ReturnsAsync(AuthorizationResult.Success());

        var result = await Act(new GetAttachmentByIdQuery(attachment.Id));

        result.Should().NotBeNull();
        result.Id.Should().Be(attachment.Id);
        _attachmentMappingServiceMock.Verify(
            x => x.MapWithContentAsync(
                It.Is<Domain.Entities.Attachment>(a => a.Id == attachment.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleOrphanAttachmentCreatedByOtherUserShouldThrowUnauthorizedAccessException()
    {
        var attachment = CreateTestAttachment("Orphan Attachment");
        attachment.CreatedBy = "other-user-id";
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        _authorizationServiceMock
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                It.IsAny<object>(),
                It.Is<IEnumerable<IAuthorizationRequirement>>(requirements =>
                    requirements.OfType<ViewAttachmentRequirement>().Any())))
            .ReturnsAsync(AuthorizationResult.Failed());

        var act = async () => await Act(new GetAttachmentByIdQuery(attachment.Id));

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You do not have permission to view this attachment.");
    }

    [Fact]
    public async Task HandleOrphanAttachmentCreatedByCurrentUserShouldReturnAttachmentDto()
    {
        var attachment = CreateTestAttachment("My Orphan Attachment");
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        _authorizationServiceMock
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                It.IsAny<object>(),
                It.Is<IEnumerable<IAuthorizationRequirement>>(requirements =>
                    requirements.OfType<ViewAttachmentRequirement>().Any())))
            .ReturnsAsync(AuthorizationResult.Success());

        var result = await Act(new GetAttachmentByIdQuery(attachment.Id));

        result.Should().NotBeNull();
        result.Id.Should().Be(attachment.Id);
    }

    private static Attachment CreateTestAttachment(
        string name,
        string? originalFilename = null,
        string? fileType = null,
        AttachmentType? attachmentType = null,
        byte[]? content = null,
        byte[]? previewThumbnail = null)
    {
        var attachment = new Attachment
        {
            Name = name,
            OriginalFilename = originalFilename,
            FileType = fileType,
            AttachmentType = attachmentType,
        };

        if (content != null)
        {
            attachment.AttachmentContent = new AttachmentContent
            {
                Id = attachment.Id,
                Content = content,
                Attachment = attachment,
            };
        }

        if (previewThumbnail != null)
        {
            attachment.AttachmentPreview = new AttachmentPreview
            {
                Id = attachment.Id,
                PreviewThumbnail = previewThumbnail,
                Attachment = attachment,
            };
        }

        return attachment;
    }
}
