using Collectibles.Application.Interfaces;
using Collectibles.Domain.Constants;
using Collectibles.Domain.Interfaces;

using Hangfire;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Collectibles.Infrastructure.Services;

/// <summary>
/// Reclaims soft-deleted attachments once they are past the retention window.
/// </summary>
/// <remarks>
/// Attachment deletion is a soft delete everywhere, so without this job the rows — and
/// the storage files they reference — were never reclaimed. The row delete is committed
/// first and the blobs are removed afterwards, so a storage failure leaves a recoverable
/// orphan rather than destroying content whose delete never committed.
/// </remarks>
public class AttachmentPurgeBackgroundService
{
    private const int BatchSize = 100;

    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AttachmentPurgeBackgroundService> _logger;

    public AttachmentPurgeBackgroundService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<AttachmentPurgeBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Permanently removes attachments soft-deleted longer ago than the retention window.
    /// Called by Hangfire as a recurring job.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [AutomaticRetry(Attempts = 0)]
    public async Task PurgeDeletedAttachmentsAsync()
    {
        var retentionDays = _configuration.GetValue(
            "Attachments:SoftDeleteRetentionDays",
            ApplicationConstants.TimeOperations.DeletedAttachmentRetentionDays);

        if (retentionDays <= 0)
        {
            return;
        }

        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

        using var scope = _serviceProvider.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IApplicationDbContextFactory>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        await using var context = await contextFactory.CreateDbContextAsync(CancellationToken.None);

        var expired = await context.Attachments
            .IgnoreQueryFilters()
            .Where(a => a.Deleted != null && a.Deleted < cutoff)
            .OrderBy(a => a.Deleted)
            .Take(BatchSize)
            .ToListAsync(CancellationToken.None);

        if (expired.Count == 0)
        {
            return;
        }

        var pathsToDelete = new List<string>();

        foreach (var attachment in expired)
        {
            if (!string.IsNullOrEmpty(attachment.FilePath))
            {
                pathsToDelete.Add(attachment.FilePath);
            }

            if (!string.IsNullOrEmpty(attachment.PreviewPath))
            {
                pathsToDelete.Add(attachment.PreviewPath);
            }

            context.Attachments.Remove(attachment);
        }

        await context.SaveChangesAsync(CancellationToken.None);

        var reclaimed = 0;
        foreach (var path in pathsToDelete)
        {
            try
            {
                await fileStorage.DeleteFileAsync(path, CancellationToken.None);
                reclaimed++;
            }
            catch (Exception ex)
            {
                // The row is already gone; orphan cleanup will pick the blob up later.
                _logger.LogWarning(ex, "Failed to reclaim storage file {Path} during attachment purge", path);
            }
        }

        _logger.LogInformation(
            "Purged {RowCount} attachment(s) soft-deleted before {Cutoff}; reclaimed {FileCount} storage file(s)",
            expired.Count,
            cutoff,
            reclaimed);
    }
}
