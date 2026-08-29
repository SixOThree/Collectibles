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

    public ViewAttachmentAuthorizationHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
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

            // Owner can always view
            if (showcases.Any(s => s.UserId == userId))
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
            // Attachment not associated with any item - allow viewing by uploader
            if (resource.CreatedBy == userId)
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
