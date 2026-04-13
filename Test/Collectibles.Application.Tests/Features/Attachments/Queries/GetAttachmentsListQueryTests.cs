using Collectibles.Application.Features.Attachments;
using Collectibles.Application.Features.Attachments.Queries;
using Collectibles.Application.Mappings.Explicit;
using Collectibles.Application.Tests.Common;

namespace Collectibles.Application.Tests.Features.Attachments.Queries;

public class GetAttachmentsListQueryTests : QueryTestBase<GetAttachmentsListQuery, AttachmentsListVm>
{
    protected override IRequestHandler<GetAttachmentsListQuery, AttachmentsListVm> CreateHandler()
    {
        var contextFactoryMock = new Mock<IApplicationDbContextFactory>();
        contextFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Context);

        var attachmentMappingServiceMock = new Mock<IAttachmentMappingService>();

        // Setup default behavior for the mapping service
        attachmentMappingServiceMock.Setup(x => x.MapToBriefWithPreviewAsync(
                It.IsAny<Domain.Entities.Attachment>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Attachment a, bool featured, CancellationToken ct) =>
                new AttachmentBriefDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    OriginalFilename = a.OriginalFilename,
                    FileType = a.FileType,
                    AttachmentType = a.AttachmentType,
                    Created = a.Created ?? DateTime.MinValue,
                    IsFeatured = featured,
                    Base64PreviewThumbnail = a.AttachmentPreview?.PreviewThumbnail != null
                        ? Convert.ToBase64String(a.AttachmentPreview.PreviewThumbnail) : null,
                });

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.UserId).Returns("test-user-id");

        return new GetAttachmentsListQueryHandler(contextFactoryMock.Object, attachmentMappingServiceMock.Object, currentUserServiceMock.Object);
    }

    [Fact]
    public async Task HandleEmptyDatabaseShouldReturnEmptyList()
    {
        var query = new GetAttachmentsListQuery();

        var result = await Act(query);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task HandleDatabaseWithAttachmentsShouldReturnOrderedList()
    {
        var attachments = new[]
        {
            CreateTestAttachment("Charlie Document", "charlie.pdf"),
            CreateTestAttachment("Alpha Image", "alpha.jpg"),
            CreateTestAttachment("Beta Video", "beta.mp4"),
        };

        await Context.Attachments.AddRangeAsync(attachments);
        await Context.SaveChangesAsync();

        var query = new GetAttachmentsListQuery();

        var result = await Act(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.Items[0].Name.Should().Be("Alpha Image");
        result.Items[1].Name.Should().Be("Beta Video");
        result.Items[2].Name.Should().Be("Charlie Document");
    }

    [Fact]
    public async Task HandleSearchByNameShouldReturnFilteredResults()
    {
        var attachments = new[]
        {
            CreateTestAttachment("Important Document", "important.pdf"),
            CreateTestAttachment("Random Image", "random.jpg"),
            CreateTestAttachment("Important Video", "important.mp4"),
        };

        await Context.Attachments.AddRangeAsync(attachments);
        await Context.SaveChangesAsync();

        var query = new GetAttachmentsListQuery { SearchTerm = "Important" };

        var result = await Act(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(item => item.Name.Contains("Important"));
    }

    [Fact]
    public async Task HandleSearchByOriginalFilenameShouldReturnFilteredResults()
    {
        var attachments = new[]
        {
            CreateTestAttachment("Document A", "test-file.pdf"),
            CreateTestAttachment("Document B", "another.pdf"),
            CreateTestAttachment("Document C", "test-file.docx"),
        };

        await Context.Attachments.AddRangeAsync(attachments);
        await Context.SaveChangesAsync();

        var query = new GetAttachmentsListQuery { SearchTerm = "test-file" };

        var result = await Act(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(item => item.OriginalFilename!.Contains("test-file"));
    }

    [Fact]
    public async Task HandleSearchWithNoMatchesShouldReturnEmptyList()
    {
        var attachments = new[]
        {
            CreateTestAttachment("Document", "file.pdf"),
            CreateTestAttachment("Image", "photo.jpg"),
        };

        await Context.Attachments.AddRangeAsync(attachments);
        await Context.SaveChangesAsync();

        var query = new GetAttachmentsListQuery { SearchTerm = "NonExistent" };

        var result = await Act(query);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Theory]
    [InlineData(AttachmentType.Document, 2)]
    [InlineData(AttachmentType.Image, 1)]
    [InlineData(AttachmentType.Video, 1)]
    [InlineData(AttachmentType.Audio, 0)]
    public async Task HandleFilterByAttachmentTypeShouldReturnCorrectResults(AttachmentType filterType, int expectedCount)
    {
        var attachments = new[]
        {
            CreateTestAttachment("Doc 1", attachmentType: AttachmentType.Document),
            CreateTestAttachment("Doc 2", attachmentType: AttachmentType.Document),
            CreateTestAttachment("Image 1", attachmentType: AttachmentType.Image),
            CreateTestAttachment("Video 1", attachmentType: AttachmentType.Video),
        };

        await Context.Attachments.AddRangeAsync(attachments);
        await Context.SaveChangesAsync();

        var query = new GetAttachmentsListQuery { AttachmentType = filterType };

        var result = await Act(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(expectedCount);
        result.TotalCount.Should().Be(expectedCount);
        if (expectedCount > 0)
        {
            result.Items.Should().OnlyContain(item => item.AttachmentType == filterType);
        }
    }

    [Fact]
    public async Task HandleCombinedFiltersShouldReturnCorrectResults()
    {
        var attachments = new[]
        {
            CreateTestAttachment("Important Document", "important.pdf", AttachmentType.Document),
            CreateTestAttachment("Important Image", "important.jpg", AttachmentType.Image),
            CreateTestAttachment("Regular Document", "regular.pdf", AttachmentType.Document),
            CreateTestAttachment("Regular Image", "regular.jpg", AttachmentType.Image),
        };

        await Context.Attachments.AddRangeAsync(attachments);
        await Context.SaveChangesAsync();

        var query = new GetAttachmentsListQuery
        {
            SearchTerm = "Important",
            AttachmentType = AttachmentType.Document,
        };

        var result = await Act(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Items[0].Name.Should().Be("Important Document");
        result.Items[0].AttachmentType.Should().Be(AttachmentType.Document);
    }

    [Fact]
    public async Task HandlePaginationShouldReturnCorrectPage()
    {
        var attachments = Enumerable.Range(1, 25)
            .Select(i => CreateTestAttachment($"Attachment {i:D2}"))
            .ToArray();

        await Context.Attachments.AddRangeAsync(attachments);
        await Context.SaveChangesAsync();

        var query = new GetAttachmentsListQuery
        {
            PageNumber = 2,
            PageSize = 10,
        };

        var result = await Act(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(3);
        result.Items[0].Name.Should().Be("Attachment 11");
        result.Items[9].Name.Should().Be("Attachment 20");
    }

    [Fact]
    public async Task HandleLastPageShouldReturnRemainingItems()
    {
        var attachments = Enumerable.Range(1, 25)
            .Select(i => CreateTestAttachment($"Attachment {i:D2}"))
            .ToArray();

        await Context.Attachments.AddRangeAsync(attachments);
        await Context.SaveChangesAsync();

        var query = new GetAttachmentsListQuery
        {
            PageNumber = 3,
            PageSize = 10,
        };

        var result = await Act(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(25);
        result.PageNumber.Should().Be(3);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task HandlePageBeyondRangeShouldReturnEmptyList()
    {
        var attachments = new[]
        {
            CreateTestAttachment("Document 1"),
            CreateTestAttachment("Document 2"),
        };

        await Context.Attachments.AddRangeAsync(attachments);
        await Context.SaveChangesAsync();

        var query = new GetAttachmentsListQuery
        {
            PageNumber = 5,
            PageSize = 10,
        };

        var result = await Act(query);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(2);
        result.PageNumber.Should().Be(5);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task HandleCustomPageSizeShouldRespectPageSize()
    {
        var attachments = Enumerable.Range(1, 10)
            .Select(i => CreateTestAttachment($"Attachment {i}"))
            .ToArray();

        await Context.Attachments.AddRangeAsync(attachments);
        await Context.SaveChangesAsync();

        var query = new GetAttachmentsListQuery { PageSize = 3 };

        var result = await Act(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(10);
        result.PageSize.Should().Be(3);
        result.TotalPages.Should().Be(4);
    }

    [Fact]
    public async Task HandleAttachmentWithPreviewThumbnailShouldIncludeThumbnailInResult()
    {
        var thumbnail = "Thumbnail content"u8.ToArray();
        var attachment = CreateTestAttachment("Test Attachment", previewThumbnail: thumbnail);

        await Context.Attachments.AddAsync(attachment);
        await Context.SaveChangesAsync();

        var query = new GetAttachmentsListQuery();

        var result = await Act(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items[0].Base64PreviewThumbnail.Should().Be(Convert.ToBase64String(thumbnail));
    }

    [Fact]
    public void HandleDefaultParametersShouldUseCorrectDefaults()
    {
        var query = new GetAttachmentsListQuery();

        query.PageNumber.Should().Be(1);
        query.PageSize.Should().Be(10);
        query.SearchTerm.Should().BeNull();
        query.AttachmentType.Should().BeNull();
    }

    [Fact]
    public async Task HandleShouldIncludeAttachmentsOwnedViaShowcaseHierarchy()
    {
        // Create a showcase owned by the test user
        var showcase = new Showcase { Name = "My Showcase", UserId = "test-user-id" };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();

        // Create an attachment with null CreatedBy (simulating legacy data)
        var legacyAttachment = new Attachment { Name = "Legacy Attachment", CreatedBy = "other-user-id" };
        Context.Attachments.Add(legacyAttachment);
        await Context.SaveChangesAsync();

        // Create a collectible item linked to the showcase
        var item = new CollectibleItem { Name = "Test Item" };
        item.Showcases.Add(showcase);
        Context.CollectibleItems.Add(item);
        await Context.SaveChangesAsync();

        // Link the attachment to the item
        var cia = new CollectibleItemAttachment
        {
            CollectibleItemId = item.Id,
            AttachmentId = legacyAttachment.Id,
        };
        Context.CollectibleItemAttachments.Add(cia);
        await Context.SaveChangesAsync();

        var query = new GetAttachmentsListQuery();

        var result = await Act(query);

        // Should include the legacy attachment because it's owned via showcase hierarchy
        result.Items.Should().Contain(a => a.Name == "Legacy Attachment");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task HandleEmptyOrWhitespaceSearchTermShouldIgnoreFilter(string? searchTerm)
    {
        var attachments = new[]
        {
            CreateTestAttachment("Document A"),
            CreateTestAttachment("Document B"),
        };

        await Context.Attachments.AddRangeAsync(attachments);
        await Context.SaveChangesAsync();

        var query = new GetAttachmentsListQuery { SearchTerm = searchTerm };

        var result = await Act(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    private static Attachment CreateTestAttachment(
        string name,
        string? originalFilename = null,
        AttachmentType? attachmentType = null,
        byte[]? previewThumbnail = null)
    {
        var attachment = new Attachment
        {
            Name = name,
            OriginalFilename = originalFilename,
            AttachmentType = attachmentType,
        };

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
