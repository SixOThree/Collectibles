using Collectibles.Application.Features.Attachments.Dtos;
using Collectibles.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Features.Attachments.Commands;

public class CleanupMigratedAttachmentsCommandHandler : IRequestHandler<CleanupMigratedAttachmentsCommand, CleanupResult>
{
    private readonly IApplicationDbContextFactory _dbContextFactory;
    private readonly ILogger<CleanupMigratedAttachmentsCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public CleanupMigratedAttachmentsCommandHandler(
        IApplicationDbContextFactory dbContextFactory,
        ILogger<CleanupMigratedAttachmentsCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<CleanupResult> Handle(CleanupMigratedAttachmentsCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAdministrator)
        {
            throw new UnauthorizedAccessException("Only administrators can clean up migrated attachments.");
        }

        var result = new CleanupResult
        {
            StartTime = DateTime.UtcNow,
        };

        try
        {
            _logger.LogInformation(
                "Starting cleanup of migrated attachments with retention days: {RetentionDays}, preview only: {PreviewOnly}",
                request.RetentionDays, request.PreviewOnly);

            var cutoffDate = DateTime.UtcNow.AddDays(-request.RetentionDays);
            var lastProcessedId = 0L;
            var processedCount = 0;

            // Get total count of eligible attachments
            await using (var countContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
            {
                var eligibleQuery = countContext.Attachments
                    .Include(a => a.AttachmentContent)
                    .Include(a => a.AttachmentPreview)
                    .Where(a => a.IsMigrated
                        && a.MigrationDate != null
                        && a.MigrationDate < cutoffDate
                        && a.Deleted == null
                        && a.AttachmentContent != null
                        && a.AttachmentContent.Content != null);

                // For now, we'll process all migrated attachments since we don't have verification tracking
                // The OnlyVerified flag will be used when verification tracking is added
                if (request.OnlyVerified)
                {
                    _logger.LogWarning("OnlyVerified flag is set but verification tracking is not yet implemented. Processing all eligible attachments.");
                }

                result.TotalEligible = await eligibleQuery.CountAsync(cancellationToken);
            }

            _logger.LogInformation("Found {Count} attachments eligible for cleanup", result.TotalEligible);

            if (result.TotalEligible == 0)
            {
                result.EndTime = DateTime.UtcNow;
                return result;
            }

            // Process in batches
            while (processedCount < result.TotalEligible)
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

                // Get next batch of eligible attachments
                var attachmentsBatch = await context.Attachments
                    .Include(a => a.AttachmentContent)
                    .Include(a => a.AttachmentPreview)
                    .Where(a => a.IsMigrated
                        && a.MigrationDate != null
                        && a.MigrationDate < cutoffDate
                        && a.Deleted == null
                        && a.Id > lastProcessedId
                        && a.AttachmentContent != null
                        && a.AttachmentContent.Content != null)
                    .OrderBy(a => a.Id)
                    .Take(request.BatchSize)
                    .ToListAsync(cancellationToken);

                if (attachmentsBatch.Count == 0)
                {
                    break;
                }

                foreach (var attachment in attachmentsBatch)
                {
                    try
                    {
                        // Calculate space to be reclaimed
                        long spaceToReclaim = 0;

                        if (attachment.AttachmentContent?.Content != null)
                        {
                            spaceToReclaim += attachment.AttachmentContent.Content.Length;
                        }

                        if (attachment.AttachmentPreview?.PreviewThumbnail != null)
                        {
                            spaceToReclaim += attachment.AttachmentPreview.PreviewThumbnail.Length;
                        }

                        if (!request.PreviewOnly)
                        {
                            // Clear the database content
                            if (attachment.AttachmentContent != null)
                            {
                                attachment.AttachmentContent.Content = null;
                            }

                            if (attachment.AttachmentPreview != null)
                            {
                                attachment.AttachmentPreview.PreviewThumbnail = null;
                            }

                            // Save changes for this attachment
                            await context.SaveChangesAsync(cancellationToken);
                        }

                        result.CleanedCount++;
                        result.SpaceReclaimed += spaceToReclaim;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error cleaning attachment {AttachmentId} ({AttachmentName})",
                            attachment.Id, attachment.Name);

                        result.SkippedCount++;
                        result.Errors.Add(new CleanupError
                        {
                            AttachmentId = attachment.Id,
                            AttachmentName = attachment.Name,
                            Reason = ex.Message,
                            ErrorType = CleanupErrorType.DatabaseError,
                        });
                    }
                }

                processedCount += attachmentsBatch.Count;
                lastProcessedId = attachmentsBatch.Last().Id;

                // Log progress periodically
                if (processedCount % 100 == 0 || processedCount == result.TotalEligible)
                {
                    var spaceMB = result.SpaceReclaimed / (1024.0 * 1024.0);
                    _logger.LogInformation(
                        "Cleanup progress: {Processed}/{Total} ({Percentage:F1}%) - " +
                        "Cleaned: {Cleaned}, Skipped: {Skipped} - " +
                        "Space reclaimed: {SpaceMB:F2} MB",
                        processedCount,
                        result.TotalEligible,
                        (processedCount * 100.0) / result.TotalEligible,
                        result.CleanedCount,
                        result.SkippedCount,
                        spaceMB);
                }

                // Allow cancellation between batches
                cancellationToken.ThrowIfCancellationRequested();
            }

            result.EndTime = DateTime.UtcNow;

            var totalSpaceMB = result.SpaceReclaimed / (1024.0 * 1024.0);
            _logger.LogInformation(
                "Attachment cleanup completed. Total eligible: {Total}, Cleaned: {Cleaned}, " +
                "Skipped: {Skipped}, Space reclaimed: {SpaceMB:F2} MB, Preview only: {PreviewOnly}",
                result.TotalEligible,
                result.CleanedCount,
                result.SkippedCount,
                totalSpaceMB,
                request.PreviewOnly);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during attachment cleanup");

            result.EndTime = DateTime.UtcNow;
            result.Errors.Add(new CleanupError
            {
                AttachmentId = 0,
                AttachmentName = "System",
                Reason = $"Fatal error: {ex.Message}",
                ErrorType = CleanupErrorType.Other,
            });

            throw;
        }
    }
}
