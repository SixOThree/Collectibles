using Collectibles.Application.Interfaces;
using Collectibles.Domain.Interfaces;

using Hangfire;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Collectibles.Infrastructure.Services;

/// <summary>
/// Background service for computing content hashes for existing attachments.
/// </summary>
public class AttachmentIndexingBackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AttachmentIndexingBackgroundService> _logger;
    private const int BatchSize = 50;

    public AttachmentIndexingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AttachmentIndexingBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Processes attachments that don't have content hashes yet.
    /// This method is called by Hangfire as a recurring job.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessUnhashedAttachmentsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IApplicationDbContextFactory>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var hashService = scope.ServiceProvider.GetRequiredService<IAttachmentHashService>();
        var sysLogService = scope.ServiceProvider.GetRequiredService<ISysLogService>();

        await using var context = await contextFactory.CreateDbContextAsync(CancellationToken.None);

        // Get attachments without hashes (batch processing)
        var unhashedAttachments = await context.Attachments
            .Where(a => a.ContentHash == null && a.FilePath != null)
            .OrderBy(a => a.Created)
            .Take(BatchSize)
            .ToListAsync(CancellationToken.None);

        if (unhashedAttachments.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Processing {Count} attachments for hash computation", unhashedAttachments.Count);

        await sysLogService.LogInformationAsync(
            $"Processing {unhashedAttachments.Count} attachments for hash computation",
            "Attachment.Indexing",
            new Dictionary<string, object>
            {
                ["BatchSize"] = unhashedAttachments.Count,
            });

        var successCount = 0;
        var failureCount = 0;

        foreach (var attachment in unhashedAttachments)
        {
            try
            {
                await ComputeAndSaveHashAsync(attachment, fileStorage, hashService, context);
                successCount++;
            }
            catch (Exception ex)
            {
                failureCount++;
                _logger.LogError(ex, "Failed to compute hash for attachment {AttachmentId}", attachment.Id);

                await sysLogService.LogErrorAsync(
                    $"Failed to compute hash for attachment {attachment.Id}",
                    ex,
                    "Attachment.Indexing",
                    new Dictionary<string, object>
                    {
                        ["AttachmentId"] = attachment.Id,
                        ["AttachmentName"] = attachment.Name,
                        ["FilePath"] = attachment.FilePath ?? "null",
                    });
            }
        }

        if (successCount > 0 || failureCount > 0)
        {
            _logger.LogInformation(
                "Attachment indexing batch completed: {Success} succeeded, {Failed} failed",
                successCount, failureCount);

            await sysLogService.LogInformationAsync(
                $"Attachment indexing batch completed: {successCount} succeeded, {failureCount} failed",
                "Attachment.Indexing",
                new Dictionary<string, object>
                {
                    ["SuccessCount"] = successCount,
                    ["FailureCount"] = failureCount,
                });
        }
    }

    private async Task ComputeAndSaveHashAsync(
        Domain.Entities.Attachment attachment,
        IFileStorage fileStorage,
        IAttachmentHashService hashService,
        IApplicationDbContext dbContext)
    {
        if (string.IsNullOrEmpty(attachment.FilePath))
        {
            _logger.LogWarning("Attachment {AttachmentId} has no file path, skipping", attachment.Id);
            return;
        }

        // Get file content from storage
        var content = await fileStorage.GetFileAsync(attachment.FilePath, CancellationToken.None);
        if (content == null)
        {
            _logger.LogWarning(
                "File not found for attachment {AttachmentId}: {FilePath}",
                attachment.Id, attachment.FilePath);
            return;
        }

        // Compute hash
        var hash = hashService.ComputeHash(content);

        // Update attachment
        attachment.ContentHash = hash;
        attachment.HashComputedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(CancellationToken.None);

        _logger.LogDebug(
            "Computed hash for attachment {AttachmentId}: {Hash}",
            attachment.Id, hash);
    }
}
