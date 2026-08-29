using System.Net.Http;

using Collectibles.SyncTool.Services;
using Collectibles.SyncTool.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace Collectibles.SyncTool;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = new MainWindow
        {
            DataContext = _serviceProvider.GetRequiredService<MainViewModel>(),
        };
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<SettingsService>();
        services.AddSingleton<FileHashService>();
        services.AddSingleton<SyncComparisonService>();

        services.AddSingleton<ApiKeyProvider>();

        // HTTP clients
        services.AddSingleton(sp =>
        {
            var apiKeyProvider = sp.GetRequiredService<ApiKeyProvider>();

            var apiClient = new HttpClient(new ApiKeyMessageHandler(apiKeyProvider))
            {
                // These clients move whole media files: a single 8 MB block needs a
                // sustained 80 KB/s, and a 200 MB PUT needs 2 MB/s, to finish inside the
                // framework's 100-second default. Transfers are bounded by per-operation
                // cancellation instead.
                Timeout = Timeout.InfiniteTimeSpan,
            };
            apiClient.DefaultRequestHeaders.UserAgent.ParseAdd("CollectiblesSyncTool/1.0");

            var azureClient = new HttpClient // Azure doesn't need the API key header
            {
                Timeout = Timeout.InfiniteTimeSpan,
            };

            return new CollectiblesApiClient(apiClient, azureClient, apiKeyProvider);
        });

        // ViewModels
        services.AddSingleton<MainViewModel>();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
