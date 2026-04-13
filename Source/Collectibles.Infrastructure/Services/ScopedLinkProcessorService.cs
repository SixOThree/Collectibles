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

    public ScopedLinkProcessorService(IApplicationDbContext context, ILogger<ScopedLinkProcessorService> logger, IFileStorage fileStorage, IConfiguration configuration, IOptions<ExternalLinksOptions> externalLinksOptions)
    {
        _context = context;
        _logger = logger;
        _fileStorage = fileStorage;
        _configuration = configuration;
        _externalLinksOptions = externalLinksOptions.Value;
    }

    public async Task ProcessPendingLinks(CancellationToken cancellationToken)
    {
        if (!_externalLinksOptions.CachingEnabled)
        {
            return;
        }

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

        foreach (var cache in pendingCaches)
        {
            cache.Status = LinkCacheStatus.Processing;
            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                var page = await browser.NewPageAsync();
                await page.GotoAsync(cache.LinkInfo.Url);

                // Update title
                cache.LinkInfo.Title = await page.TitleAsync();

                // Determine storage paths based on showcase associations
                var showcaseIds = cache.LinkInfo.CollectibleItem?.Showcases?.Select(s => s.Id).ToList() ?? new List<long>();

                // Create MHTML
                var htmlContent = await page.ContentAsync();
                var htmlBytes = System.Text.Encoding.UTF8.GetBytes(htmlContent);

                // Create Screenshot
                var screenshotBytes = await page.ScreenshotAsync(new PageScreenshotOptions { FullPage = true });

                if (showcaseIds.Count != 0)
                {
                    // Store in the first showcase's folder (primary showcase)
                    var primaryShowcaseId = showcaseIds.First();
                    var htmlFileName = $"LinkCaches/{Guid.NewGuid()}.html";
                    var screenshotFileName = $"LinkCaches/{Guid.NewGuid()}.png";

                    cache.CachedContentPath = await _fileStorage.SaveFileAsync(new MemoryStream(htmlBytes), htmlFileName, "text/html", primaryShowcaseId, cancellationToken);

                    cache.ScreenshotPath = await _fileStorage.SaveFileAsync(new MemoryStream(screenshotBytes), screenshotFileName, "image/png", primaryShowcaseId, cancellationToken);
                }
                else
                {
                    // No showcase association, store in general LinkCaches folder
                    var htmlFileName = $"LinkCaches/{Guid.NewGuid()}.html";
                    var screenshotFileName = $"LinkCaches/{Guid.NewGuid()}.png";

                    cache.CachedContentPath = await _fileStorage.SaveFileAsync(new MemoryStream(htmlBytes), htmlFileName, "text/html", cancellationToken: cancellationToken);

                    cache.ScreenshotPath = await _fileStorage.SaveFileAsync(new MemoryStream(screenshotBytes), screenshotFileName, "image/png", cancellationToken: cancellationToken);
                }

                cache.Status = LinkCacheStatus.Completed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process link {LinkInfoId}", cache.LinkInfoId);
                cache.Status = LinkCacheStatus.Failed;
                cache.FailureReason = ex.Message;
            }
            finally
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
