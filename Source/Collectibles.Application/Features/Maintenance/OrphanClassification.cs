using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Maintenance;

/// <summary>
/// The single source of truth for what counts as "orphaned" and what counts as a
/// "known" blob path.
/// </summary>
/// <remarks>
/// These queries used to exist as three hand-copied implementations (cleanup, stats,
/// details) that had drifted apart: cleanup excluded soft-deleted attachments from the
/// known-path set, so their blobs were classified as orphans and permanently deleted even
/// though the rows are restorable — while the dashboard reported different numbers than
/// cleanup would actually delete.
/// </remarks>
public static class OrphanClassification
{
    /// <summary>
    /// Attachments that no live item links to and that no item or showcase uses as a
    /// preview image. Soft-deleted attachments are excluded by the global query filter:
    /// they are restorable, not orphans.
    /// </summary>
    /// <returns></returns>
    public static IQueryable<Attachment> OrphanedAttachments(IApplicationDbContext context)
    {
        return context.Attachments
            .Where(a => !context.CollectibleItemAttachments.Any(cia =>
                cia.AttachmentId == a.Id &&
                context.CollectibleItems.Any(ci => ci.Id == cia.CollectibleItemId)))
            .Where(a => !context.CollectibleItems.Any(ci => ci.PreviewImageId == a.Id))
            .Where(a => !context.Showcases.Any(s => EF.Property<long?>(s, "PreviewImageId") == a.Id));
    }

    /// <summary>
    /// Live items that hold no attachments and have no live children.
    /// </summary>
    /// <returns></returns>
    public static IQueryable<CollectibleItem> EmptyItems(IApplicationDbContext context)
    {
        return context.CollectibleItems
            .Where(ci => !ci.CollectibleItemAttachments.Any())
            .Where(ci => !context.CollectibleItems.Any(child => child.ParentId == ci.Id));
    }

    /// <summary>
    /// Every storage path the database still references.
    /// </summary>
    /// <remarks>
    /// Soft-deleted attachments are deliberately included (via <c>IgnoreQueryFilters</c>):
    /// their rows are restorable, so their blobs are still referenced and must never be
    /// treated as orphans. Only <c>AttachmentPurgeBackgroundService</c> removes them, and
    /// only after the retention window and after the row delete has committed.
    /// </remarks>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task<HashSet<string>> GetAllKnownBlobPathsAsync(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        var knownPathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var attachmentPaths = await context.Attachments
            .IgnoreQueryFilters()
            .Where(a => a.FilePath != null || a.PreviewPath != null)
            .Select(a => new { a.FilePath, a.PreviewPath })
            .ToListAsync(cancellationToken);

        foreach (var paths in attachmentPaths)
        {
            if (!string.IsNullOrEmpty(paths.FilePath))
            {
                knownPathSet.Add(paths.FilePath);
            }

            if (!string.IsNullOrEmpty(paths.PreviewPath))
            {
                knownPathSet.Add(paths.PreviewPath);
            }
        }

        var linkCachePaths = await context.LinkCaches
            .Where(lc => lc.CachedContentPath != null || lc.ScreenshotPath != null)
            .Select(lc => new { lc.CachedContentPath, lc.ScreenshotPath })
            .ToListAsync(cancellationToken);

        foreach (var paths in linkCachePaths)
        {
            if (!string.IsNullOrEmpty(paths.CachedContentPath))
            {
                knownPathSet.Add(paths.CachedContentPath);
            }

            if (!string.IsNullOrEmpty(paths.ScreenshotPath))
            {
                knownPathSet.Add(paths.ScreenshotPath);
            }
        }

        return knownPathSet;
    }

    /// <summary>
    /// Storage blobs that the database no longer references.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task<List<StorageBlobInfo>> GetOrphanedBlobsAsync(
        IApplicationDbContext context,
        IFileStorage fileStorage,
        CancellationToken cancellationToken)
    {
        try
        {
            var storageBlobs = await fileStorage.ListBlobsAsync(cancellationToken);
            if (storageBlobs.Count == 0)
            {
                return [];
            }

            var knownPathSet = await GetAllKnownBlobPathsAsync(context, cancellationToken);

            return storageBlobs.Where(b => !knownPathSet.Contains(b.Name)).ToList();
        }
        catch
        {
            // Listing is not supported by every provider (e.g. database storage).
            return [];
        }
    }
}
