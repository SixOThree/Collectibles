using Collectibles.Application.Features.CollectibleItems.Queries;
using Collectibles.Application.Mappings.Explicit;
using Collectibles.Application.Tests.Common;

namespace Collectibles.Application.Tests.Features.CollectibleItems.Queries;

public class GetCollectibleItemByIdQueryAuthorizationTests : BaseTestFixture
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;

    public GetCollectibleItemByIdQueryAuthorizationTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.UserId).Returns("test-user-id");
    }

    [Fact]
    public async Task Handle_ItemInOtherUsersPrivateShowcase_ReturnsNull()
    {
        // Arrange
        var showcase = new Showcase
        {
            Name = "Private Showcase",
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

        var handler = new GetCollectibleItemByIdQueryHandler(
            Context,
            Mock.Of<ICollectibleItemMappingService>(),
            Mock.Of<IEventLogService>(),
            _currentUserServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new GetCollectibleItemByIdQuery { Id = collectibleItem.Id },
            CancellationToken);

        // Assert
        result.Should().BeNull();
    }
}
