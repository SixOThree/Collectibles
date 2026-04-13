using Collectibles.Application.Interfaces;
using Collectibles.Domain.Configuration;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Collectibles.Application.Features.CollectibleItems.Commands;

public class AddLinkToCollectibleItemCommandHandler : IRequestHandler<AddLinkToCollectibleItemCommand, long>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ExternalLinksOptions _externalLinksOptions;

    public AddLinkToCollectibleItemCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IOptions<ExternalLinksOptions> externalLinksOptions)
    {
        _context = context;
        _currentUserService = currentUserService;
        _externalLinksOptions = externalLinksOptions.Value;
    }

    public async Task<long> Handle(AddLinkToCollectibleItemCommand request, CancellationToken cancellationToken)
    {
        if (!_externalLinksOptions.Enabled)
        {
            throw new InvalidOperationException("External links are disabled.");
        }

        // Verify current user owns the item through its showcases
        var ownsItem = await _context.CollectibleItems
            .Where(ci => ci.Id == request.CollectibleItemId)
            .SelectMany(ci => ci.Showcases)
            .AnyAsync(s => s.UserId == _currentUserService.UserId, cancellationToken);

        if (!ownsItem)
        {
            throw new UnauthorizedAccessException("You are not authorized to add links to this item.");
        }

        var linkInfo = new LinkInfo
        {
            CollectibleItemId = request.CollectibleItemId,
            Url = request.Url,
        };

        var linkCache = new LinkCache
        {
            LinkInfo = linkInfo,
            Status = LinkCacheStatus.Pending,
            CachedDate = DateTime.UtcNow,
        };

        linkInfo.Caches.Add(linkCache);

        _context.LinkInfos.Add(linkInfo);

        await _context.SaveChangesAsync(cancellationToken);

        return linkInfo.Id;
    }
}
