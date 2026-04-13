using Collectibles.Application.Interfaces;
using Collectibles.Domain.Configuration;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Options;

namespace Collectibles.Application.Features.CollectibleItems.Commands;

public class RefreshLinkCacheCommandHandler : IRequestHandler<RefreshLinkCacheCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ExternalLinksOptions _externalLinksOptions;

    public RefreshLinkCacheCommandHandler(IApplicationDbContext context, IOptions<ExternalLinksOptions> externalLinksOptions)
    {
        _context = context;
        _externalLinksOptions = externalLinksOptions.Value;
    }

    public async Task Handle(RefreshLinkCacheCommand request, CancellationToken cancellationToken)
    {
        if (!_externalLinksOptions.CachingEnabled)
        {
            throw new InvalidOperationException("Link caching is disabled.");
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
