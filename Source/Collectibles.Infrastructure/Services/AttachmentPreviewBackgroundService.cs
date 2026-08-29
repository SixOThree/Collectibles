using Collectibles.Application.Interfaces;
using Collectibles.Domain.Configuration;
using Collectibles.Domain.Interfaces;

using Hangfire;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Collectibles.Infrastructure.Services;

/// <summary>
/// Background service for generating preview thumbnails for attachments that don't have them.
/// This handles large files that were uploaded directly to Azure without preview generation.
/// </summary>
public class AttachmentPreviewBackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AttachmentPreviewBackgroundService> _logger;
    private const int BatchSize = 10;

    /// <summary>Hours to wait before retrying an attachment whose preview generation failed.</summary>
    private const int PreviewRetryBackoffHours = 6;

    public AttachmentPreviewBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AttachmentPreviewBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Processes attachments that don't have preview thumbnails yet.
    /// This method is called by Hangfire as a recurring job.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessMissingPreviewsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IApplicationDbContextFactory>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var fileProcessingService = scope.ServiceProvider.GetRequiredService<IFileProcessingService>();
        var sysLogService = scope.ServiceProvider.GetRequiredService<ISysLogService>();
        var previewSettings = scope.ServiceProvider.GetRequiredService<IOptions<PreviewGenerationSettings>>().Value;

        await using var context = await contextFactory.CreateDbContextAsync(CancellationToken.None);

        // The content-type predicate is applied in SQL, before pagination. Paginating first
        // and filtering in memory meant that once BatchSize old non-previewable rows
        // existed, every run selected the same rows, filtered them to nothing, and newer
        // previewable attachments were never reached.
        var previewableTypes = GetPreviewableContentTypes(previewSettings);
        var previewableTypePrefixes = GetPreviewableContentTypePrefixes(previewSettings);

        if (previewableTypes.Count == 0 && previewableTypePrefixes.Count == 0)
        {
            return;
        }

        // Rows that keep failing are parked by PreviewAttemptedAt so they cannot starve the
        // queue either; they are retried after the backoff window.
        var retryCutoff = DateTime.UtcNow.AddHours(-PreviewRetryBackoffHours);

        var attachmentsNeedingPreviews = await context.Attachments
            .Where(a => a.PreviewPath == null
                && a.FilePath != null
                && a.FileType != null
                && (a.PreviewAttemptedAt == null || a.PreviewAttemptedAt < retryCutoff))
            .Where(a => previewableTypes.Contains(a.FileType!)
                || previewableTypePrefixes.Any(prefix => a.FileType!.StartsWith(prefix)))
            .OrderBy(a => a.Created)
            .Take(BatchSize)
            .ToListAsync(CancellationToken.None);

        if (attachmentsNeedingPreviews.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Processing {Count} attachments for preview generation", attachmentsNeedingPreviews.Count);

        await sysLogService.LogInformationAsync(
            $"Processing {attachmentsNeedingPreviews.Count} attachments for preview generation",
            "Attachment.PreviewGeneration",
            new Dictionary<string, object>
            {
                ["BatchSize"] = attachmentsNeedingPreviews.Count,
            });

        var successCount = 0;
        var failureCount = 0;

        foreach (var attachment in attachmentsNeedingPreviews)
        {
            // Stamp the attempt regardless of outcome so a permanently failing row backs
            // off instead of being re-selected on every run.
            attachment.PreviewAttemptedAt = DateTime.UtcNow;

            try
            {
                var generated = await GenerateAndSavePreviewAsync(
                    attachment,
                    fileStorage,
                    fileProcessingService,
                    context);

                if (generated)
                {
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                failureCount++;
                _logger.LogError(ex, "Failed to generate preview for attachment {AttachmentId}", attachment.Id);

                await sysLogService.LogErrorAsync(
                    $"Failed to generate preview for attachment {attachment.Id}",
                    ex,
                    "Attachment.PreviewGeneration",
                    new Dictionary<string, object>
                    {
                        ["AttachmentId"] = attachment.Id,
                        ["AttachmentName"] = attachment.Name,
                        ["FilePath"] = attachment.FilePath ?? "null",
                        ["FileType"] = attachment.FileType ?? "null",
                    });
            }
        }

        await context.SaveChangesAsync(CancellationToken.None);

        if (successCount > 0 || failureCount > 0)
        {
            _logger.LogInformation(
                "Attachment preview generation batch completed: {Success} succeeded, {Failed} failed",
                successCount, failureCount);

            await sysLogService.LogInformationAsync(
                $"Attachment preview generation batch completed: {successCount} succeeded, {failureCount} failed",
                "Attachment.PreviewGeneration",
                new Dictionary<string, object>
                {
                    ["SuccessCount"] = successCount,
                    ["FailureCount"] = failureCount,
                });
        }
    }

    private async Task<bool> GenerateAndSavePreviewAsync(
        Domain.Entities.Attachment attachment,
        IFileStorage fileStorage,
        IFileProcessingService fileProcessingService,
        IApplicationDbContext dbContext)
    {
        if (string.IsNullOrEmpty(attachment.FilePath) || string.IsNullOrEmpty(attachment.FileType))
        {
            _logger.LogWarning("Attachment {AttachmentId} has no file path or file type, skipping", attachment.Id);
            return false;
        }

        string? tempFilePath = null;

        try
        {
            // For large files, stream to a temp file instead of loading into memory
            var fileStream = await fileStorage.GetFileStreamAsync(attachment.FilePath, CancellationToken.None);
            if (fileStream == null)
            {
                _logger.LogWarning(
                    "File not found for attachment {AttachmentId}: {FilePath}",
                    attachment.Id, attachment.FilePath);
                return false;
            }

            byte[] fileContent;

            // Check if we should use streaming (for files > 50MB)
            if (attachment.FileSize > 50 * 1024 * 1024)
            {
                // Stream to temp file for large files
                var extension = Path.GetExtension(attachment.OriginalFilename ?? attachment.FilePath) ?? ".tmp";
                tempFilePath = Path.Combine(Path.GetTempPath(), $"preview_{Guid.NewGuid()}{extension}");

                await using (var tempFileStream = File.Create(tempFilePath))
                {
                    await fileStream.CopyToAsync(tempFileStream);
                }

                fileStream.Dispose();

                // Read back for preview generation (the preview service needs byte[])
                // For very large videos, this is still memory-intensive but necessary
                // TODO: Consider modifying IFileProcessingService to accept file paths for videos
                fileContent = await File.ReadAllBytesAsync(tempFilePath);
            }
            else
            {
                // For smaller files, read directly into memory
                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                fileStream.Dispose();
                fileContent = memoryStream.ToArray();
            }

            // Generate preview
            var previewBytes = await fileProcessingService.GeneratePreviewAsync(
                fileContent,
                attachment.FileType,
                CancellationToken.None);

            if (previewBytes == null || previewBytes.Length == 0)
            {
                _logger.LogDebug(
                    "No preview generated for attachment {AttachmentId} (type: {FileType})",
                    attachment.Id, attachment.FileType);
                return false;
            }

            // Generate preview filename using same pattern as direct uploads
            var guidPart = Path.GetFileNameWithoutExtension(attachment.FilePath.Split('/').Last());
            var previewFileName = $"{guidPart}_preview.jpg";

            // Preserve the folder structure for the preview
            var folderPath = attachment.FilePath.Contains('/')
                ? string.Join("/", attachment.FilePath.Split('/').SkipLast(1))
                : null;

            var fullPreviewFileName = folderPath != null
                ? $"{folderPath}/{previewFileName}"
                : previewFileName;

            // Save preview to storage
            var previewPath = await fileStorage.SaveFileAsync(
                previewBytes,
                fullPreviewFileName,
                "image/jpeg",
                null,
                CancellationToken.None);

            // Update attachment with preview path
            attachment.PreviewPath = previewPath;
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _logger.LogDebug(
                "Generated preview for attachment {AttachmentId}: {PreviewPath}",
                attachment.Id, previewPath);

            return true;
        }
        finally
        {
            // Clean up temp file if created
            if (tempFilePath != null && File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temp file: {TempFilePath}", tempFilePath);
                }
            }
        }
    }

    /// <summary>
    /// Exact content types that can be previewed under the current settings.
    /// </summary>
    private static List<string> GetPreviewableContentTypes(PreviewGenerationSettings settings)
    {
        var types = new List<string>();

        if (settings.Pdf)
        {
            types.Add("application/pdf");
        }

        if (settings.Word)
        {
            types.Add("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            types.Add("application/msword");
        }

        if (settings.PowerPoint)
        {
            types.Add("application/vnd.openxmlformats-officedocument.presentationml.presentation");
            types.Add("application/vnd.ms-powerpoint");
        }

        return types;
    }

    /// <summary>
    /// Content-type prefixes that can be previewed under the current settings.
    /// </summary>
    private static List<string> GetPreviewableContentTypePrefixes(PreviewGenerationSettings settings)
    {
        var prefixes = new List<string>();

        if (settings.Images)
        {
            prefixes.Add("image/");
        }

        if (settings.Video)
        {
            prefixes.Add("video/");
        }

        return prefixes;
    }

    private static bool IsPreviewableType(string contentType, PreviewGenerationSettings settings)
    {
        // Check if the category is enabled in settings
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return settings.Images;
        }

        if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            return settings.Video;
        }

        if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return settings.Pdf;
        }

        if (contentType.Equals("application/vnd.openxmlformats-officedocument.wordprocessingml.document", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/msword", StringComparison.OrdinalIgnoreCase))
        {
            return settings.Word;
        }

        if (contentType.Equals("application/vnd.openxmlformats-officedocument.presentationml.presentation", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/vnd.ms-powerpoint", StringComparison.OrdinalIgnoreCase))
        {
            return settings.PowerPoint;
        }

        return false;
    }
}
