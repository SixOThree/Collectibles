using Collectibles.Application.Interfaces;
using Collectibles.Domain.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Collectibles.Infrastructure.Services;

public class LinkProcessorService : IHostedService, IDisposable
{
    private readonly ILogger<LinkProcessorService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private Timer? _timer;

    public LinkProcessorService(ILogger<LinkProcessorService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Link Processor Service is starting.");
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(ApplicationConstants.BackgroundServices.LinkProcessorIntervalMinutes));
        return Task.CompletedTask;
    }

    private void DoWork(object? state)
    {
        // Use Task.Run to properly handle async operation without blocking the timer callback
        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("Link Processor Service is working.");
                using var scope = _serviceProvider.CreateScope();
                var linkProcessor = scope.ServiceProvider.GetRequiredService<ILinkProcessorService>();
                await linkProcessor.ProcessPendingLinks(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing pending links.");
            }
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Link Processor Service is stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
