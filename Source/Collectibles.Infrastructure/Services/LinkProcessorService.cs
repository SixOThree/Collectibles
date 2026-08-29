using Collectibles.Application.Interfaces;
using Collectibles.Domain.Constants;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Collectibles.Infrastructure.Services;

/// <summary>
/// Drives <see cref="ILinkProcessorService"/> on a fixed interval. Implemented as a
/// <see cref="BackgroundService"/> loop rather than a timer so runs can never overlap,
/// in-flight work is awaited on shutdown, and the stopping token reaches the capture.
/// </summary>
public class LinkProcessorService : BackgroundService
{
    private static readonly TimeSpan Interval =
        TimeSpan.FromMinutes(ApplicationConstants.BackgroundServices.LinkProcessorIntervalMinutes);

    private readonly ILogger<LinkProcessorService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public LinkProcessorService(ILogger<LinkProcessorService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Link Processor Service is starting.");

        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                _logger.LogInformation("Link Processor Service is working.");
                using var scope = _serviceProvider.CreateScope();
                var linkProcessor = scope.ServiceProvider.GetRequiredService<ILinkProcessorService>();
                await linkProcessor.ProcessPendingLinks(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing pending links.");
            }
        }
        while (await SafeWaitForNextTickAsync(timer, stoppingToken).ConfigureAwait(false));

        _logger.LogInformation("Link Processor Service is stopping.");
    }

    private static async Task<bool> SafeWaitForNextTickAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
