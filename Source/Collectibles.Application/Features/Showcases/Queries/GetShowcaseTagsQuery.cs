using Collectibles.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Showcases.Queries;

/// <summary>
/// Query to retrieve all unique tags associated with showcases.
/// </summary>
public record GetShowcaseTagsQuery : IRequest<IList<ShowcaseTagDto>>
{
}

/// <summary>
/// Handler for retrieving showcase tags with usage count.
/// </summary>
public class GetShowcaseTagsQueryHandler : IRequestHandler<GetShowcaseTagsQuery, IList<ShowcaseTagDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetShowcaseTagsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IList<ShowcaseTagDto>> Handle(GetShowcaseTagsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        // The facet counts are derived only from showcases the caller may see; otherwise
        // this aggregated across every user's private showcases.
        var showcaseTags = await _context.ShowcaseTags
            .Where(st => !st.Showcase.IsPrivate || st.Showcase.UserId == userId)
            .GroupBy(st => new { st.TagId, st.Tag.Name })
            .Select(g => new ShowcaseTagDto
            {
                Id = g.Key.TagId,
                Name = g.Key.Name,
                ShowcaseCount = g.Count(),
            })
            .OrderBy(t => t.Name) // Order alphabetically by tag name
            .ToListAsync(cancellationToken);

        return showcaseTags;
    }
}
