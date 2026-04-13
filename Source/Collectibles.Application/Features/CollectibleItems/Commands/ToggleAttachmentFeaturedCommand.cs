using Collectibles.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.CollectibleItems.Commands;

public class ToggleAttachmentFeaturedCommand : IRequest<bool>
{
    public long CollectibleItemId { get; set; }
    public long AttachmentId { get; set; }
}

public class ToggleAttachmentFeaturedCommandHandler : IRequestHandler<ToggleAttachmentFeaturedCommand, bool>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ICurrentUserService _currentUserService;

    public ToggleAttachmentFeaturedCommandHandler(IApplicationDbContextFactory contextFactory, ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(ToggleAttachmentFeaturedCommand request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Check if the current user has permission to edit this collectible item
        var hasPermission = await context.CollectibleItems
            .Where(ci => ci.Id == request.CollectibleItemId && ci.Deleted == null)
            .SelectMany(ci => ci.Showcases)
            .AnyAsync(s => s.UserId == _currentUserService.UserId, cancellationToken);

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException("You don't have permission to modify this item.");
        }

        var itemAttachment = await context.CollectibleItemAttachments
            .FirstOrDefaultAsync(
                cia =>
                cia.CollectibleItemId == request.CollectibleItemId &&
                cia.AttachmentId == request.AttachmentId,
                cancellationToken);

        if (itemAttachment == null)
        {
            throw new InvalidOperationException("Attachment not found for this collectible item.");
        }

        // Toggle the featured status
        itemAttachment.IsFeatured = !itemAttachment.IsFeatured;

        if (itemAttachment.IsFeatured)
        {
            itemAttachment.FeaturedDate = DateTime.UtcNow;

            // Get the max display order for featured items
            var maxDisplayOrder = await context.CollectibleItemAttachments
                .Where(cia => cia.CollectibleItemId == request.CollectibleItemId && cia.IsFeatured)
                .MaxAsync(cia => (int?)cia.DisplayOrder, cancellationToken) ?? -1;

            itemAttachment.DisplayOrder = maxDisplayOrder + 1;
        }
        else
        {
            itemAttachment.FeaturedDate = null;
            itemAttachment.DisplayOrder = 0;
        }

        await context.SaveChangesAsync(cancellationToken);

        return itemAttachment.IsFeatured;
    }
}
