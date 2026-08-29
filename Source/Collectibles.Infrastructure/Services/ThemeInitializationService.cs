using Collectibles.Application.Interfaces;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Collectibles.Infrastructure.Services;

public class ThemeInitializationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ThemeInitializationService> _logger;

    public ThemeInitializationService(
        IServiceProvider serviceProvider,
        ILogger<ThemeInitializationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting theme initialization service...");

        using var scope = _serviceProvider.CreateScope();
        var themeService = scope.ServiceProvider.GetRequiredService<IThemeService>();

        // Use the ThemeService to initialize the theme configuration
        await themeService.InitializeThemeAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
