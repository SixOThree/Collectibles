using Collectibles.Application.Common.Authorization.Handlers;
using Collectibles.Application.Common.Authorization.Requirements;
using Collectibles.Application.Interfaces;
using Collectibles.Application.Tests.Common;
using Collectibles.Domain.Entities;

using Microsoft.AspNetCore.Authorization;

namespace Collectibles.Application.Tests.Features.Authorization;

/// <summary>
/// Covers who may view an attachment.
///
/// Two defects motivated these: an anonymous caller matched an attachment whose creator was unset,
/// because two null values compared equal in a security decision; and a valid share token for a
/// private showcase was denied here, because the grant the endpoint had already proven was not
/// represented in the authorization context.
/// </summary>
public class ViewAttachmentAuthorizationHandlerTests : BaseTestFixture
{
    private const string OwnerId = "owner-user-id";
    private const string OtherUserId = "other-user-id";

    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly ShareAccessContextStub _shareAccessContext = new();

    private ViewAttachmentAuthorizationHandler CreateHandler() =>
        new(Context, _currentUserServiceMock.Object, _shareAccessContext);

    private static AuthorizationHandlerContext ContextFor(Attachment attachment) =>
        new([new ViewAttachmentRequirement()], new System.Security.Claims.ClaimsPrincipal(), attachment);

    private async Task<(Attachment Attachment, Showcase Showcase)> AddAttachmentInShowcaseAsync(
        string ownerId,
        bool isPrivate)
    {
        var showcase = new Showcase { Name = "Showcase", UserId = ownerId, IsPrivate = isPrivate };
        Context.Showcases.Add(showcase);
        await Context.SaveChangesAsync();

        var attachment = new Attachment { Name = "Attachment" };
        Context.Attachments.Add(attachment);

        var item = new CollectibleItem { Name = "Item" };
        item.Showcases.Add(showcase);
        Context.CollectibleItems.Add(item);
        await Context.SaveChangesAsync();

        Context.CollectibleItemAttachments.Add(new CollectibleItemAttachment
        {
            AttachmentId = attachment.Id,
            CollectibleItemId = item.Id,
        });
        await Context.SaveChangesAsync();

        return (attachment, showcase);
    }

    [Fact]
    public async Task AnonymousCallerShouldNotViewAnOrphanedAttachmentWithNoRecordedCreator()
    {
        // The defect: userId and CreatedBy were both null, and null == null succeeded.
        _currentUserServiceMock.Setup(x => x.UserId).Returns((string?)null);

        var attachment = new Attachment { Name = "Orphan", CreatedBy = null };
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        var context = ContextFor(attachment);
        await CreateHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task UploaderShouldViewTheirOwnOrphanedAttachment()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(OwnerId);

        var attachment = new Attachment { Name = "Orphan", CreatedBy = OwnerId };
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        var context = ContextFor(attachment);
        await CreateHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task AnotherUserShouldNotViewSomeoneElsesOrphanedAttachment()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(OtherUserId);

        var attachment = new Attachment { Name = "Orphan", CreatedBy = OwnerId };
        Context.Attachments.Add(attachment);
        await Context.SaveChangesAsync();

        var context = ContextFor(attachment);
        await CreateHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task OwnerShouldViewAnAttachmentInTheirPrivateShowcase()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(OwnerId);

        var (attachment, _) = await AddAttachmentInShowcaseAsync(OwnerId, isPrivate: true);

        var context = ContextFor(attachment);
        await CreateHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task AnonymousCallerShouldViewAnAttachmentInAPublicShowcase()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((string?)null);

        var (attachment, _) = await AddAttachmentInShowcaseAsync(OwnerId, isPrivate: false);

        var context = ContextFor(attachment);
        await CreateHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task AnonymousCallerShouldNotViewAPrivateShowcaseWithoutAShareToken()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((string?)null);

        var (attachment, _) = await AddAttachmentInShowcaseAsync(OwnerId, isPrivate: true);

        var context = ContextFor(attachment);
        await CreateHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task AValidatedShareTokenShouldAllowViewingAPrivateShowcase()
    {
        // The defect: this denied a legitimate share link, and the endpoint reported the denial
        // as a server error while writing the token to the log.
        _currentUserServiceMock.Setup(x => x.UserId).Returns((string?)null);

        var (attachment, showcase) = await AddAttachmentInShowcaseAsync(OwnerId, isPrivate: true);
        _shareAccessContext.GrantShowcaseAccess(showcase.Id);

        var context = ContextFor(attachment);
        await CreateHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task AShareTokenForADifferentShowcaseShouldNotGrantAccess()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((string?)null);

        var (attachment, showcase) = await AddAttachmentInShowcaseAsync(OwnerId, isPrivate: true);
        _shareAccessContext.GrantShowcaseAccess(showcase.Id + 1000);

        var context = ContextFor(attachment);
        await CreateHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    /// <summary>
    /// Records grants the way the request-scoped implementation does, without needing a request.
    /// </summary>
    private sealed class ShareAccessContextStub : IShareAccessContext
    {
        private readonly HashSet<long> _granted = [];

        public void GrantShowcaseAccess(long showcaseId) => _granted.Add(showcaseId);

        public bool HasAccessTo(long showcaseId) => _granted.Contains(showcaseId);

        public bool HasAccessToAny(IEnumerable<long> showcaseIds) => showcaseIds.Any(_granted.Contains);
    }
}
