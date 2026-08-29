using Collectibles.Application.Services;
using Collectibles.Infrastructure.Persistence;

using Microsoft.Extensions.Logging;

namespace Collectibles.Infrastructure.Services;

public class CollectibleItemPreviewResolver : ICollectibleItemPreviewResolver
{
    private readonly ApplicationDbContext _context;
    private readonly IHashIdsService _hashIdsService;
    private readonly ILogger<CollectibleItemPreviewResolver> _logger;

    public CollectibleItemPreviewResolver(
        ApplicationDbContext context,
        IHashIdsService hashIdsService,
        ILogger<CollectibleItemPreviewResolver> logger)
    {
        _context = context;
        _hashIdsService = hashIdsService;
        _logger = logger;
    }

    public async Task<string?> GetPreviewUrlAsync(CollectibleItem item, CancellationToken cancellationToken = default)
    {
        try
        {
            // First priority: Explicit preview image
            if (item.PreviewImage != null)
            {
                return GeneratePreviewUrl(item.PreviewImage.Id);
            }

            // Second priority: First image attachment
            if (item.CollectibleItemAttachments?.Any() == true)
            {
                var firstImage = item.CollectibleItemAttachments
                    .Where(cia => cia.Attachment != null &&
                           cia.Attachment.FileType != null &&
                           cia.Attachment.FileType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    .Select(cia => cia.Attachment)
                    .FirstOrDefault();

                if (firstImage != null)
                {
                    return GeneratePreviewUrl(firstImage.Id);
                }
            }

            // Third priority: Check if we need to load attachments from database
            if (item.CollectibleItemAttachments == null)
            {
                var firstImageId = await _context.CollectibleItemAttachments
                    .Where(cia => cia.CollectibleItemId == item.Id &&
                           cia.Attachment.FileType != null &&
                           cia.Attachment.FileType.StartsWith("image/"))
                    .OrderBy(cia => cia.DisplayOrder)
                    .ThenBy(cia => cia.AttachmentId)
                    .Select(cia => cia.AttachmentId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (firstImageId > 0)
                {
                    return GeneratePreviewUrl(firstImageId);
                }
            }

            _logger.LogDebug("No preview found for item {ItemId}", item.Id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving preview for item {ItemId}", item.Id);
            return null;
        }
    }

    public async Task<string?> GetPreviewUrlAsync(long itemId, CancellationToken cancellationToken = default)
    {
        // First check for explicit preview image
        var previewImageId = await _context.CollectibleItems
            .Where(ci => ci.Id == itemId && ci.PreviewImageId != null)
            .Select(ci => ci.PreviewImageId)
            .FirstOrDefaultAsync(cancellationToken);

        if (previewImageId.HasValue)
        {
            return GeneratePreviewUrl(previewImageId.Value);
        }

        // Then check for first image attachment
        var firstImageId = await _context.CollectibleItemAttachments
            .Where(cia => cia.CollectibleItemId == itemId &&
                   cia.Attachment.FileType != null &&
                   cia.Attachment.FileType.StartsWith("image/"))
            .OrderBy(cia => cia.DisplayOrder)
            .ThenBy(cia => cia.AttachmentId)
            .Select(cia => cia.AttachmentId)
            .FirstOrDefaultAsync(cancellationToken);

        if (firstImageId > 0)
        {
            return GeneratePreviewUrl(firstImageId);
        }

        return null;
    }

    public async Task<Dictionary<long, string?>> GetPreviewUrlsAsync(IEnumerable<long> itemIds, CancellationToken cancellationToken = default)
    {
        var itemIdList = itemIds.ToList();
        var result = new Dictionary<long, string?>();

        // Get all items with preview images
        var itemsWithPreviews = await _context.CollectibleItems
            .Where(ci => itemIdList.Contains(ci.Id) && ci.PreviewImageId != null)
            .Select(ci => new { ci.Id, ci.PreviewImageId })
            .ToListAsync(cancellationToken);

        foreach (var item in itemsWithPreviews)
        {
            result[item.Id] = GeneratePreviewUrl(item.PreviewImageId!.Value);
        }

        // For items without preview images, get first image attachment
        var itemsWithoutPreviews = itemIdList.Except(result.Keys).ToList();
        if (itemsWithoutPreviews.Any())
        {
            var firstImages = await _context.CollectibleItemAttachments
                .Where(cia => itemsWithoutPreviews.Contains(cia.CollectibleItemId) &&
                       cia.Attachment.FileType != null &&
                       cia.Attachment.FileType.StartsWith("image/"))
                .GroupBy(cia => cia.CollectibleItemId)
                .Select(g => new
                {
                    ItemId = g.Key,
                    AttachmentId = g.OrderBy(cia => cia.DisplayOrder)
                                    .ThenBy(cia => cia.AttachmentId)
                                    .Select(cia => cia.AttachmentId)
                                    .FirstOrDefault(),
                })
                .ToListAsync(cancellationToken);

            foreach (var item in firstImages)
            {
                result[item.ItemId] = item.AttachmentId > 0 ? GeneratePreviewUrl(item.AttachmentId) : null;
            }
        }

        // Set null for items with no previews
        foreach (var itemId in itemIdList.Where(id => !result.ContainsKey(id)))
        {
            result[itemId] = null;
        }

        return result;
    }

    public async Task<bool> HasPreviewAsync(CollectibleItem item, CancellationToken cancellationToken = default)
    {
        if (item.PreviewImage != null)
        {
            return true;
        }

        if (item.CollectibleItemAttachments?.Any(cia =>
            cia.Attachment?.FileType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true) == true)
        {
            return true;
        }

        // Check database if attachments not loaded
        if (item.CollectibleItemAttachments == null)
        {
            return await _context.CollectibleItemAttachments
                .AnyAsync(
                    cia => cia.CollectibleItemId == item.Id &&
                          cia.Attachment.FileType != null &&
                          cia.Attachment.FileType.StartsWith("image/"), cancellationToken);
        }

        return false;
    }

    private string GeneratePreviewUrl(long attachmentId)
    {
        return $"/api/attachments/{_hashIdsService.Encode(attachmentId)}/preview";
    }
}
