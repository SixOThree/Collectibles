using Collectibles.Application.Features.Maintenance;
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
    private readonly ICurrentUserService _currentUserService;

    public GetOrphanStatsQueryHandler(
        IApplicationDbContextFactory contextFactory,
        IFileStorage fileStorage,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _fileStorage = fileStorage;
        _currentUserService = currentUserService;
    }

    public async Task<OrphanStatsDto> Handle(GetOrphanStatsQuery request, CancellationToken cancellationToken)
    {
        // Reports across every user's content and exposes raw storage paths; CleanupOrphans
        // already requires administrator, and its read siblings must match.
        if (!_currentUserService.IsAdministrator)
        {
            throw new UnauthorizedAccessException("Only administrators can view orphan statistics.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Orphaned attachments: no CollectibleItemAttachment links and not used as a preview image
        var orphanedAttachments = await OrphanClassification.OrphanedAttachments(context)
            .Select(a => new { a.FileSize })
            .ToListAsync(cancellationToken);

        // Empty items: no attachments and no children (not soft-deleted)
        var emptyItemCount = await OrphanClassification.EmptyItems(context)
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
        var orphanedBlobs = await OrphanClassification.GetOrphanedBlobsAsync(context, _fileStorage, cancellationToken);
        return (orphanedBlobs.Count, orphanedBlobs.Sum(b => b.SizeBytes));
    }
}
