namespace Collectibles.Application.Interfaces;

/// <summary>
/// Shared service for resolving/creating collectible item hierarchies from folder paths.
/// Used by sync upload, zip upload, and the move attachment command.
/// </summary>
public interface IItemHierarchyService
{
    /// <summary>
    /// Resolves or creates the item hierarchy for a folder path within a showcase.
    /// For each segment, checks for an existing non-deleted item with matching
    /// name + parentId + showcase membership. Creates if missing.
    /// Returns the ID of the deepest (leaf) item.
    /// </summary>
    Task<long> ResolveOrCreateHierarchyAsync(
        long showcaseId,
        string[] folderSegments,
        string? userId,
        CancellationToken ct,
        long? contentDefinitionId = null);

    /// <summary>
    /// Links an attachment to an item via CollectibleItemAttachment.
    /// Skips if already linked. Does not set PreviewImageId.
    /// </summary>
    Task LinkAttachmentAsync(long itemId, long attachmentId, CancellationToken ct);

    /// <summary>
    /// Checks if an attachment with the given content hash already exists
    /// on the target item. Item-scoped (not showcase-wide) because the same
    /// image can legitimately appear under different items.
    /// Returns the attachment ID if found, null otherwise.
    /// </summary>
    Task<long?> FindDuplicateAttachmentAsync(
        long itemId, string contentHash, CancellationToken ct);

    /// <summary>
    /// Walks up the parent chain from the given item, soft-deleting items
    /// that have no attachments and no non-deleted children.
    /// </summary>
    Task CleanupEmptyParentsAsync(long itemId, long showcaseId, CancellationToken ct);
}
