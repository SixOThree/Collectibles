using Collectibles.Application.Interfaces;
using Collectibles.Application.Mappings.Explicit;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Tags.Queries;

public record GetTagsListQuery : IRequest<List<Features.Tags.TagDto>>;

public class GetTagsListQueryHandler : IRequestHandler<GetTagsListQuery, List<Features.Tags.TagDto>>
{
    private readonly IApplicationDbContextFactory _contextFactory;

    public GetTagsListQueryHandler(IApplicationDbContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Features.Tags.TagDto>> Handle(GetTagsListQuery request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Tags
            .OrderBy(t => t.Name)
            .Select(t => t.ToDto())
            .ToListAsync(cancellationToken);
    }
}
