using Collectibles.Application.Common.Authorization.Requirements;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Common.Authorization.Handlers;

public class ViewCollectibleItemAuthorizationHandler :
    AuthorizationHandler<ViewCollectibleItemRequirement, CollectibleItem>,
    IAuthorizationHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ViewCollectibleItemAuthorizationHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ViewCollectibleItemRequirement requirement,
        CollectibleItem resource)
    {
        var userId = _currentUserService.UserId;

        // Check if item belongs to any showcases
        var showcaseIds = await _context.CollectibleItems
            .Where(ci => ci.Id == resource.Id)
            .SelectMany(ci => ci.Showcases.Select(s => s.Id))
            .ToListAsync();

        if (showcaseIds.Count == 0)
        {
            // Item doesn't belong to any showcase - allow public viewing
            context.Succeed(requirement);
            return;
        }

        // Get the showcases this item belongs to
        var showcases = await _context.Showcases
            .AsNoTracking()
            .Where(s => showcaseIds.Contains(s.Id))
            .ToListAsync();

        // Check if user owns any of the showcases
        if (showcases.Any(s => s.UserId == userId))
        {
            context.Succeed(requirement);
            return;
        }

        // Non-owners can only view items in public showcases
        if (showcases.All(s => !s.IsPrivate))
        {
            context.Succeed(requirement);
        }
    }
}

public class EditCollectibleItemAuthorizationHandler :
    AuthorizationHandler<EditCollectibleItemRequirement, CollectibleItem>,
    IAuthorizationHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public EditCollectibleItemAuthorizationHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        EditCollectibleItemRequirement requirement,
        CollectibleItem resource)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        // Get the showcases this item belongs to
        var showcaseIds = await _context.CollectibleItems
            .Where(ci => ci.Id == resource.Id)
            .SelectMany(ci => ci.Showcases.Select(s => s.Id))
            .ToListAsync();

        if (showcaseIds.Count == 0)
        {
            // Item doesn't belong to any showcase - no one can edit
            return;
        }

        var showcases = await _context.Showcases
            .AsNoTracking()
            .Where(s => showcaseIds.Contains(s.Id))
            .ToListAsync();

        // Only showcase owner can edit items
        if (showcases.Any(s => s.UserId == userId))
        {
            context.Succeed(requirement);
        }
    }
}

public class DeleteCollectibleItemAuthorizationHandler :
    AuthorizationHandler<DeleteCollectibleItemRequirement, CollectibleItem>,
    IAuthorizationHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteCollectibleItemAuthorizationHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DeleteCollectibleItemRequirement requirement,
        CollectibleItem resource)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        // Get the showcases this item belongs to
        var showcaseIds = await _context.CollectibleItems
            .Where(ci => ci.Id == resource.Id)
            .SelectMany(ci => ci.Showcases.Select(s => s.Id))
            .ToListAsync();

        if (showcaseIds.Count == 0)
        {
            // Item doesn't belong to any showcase - no one can delete
            return;
        }

        var showcases = await _context.Showcases
            .AsNoTracking()
            .Where(s => showcaseIds.Contains(s.Id))
            .ToListAsync();

        // Only showcase owner can delete items
        if (showcases.Any(s => s.UserId == userId))
        {
            context.Succeed(requirement);
        }
    }
}
