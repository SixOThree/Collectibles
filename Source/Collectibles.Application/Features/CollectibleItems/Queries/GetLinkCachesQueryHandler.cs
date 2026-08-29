using Collectibles.Application.Interfaces;
using Collectibles.Application.Mappings.Explicit;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.CollectibleItems.Queries;

public class GetLinkCachesQueryHandler : IRequestHandler<GetLinkCachesQuery, List<LinkCacheDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetLinkCachesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<LinkCacheDto>> Handle(GetLinkCachesQuery request, CancellationToken cancellationToken)
    {
        // Cache rows carry the capture's storage paths, so they follow the item's visibility.
        var userId = _currentUserService.UserId;
        var isVisible = await _context.LinkInfos
            .Where(li => li.Id == request.LinkInfoId)
            .SelectMany(li => li.CollectibleItem.Showcases)
            .AnyAsync(s => !s.IsPrivate || s.UserId == userId, cancellationToken);

        if (!isVisible && !_currentUserService.IsAdministrator)
        {
            throw new UnauthorizedAccessException("You are not authorized to view this link's cache.");
        }

        var linkCaches = await _context.LinkCaches
            .Where(lc => lc.LinkInfoId == request.LinkInfoId)
            .ToListAsync(cancellationToken);

        return linkCaches.Select(lc => lc.ToDto()).ToList();
    }
}
