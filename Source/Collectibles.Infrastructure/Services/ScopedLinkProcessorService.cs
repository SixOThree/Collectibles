using Collectibles.Application.Interfaces;
using Collectibles.Domain.Configuration;
using Collectibles.Domain.Enums;
using Collectibles.Domain.Interfaces;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace Collectibles.Infrastructure.Services;

public class ScopedLinkProcessorService : ILinkProcessorService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ScopedLinkProcessorService> _logger;
    private readonly IFileStorage _fileStorage;
    private readonly IConfiguration _configuration;
    private readonly ExternalLinksOptions _externalLinksOptions;
    private readonly IUrlEgressGuard _egressGuard;

    public ScopedLinkProcessorService(
        IApplicationDbContext context,
        ILogger<ScopedLinkProcessorService> logger,
        IFileStorage fileStorage,
        IConfiguration configuration,
        IOptions<ExternalLinksOptions> externalLinksOptions,
        IUrlEgressGuard egressGuard)
    {
        _context = context;
        _logger = logger;
        _fileStorage = fileStorage;
        _configuration = configuration;
        _externalLinksOptions = externalLinksOptions.Value;
        _egressGuard = egressGuard;
    }

    public async Task ProcessPendingLinks(CancellationToken cancellationToken)
    {
        if (!_externalLinksOptions.CachingEnabled)
        {
            return;
        }

        await ResetStuckProcessingRowsAsync(cancellationToken);

        var pendingCaches = await _context.LinkCaches
            .Include(lc => lc.LinkInfo)
                .ThenInclude(li => li.CollectibleItem)
                    .ThenInclude(ci => ci.Showcases)
            .Where(lc => lc.Status == LinkCacheStatus.Pending)
            .ToListAsync(cancellationToken);

        if (pendingCaches.Count == 0)
        {
            return;
        }

        var browsersPath = _configuration["Playwright:BrowsersPath"];
        if (!string.IsNullOrEmpty(browsersPath))
        {
            Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", browsersPath);
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();

        var captureTimeout = (float)TimeSpan.FromSeconds(Math.Max(1, _externalLinksOptions.CaptureTimeoutSeconds)).TotalMilliseconds;

        foreach (var cache in pendingCaches)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Validate the target before it is ever fetched: the browser runs from a
            // privileged network position, so an unvalidated URL is an SSRF primitive.
            var egress = await _egressGuard.ValidateAsync(cache.LinkInfo.Url, cancellationToken);
            if (!egress.IsAllowed)
            {
                _logger.LogWarning(
                    "Refusing to capture link {LinkInfoId}: {Reason}",
                    cache.LinkInfoId,
                    egress.Reason);

                cache.Status = LinkCacheStatus.Failed;
                cache.FailureReason = egress.Reason;
                await _context.SaveChangesAsync(cancellationToken);
                continue;
            }

            cache.Status = LinkCacheStatus.Processing;
            cache.ProcessingStartedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            IPage? page = null;
            try
            {
                page = await browser.NewPageAsync();
                page.SetDefaultTimeout(captureTimeout);
                page.SetDefaultNavigationTimeout(captureTimeout);

                // Re-validate every top-level navigation so a redirect cannot land on an
                // internal target after the initial check passed.
                await page.RouteAsync("**/*", async route =>
                {
                    if (!string.Equals(route.Request.ResourceType, "document", StringComparison.Ordinal))
                    {
                        await route.ContinueAsync();
                        return;
                    }

                    var hop = await _egressGuard.ValidateAsync(route.Request.Url, CancellationToken.None);
                    if (hop.IsAllowed)
                    {
                        await route.ContinueAsync();
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Blocked navigation to {Url} while capturing link {LinkInfoId}: {Reason}",
                            route.Request.Url,
                            cache.LinkInfoId,
                            hop.Reason);
                        await route.AbortAsync("blockedbyclient");
                    }
                });

                await page.GotoAsync(egress.Uri!.ToString(), new PageGotoOptions { Timeout = captureTimeout });

                // Update title
                cache.LinkInfo.Title = await page.TitleAsync();

                // Determine storage paths based on showcase associations
                var showcaseIds = cache.LinkInfo.CollectibleItem?.Showcases?.Select(s => s.Id).ToList() ?? [];

                // Create MHTML
                var htmlContent = await page.ContentAsync();
                var htmlBytes = System.Text.Encoding.UTF8.GetBytes(htmlContent);

                // Create Screenshot
                var screenshotBytes = await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    FullPage = true,
                    Timeout = captureTimeout,
                });

                var htmlFileName = $"LinkCaches/{Guid.NewGuid()}.html";
                var screenshotFileName = $"LinkCaches/{Guid.NewGuid()}.png";

                if (showcaseIds.Count != 0)
                {
                    // Store in the first showcase's folder (primary showcase)
                    var primaryShowcaseId = showcaseIds[0];

                    cache.CachedContentPath = await _fileStorage.SaveFileAsync(new MemoryStream(htmlBytes), htmlFileName, "text/html", primaryShowcaseId, cancellationToken);

                    cache.ScreenshotPath = await _fileStorage.SaveFileAsync(new MemoryStream(screenshotBytes), screenshotFileName, "image/png", primaryShowcaseId, cancellationToken);
                }
                else
                {
                    // No showcase association, store in general LinkCaches folder
                    cache.CachedContentPath = await _fileStorage.SaveFileAsync(new MemoryStream(htmlBytes), htmlFileName, "text/html", cancellationToken: cancellationToken);

                    cache.ScreenshotPath = await _fileStorage.SaveFileAsync(new MemoryStream(screenshotBytes), screenshotFileName, "image/png", cancellationToken: cancellationToken);
                }

                cache.Status = LinkCacheStatus.Completed;
                cache.ProcessingStartedAt = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process link {LinkInfoId}", cache.LinkInfoId);
                cache.Status = LinkCacheStatus.Failed;
                cache.FailureReason = ex.Message;
                cache.ProcessingStartedAt = null;
            }
            finally
            {
                if (page is not null)
                {
                    // Pages are not disposed by the browser until it closes; leaving them
                    // open leaks a renderer per captured link.
                    await page.CloseAsync();
                }

                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    /// <summary>
    /// Returns rows abandoned mid-capture (process restart, crash) to <c>Pending</c>.
    /// Without this they stay in <c>Processing</c> forever because only <c>Pending</c>
    /// rows are ever queried.
    /// </summary>
    private async Task ResetStuckProcessingRowsAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-Math.Max(1, _externalLinksOptions.StuckProcessingResetMinutes));

        var stuck = await _context.LinkCaches
            .Where(lc => lc.Status == LinkCacheStatus.Processing
                && (lc.ProcessingStartedAt == null || lc.ProcessingStartedAt < cutoff))
            .ToListAsync(cancellationToken);

        if (stuck.Count == 0)
        {
            return;
        }

        foreach (var cache in stuck)
        {
            cache.Status = LinkCacheStatus.Pending;
            cache.ProcessingStartedAt = null;
        }

        _logger.LogWarning("Reset {Count} link cache row(s) stuck in Processing back to Pending", stuck.Count);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
