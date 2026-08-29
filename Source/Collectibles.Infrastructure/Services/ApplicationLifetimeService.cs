using System.Diagnostics;
using System.Reflection;

using Collectibles.Application.Interfaces;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Collectibles.Infrastructure.Services;

public class ApplicationLifetimeService : IHostedService
{
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger<ApplicationLifetimeService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly Stopwatch _startupTimer;

    public ApplicationLifetimeService(
        IHostApplicationLifetime appLifetime,
        ILogger<ApplicationLifetimeService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _appLifetime = appLifetime;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _environment = environment;
        _startupTimer = new Stopwatch();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _startupTimer.Start();

        _appLifetime.ApplicationStarted.Register(async () => await OnStarted());
        _appLifetime.ApplicationStopping.Register(async () => await OnStopping());
        _appLifetime.ApplicationStopped.Register(async () => await OnStopped());

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task OnStarted()
    {
        _startupTimer.Stop();

        var assembly = Assembly.GetEntryAssembly();
        var version = assembly?.GetName().Version?.ToString() ?? "Unknown";
        var assemblyName = assembly?.GetName().Name ?? "Unknown";

        var startupInfo = new Dictionary<string, object>
        {
            ["ApplicationName"] = assemblyName,
            ["Version"] = version,
            ["Environment"] = _environment.EnvironmentName,
            ["MachineName"] = Environment.MachineName,
            ["OSVersion"] = Environment.OSVersion.ToString(),
            ["ProcessId"] = Environment.ProcessId,
            ["StartupDuration"] = $"{_startupTimer.ElapsedMilliseconds}ms",
            ["WorkingDirectory"] = Directory.GetCurrentDirectory(),
            ["DotNetVersion"] = Environment.Version.ToString(),
            ["ProcessorCount"] = Environment.ProcessorCount,
            ["Is64BitProcess"] = Environment.Is64BitProcess,
            ["UserName"] = Environment.UserName,
        };

        // Log database connection string (masked)
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            startupInfo["DatabaseConnection"] = MaskConnectionString(connectionString);
        }

        // Log configured services
        var emailProvider = _configuration["EmailSettings:Provider"];
        if (!string.IsNullOrEmpty(emailProvider))
        {
            startupInfo["EmailProvider"] = emailProvider;
        }

        var hangfireDashboard = _configuration["Hangfire:DashboardPath"];
        if (!string.IsNullOrEmpty(hangfireDashboard))
        {
            startupInfo["HangfireDashboard"] = hangfireDashboard;
        }

        _logger.LogInformation("Application started successfully");

        using var scope = _serviceProvider.CreateScope();
        var sysLogService = scope.ServiceProvider.GetRequiredService<ISysLogService>();

        await sysLogService.LogInformationAsync(
            $"Application '{assemblyName}' v{version} started successfully in {_startupTimer.ElapsedMilliseconds}ms",
            "Application.Startup",
            startupInfo);
    }

    private async Task OnStopping()
    {
        _logger.LogInformation("Application is shutting down...");

        using var scope = _serviceProvider.CreateScope();
        var sysLogService = scope.ServiceProvider.GetRequiredService<ISysLogService>();

        await sysLogService.LogWarningAsync(
            "Application shutdown initiated",
            "Application.Shutdown",
            new Dictionary<string, object>
            {
                ["ShutdownReason"] = "User initiated",
                ["Uptime"] = GetUptime(),
            });
    }

    private async Task OnStopped()
    {
        _logger.LogInformation("Application stopped");

        using var scope = _serviceProvider.CreateScope();
        var sysLogService = scope.ServiceProvider.GetRequiredService<ISysLogService>();

        await sysLogService.LogInformationAsync(
            "Application shutdown completed",
            "Application.Shutdown",
            new Dictionary<string, object>
            {
                ["Uptime"] = GetUptime(),
            });
    }

    private static string MaskConnectionString(string connectionString)
    {
        // Mask sensitive parts of connection string
        var parts = connectionString.Split(';');
        var maskedParts = parts.Select(part =>
        {
            if (part.Contains("Password=", StringComparison.OrdinalIgnoreCase) ||
                part.Contains("Pwd=", StringComparison.OrdinalIgnoreCase))
            {
                var index = part.IndexOf('=');
                return string.Concat(part.AsSpan(0, index + 1), "****");
            }

            return part;
        });

        return string.Join(';', maskedParts);
    }

    private static string GetUptime()
    {
        var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
        return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
    }
}
