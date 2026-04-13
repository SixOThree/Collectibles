using Collectibles.Application.Common.Authorization.Requirements;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace Collectibles.Application.Common.Authorization.Handlers;

public class ShowcaseAuthorizationHandler :
    AuthorizationHandler<ViewShowcaseRequirement, Showcase>,
    IAuthorizationHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ShowcaseAuthorizationHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ViewShowcaseRequirement requirement,
        Showcase resource)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            return Task.CompletedTask;
        }

        // Owner can always view
        if (resource.UserId == userId)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Non-owners can only view public showcases
        if (!resource.IsPrivate)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public class EditShowcaseAuthorizationHandler :
    AuthorizationHandler<EditShowcaseRequirement, Showcase>,
    IAuthorizationHandler
{
    private readonly ICurrentUserService _currentUserService;

    public EditShowcaseAuthorizationHandler(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        EditShowcaseRequirement requirement,
        Showcase resource)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            return Task.CompletedTask;
        }

        // Only owner can edit
        if (resource.UserId == userId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public class DeleteShowcaseAuthorizationHandler :
    AuthorizationHandler<DeleteShowcaseRequirement, Showcase>,
    IAuthorizationHandler
{
    private readonly ICurrentUserService _currentUserService;

    public DeleteShowcaseAuthorizationHandler(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DeleteShowcaseRequirement requirement,
        Showcase resource)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            return Task.CompletedTask;
        }

        // Only owner can delete
        if (resource.UserId == userId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
