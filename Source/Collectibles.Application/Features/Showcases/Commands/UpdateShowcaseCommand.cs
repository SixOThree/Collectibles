using Collectibles.Application.Common.Authorization.Requirements;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Showcases.Commands;

public record UpdateShowcaseCommand : IRequest
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public long? PreviewImageId { get; init; }
    public bool IsPrivate { get; init; } = true;
    public ShowcaseSortOrder SortOrder { get; init; } = ShowcaseSortOrder.Alphabetical;
    public List<long> TagIds { get; init; } = new();
}

public class UpdateShowcaseCommandHandler : IRequestHandler<UpdateShowcaseCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateShowcaseCommandHandler(
        IApplicationDbContext context,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateShowcaseCommand request, CancellationToken cancellationToken)
    {
        var showcase = await _context.Showcases
            .Include(s => s.ShowcaseTags)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (showcase == null)
        {
            throw new KeyNotFoundException($"Showcase with ID {request.Id} not found.");
        }

        // Check authorization
        var authResult = await _authorizationService.AuthorizeAsync(
            new System.Security.Claims.ClaimsPrincipal(),
            showcase,
            new EditShowcaseRequirement());

        if (!authResult.Succeeded)
        {
            throw new UnauthorizedAccessException("You don't have permission to edit this showcase.");
        }

        showcase.Name = request.Name;
        showcase.Description = request.Description;
        showcase.IsPrivate = request.IsPrivate;
        showcase.SortOrder = request.SortOrder;

        if (request.PreviewImageId.HasValue)
        {
            showcase.PreviewImage = await _context.Attachments
                .FindAsync(new object[] { request.PreviewImageId.Value }, cancellationToken);
        }
        else
        {
            showcase.PreviewImage = null;
        }

        // Update tags
        showcase.ShowcaseTags.Clear();

        if (request.TagIds.Count != 0)
        {
            foreach (var tagId in request.TagIds)
            {
                showcase.ShowcaseTags.Add(new Domain.Entities.ShowcaseTag
                {
                    ShowcaseId = showcase.Id,
                    TagId = tagId,
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
