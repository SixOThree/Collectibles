using Collectibles.Application.Features.CollectibleItems.Commands;
using Collectibles.Application.Services;
using Collectibles.Application.Tests.Common;

namespace Collectibles.Application.Tests.Features.CollectibleItems.Commands;

/// <summary>
/// The attachment junction carries curation payload (featured flag, featured date, display
/// order). Updating an item used to clear and rebuild those rows, so any edit — even a
/// rename — silently discarded the curation.
/// </summary>
public class UpdateCollectibleItemJunctionTests : BaseTestFixture
{
    private const string CallerId = "test-user-id";

    private readonly Mock<IApplicationDbContextFactory> _contextFactoryMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IEventLogService> _eventLogServiceMock = new();
    private readonly Mock<ICollectibleItemPreviewService> _previewServiceMock = new();
    private readonly Mock<IBackgroundJobScheduler> _backgroundJobSchedulerMock = new();

    public UpdateCollectibleItemJunctionTests()
    {
        _contextFactoryMock
            .Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new NonDisposableDbContextWrapper(Context));

        _currentUserServiceMock.Setup(x => x.UserId).Returns(CallerId);
    }

    private UpdateCollectibleItemCommandHandler CreateHandler() => new(
        _contextFactoryMock.Object,
        _currentUserServiceMock.Object,
        _eventLogServiceMock.Object,
        _previewServiceMock.Object,
        _backgroundJobSchedulerMock.Object);

    [Fact]
    public async Task RenamingAnItemPreservesFeaturedAttachmentCuration()
    {
        var showcase = new Showcase { Name = "Showcase", UserId = CallerId };
        var featured = new Attachment { Name = "Featured", FileType = "image/jpeg" };
        var other = new Attachment { Name = "Other", FileType = "image/jpeg" };

        var item = new CollectibleItem { Name = "Before" };
        item.Showcases.Add(showcase);
        item.CollectibleItemAttachments.Add(new CollectibleItemAttachment
        {
            Attachment = featured,
            IsFeatured = true,
            FeaturedDate = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            DisplayOrder = 3,
        });
        item.CollectibleItemAttachments.Add(new CollectibleItemAttachment { Attachment = other });

        Context.CollectibleItems.Add(item);
        await Context.SaveChangesAsync();

        await CreateHandler().Handle(
            new UpdateCollectibleItemCommand
            {
                Id = item.Id,
                Name = "After",
                AttachmentIds = [featured.Id, other.Id],
            },
            CancellationToken);

        var link = await Context.CollectibleItemAttachments
            .FirstAsync(cia => cia.CollectibleItemId == item.Id && cia.AttachmentId == featured.Id);

        link.IsFeatured.Should().BeTrue();
        link.FeaturedDate.Should().Be(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        link.DisplayOrder.Should().Be(3);

        var reloaded = await Context.CollectibleItems.FirstAsync(ci => ci.Id == item.Id);
        reloaded.Name.Should().Be("After");
    }

    [Fact]
    public async Task RemovingAnAttachmentDropsOnlyThatJunctionRow()
    {
        var showcase = new Showcase { Name = "Showcase", UserId = CallerId };
        var kept = new Attachment { Name = "Kept", FileType = "image/jpeg" };
        var removed = new Attachment { Name = "Removed", FileType = "image/jpeg" };

        var item = new CollectibleItem { Name = "Item" };
        item.Showcases.Add(showcase);
        item.CollectibleItemAttachments.Add(new CollectibleItemAttachment { Attachment = kept, IsFeatured = true });
        item.CollectibleItemAttachments.Add(new CollectibleItemAttachment { Attachment = removed });

        Context.CollectibleItems.Add(item);
        await Context.SaveChangesAsync();

        await CreateHandler().Handle(
            new UpdateCollectibleItemCommand
            {
                Id = item.Id,
                Name = "Item",
                AttachmentIds = [kept.Id],
            },
            CancellationToken);

        var links = await Context.CollectibleItemAttachments
            .Where(cia => cia.CollectibleItemId == item.Id)
            .ToListAsync();

        links.Should().ContainSingle();
        links[0].AttachmentId.Should().Be(kept.Id);
        links[0].IsFeatured.Should().BeTrue();
    }
}
