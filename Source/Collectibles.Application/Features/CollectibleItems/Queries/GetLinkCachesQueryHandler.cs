using Collectibles.Application.Interfaces;
using Collectibles.Application.Mappings.Explicit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.CollectibleItems.Queries;

public class GetLinkCachesQueryHandler : IRequestHandler<GetLinkCachesQuery, List<LinkCacheDto>>
{
    private readonly IApplicationDbContext _context;

    public GetLinkCachesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<LinkCacheDto>> Handle(GetLinkCachesQuery request, CancellationToken cancellationToken)
    {
        var linkCaches = await _context.LinkCaches
            .Where(lc => lc.LinkInfoId == request.LinkInfoId)
            .ToListAsync(cancellationToken);

        return linkCaches.Select(lc => lc.ToDto()).ToList();
    }
}
