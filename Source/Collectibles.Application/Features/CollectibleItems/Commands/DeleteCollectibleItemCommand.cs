using Collectibles.Application.Interfaces;
using Collectibles.Application.Services;
using Collectibles.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.CollectibleItems.Commands;

public enum ChildItemAction
{
    Orphan,
    Delete,
}

public class DeleteCollectibleItemCommand : IRequest<DeleteCollectibleItemResult>
{
    public long Id { get; set; }
    public ChildItemAction ChildItemAction { get; set; } = ChildItemAction.Orphan;
}

public class DeleteCollectibleItemResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int DeletedItemsCount { get; set; }
    public int OrphanedItemsCount { get; set; }
    public int DeletedAttachmentsCount { get; set; }
}

public class DeleteCollectibleItemCommandHandler : IRequestHandler<DeleteCollectibleItemCommand, DeleteCollectibleItemResult>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEventLogService _eventLogService;
    private readonly IHashIdsService _hashIdsService;

    public DeleteCollectibleItemCommandHandler(
        IApplicationDbContextFactory contextFactory,
        ICurrentUserService currentUserService,
        IEventLogService eventLogService,
        IHashIdsService hashIdsService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _eventLogService = eventLogService;
        _hashIdsService = hashIdsService;
    }

    public async Task<DeleteCollectibleItemResult> Handle(DeleteCollectibleItemCommand request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var collectibleItem = await context.CollectibleItems
            .Include(ci => ci.CollectibleItemAttachments)
                .ThenInclude(cia => cia.Attachment)
            .Include(ci => ci.CollectibleItemTags)
            .Include(ci => ci.CollectibleItemRelatedTags)
            .Include(ci => ci.Children)
            .Include(ci => ci.Showcases)
            .Include(ci => ci.ExternalReferences)
            .FirstOrDefaultAsync(ci => ci.Id == request.Id && ci.Deleted == null, cancellationToken);

        if (collectibleItem == null)
        {
            return new DeleteCollectibleItemResult
            {
                Success = false,
                ErrorMessage = $"Collectible item with ID {request.Id} not found.",
            };
        }

        // Check if the current user owns any of the showcases this item belongs to
        if (!collectibleItem.Showcases.Any(s => s.UserId == _currentUserService.UserId))
        {
            return new DeleteCollectibleItemResult
            {
                Success = false,
                ErrorMessage = "You don't have permission to delete this item.",
            };
        }

        var result = new DeleteCollectibleItemResult { Success = true };

        // Handle child items based on the specified action
        if (collectibleItem.Children != null && collectibleItem.Children.Count != 0)
        {
            if (request.ChildItemAction == ChildItemAction.Delete)
            {
                // Recursively delete all child items
                result.DeletedItemsCount = await DeleteChildItemsRecursively(context, collectibleItem, cancellationToken);
            }
            else
            {
                // Orphan the child items by removing their parent reference
                foreach (var child in collectibleItem.Children)
                {
                    child.ParentId = null;
                    result.OrphanedItemsCount++;
                }
            }
        }

        // Remove all tag associations
        context.CollectibleItemTags.RemoveRange(collectibleItem.CollectibleItemTags);
        context.CollectibleItemRelatedTags.RemoveRange(collectibleItem.CollectibleItemRelatedTags);

        // Remove external references
        if (collectibleItem.ExternalReferences != null && collectibleItem.ExternalReferences.Count != 0)
        {
            context.LinkInfos.RemoveRange(collectibleItem.ExternalReferences);
        }

        // Remove the item from all showcases (clear the many-to-many relationship)
        if (collectibleItem.Showcases != null && collectibleItem.Showcases.Count != 0)
        {
            collectibleItem.Showcases.Clear();
        }

        // Handle attachments
        var attachmentIds = collectibleItem.CollectibleItemAttachments.Select(cia => cia.AttachmentId).ToList();

        // Remove the collectible item attachment associations
        context.CollectibleItemAttachments.RemoveRange(collectibleItem.CollectibleItemAttachments);

        // Check if these attachments are used by other items
        foreach (var attachmentId in attachmentIds)
        {
            var isAttachmentUsedElsewhere = await context.CollectibleItemAttachments
                .AnyAsync(cia => cia.AttachmentId == attachmentId && cia.CollectibleItemId != request.Id, cancellationToken);

            if (!isAttachmentUsedElsewhere)
            {
                // Mark attachment as deleted if not used elsewhere
                var attachment = await context.Attachments.FindAsync(new object[] { attachmentId }, cancellationToken);
                if (attachment != null)
                {
                    attachment.Deleted = DateTime.UtcNow;
                    attachment.DeletedBy = _currentUserService.UserId;
                    result.DeletedAttachmentsCount++;
                }
            }
        }

        // Unassign QR code if any
        if (collectibleItem.QRCodeId.HasValue)
        {
            collectibleItem.QRCodeId = null;
        }

        // Mark the collectible item as deleted
        collectibleItem.Deleted = DateTime.UtcNow;
        collectibleItem.DeletedBy = _currentUserService.UserId;
        result.DeletedItemsCount++;

        // Log the deletion event
        await _eventLogService.LogEventAsync(
            EventAction.Delete,
            "CollectibleItem",
            collectibleItem.Id,
            collectibleItem.Name,
            new
            {
                Id = collectibleItem.Id,
                Name = collectibleItem.Name,
                HasChildren = collectibleItem.Children?.Any() ?? false,
                ChildCount = collectibleItem.Children?.Count ?? 0,
            },
            null,
            System.Text.Json.JsonSerializer.Serialize(new
            {
                ChildItemAction = request.ChildItemAction.ToString(),
                DeletedItemsCount = result.DeletedItemsCount,
                OrphanedItemsCount = result.OrphanedItemsCount,
                DeletedAttachmentsCount = result.DeletedAttachmentsCount,
            }),
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return result;
    }

    private async Task<int> DeleteChildItemsRecursively(
        IApplicationDbContext context,
        CollectibleItem parent,
        CancellationToken cancellationToken)
    {
        int deletedCount = 0;

        if (parent.Children == null || parent.Children.Count == 0)
        {
            return deletedCount;
        }

        foreach (var child in parent.Children)
        {
            // Load the full child with its relationships
            var fullChild = await context.CollectibleItems
                .Include(ci => ci.CollectibleItemAttachments)
                .Include(ci => ci.CollectibleItemTags)
                .Include(ci => ci.CollectibleItemRelatedTags)
                .Include(ci => ci.Children)
                .Include(ci => ci.Showcases)
                .Include(ci => ci.ExternalReferences)
                .FirstOrDefaultAsync(ci => ci.Id == child.Id, cancellationToken);

            if (fullChild != null)
            {
                // Recursively delete children of this child
                deletedCount += await DeleteChildItemsRecursively(context, fullChild, cancellationToken);

                // Remove tag associations
                context.CollectibleItemTags.RemoveRange(fullChild.CollectibleItemTags);
                context.CollectibleItemRelatedTags.RemoveRange(fullChild.CollectibleItemRelatedTags);

                // Remove external references
                if (fullChild.ExternalReferences != null && fullChild.ExternalReferences.Count != 0)
                {
                    context.LinkInfos.RemoveRange(fullChild.ExternalReferences);
                }

                // Remove the item from all showcases (clear the many-to-many relationship)
                if (fullChild.Showcases != null && fullChild.Showcases.Count != 0)
                {
                    fullChild.Showcases.Clear();
                }

                // Remove attachment associations
                context.CollectibleItemAttachments.RemoveRange(fullChild.CollectibleItemAttachments);

                // Unassign QR code if any
                if (fullChild.QRCodeId.HasValue)
                {
                    fullChild.QRCodeId = null;
                }

                // Mark the child as deleted
                fullChild.Deleted = DateTime.UtcNow;
                fullChild.DeletedBy = _currentUserService.UserId;
                deletedCount++;
            }
        }

        return deletedCount;
    }
}
