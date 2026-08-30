using Collectibles.Application.Common.Authorization.Requirements;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Common.Authorization.Handlers;

public class ViewAttachmentAuthorizationHandler :
    AuthorizationHandler<ViewAttachmentRequirement, Attachment>,
    IAuthorizationHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IShareAccessContext _shareAccessContext;

    public ViewAttachmentAuthorizationHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IShareAccessContext shareAccessContext)
    {
        _context = context;
        _currentUserService = currentUserService;
        _shareAccessContext = shareAccessContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ViewAttachmentRequirement requirement,
        Attachment resource)
    {
        var userId = _currentUserService.UserId;

        // Check if attachment is associated with any collectible items
        var attachmentItems = await _context.CollectibleItems
            .Where(ci => ci.CollectibleItemAttachments.Any(cia => cia.AttachmentId == resource.Id))
            .Include(ci => ci.Showcases)
            .ToListAsync();

        if (attachmentItems.Count != 0)
        {
            // Get all showcases containing items with this attachment
            var showcases = attachmentItems.SelectMany(i => i.Showcases).Distinct().ToList();

            // Owner can always view. Compared only when we actually have an identity, so an
            // anonymous caller cannot match a showcase whose owner is somehow unset.
            if (!string.IsNullOrEmpty(userId)
                && showcases.Any(s => string.Equals(s.UserId, userId, StringComparison.Ordinal)))
            {
                context.Succeed(requirement);
                return;
            }

            // A validated share token is the one way an anonymous caller reaches a private
            // showcase; without this the endpoint's token check and this decision disagree.
            if (_shareAccessContext.HasAccessToAny(showcases.Select(s => s.Id)))
            {
                context.Succeed(requirement);
                return;
            }

            // Non-owners can only view attachments in public showcases
            if (showcases.All(s => !s.IsPrivate))
            {
                context.Succeed(requirement);
            }
        }
        else
        {
            // Attachment not associated with any item - allow viewing by uploader.
            // Both sides must be present: "anonymous" and "creator unknown" are not the same
            // thing, and a null-to-null comparison would otherwise satisfy this for any caller.
            if (!string.IsNullOrEmpty(userId)
                && !string.IsNullOrEmpty(resource.CreatedBy)
                && string.Equals(resource.CreatedBy, userId, StringComparison.Ordinal))
            {
                context.Succeed(requirement);
            }
        }
    }
}

public class EditAttachmentAuthorizationHandler :
    AuthorizationHandler<EditAttachmentRequirement, Attachment>,
    IAuthorizationHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public EditAttachmentAuthorizationHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        EditAttachmentRequirement requirement,
        Attachment resource)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        // Check if attachment is associated with any collectible items
        var attachmentItems = await _context.CollectibleItems
            .Where(ci => ci.CollectibleItemAttachments.Any(cia => cia.AttachmentId == resource.Id))
            .Include(ci => ci.Showcases)
            .ToListAsync();

        if (attachmentItems.Count != 0)
        {
            // Get all showcases containing items with this attachment
            var showcases = attachmentItems.SelectMany(i => i.Showcases).Distinct().ToList();

            // Only showcase owner can edit attachments
            if (showcases.Any(s => s.UserId == userId))
            {
                context.Succeed(requirement);
            }
        }
        else
        {
            // Attachment not associated with any item - allow editing by uploader
            if (resource.CreatedBy == userId)
            {
                context.Succeed(requirement);
            }
        }
    }
}

public class DeleteAttachmentAuthorizationHandler :
    AuthorizationHandler<DeleteAttachmentRequirement, Attachment>,
    IAuthorizationHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteAttachmentAuthorizationHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DeleteAttachmentRequirement requirement,
        Attachment resource)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        // Check if attachment is associated with any collectible items
        var attachmentItems = await _context.CollectibleItems
            .Where(ci => ci.CollectibleItemAttachments.Any(cia => cia.AttachmentId == resource.Id))
            .Include(ci => ci.Showcases)
            .ToListAsync();

        if (attachmentItems.Count != 0)
        {
            // Get all showcases containing items with this attachment
            var showcases = attachmentItems.SelectMany(i => i.Showcases).Distinct().ToList();

            // Only showcase owner can delete attachments
            if (showcases.Any(s => s.UserId == userId))
            {
                context.Succeed(requirement);
            }
        }
        else
        {
            // Attachment not associated with any item - allow deletion by uploader
            if (resource.CreatedBy == userId)
            {
                context.Succeed(requirement);
            }
        }
    }
}
