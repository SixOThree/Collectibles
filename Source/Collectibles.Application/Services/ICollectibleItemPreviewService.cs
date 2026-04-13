namespace Collectibles.Application.Services;

public interface ICollectibleItemPreviewService
{
    Task<bool> NeedsCollagePreviewAsync(long collectibleItemId, CancellationToken cancellationToken = default);
    Task<bool> GenerateCollagePreviewAsync(long collectibleItemId, CancellationToken cancellationToken = default, bool useRandomSelection = false, long? overrideShowcaseId = null);
    Task<int> GenerateCollagePreviewsForShowcaseAsync(long showcaseId, CancellationToken cancellationToken = default);
    Task<int> GenerateMissingCollagePreviewsAsync(int batchSize = 10, CancellationToken cancellationToken = default);
}
