using Collectibles.Application.Interfaces;
using Collectibles.Application.Mappings.Explicit;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Tags.Queries;

public record SearchTagsQuery : IRequest<List<TagDto>>
{
    public string SearchTerm { get; init; } = string.Empty;
    public int MaxResults { get; init; } = 10;
}

public class SearchTagsQueryHandler : IRequestHandler<SearchTagsQuery, List<TagDto>>
{
    private readonly IApplicationDbContextFactory _contextFactory;

    public SearchTagsQueryHandler(IApplicationDbContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<TagDto>> Handle(SearchTagsQuery request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Tags.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();
            query = query.Where(t => t.Name.Contains(searchTerm));
        }

        return await query
            .OrderBy(t => t.Name)
            .Take(request.MaxResults)
            .Select(t => t.ToDto())
            .ToListAsync(cancellationToken);
    }
}
