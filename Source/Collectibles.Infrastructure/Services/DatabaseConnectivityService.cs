using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;

namespace Collectibles.Infrastructure.Services;

/// <summary>
/// Service that ensures database connectivity before allowing application startup to proceed.
/// This helps handle scenarios where SQL Server is still starting up after a system reboot.
/// </summary>
public class DatabaseConnectivityService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseConnectivityService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public DatabaseConnectivityService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseConnectivityService> logger,
        IConfiguration configuration,
        IHostApplicationLifetime applicationLifetime)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        _applicationLifetime = applicationLifetime;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Database connectivity service starting - ensuring SQL Server is available...");

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogError("Connection string 'DefaultConnection' not found");
            _applicationLifetime.StopApplication();
            return;
        }

        // Configure retry policy with exponential backoff
        var retryPolicy = Policy
            .Handle<SqlException>()
            .Or<InvalidOperationException>()
            .WaitAndRetryAsync(
                retryCount: 30, // Try for up to ~8 minutes total
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Min(Math.Pow(2, retryAttempt), 30)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Database connection attempt {RetryCount} failed. Waiting {TimeSpan} seconds before next attempt. Error: {Message}",
                        retryCount,
                        timeSpan.TotalSeconds,
                        exception.Message);
                });

        try
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);

                // Execute a simple query to ensure the database is responsive
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync(cancellationToken);

                _logger.LogInformation("Successfully connected to SQL Server database");
            });
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to establish database connection after all retry attempts. Application cannot start.");

            // Stop the application if we can't connect to the database
            _applicationLifetime.StopApplication();
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
