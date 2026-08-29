using Collectibles.Application.Features.Attachments.Dtos;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Features.Attachments.Commands;

public class RollbackAttachmentMigrationCommandHandler : IRequestHandler<RollbackAttachmentMigrationCommand, RollbackResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<RollbackAttachmentMigrationCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public RollbackAttachmentMigrationCommandHandler(
        IApplicationDbContext context,
        IFileStorage fileStorage,
        ILogger<RollbackAttachmentMigrationCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _fileStorage = fileStorage;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<RollbackResult> Handle(RollbackAttachmentMigrationCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAdministrator)
        {
            throw new UnauthorizedAccessException("Only administrators can roll back attachment migrations.");
        }

        var result = new RollbackResult
        {
            StartTime = DateTime.UtcNow,
        };

        try
        {
            // Get attachments to rollback
            var query = _context.Attachments
                .Where(a => a.IsMigrated);

            if (request.AttachmentIds.Count != 0)
            {
                query = query.Where(a => request.AttachmentIds.Contains(a.Id));
            }

            var attachments = await query
                .OrderBy(a => a.Id)
                .ToListAsync(cancellationToken);

            result.TotalProcessed = attachments.Count;
            _logger.LogInformation("Starting rollback of {Count} attachments", attachments.Count);

            // Process in batches
            for (int i = 0; i < attachments.Count; i += request.BatchSize)
            {
                var batch = attachments.Skip(i).Take(request.BatchSize).ToList();
                await ProcessBatchAsync(batch, request.DeleteFromStorage, result, cancellationToken);
            }

            result.EndTime = DateTime.UtcNow;
            _logger.LogInformation(
                "Rollback completed. Success: {Success}, Failed: {Failed}, Duration: {Duration}",
                result.SuccessCount, result.FailureCount, result.Duration);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during rollback operation");
            result.EndTime = DateTime.UtcNow;
            result.Errors.Add(new RollbackError
            {
                AttachmentId = 0,
                AttachmentName = "Batch Operation",
                ErrorMessage = ex.Message,
                ErrorType = RollbackErrorType.Other,
            });
            return result;
        }
    }

    private async Task ProcessBatchAsync(List<Attachment> batch, bool deleteFromStorage, RollbackResult result, CancellationToken cancellationToken)
    {
        foreach (var attachment in batch)
        {
            await RollbackAttachmentAsync(attachment, deleteFromStorage, result, cancellationToken);
        }
    }

    private async Task RollbackAttachmentAsync(Attachment attachment, bool deleteFromStorage, RollbackResult result, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Rolling back attachment {Id}: {Name}", attachment.Id, attachment.OriginalFilename ?? attachment.Name);

            // Rolling back means "the database copy is authoritative again", so it must
            // actually exist. CleanupMigratedAttachments nulls AttachmentContent to reclaim
            // space; rolling back such an attachment would leave content in neither place.
            var hasDatabaseCopy = await _context.AttachmentContents
                .AnyAsync(
                    ac => ac.Id == attachment.Id && ac.Content != null && ac.Content.Length > 0,
                    cancellationToken);

            if (!hasDatabaseCopy)
            {
                _logger.LogWarning(
                    "Skipping rollback of attachment {Id}: no database copy of the content exists, so storage is the only copy",
                    attachment.Id);

                result.Errors.Add(new RollbackError
                {
                    AttachmentId = attachment.Id,
                    AttachmentName = attachment.OriginalFilename ?? attachment.Name,
                    ErrorMessage = "The database copy of this attachment's content has been cleaned up, so storage holds the only copy. Re-download or re-upload the content before rolling back.",
                    ErrorType = RollbackErrorType.MissingDatabaseCopy,
                });
                result.FailureCount++;
                return;
            }

            // Commit the database change first: the storage delete is irreversible, so it
            // must never run before the surviving copy is durably recorded as authoritative.
            var previousFilePath = attachment.FilePath;
            var previousPreviewPath = attachment.PreviewPath;

            attachment.FilePath = null;
            attachment.PreviewPath = null;
            attachment.IsMigrated = false;
            attachment.MigrationDate = null;

            _context.Attachments.Update(attachment);
            await _context.SaveChangesAsync(cancellationToken);

            // Delete from storage if requested (best effort; a failure now leaves an orphan
            // blob that orphan-cleanup can reclaim, not lost content).
            if (deleteFromStorage && !string.IsNullOrEmpty(previousFilePath))
            {
                try
                {
                    await _fileStorage.DeleteFileAsync(previousFilePath, cancellationToken);
                    _logger.LogInformation("Deleted file from storage: {Path}", previousFilePath);

                    // Delete preview if exists
                    if (!string.IsNullOrEmpty(previousPreviewPath))
                    {
                        await _fileStorage.DeleteFileAsync(previousPreviewPath, cancellationToken);
                        _logger.LogInformation("Deleted preview from storage: {Path}", previousPreviewPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete files from storage for attachment {Id}", attachment.Id);
                    result.Errors.Add(new RollbackError
                    {
                        AttachmentId = attachment.Id,
                        AttachmentName = attachment.OriginalFilename ?? attachment.Name,
                        ErrorMessage = $"Rollback succeeded but storage deletion failed (the blob is now an orphan): {ex.Message}",
                        ErrorType = RollbackErrorType.StorageDeletionFailed,
                    });
                }
            }

            result.SuccessCount++;
            _logger.LogInformation("Successfully rolled back attachment {Id}", attachment.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback attachment {Id}: {Name}", attachment.Id, attachment.OriginalFilename ?? attachment.Name);

            result.Errors.Add(new RollbackError
            {
                AttachmentId = attachment.Id,
                AttachmentName = attachment.OriginalFilename ?? attachment.Name,
                ErrorMessage = ex.Message,
                ErrorType = RollbackErrorType.DatabaseUpdateFailed,
            });
            result.FailureCount++;
        }
    }
}
