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
                .Where(a => a.IsMigrated && a.Deleted == null);

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
        // Note: IApplicationDbContext doesn't expose Database property directly
        // Transaction handling would need to be done at the Infrastructure layer
        // For now, we'll process without explicit transaction control
        try
        {
            _logger.LogInformation("Rolling back attachment {Id}: {Name}", attachment.Id, attachment.OriginalFilename ?? attachment.Name);

            // Delete from storage if requested
            if (deleteFromStorage && !string.IsNullOrEmpty(attachment.FilePath))
            {
                try
                {
                    await _fileStorage.DeleteFileAsync(attachment.FilePath, cancellationToken);
                    _logger.LogInformation("Deleted file from storage: {Path}", attachment.FilePath);

                    // Delete preview if exists
                    if (!string.IsNullOrEmpty(attachment.PreviewPath))
                    {
                        await _fileStorage.DeleteFileAsync(attachment.PreviewPath, cancellationToken);
                        _logger.LogInformation("Deleted preview from storage: {Path}", attachment.PreviewPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete files from storage for attachment {Id}", attachment.Id);
                    result.Errors.Add(new RollbackError
                    {
                        AttachmentId = attachment.Id,
                        AttachmentName = attachment.OriginalFilename ?? attachment.Name,
                        ErrorMessage = $"Storage deletion failed: {ex.Message}",
                        ErrorType = RollbackErrorType.StorageDeletionFailed,
                    });
                    result.FailureCount++;
                    return;
                }
            }

            // Update attachment in database
            attachment.FilePath = null;
            attachment.PreviewPath = null;
            attachment.IsMigrated = false;
            attachment.MigrationDate = null;

            _context.Attachments.Update(attachment);
            await _context.SaveChangesAsync(cancellationToken);

            // Transaction commit would be handled at Infrastructure layer
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
