using Collectibles.Application.Interfaces;
using Collectibles.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Maintenance.Queries;

public record OrphanedAttachmentDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? OriginalFilename { get; init; }
    public string? FileType { get; init; }
    public long FileSize { get; init; }
    public DateTime? Created { get; init; }
}

public record OrphanedBlobDto
{
    public string Name { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
}

public record OrphanedItemDto
{
    public long Id { get; init; }
    public string? Name { get; init; }
    public DateTime? Created { get; init; }
}

public record OrphanDetailsDto
{
    public List<OrphanedAttachmentDto> Attachments { get; init; } = new();
    public List<OrphanedBlobDto> Blobs { get; init; } = new();
    public List<OrphanedItemDto> Items { get; init; } = new();
}

public record GetOrphanDetailsQuery : IRequest<OrphanDetailsDto>;

public class GetOrphanDetailsQueryHandler : IRequestHandler<GetOrphanDetailsQuery, OrphanDetailsDto>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly IFileStorage _fileStorage;

    public GetOrphanDetailsQueryHandler(IApplicationDbContextFactory contextFactory, IFileStorage fileStorage)
    {
        _contextFactory = contextFactory;
        _fileStorage = fileStorage;
    }

    public async Task<OrphanDetailsDto> Handle(GetOrphanDetailsQuery request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var orphanedAttachments = await context.Attachments
            .Where(a => a.Deleted == null)
            .Where(a => !context.CollectibleItemAttachments.Any(cia => cia.AttachmentId == a.Id))
            .Where(a => !context.CollectibleItems.Any(ci => ci.PreviewImageId == a.Id && ci.Deleted == null))
            .Where(a => !context.Showcases.Any(s => EF.Property<long?>(s, "PreviewImageId") == a.Id && s.Deleted == null))
            .OrderBy(a => a.Name)
            .Select(a => new OrphanedAttachmentDto
            {
                Id = a.Id,
                Name = a.Name,
                OriginalFilename = a.OriginalFilename,
                FileType = a.FileType,
                FileSize = a.FileSize,
                Created = a.Created,
            })
            .ToListAsync(cancellationToken);

        var emptyItems = await context.CollectibleItems
            .Where(ci => ci.Deleted == null)
            .Where(ci => !ci.CollectibleItemAttachments.Any())
            .Where(ci => !context.CollectibleItems.Any(child => child.ParentId == ci.Id && child.Deleted == null))
            .OrderBy(ci => ci.Name)
            .Select(ci => new OrphanedItemDto
            {
                Id = ci.Id,
                Name = ci.Name,
                Created = ci.Created,
            })
            .ToListAsync(cancellationToken);

        var blobs = await GetOrphanedBlobs(context, cancellationToken);

        return new OrphanDetailsDto
        {
            Attachments = orphanedAttachments,
            Blobs = blobs,
            Items = emptyItems,
        };
    }

    private async Task<List<OrphanedBlobDto>> GetOrphanedBlobs(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            var storageBlobs = await _fileStorage.ListBlobsAsync(cancellationToken);
            if (storageBlobs.Count == 0)
                return new();

            // Get all known file paths from the database (attachments + link caches)
            var knownPathSet = await GetAllKnownBlobPaths(context, cancellationToken);

            return storageBlobs
                .Where(b => !knownPathSet.Contains(b.Name))
                .OrderBy(b => b.Name)
                .Select(b => new OrphanedBlobDto { Name = b.Name, SizeBytes = b.SizeBytes })
                .ToList();
        }
        catch
        {
            return new();
        }
    }

    private static async Task<HashSet<string>> GetAllKnownBlobPaths(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        var knownPathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
