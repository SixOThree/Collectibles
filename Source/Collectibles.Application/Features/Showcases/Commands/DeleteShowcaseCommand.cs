using Collectibles.Application.Common.Authorization.Requirements;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Showcases.Commands;

public record DeleteShowcaseCommand(long ShowcaseId) : IRequest<Unit>;

public class DeleteShowcaseCommandHandler : IRequestHandler<DeleteShowcaseCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEventLogService _eventLogService;

    public DeleteShowcaseCommandHandler(
        IApplicationDbContext context,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService,
        IEventLogService eventLogService)
    {
        _context = context;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
        _eventLogService = eventLogService;
    }

    public async Task<Unit> Handle(DeleteShowcaseCommand request, CancellationToken cancellationToken)
    {
        var showcase = await _context.Showcases
            .Include(s => s.CollectibleItems)
            .Include(s => s.ShowcaseTags)
            .FirstOrDefaultAsync(s => s.Id == request.ShowcaseId, cancellationToken);

        if (showcase == null)
        {
            throw new InvalidOperationException("Showcase not found.");
        }

        var authorizationResult = await _authorizationService.AuthorizeAsync(
            new System.Security.Claims.ClaimsPrincipal(),
            showcase,
            new DeleteShowcaseRequirement());

        if (!authorizationResult.Succeeded)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete this showcase.");
        }

        // Capture showcase information for event logging
        var deletedShowcaseInfo = new
        {
            Name = showcase.Name,
            Description = showcase.Description,
            IsPrivate = showcase.IsPrivate,
            ItemCount = showcase.CollectibleItems.Count,
            TagCount = showcase.ShowcaseTags.Count,
            DeletedItems = showcase.CollectibleItems.Select(i => new { i.Id, i.Name }).ToList(),
        };

        showcase.Deleted = DateTime.UtcNow;
        showcase.DeletedBy = _currentUserService.UserId;

        foreach (var item in showcase.CollectibleItems)
        {
            item.Deleted = DateTime.UtcNow;
            item.DeletedBy = _currentUserService.UserId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Log the delete event
        await _eventLogService.LogEventAsync(
            EventAction.Delete,
            nameof(Showcase),
            showcase.Id,
            showcase.Name,
            deletedShowcaseInfo,
            null,
            cancellationToken: cancellationToken);

        return Unit.Value;
    }
}
