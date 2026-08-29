using System.Diagnostics;

using Collectibles.Application.Features.Attachments.Dtos;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Features.Attachments.Commands;

public class MigrateAttachmentsToAzureCommandHandler : IRequestHandler<MigrateAttachmentsToAzureCommand, MigrationResult>
{
    private readonly IApplicationDbContextFactory _dbContextFactory;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<MigrateAttachmentsToAzureCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public MigrateAttachmentsToAzureCommandHandler(
        IApplicationDbContextFactory dbContextFactory,
        IFileStorage fileStorage,
        ILogger<MigrateAttachmentsToAzureCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _dbContextFactory = dbContextFactory;
        _fileStorage = fileStorage;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<MigrationResult> Handle(MigrateAttachmentsToAzureCommand request, CancellationToken cancellationToken)
    {
        // Its Cleanup/Rollback siblings already require administrator; this bulk storage
        // operation is at least as privileged.
        if (!_currentUserService.IsAdministrator)
        {
            throw new UnauthorizedAccessException("Only administrators can migrate attachments to Azure.");
        }

        var result = new MigrationResult
        {
            StartTime = DateTime.UtcNow,
        };

        try
        {
            _logger.LogInformation("Starting attachment migration to Azure with batch size: {BatchSize}", request.BatchSize);

            // Track processing speed for time estimation
            var stopwatch = Stopwatch.StartNew();
            var processedCount = 0;
            var lastProcessedId = 0L;

            // Get total count of unmigrated attachments
            await using (var countContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
            {
                var totalCount = await countContext.Attachments
                    .Where(a => !a.IsMigrated && a.AttachmentContent != null && a.AttachmentContent.Content != null)
                    .CountAsync(cancellationToken);

                _logger.LogInformation("Found {TotalCount} attachments to migrate", totalCount);
                result.TotalProcessed = 0;
            }

            // Process attachments in batches
            var hasMore = true;
            while (hasMore && !cancellationToken.IsCancellationRequested)
            {
                await using (var batchContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
                {
                    // Query next batch of unmigrated attachments
                    var batch = await batchContext.Attachments
                        .Include(a => a.AttachmentContent)
                        .Include(a => a.AttachmentPreview)
                        .Where(a => !a.IsMigrated &&
                                   a.AttachmentContent != null &&
                                   a.AttachmentContent.Content != null &&
                                   a.Id > lastProcessedId)
                        .OrderBy(a => a.Id)
                        .Take(request.BatchSize)
                        .ToListAsync(cancellationToken);

                    hasMore = batch.Count == request.BatchSize;

                    if (batch.Count == 0)
                    {
                        _logger.LogInformation("No more attachments to migrate");
                        break;
                    }

                    _logger.LogInformation("Processing batch of {BatchCount} attachments", batch.Count);

                    // Process each attachment in the batch
                    foreach (var attachment in batch)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        var attachmentStopwatch = Stopwatch.StartNew();

                        try
                        {
                            _logger.LogDebug(
                                "Processing attachment {AttachmentId}: {AttachmentName}",
                                attachment.Id, attachment.Name);

                            // Use a separate context for each attachment to enable individual save operations
                            await using (var attachmentContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
                            {
                                try
                                {
                                    // Re-query the attachment with tracking enabled
                                    var trackedAttachment = await attachmentContext.Attachments
                                        .Include(a => a.AttachmentContent)
                                        .Include(a => a.AttachmentPreview)
                                        .FirstAsync(a => a.Id == attachment.Id, cancellationToken);

                                    var migrated = false;

                                    // Upload main content if exists
                                    if (trackedAttachment.AttachmentContent?.Content != null)
                                    {
                                        _logger.LogDebug(
                                            "Uploading content for attachment {AttachmentId}, size: {Size} bytes",
                                            attachment.Id, trackedAttachment.AttachmentContent.Content.Length);

                                        var contentPath = await _fileStorage.SaveFileAsync(
                                            trackedAttachment.AttachmentContent.Content,
                                            $"{attachment.Id}_{attachment.OriginalFilename ?? attachment.Name}",
                                            attachment.FileType ?? "application/octet-stream",
                                            null, // No showcase ID for migration
                                            cancellationToken);

                                        trackedAttachment.FilePath = contentPath;
                                        migrated = true;

                                        _logger.LogDebug("Content uploaded successfully to: {FilePath}", contentPath);

                                        // Verify upload if requested
                                        if (!request.SkipVerification)
                                        {
                                            var verifyData = await _fileStorage.GetFileAsync(contentPath, cancellationToken);
                                            if (verifyData == null || verifyData.Length != trackedAttachment.AttachmentContent.Content.Length)
                                            {
                                                throw new InvalidOperationException(
                                                    $"Verification failed: uploaded file size mismatch. " +
                                                    $"Expected: {trackedAttachment.AttachmentContent.Content.Length}, " +
                                                    $"Actual: {verifyData?.Length ?? 0}");
                                            }
                                        }
                                    }

                                    // Upload preview if exists
                                    if (trackedAttachment.AttachmentPreview?.PreviewThumbnail != null)
                                    {
                                        _logger.LogDebug(
                                            "Uploading preview for attachment {AttachmentId}, size: {Size} bytes",
                                            attachment.Id, trackedAttachment.AttachmentPreview.PreviewThumbnail.Length);

                                        var previewPath = await _fileStorage.SaveFileAsync(
                                            trackedAttachment.AttachmentPreview.PreviewThumbnail,
                                            $"{attachment.Id}_preview_{attachment.OriginalFilename ?? attachment.Name}",
                                            "image/jpeg", // Previews are typically JPEG
                                            null, // No showcase ID for migration
                                            cancellationToken);

                                        trackedAttachment.PreviewPath = previewPath;
                                        migrated = true;

                                        _logger.LogDebug("Preview uploaded successfully to: {PreviewPath}", previewPath);
                                    }

                                    // Update migration status
                                    if (migrated)
                                    {
                                        trackedAttachment.IsMigrated = true;
                                        trackedAttachment.MigrationDate = DateTime.UtcNow;

                                        await attachmentContext.SaveChangesAsync(cancellationToken);

                                        result.SuccessCount++;
                                        _logger.LogInformation(
                                            "Successfully migrated attachment {AttachmentId}: {AttachmentName}",
                                            attachment.Id, attachment.Name);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Attachment {AttachmentId} has no content to migrate", attachment.Id);
                                    }
                                }
                                catch (Exception)
                                {
                                    throw;
                                }
                            }

                            processedCount++;
                            result.TotalProcessed++;
                            lastProcessedId = attachment.Id;

                            // Calculate and log progress
                            if (processedCount % 10 == 0)
                            {
                                var elapsed = stopwatch.Elapsed;
                                var avgTimePerAttachment = elapsed.TotalSeconds / processedCount;

                                _logger.LogInformation(
                                    "Progress: {Processed} attachments processed. " +
                                    "Success: {Success}, Failed: {Failed}. " +
                                    "Success rate: {SuccessRate:P}. " +
                                    "Avg time per attachment: {AvgTime:F2}s",
                                    result.TotalProcessed,
                                    result.SuccessCount,
                                    result.FailureCount,
                                    result.SuccessCount / (double)result.TotalProcessed,
                                    avgTimePerAttachment);
                            }
                        }
                        catch (Exception ex)
                        {
                            result.FailureCount++;
                            result.TotalProcessed++;
                            lastProcessedId = attachment.Id;

                            var error = new MigrationError
                            {
                                AttachmentId = attachment.Id,
                                AttachmentName = attachment.Name,
                                ErrorMessage = ex.Message,
                                ErrorType = ex.GetType().Name,
                                OccurredAt = DateTime.UtcNow,
                            };

                            result.Errors.Add(error);

                            _logger.LogError(
                                ex,
                                "Failed to migrate attachment {AttachmentId}: {AttachmentName}. Error: {ErrorMessage}",
                                attachment.Id, attachment.Name, ex.Message);

                            // Continue with next attachment
                        }
                        finally
                        {
                            attachmentStopwatch.Stop();
                            _logger.LogDebug(
                                "Attachment {AttachmentId} processing time: {ElapsedMs}ms",
                                attachment.Id, attachmentStopwatch.ElapsedMilliseconds);
                        }
                    }
                }

                // Small delay between batches to avoid overwhelming the system
                if (hasMore && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }

            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
            result.Success = result.FailureCount == 0;

            _logger.LogInformation(
                "Migration completed. Total: {Total}, Success: {Success}, Failed: {Failed}, Duration: {Duration}",
                result.TotalProcessed, result.SuccessCount, result.FailureCount, result.Duration);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during attachment migration");
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
            result.Success = false;
            result.Errors.Add(new MigrationError
            {
                ErrorMessage = ex.Message,
                ErrorType = ex.GetType().Name,
                OccurredAt = DateTime.UtcNow,
            });
            return result;
        }
    }
}
