using Collectibles.Application.Interfaces;
using Collectibles.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Maintenance.Queries;

public record OrphanStatsDto
{
    public int OrphanedAttachmentCount { get; init; }
    public long OrphanedAttachmentBytes { get; init; }
    public int OrphanedBlobCount { get; init; }
    public long OrphanedBlobBytes { get; init; }
    public int EmptyItemCount { get; init; }
}

public record GetOrphanStatsQuery : IRequest<OrphanStatsDto>;

public class GetOrphanStatsQueryHandler : IRequestHandler<GetOrphanStatsQuery, OrphanStatsDto>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly IFileStorage _fileStorage;

    public GetOrphanStatsQueryHandler(IApplicationDbContextFactory contextFactory, IFileStorage fileStorage)
    {
        _contextFactory = contextFactory;
        _fileStorage = fileStorage;
    }

    public async Task<OrphanStatsDto> Handle(GetOrphanStatsQuery request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Orphaned attachments: no CollectibleItemAttachment links and not used as a preview image
        var orphanedAttachments = await context.Attachments
            .Where(a => a.Deleted == null)
            .Where(a => !context.CollectibleItemAttachments.Any(cia => cia.AttachmentId == a.Id))
            .Where(a => !context.CollectibleItems.Any(ci => ci.PreviewImageId == a.Id && ci.Deleted == null))
            .Where(a => !context.Showcases.Any(s => EF.Property<long?>(s, "PreviewImageId") == a.Id && s.Deleted == null))
            .Select(a => new { a.FileSize })
            .ToListAsync(cancellationToken);

        // Empty items: no attachments and no children (not soft-deleted)
        var emptyItemCount = await context.CollectibleItems
            .Where(ci => ci.Deleted == null)
            .Where(ci => !ci.CollectibleItemAttachments.Any())
            .Where(ci => !context.CollectibleItems.Any(child => child.ParentId == ci.Id && child.Deleted == null))
            .CountAsync(cancellationToken);

        // Orphaned blobs: files in storage with no matching Attachment record
        var (orphanedBlobCount, orphanedBlobBytes) = await GetOrphanedBlobStats(context, cancellationToken);

        return new OrphanStatsDto
        {
            OrphanedAttachmentCount = orphanedAttachments.Count,
            OrphanedAttachmentBytes = orphanedAttachments.Sum(a => a.FileSize),
            OrphanedBlobCount = orphanedBlobCount,
            OrphanedBlobBytes = orphanedBlobBytes,
            EmptyItemCount = emptyItemCount,
        };
    }

    private async Task<(int Count, long Bytes)> GetOrphanedBlobStats(IApplicationDbContext context, CancellationToken cancellationToken)
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
            return (orphanedBlobs.Count, orphanedBlobs.Sum(b => b.SizeBytes));
        }
        catch
        {
            // If listing fails (e.g., DB storage provider), return zeros
            return (0, 0);
        }
    }

    private static async Task<HashSet<string>> GetAllKnownBlobPaths(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        var knownPathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Attachment file paths
        var attachmentPaths = await context.Attachments
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
