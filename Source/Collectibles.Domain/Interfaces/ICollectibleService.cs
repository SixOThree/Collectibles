using Collectibles.Domain.Entities;

namespace Collectibles.Domain.Interfaces;

/// <summary>
/// Domain service interface for collectible item operations.
/// </summary>
public interface ICollectibleService
{
    Task<CollectibleItem> CreateCollectibleItemAsync(
        string name,
        string? detailedDescription = null,
        CancellationToken cancellationToken = default);

    Task<CollectibleItem?> GetCollectibleItemAsync(
        long id,
        bool includeAttachments = false,
        bool includeTags = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CollectibleItem>> GetCollectibleItemsAsync(
        string? searchTerm = null,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default);

    Task UpdateCollectibleItemAsync(
        CollectibleItem collectibleItem,
        CancellationToken cancellationToken = default);

    Task DeleteCollectibleItemAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task AddTagToCollectibleItemAsync(
        long collectibleItemId,
        long tagId,
        CancellationToken cancellationToken = default);

    Task RemoveTagFromCollectibleItemAsync(
        long collectibleItemId,
        long tagId,
        CancellationToken cancellationToken = default);
}
