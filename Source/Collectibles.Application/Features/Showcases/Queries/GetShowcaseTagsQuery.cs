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

    public GetShowcaseTagsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IList<ShowcaseTagDto>> Handle(GetShowcaseTagsQuery request, CancellationToken cancellationToken)
    {
        // Query to get all unique tags used in showcases with count
        var showcaseTags = await _context.ShowcaseTags
            .Where(st => st.Deleted == null) // Only include non-deleted showcase-tag relationships
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
