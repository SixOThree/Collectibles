using Collectibles.Application.Features.Attachments.Commands;
using Collectibles.Application.Tests.Common;
using Collectibles.Domain.Entities;

namespace Collectibles.Application.Tests.Features.Attachments.Commands;

public class MoveAttachmentCommandAuthorizationTests : BaseTestFixture
{
    private readonly Mock<IApplicationDbContextFactory> _contextFactoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;

    public MoveAttachmentCommandAuthorizationTests()
    {
        _contextFactoryMock = new Mock<IApplicationDbContextFactory>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.UserId).Returns("test-user-id");
        _currentUserServiceMock.Setup(x => x.IsAdministrator).Returns(false);

        _contextFactoryMock
            .Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new NonDisposableDbContextWrapper(Context as IApplicationDbContext));
    }

    [Fact]
    public async Task HandleOwnedShowcaseShouldRenameAttachment()
    {
        var showcase = new Showcase
        {
            Name = "Owned Showcase",
            UserId = "test-user-id",
        };
        Context.Showcases.Add(showcase);

        var item = new CollectibleItem
        {
            Name = "Original Name",
        };
        item.Showcases.Add(showcase);
        Context.CollectibleItems.Add(item);

        var attachment = new Attachment
        {
            Name = "original",
            OriginalFilename = "original.jpg",
        };
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        Context.CollectibleItemAttachments.Add(new CollectibleItemAttachment
        {
            CollectibleItemId = item.Id,
            AttachmentId = attachment.Id,
        });
        await Context.SaveChangesAsync();

        var handler = new MoveAttachmentCommandHandler(
            _contextFactoryMock.Object,
            Mock.Of<IEventLogService>(),
            Mock.Of<IItemHierarchyService>(),
            _currentUserServiceMock.Object);

        await handler.Handle(
            new MoveAttachmentCommand
            {
                AttachmentId = attachment.Id,
                RelativePath = "renamed.jpg",
                ShowcaseId = showcase.Id,
            }, CancellationToken);

        attachment.OriginalFilename.Should().Be("renamed.jpg");
        attachment.Name.Should().Be("renamed");
        item.Name.Should().Be("renamed");
    }

    [Fact]
    public async Task HandleOtherUsersShowcaseShouldThrowUnauthorizedAccessException()
    {
        var showcase = new Showcase
        {
            Name = "Other Showcase",
            UserId = "other-user-id",
        };
        Context.Showcases.Add(showcase);

        var item = new CollectibleItem
        {
            Name = "Other Item",
        };
        item.Showcases.Add(showcase);
        Context.CollectibleItems.Add(item);

        var attachment = new Attachment
        {
            Name = "original",
            OriginalFilename = "original.jpg",
        };
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        Context.CollectibleItemAttachments.Add(new CollectibleItemAttachment
        {
            CollectibleItemId = item.Id,
            AttachmentId = attachment.Id,
        });
        await Context.SaveChangesAsync();

        var handler = new MoveAttachmentCommandHandler(
            _contextFactoryMock.Object,
            Mock.Of<IEventLogService>(),
            Mock.Of<IItemHierarchyService>(),
            _currentUserServiceMock.Object);

        var act = async () => await handler.Handle(
            new MoveAttachmentCommand
            {
                AttachmentId = attachment.Id,
                RelativePath = "blocked.jpg",
                ShowcaseId = showcase.Id,
            }, CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You are not authorized to move attachments in this showcase.");
    }
}
