using Collectibles.Application.Interfaces;
using Collectibles.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.CollectibleItems.Commands;

public class DeleteLinkCommandHandler : IRequestHandler<DeleteLinkCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorage _fileStorage;
    private readonly ICurrentUserService _currentUserService;

    public DeleteLinkCommandHandler(IApplicationDbContext context, IFileStorage fileStorage, ICurrentUserService currentUserService)
    {
        _context = context;
        _fileStorage = fileStorage;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteLinkCommand request, CancellationToken cancellationToken)
    {
        var linkInfo = await _context.LinkInfos
            .Include(li => li.Caches)
            .FirstOrDefaultAsync(li => li.Id == request.LinkInfoId, cancellationToken)
            ?? throw new ArgumentException($"Link not found: {request.LinkInfoId}");

        // Verify current user owns the item this link belongs to
        var ownsItem = await _context.CollectibleItems
            .Where(ci => ci.Id == linkInfo.CollectibleItemId)
            .SelectMany(ci => ci.Showcases)
            .AnyAsync(s => s.UserId == _currentUserService.UserId, cancellationToken);

        if (!ownsItem)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete this link.");
        }

        // Delete cached files from storage
        foreach (var cache in linkInfo.Caches)
        {
            if (!string.IsNullOrEmpty(cache.CachedContentPath))
            {
                await _fileStorage.DeleteFileAsync(cache.CachedContentPath, cancellationToken);
            }
            if (!string.IsNullOrEmpty(cache.ScreenshotPath))
            {
                await _fileStorage.DeleteFileAsync(cache.ScreenshotPath, cancellationToken);
            }
        }

        _context.LinkInfos.Remove(linkInfo);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
