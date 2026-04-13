using Collectibles.Application.Features.CollectibleItems.Commands;
using Collectibles.Application.Tests.Common;
using Collectibles.Domain.Configuration;
using Microsoft.Extensions.Options;

namespace Collectibles.Application.Tests.Features.CollectibleItems.Commands;

public class AddLinkAuthorizationTests : BaseTestFixture
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;

    public AddLinkAuthorizationTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.UserId).Returns("test-user-id");
    }

    [Fact]
    public async Task HandleOtherUsersItemShouldThrowUnauthorizedAccessException()
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

        var command = new AddLinkToCollectibleItemCommand
        {
            CollectibleItemId = item.Id,
            Url = "https://example.com",
        };

        var externalLinksOptions = Options.Create(new ExternalLinksOptions { Enabled = true });
        var handler = new AddLinkToCollectibleItemCommandHandler(Context, _currentUserServiceMock.Object, externalLinksOptions);
        var act = async () => await handler.Handle(command, CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You are not authorized to add links to this item.");
    }
}
