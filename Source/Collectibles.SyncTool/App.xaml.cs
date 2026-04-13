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

        // HTTP clients
        services.AddSingleton(sp =>
        {
            var settingsService = sp.GetRequiredService<SettingsService>();
            var settings = settingsService.Load();

            var apiClient = new HttpClient();
            apiClient.DefaultRequestHeaders.UserAgent.ParseAdd("CollectiblesSyncTool/1.0");
            var azureClient = new HttpClient(); // Azure doesn't need custom handler
            return new CollectiblesApiClient(apiClient, azureClient);
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
