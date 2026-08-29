using Collectibles.Application.Services;
using Collectibles.Domain.Constants;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Collectibles.Infrastructure.Services;

public class CollectibleItemPreviewBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CollectibleItemPreviewBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(ApplicationConstants.BackgroundServices.CollectiblePreviewCheckMinutes);
    private readonly int _batchSize = 20;

    public CollectibleItemPreviewBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<CollectibleItemPreviewBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Collectible Item Preview Background Service starting");

        // Wait a bit before starting to allow the application to fully initialize
        await Task.Delay(TimeSpan.FromSeconds(ApplicationConstants.BackgroundServices.CollectiblePreviewInitialDelaySeconds), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessMissingPreviews(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in preview generation background service");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Collectible Item Preview Background Service stopping");
    }

    private async Task ProcessMissingPreviews(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var previewService = scope.ServiceProvider.GetRequiredService<ICollectibleItemPreviewService>();

        try
        {
            var generatedCount = await previewService.GenerateMissingCollagePreviewsAsync(_batchSize, cancellationToken);

            if (generatedCount > 0)
            {
                _logger.LogInformation("Generated {Count} collage previews in background processing", generatedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process missing previews batch");
        }
    }
}
