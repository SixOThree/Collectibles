using Collectibles.Application.Interfaces;

namespace Collectibles.Infrastructure.Services;

/// <summary>
/// Service for detecting duplicate attachments based on content hash.
/// </summary>
public class AttachmentDuplicateDetectionService(IApplicationDbContextFactory contextFactory) : IAttachmentDuplicateDetectionService
{
    private readonly IApplicationDbContextFactory _contextFactory = contextFactory;

    /// <inheritdoc />
    public async Task<DuplicateCheckResult> CheckForDuplicatesAsync(
        string contentHash,
        long? collectibleItemId,
        long? excludeAttachmentId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(contentHash))
        {
            return new DuplicateCheckResult();
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Find attachments with matching hash
        var matchingAttachmentsQuery = context.Attachments
            .Where(a => a.ContentHash == contentHash);

        // Exclude the attachment being checked (for update scenarios)
        if (excludeAttachmentId.HasValue)
        {
            matchingAttachmentsQuery = matchingAttachmentsQuery.Where(a => a.Id != excludeAttachmentId.Value);
        }

        var matchingAttachments = await matchingAttachmentsQuery
            .Select(a => new
            {
                a.Id,
                a.Name,
                CollectibleItems = a.CollectibleItemAttachments
                    .Select(cia => new { cia.CollectibleItemId, cia.CollectibleItem.Name })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        if (matchingAttachments.Count == 0)
        {
            return new DuplicateCheckResult();
        }

        // Check if any match is within the same collectible item
        if (collectibleItemId.HasValue)
        {
            var withinItemMatch = matchingAttachments
                .FirstOrDefault(a => a.CollectibleItems.Any(ci => ci.CollectibleItemId == collectibleItemId));

            if (withinItemMatch != null)
            {
                var itemInfo = withinItemMatch.CollectibleItems
                    .First(ci => ci.CollectibleItemId == collectibleItemId);

                return new DuplicateCheckResult
                {
                    IsDuplicateWithinItem = true,
                    DuplicateAttachmentId = withinItemMatch.Id,
                    DuplicateAttachmentName = withinItemMatch.Name,
                    DuplicateCollectibleItemId = itemInfo.CollectibleItemId,
                    DuplicateCollectibleItemName = itemInfo.Name,
                };
            }
        }

        // Match exists but not within the same item
        var otherMatch = matchingAttachments.First();
        var otherItemInfo = otherMatch.CollectibleItems.FirstOrDefault();

        return new DuplicateCheckResult
        {
            IsDuplicateElsewhere = true,
            DuplicateAttachmentId = otherMatch.Id,
            DuplicateAttachmentName = otherMatch.Name,
            DuplicateCollectibleItemId = otherItemInfo?.CollectibleItemId,
            DuplicateCollectibleItemName = otherItemInfo?.Name,
        };
    }

    /// <inheritdoc />
    public async Task<AttachmentIndexingStats> GetIndexingStatsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var total = await context.Attachments
            .Where(a => a.FilePath != null)
            .CountAsync(cancellationToken);

        var indexed = await context.Attachments
            .Where(a => a.ContentHash != null)
            .CountAsync(cancellationToken);

        return new AttachmentIndexingStats
        {
            TotalAttachments = total,
            IndexedAttachments = indexed,
            PendingAttachments = total - indexed,
            PercentComplete = total > 0 ? Math.Round((double)indexed / total * 100, 1) : 100,
        };
    }
}
