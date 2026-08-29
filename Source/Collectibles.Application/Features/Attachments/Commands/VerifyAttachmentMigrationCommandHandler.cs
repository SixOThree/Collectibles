using System.Diagnostics;

using Collectibles.Application.Features.Attachments.Dtos;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Features.Attachments.Commands;

public class VerifyAttachmentMigrationCommandHandler : IRequestHandler<VerifyAttachmentMigrationCommand, VerificationResult>
{
    private readonly IApplicationDbContextFactory _dbContextFactory;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<VerifyAttachmentMigrationCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public VerifyAttachmentMigrationCommandHandler(
        IApplicationDbContextFactory dbContextFactory,
        IFileStorage fileStorage,
        ILogger<VerifyAttachmentMigrationCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _dbContextFactory = dbContextFactory;
        _fileStorage = fileStorage;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<VerificationResult> Handle(VerifyAttachmentMigrationCommand request, CancellationToken cancellationToken)
    {
        // Its Cleanup/Rollback siblings already require administrator; this bulk storage
        // operation is at least as privileged.
        if (!_currentUserService.IsAdministrator)
        {
            throw new UnauthorizedAccessException("Only administrators can verify attachment migrations.");
        }

        var result = new VerificationResult
        {
            StartTime = DateTime.UtcNow,
        };

        try
        {
            _logger.LogInformation("Starting attachment migration verification with batch size: {BatchSize}", request.BatchSize);

            // Track processing speed for time estimation
            var stopwatch = Stopwatch.StartNew();
            var processedCount = 0;
            var lastProcessedId = 0L;

            // Get total count of migrated attachments
            await using (var countContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
            {
                result.TotalMigratedAttachments = await countContext.Attachments
                    .Where(a => a.IsMigrated)
                    .CountAsync(cancellationToken);
            }

            _logger.LogInformation("Found {Count} migrated attachments to verify", result.TotalMigratedAttachments);

            if (result.TotalMigratedAttachments == 0)
            {
                result.EndTime = DateTime.UtcNow;
                result.Duration = result.EndTime - result.StartTime;
                return result;
            }

            // Process in batches to prevent memory issues
            while (processedCount < result.TotalMigratedAttachments)
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

                // Get next batch of migrated attachments
                var attachmentsBatch = await context.Attachments
                    .Where(a => a.IsMigrated && a.Id > lastProcessedId)
                    .OrderBy(a => a.Id)
                    .Take(request.BatchSize)
                    .Select(a => new
                    {
                        a.Id,
                        a.Name,
                        a.FilePath,
                        a.PreviewPath,
                        a.FileSize,
                        a.FileType,
                    })
                    .ToListAsync(cancellationToken);

                if (attachmentsBatch.Count == 0)
                {
                    break;
                }

                // Process each attachment in the batch
                var verificationTasks = attachmentsBatch.Select(async attachment =>
                {
                    var errors = new List<VerificationError>();

                    try
                    {
                        // Verify main file exists
                        if (request.VerifyFileExists && !string.IsNullOrEmpty(attachment.FilePath))
                        {
                            var fileSize = await _fileStorage.GetFileSizeAsync(attachment.FilePath, cancellationToken);

                            if (fileSize == null)
                            {
                                errors.Add(new VerificationError
                                {
                                    AttachmentId = attachment.Id,
                                    AttachmentName = attachment.Name,
                                    FilePath = attachment.FilePath,
                                    ErrorType = VerificationErrorType.FileNotFound,
                                    ErrorDetails = "Main file not found in storage",
                                });
                            }
                            else if (request.VerifyFileSize && fileSize != attachment.FileSize)
                            {
                                errors.Add(new VerificationError
                                {
                                    AttachmentId = attachment.Id,
                                    AttachmentName = attachment.Name,
                                    FilePath = attachment.FilePath,
                                    ErrorType = VerificationErrorType.SizeMismatch,
                                    ErrorDetails = $"File size mismatch - Expected: {attachment.FileSize} bytes, Actual: {fileSize} bytes",
                                    ExpectedSize = attachment.FileSize,
                                    ActualSize = fileSize,
                                });
                            }
                        }

                        // Verify preview file if it exists
                        if (request.VerifyFileExists && !string.IsNullOrEmpty(attachment.PreviewPath))
                        {
                            var previewSize = await _fileStorage.GetFileSizeAsync(attachment.PreviewPath, cancellationToken);

                            if (previewSize == null)
                            {
                                errors.Add(new VerificationError
                                {
                                    AttachmentId = attachment.Id,
                                    AttachmentName = attachment.Name,
                                    FilePath = attachment.PreviewPath,
                                    ErrorType = VerificationErrorType.PreviewNotFound,
                                    ErrorDetails = "Preview file not found in storage",
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error verifying attachment {AttachmentId}", attachment.Id);
                        errors.Add(new VerificationError
                        {
                            AttachmentId = attachment.Id,
                            AttachmentName = attachment.Name,
                            FilePath = attachment.FilePath ?? string.Empty,
                            ErrorType = VerificationErrorType.Other,
                            ErrorDetails = $"Verification error: {ex.Message}",
                        });
                    }

                    return errors;
                });

                var batchResults = await Task.WhenAll(verificationTasks);
                var batchErrors = batchResults.SelectMany(e => e).ToList();

                // Update results
                result.VerifiedCount += attachmentsBatch.Count;
                result.FailedCount += batchErrors.Select(e => e.AttachmentId).Distinct().Count();
                result.PassedCount = result.VerifiedCount - result.FailedCount;
                result.VerificationErrors.AddRange(batchErrors);

                processedCount += attachmentsBatch.Count;
                lastProcessedId = attachmentsBatch.Last().Id;

                // Log progress periodically
                if (processedCount % 500 == 0 || processedCount == result.TotalMigratedAttachments)
                {
                    var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                    var rate = processedCount / elapsedSeconds;
                    var remainingCount = result.TotalMigratedAttachments - processedCount;
                    var estimatedRemainingSeconds = remainingCount / rate;

                    _logger.LogInformation(
                        "Verification progress: {Processed}/{Total} ({Percentage:F1}%) - " +
                        "Passed: {Passed}, Failed: {Failed} - " +
                        "Rate: {Rate:F1}/sec - ETA: {ETA}",
                        processedCount,
                        result.TotalMigratedAttachments,
                        (processedCount * 100.0) / result.TotalMigratedAttachments,
                        result.PassedCount,
                        result.FailedCount,
                        rate,
                        TimeSpan.FromSeconds(estimatedRemainingSeconds).ToString(@"mm\:ss"));
                }

                // Allow cancellation between batches
                cancellationToken.ThrowIfCancellationRequested();
            }

            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;

            _logger.LogInformation(
                "Attachment migration verification completed. Total: {Total}, Verified: {Verified}, " +
                "Passed: {Passed}, Failed: {Failed}, Duration: {Duration}",
                result.TotalMigratedAttachments,
                result.VerifiedCount,
                result.PassedCount,
                result.FailedCount,
                result.Duration);

            // Log summary of errors by type
            if (result.VerificationErrors.Count != 0)
            {
                var errorSummary = result.VerificationErrors
                    .GroupBy(e => e.ErrorType)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count);

                foreach (var errorGroup in errorSummary)
                {
                    _logger.LogWarning(
                        "Error type {ErrorType}: {Count} occurrences",
                        errorGroup.Type, errorGroup.Count);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during attachment migration verification");

            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
            result.VerificationErrors.Add(new VerificationError
            {
                AttachmentId = 0,
                AttachmentName = "System",
                FilePath = string.Empty,
                ErrorType = VerificationErrorType.Other,
                ErrorDetails = $"Fatal error: {ex.Message}",
            });

            throw;
        }
    }
}
