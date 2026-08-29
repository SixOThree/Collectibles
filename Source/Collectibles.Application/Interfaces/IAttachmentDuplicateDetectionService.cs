namespace Collectibles.Application.Interfaces;

/// <summary>
/// Service for detecting duplicate attachments based on content hash.
/// </summary>
public interface IAttachmentDuplicateDetectionService
{
    /// <summary>
    /// Checks for duplicate attachments based on content hash.
    /// </summary>
    /// <param name="contentHash">The SHA-256 hash of the file content.</param>
    /// <param name="collectibleItemId">The collectible item ID to check for duplicates within.</param>
    /// <param name="excludeAttachmentId">Optional attachment ID to exclude from the check (e.g., when updating).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The duplicate check result.</returns>
    Task<DuplicateCheckResult> CheckForDuplicatesAsync(
        string contentHash,
        long? collectibleItemId,
        long? excludeAttachmentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets indexing statistics for the dashboard.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Indexing statistics.</returns>
    Task<AttachmentIndexingStats> GetIndexingStatsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a duplicate attachment check.
/// </summary>
public record DuplicateCheckResult
{
    /// <summary>
    /// Gets a value indicating whether true if a duplicate exists within the same collectible item.
    /// </summary>
    public bool IsDuplicateWithinItem { get; init; }

    /// <summary>
    /// Gets a value indicating whether true if a duplicate exists in a different collectible item.
    /// </summary>
    public bool IsDuplicateElsewhere { get; init; }

    /// <summary>
    /// Gets the ID of the duplicate attachment, if found.
    /// </summary>
    public long? DuplicateAttachmentId { get; init; }

    /// <summary>
    /// Gets the name of the duplicate attachment, if found.
    /// </summary>
    public string? DuplicateAttachmentName { get; init; }

    /// <summary>
    /// Gets the ID of the collectible item containing the duplicate, if applicable.
    /// </summary>
    public long? DuplicateCollectibleItemId { get; init; }

    /// <summary>
    /// Gets the name of the collectible item containing the duplicate, if applicable.
    /// </summary>
    public string? DuplicateCollectibleItemName { get; init; }
}

/// <summary>
/// Statistics about attachment indexing progress.
/// </summary>
public record AttachmentIndexingStats
{
    /// <summary>
    /// Gets total number of attachments with file paths.
    /// </summary>
    public int TotalAttachments { get; init; }

    /// <summary>
    /// Gets number of attachments that have been indexed (hash computed).
    /// </summary>
    public int IndexedAttachments { get; init; }

    /// <summary>
    /// Gets number of attachments pending indexing.
    /// </summary>
    public int PendingAttachments { get; init; }

    /// <summary>
    /// Gets percentage of indexing complete (0-100).
    /// </summary>
    public double PercentComplete { get; init; }
}
