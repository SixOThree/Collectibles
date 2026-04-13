using Collectibles.Domain.Entities;

namespace Collectibles.Application.Services;

public interface ICollectibleItemPreviewResolver
{
    /// <summary>
    /// Gets the preview image URL for a collectible item.
    /// Checks in order: PreviewImage, first image attachment, collage preview
    /// </summary>
    Task<string?> GetPreviewUrlAsync(CollectibleItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the preview image URL for a collectible item by ID.
    /// </summary>
    Task<string?> GetPreviewUrlAsync(long itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets preview URLs for multiple items efficiently.
    /// </summary>
    Task<Dictionary<long, string?>> GetPreviewUrlsAsync(IEnumerable<long> itemIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if an item has any form of preview available.
    /// </summary>
    Task<bool> HasPreviewAsync(CollectibleItem item, CancellationToken cancellationToken = default);
}