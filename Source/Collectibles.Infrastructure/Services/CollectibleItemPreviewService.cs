using Collectibles.Application.Interfaces;
using Collectibles.Application.Services;
using Collectibles.Domain.Common.Enums;
using Collectibles.Domain.Configuration.Storage;
using Collectibles.Domain.Enums;
using Collectibles.Domain.Interfaces;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Collectibles.Infrastructure.Services;

public class CollectibleItemPreviewService(
    IApplicationDbContextFactory contextFactory,
    IFileProcessingService fileProcessingService,
    IFileStorage fileStorage,
    ILogger<CollectibleItemPreviewService> logger,
    IOptions<StorageSettings> storageOptions) : ICollectibleItemPreviewService
{
    private readonly IApplicationDbContextFactory _contextFactory = contextFactory;
    private readonly IFileProcessingService _fileProcessingService = fileProcessingService;
    private readonly IFileStorage _fileStorage = fileStorage;
    private readonly ILogger<CollectibleItemPreviewService> _logger = logger;
    private readonly StorageSettings _storageSettings = storageOptions.Value;

    public async Task<bool> NeedsCollagePreviewAsync(long collectibleItemId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Use projection to load only the data we need - avoid loading entire object graphs
        var item = await context.CollectibleItems
            .AsNoTracking() // Read-only query
            .Where(i => i.Id == collectibleItemId)
            .Select(i => new
            {
                i.Id,
                HasAttachments = i.CollectibleItemAttachments.Any(),
                i.PreviewImageId,
                Children = i.Children.Select(c => new CollectibleItem { Id = c.Id }).ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (item == null)
        {
            return false;
        }

        // Check if item has no attachments but has children with images (recursively)
        if (item.HasAttachments)
        {
            return false;
        }

        // Check if item already has a preview image
        if (item.PreviewImageId.HasValue)
        {
            return false;
        }

        // Check if children have any image attachments recursively (up to depth 4)
        const int maxDepth = 4;
        return await HasImagesRecursively(item.Children, 1, maxDepth, cancellationToken);
    }

    private async Task<bool> HasImagesRecursively(IEnumerable<CollectibleItem> items, int currentDepth, int maxDepth, CancellationToken cancellationToken = default)
    {
        if (currentDepth > maxDepth || !items.Any())
        {
            return false;
        }

        // Use a single DbContext and BFS to avoid creating contexts in loops
        // This is a major performance improvement over the recursive approach
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var currentLevelIds = items.Select(i => i.Id).ToList();
        var processedIds = new HashSet<long>();

        // Process items level by level using BFS (up to maxDepth)
        for (int depth = currentDepth; depth <= maxDepth; depth++)
        {
            if (!currentLevelIds.Any())
            {
                break;
            }

            // Mark current IDs as processed to avoid cycles
            foreach (var id in currentLevelIds)
            {
                processedIds.Add(id);
            }

            // Use projection to load only the data we need for checking images
            // This avoids loading entire object graphs
            var itemsWithImages = await context.CollectibleItems
                .AsNoTracking() // Read-only query
                .Where(x => currentLevelIds.Contains(x.Id))
                .Select(i => new
                {
                    i.Id,
                    HasPreviewInDb = i.PreviewImage != null && i.PreviewImage.AttachmentPreview != null && i.PreviewImage.AttachmentPreview.PreviewThumbnail != null,
                    HasPreviewInStorage = i.PreviewImage != null && !string.IsNullOrEmpty(i.PreviewImage.PreviewPath),
                    HasAttachmentPreviewInDb = i.CollectibleItemAttachments.Any(a => a.Attachment != null && a.Attachment.AttachmentPreview != null && a.Attachment.AttachmentPreview.PreviewThumbnail != null),
                    HasAttachmentPreviewInStorage = i.CollectibleItemAttachments.Any(a => a.Attachment != null && !string.IsNullOrEmpty(a.Attachment.PreviewPath)),
                    ChildrenIds = i.Children.Select(c => c.Id).ToList(),
                })
                .ToListAsync(cancellationToken);

            // Check if any item at this level has images
            if (itemsWithImages.Any(i => i.HasPreviewInDb || i.HasPreviewInStorage || i.HasAttachmentPreviewInDb || i.HasAttachmentPreviewInStorage))
            {
                return true;
            }

            // Prepare next level (children that haven't been processed yet)
            currentLevelIds = itemsWithImages
                .SelectMany(i => i.ChildrenIds)
                .Where(id => !processedIds.Contains(id))
                .Distinct()
                .ToList();
        }

        return false;
    }

    public async Task<bool> GenerateCollagePreviewAsync(long collectibleItemId, CancellationToken cancellationToken = default, bool useRandomSelection = false, long? overrideShowcaseId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Load the item with its showcases to get the showcase ID
        var item = await context.CollectibleItems
            .Include(i => i.Showcases)
            .FirstOrDefaultAsync(i => i.Id == collectibleItemId, cancellationToken);

        if (item == null)
        {
            _logger.LogWarning("CollectibleItem {ItemId} not found", collectibleItemId);
            return false;
        }

        // Get candidate images from children (up to 32 images)
        var candidateImages = await GetCandidateImagesAsync(collectibleItemId, maxCandidates: 32, maxDepth: 4, cancellationToken);

        if (!candidateImages.Any())
        {
            _logger.LogInformation("No suitable images found for collage generation for item {ItemId}", collectibleItemId);
            return false;
        }

        // Select images for the collage
        List<byte[]> imageContents;
        const int maxImages = 4;

        if (useRandomSelection && candidateImages.Count > maxImages)
        {
            // Randomly select 4 images from candidates
            var random = new Random();
            imageContents = candidateImages
                .OrderBy(x => random.Next())
                .Take(maxImages)
                .ToList();
        }
        else
        {
            // Take first 4 images
            imageContents = candidateImages.Take(maxImages).ToList();
        }

        try
        {
            // Generate the collage
            var collageBytes = await _fileProcessingService.GenerateCollagePreviewAsync(imageContents, cancellationToken);
            if (collageBytes == null || collageBytes.Length == 0)
            {
                _logger.LogWarning("Failed to generate collage for item {ItemId}", collectibleItemId);
                return false;
            }

            // Use the provided showcase ID or get it from the item (items can belong to multiple showcases, take the first one)
            long? showcaseId = overrideShowcaseId ?? item.Showcases?.FirstOrDefault()?.Id;

            // Save collage to storage
            var fileName = $"collage_{collectibleItemId}_{DateTime.UtcNow.Ticks}.jpg";
            var filePath = await _fileStorage.SaveFileAsync(
                collageBytes,
                fileName,
                "image/jpeg",
                showcaseId: showcaseId,
                cancellationToken);

            // Create attachment entity for the collage
            var attachment = new Attachment
            {
                Name = $"Collage for {item.Name}",
                OriginalFilename = "collage.jpg",
                FileType = "image/jpeg",
                AttachmentType = AttachmentType.Image,
                FileSize = collageBytes.Length,
                FilePath = filePath,
                PreviewPath = filePath, // For images, preview path is same as file path
                Created = DateTime.UtcNow,
                CreatedBy = "System",
            };

            // Add attachment to context first
            context.Attachments.Add(attachment);
            await context.SaveChangesAsync(cancellationToken);

            // Only store preview in the database when using the Database storage provider
            if (_storageSettings.Provider == StorageProvider.Database)
            {
                var preview = new AttachmentPreview
                {
                    Id = attachment.Id,
                    PreviewThumbnail = collageBytes,
                };

                context.AttachmentPreviews.Add(preview);
                await context.SaveChangesAsync(cancellationToken);
            }

            // Link the collage as an attachment of the item so it appears in the attachments list
            context.CollectibleItemAttachments.Add(new CollectibleItemAttachment
            {
                CollectibleItemId = item.Id,
                AttachmentId = attachment.Id,
            });

            // Update collectible item with preview
            item.PreviewImageId = attachment.Id;
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully generated collage preview for item {ItemId} with {ImageCount} images from {CandidateCount} candidates",
                collectibleItemId, imageContents.Count, candidateImages.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating collage preview for item {ItemId}", collectibleItemId);
            return false;
        }
    }

    private async Task<List<byte[]>> GetCandidateImagesAsync(
        long collectibleItemId,
        int maxCandidates = 32,
        int maxDepth = 4,
        CancellationToken cancellationToken = default)
    {
        var candidateImages = new List<byte[]>();
        var processedItemIds = new HashSet<long>();

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Start with the root item's children
        var rootItem = await context.CollectibleItems
            .AsNoTracking() // Read-only query
            .Where(i => i.Id == collectibleItemId)
            .Select(i => new { i.Id, HasChildren = i.Children.Any() })
            .FirstOrDefaultAsync(cancellationToken);

        if (rootItem == null || !rootItem.HasChildren)
        {
            return candidateImages;
        }

        // Process items level by level using BFS
        var currentLevelIds = new List<long> { collectibleItemId };

        for (int depth = 1; depth <= maxDepth && candidateImages.Count < maxCandidates; depth++)
        {
            // Get children of current level
            var nextLevelIds = await context.CollectibleItems
                .AsNoTracking() // Read-only query
                .Where(i => currentLevelIds.Contains(i.ParentId!.Value))
                .Select(i => i.Id)
                .ToListAsync(cancellationToken);

            if (!nextLevelIds.Any())
            {
                break;
            }

            // Load items with their attachments and preview images for this level
            var itemsWithImages = await context.CollectibleItems
                .AsNoTracking() // Read-only query
                .Where(i => nextLevelIds.Contains(i.Id))
                .Select(i => new
                {
                    i.Id,
                    PreviewImage = i.PreviewImage != null ? new
                    {
                        i.PreviewImage.Id,
                        i.PreviewImage.PreviewPath,
                        PreviewThumbnail = i.PreviewImage.AttachmentPreview != null
                            ? i.PreviewImage.AttachmentPreview.PreviewThumbnail
                            : null,
                    }
                    : null,
                    Attachments = i.CollectibleItemAttachments
                        .Where(a => a.Attachment != null &&
                               (a.Attachment.AttachmentType == AttachmentType.Image))
                        .Select(a => new
                        {
                            a.Attachment.Id,
                            a.Attachment.PreviewPath,
                            PreviewThumbnail = a.Attachment.AttachmentPreview != null
                                ? a.Attachment.AttachmentPreview.PreviewThumbnail
                                : null,
                        })
                        .ToList(),
                })
                .ToListAsync(cancellationToken);

            // Collect images from this level
            foreach (var item in itemsWithImages)
            {
                if (candidateImages.Count >= maxCandidates)
                {
                    break;
                }

                // Check preview image
                if (item.PreviewImage != null)
                {
                    byte[]? imageData = null;

                    // Prefer external storage when a path is available
                    if (!string.IsNullOrEmpty(item.PreviewImage.PreviewPath))
                    {
                        try
                        {
                            imageData = await LoadPreviewFromStorageAsync(item.PreviewImage.PreviewPath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to load preview from storage for item {ItemId}", item.Id);
                        }
                    }

                    // Fall back to database
                    if (imageData == null && item.PreviewImage.PreviewThumbnail != null)
                    {
                        imageData = item.PreviewImage.PreviewThumbnail;
                    }

                    if (imageData != null && imageData.Length > 0)
                    {
                        candidateImages.Add(imageData);
                        if (candidateImages.Count >= maxCandidates)
                        {
                            break;
                        }
                    }
                }

                // Check attachments
                foreach (var attachment in item.Attachments)
                {
                    if (candidateImages.Count >= maxCandidates)
                    {
                        break;
                    }

                    byte[]? imageData = null;

                    // Prefer external storage when a path is available
                    if (!string.IsNullOrEmpty(attachment.PreviewPath))
                    {
                        try
                        {
                            imageData = await LoadPreviewFromStorageAsync(attachment.PreviewPath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to load attachment preview from storage for item {ItemId}", item.Id);
                        }
                    }

                    // Fall back to database
                    if (imageData == null && attachment.PreviewThumbnail != null)
                    {
                        imageData = attachment.PreviewThumbnail;
                    }

                    if (imageData != null && imageData.Length > 0)
                    {
                        candidateImages.Add(imageData);
                    }
                }
            }

            // Move to next level
            currentLevelIds = nextLevelIds;
        }

        _logger.LogInformation(
            "Collected {Count} candidate images for item {ItemId} (max depth: {MaxDepth})",
            candidateImages.Count, collectibleItemId, maxDepth);

        return candidateImages;
    }

    private async Task<byte[]?> LoadPreviewFromStorageAsync(string previewPath)
    {
        try
        {
            // Try to load the preview from external storage
            var previewBytes = await _fileStorage.GetFileAsync(previewPath);
            return previewBytes;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load preview from storage at path: {Path}", previewPath);
            return null;
        }
    }

    public async Task<int> GenerateCollagePreviewsForShowcaseAsync(long showcaseId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Get all items in showcase that might need collage previews
        var itemIds = await context.Showcases
            .AsNoTracking() // Read-only query
            .Where(s => s.Id == showcaseId)
            .SelectMany(s => s.CollectibleItems)
            .Where(i => !i.PreviewImageId.HasValue)
            .Where(i => !i.CollectibleItemAttachments.Any())
            .Where(i => i.Children.Any())
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        int successCount = 0;
        foreach (var itemId in itemIds)
        {
            if (await NeedsCollagePreviewAsync(itemId, cancellationToken))
            {
                if (await GenerateCollagePreviewAsync(itemId, cancellationToken, false, showcaseId))
                {
                    successCount++;
                }
            }
        }

        _logger.LogInformation("Generated {Count} collage previews for showcase {ShowcaseId}", successCount, showcaseId);
        return successCount;
    }

    public async Task<int> GenerateMissingCollagePreviewsAsync(int batchSize = 10, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Find items that potentially need collage previews
        var candidateIds = await context.CollectibleItems
            .AsNoTracking() // Read-only query
            .Where(i => !i.PreviewImageId.HasValue)
            .Where(i => !i.CollectibleItemAttachments.Any())
            .Where(i => i.Children.Any(c =>
                c.CollectibleItemAttachments.Any() || c.PreviewImageId.HasValue))
            .Take(batchSize)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        int successCount = 0;
        foreach (var itemId in candidateIds)
        {
            if (await GenerateCollagePreviewAsync(itemId, cancellationToken))
            {
                successCount++;
            }
        }

        _logger.LogInformation(
            "Generated {Count} missing collage previews from batch of {BatchSize}",
            successCount, candidateIds.Count);
        return successCount;
    }
}
