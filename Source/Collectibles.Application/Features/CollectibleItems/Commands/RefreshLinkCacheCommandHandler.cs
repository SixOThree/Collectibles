using Collectibles.Application.Interfaces;
using Collectibles.Domain.Configuration;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Collectibles.Application.Features.CollectibleItems.Commands;

public class RefreshLinkCacheCommandHandler : IRequestHandler<RefreshLinkCacheCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ExternalLinksOptions _externalLinksOptions;

    public RefreshLinkCacheCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IOptions<ExternalLinksOptions> externalLinksOptions)
    {
        _context = context;
        _currentUserService = currentUserService;
        _externalLinksOptions = externalLinksOptions.Value;
    }

    public async Task Handle(RefreshLinkCacheCommand request, CancellationToken cancellationToken)
    {
        if (!_externalLinksOptions.CachingEnabled)
        {
            throw new InvalidOperationException("Link caching is disabled.");
        }

        // AddLink/DeleteLink in this feature verify item ownership; re-queueing a capture
        // (which makes the server fetch the URL again) must too.
        var ownsItem = await _context.LinkInfos
            .Where(li => li.Id == request.LinkInfoId)
            .SelectMany(li => li.CollectibleItem.Showcases)
            .AnyAsync(s => s.UserId == _currentUserService.UserId, cancellationToken);

        if (!ownsItem)
        {
            throw new UnauthorizedAccessException("You are not authorized to refresh this link.");
        }

        var linkCache = new LinkCache
        {
            LinkInfoId = request.LinkInfoId,
            Status = LinkCacheStatus.Pending,
            CachedDate = DateTime.UtcNow,
        };

        _context.LinkCaches.Add(linkCache);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
