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
    private readonly ICurrentUserService _currentUserService;

    public CleanupOrphansCommandHandler(
        IApplicationDbContextFactory contextFactory,
        IFileStorage fileStorage,
        IEventLogService eventLogService,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _fileStorage = fileStorage;
        _eventLogService = eventLogService;
        _currentUserService = currentUserService;
    }

    public async Task<CleanupOrphansResult> Handle(CleanupOrphansCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAdministrator)
        {
            throw new UnauthorizedAccessException("Only administrators can clean up orphaned records.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // 1. Delete orphaned attachments (no item links and not used as preview images)
        var orphanedAttachments = await OrphanClassification.OrphanedAttachments(context)
            .ToListAsync(cancellationToken);

        long bytesFreed = 0;

        // Paths are collected here and deleted only after the row deletes commit: an
        // irreversible storage delete must never precede the transactional write.
        var pathsToDelete = new List<string>();

        foreach (var attachment in orphanedAttachments)
        {
            if (!string.IsNullOrEmpty(attachment.FilePath))
            {
                pathsToDelete.Add(attachment.FilePath);
            }

            if (!string.IsNullOrEmpty(attachment.PreviewPath))
            {
                pathsToDelete.Add(attachment.PreviewPath);
            }

            bytesFreed += attachment.FileSize;
            context.Attachments.Remove(attachment);
        }

        // 2. Soft-delete empty items (no attachments and no children)
        var emptyItems = await OrphanClassification.EmptyItems(context)
            .ToListAsync(cancellationToken);

        foreach (var item in emptyItems)
        {
            item.Deleted = DateTime.UtcNow;
            item.DeletedBy = _currentUserService.UserId;
        }

        await context.SaveChangesAsync(cancellationToken);

        foreach (var path in pathsToDelete)
        {
            try
            {
                await _fileStorage.DeleteFileAsync(path, cancellationToken);
            }
            catch
            {
                // Storage file may already be gone; the row is deleted either way.
            }
        }

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
        var orphanedBlobs = await OrphanClassification.GetOrphanedBlobsAsync(context, _fileStorage, cancellationToken);

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
}
