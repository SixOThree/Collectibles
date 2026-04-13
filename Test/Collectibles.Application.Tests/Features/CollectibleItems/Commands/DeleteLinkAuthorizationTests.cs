using Collectibles.Application.Features.CollectibleItems.Commands;
using Collectibles.Application.Tests.Common;
using Collectibles.Domain.Enums;
using Collectibles.Domain.Interfaces;

namespace Collectibles.Application.Tests.Features.CollectibleItems.Commands;

public class DeleteLinkAuthorizationTests : BaseTestFixture
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;

    public DeleteLinkAuthorizationTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.UserId).Returns("test-user-id");
    }

    [Fact]
    public async Task HandleOtherUsersLinkShouldThrowUnauthorizedAccessException()
    {
        // Create showcase owned by another user
        var showcase = new Showcase
        {
            Name = "Other User Showcase",
            UserId = "other-user-id",
        };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();

        // Create collectible item in that showcase
        var item = new CollectibleItem
        {
            Name = "Other User Item",
        };
        item.Showcases.Add(showcase);
        Context.CollectibleItems.Add(item);
        await Context.SaveChangesAsync();

        // Create link info on that item
        var linkInfo = new LinkInfo
        {
            CollectibleItemId = item.Id,
            Url = "https://example.com",
        };
        Context.LinkInfos.Add(linkInfo);
        await Context.SaveChangesAsync();

        var command = new DeleteLinkCommand
        {
            LinkInfoId = linkInfo.Id,
        };

        var handler = new DeleteLinkCommandHandler(Context, Mock.Of<IFileStorage>(), _currentUserServiceMock.Object);
        var act = async () => await handler.Handle(command, CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You are not authorized to delete this link.");
    }
}
