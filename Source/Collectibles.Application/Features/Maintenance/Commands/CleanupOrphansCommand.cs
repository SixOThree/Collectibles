using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Maintenance.Commands;

public record CleanupOrphansResult
{
    public int AttachmentsDeleted { get; init; }
    public int OrphanedBlobsDeleted { get; init; }
    public int ItemsDeleted { get; init; }
    public long BytesFreed { get; init; }
    public long BlobBytesFreed { get; init; }
}

public record CleanupOrphansCommand : IRequest<CleanupOrphansResult>;

public class CleanupOrphansCommandHandler : IRequestHandler<CleanupOrphansCommand, CleanupOrphansResult>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly IFileStorage _fileStorage;
    private readonly IEventLogService _eventLogService;

    public CleanupOrphansCommandHandler(
        IApplicationDbContextFactory contextFactory,
        IFileStorage fileStorage,
        IEventLogService eventLogService)
    {
        _contextFactory = contextFactory;
        _fileStorage = fileStorage;
        _eventLogService = eventLogService;
    }

    public async Task<CleanupOrphansResult> Handle(CleanupOrphansCommand request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // 1. Delete orphaned attachments (no item links and not used as preview images)
        var orphanedAttachments = await context.Attachments
            .Where(a => a.Deleted == null)
            .Where(a => !context.CollectibleItemAttachments.Any(cia =>
                cia.AttachmentId == a.Id &&
                context.CollectibleItems.Any(ci => ci.Id == cia.CollectibleItemId && ci.Deleted == null)))
            .Where(a => !context.CollectibleItems.Any(ci => ci.PreviewImageId == a.Id && ci.Deleted == null))
            .Where(a => !context.Showcases.Any(s => EF.Property<long?>(s, "PreviewImageId") == a.Id && s.Deleted == null))
            .ToListAsync(cancellationToken);

        long bytesFreed = 0;

        foreach (var attachment in orphanedAttachments)
        {
            // Delete files from storage
            if (!string.IsNullOrEmpty(attachment.FilePath))
            {
                try
                { await _fileStorage.DeleteFileAsync(attachment.FilePath, cancellationToken); }
                catch { /* Storage file may already be gone */ }
            }

            if (!string.IsNullOrEmpty(attachment.PreviewPath))
            {
                try
                { await _fileStorage.DeleteFileAsync(attachment.PreviewPath, cancellationToken); }
                catch { /* Storage file may already be gone */ }
            }

            bytesFreed += attachment.FileSize;
            context.Attachments.Remove(attachment);
        }

        // 2. Soft-delete empty items (no attachments and no children)
        var emptyItems = await context.CollectibleItems
            .Where(ci => ci.Deleted == null)
            .Where(ci => !ci.CollectibleItemAttachments.Any())
            .Where(ci => !context.CollectibleItems.Any(child => child.ParentId == ci.Id && child.Deleted == null))
            .ToListAsync(cancellationToken);

        foreach (var item in emptyItems)
        {
            item.Deleted = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);

        // 3. Delete orphaned blobs (in storage but not in any Attachment record)
        var (orphanedBlobsDeleted, blobBytesFreed) = await CleanupOrphanedBlobs(context, cancellationToken);

        // Log the cleanup
        await _eventLogService.LogEventAsync(
            EventAction.Delete,
            "Maintenance",
            null,
            "Orphan cleanup",
            null,
            new
            {
                AttachmentsDeleted = orphanedAttachments.Count,
                OrphanedBlobsDeleted = orphanedBlobsDeleted,
                ItemsDeleted = emptyItems.Count,
                BytesFreed = bytesFreed,
                BlobBytesFreed = blobBytesFreed,
            },
            cancellationToken: cancellationToken);

        return new CleanupOrphansResult
        {
            AttachmentsDeleted = orphanedAttachments.Count,
            OrphanedBlobsDeleted = orphanedBlobsDeleted,
            ItemsDeleted = emptyItems.Count,
            BytesFreed = bytesFreed,
            BlobBytesFreed = blobBytesFreed,
        };
    }

    private async Task<(int Count, long Bytes)> CleanupOrphanedBlobs(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            var storageBlobs = await _fileStorage.ListBlobsAsync(cancellationToken);
            if (storageBlobs.Count == 0)
            {
                return (0, 0);
            }

            // Get all known file paths from the database (attachments + link caches)
            var knownPathSet = await GetAllKnownBlobPaths(context, cancellationToken);

            var orphanedBlobs = storageBlobs.Where(b => !knownPathSet.Contains(b.Name)).ToList();
            long blobBytesFreed = 0;
            var deletedCount = 0;

            foreach (var blob in orphanedBlobs)
            {
                try
                {
                    await _fileStorage.DeleteFileAsync(blob.Name, cancellationToken);
                    blobBytesFreed += blob.SizeBytes;
                    deletedCount++;
                }
                catch
                {
                    // Continue on individual blob deletion failures
                }
            }

            return (deletedCount, blobBytesFreed);
        }
        catch
        {
            return (0, 0);
        }
    }

    private static async Task<HashSet<string>> GetAllKnownBlobPaths(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        var knownPathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Attachment file paths
        var attachmentPaths = await context.Attachments
            .Where(a => a.Deleted == null)
            .Where(a => a.FilePath != null || a.PreviewPath != null)
            .Select(a => new { a.FilePath, a.PreviewPath })
            .ToListAsync(cancellationToken);

        foreach (var paths in attachmentPaths)
        {
            if (!string.IsNullOrEmpty(paths.FilePath))
                knownPathSet.Add(paths.FilePath);
            if (!string.IsNullOrEmpty(paths.PreviewPath))
                knownPathSet.Add(paths.PreviewPath);
        }

        // Link cache file paths
        var linkCachePaths = await context.LinkCaches
            .Where(lc => lc.CachedContentPath != null || lc.ScreenshotPath != null)
            .Select(lc => new { lc.CachedContentPath, lc.ScreenshotPath })
            .ToListAsync(cancellationToken);

        foreach (var paths in linkCachePaths)
        {
            if (!string.IsNullOrEmpty(paths.CachedContentPath))
                knownPathSet.Add(paths.CachedContentPath);
            if (!string.IsNullOrEmpty(paths.ScreenshotPath))
                knownPathSet.Add(paths.ScreenshotPath);
        }

        return knownPathSet;
    }
}
