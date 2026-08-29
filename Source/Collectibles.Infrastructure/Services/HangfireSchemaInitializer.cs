using Collectibles.Domain.Constants;

using Hangfire.SqlServer;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Polly;

namespace Collectibles.Infrastructure.Services;

/// <summary>
/// Initializes the Hangfire schema before any Hangfire jobs are configured.
/// </summary>
public class HangfireSchemaInitializer : IHangfireSchemaInitializer
{
    private readonly ILogger<HangfireSchemaInitializer> _logger;
    private readonly IConfiguration _configuration;

    public HangfireSchemaInitializer(
        ILogger<HangfireSchemaInitializer> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Ensuring Hangfire schema exists...");

        var defaultConnectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(defaultConnectionString))
        {
            _logger.LogError("Connection string 'DefaultConnection' not found");
            return;
        }

        // Use Hangfire-specific connection string if provided, otherwise use the default
        var hangfireConnectionString = _configuration["Hangfire:ConnectionString"];
        if (string.IsNullOrWhiteSpace(hangfireConnectionString))
        {
            hangfireConnectionString = defaultConnectionString;
        }

        try
        {
            // Initialize Hangfire schema with retry logic
            var retryPolicy = Policy
                .Handle<SqlException>()
                .Or<InvalidOperationException>()
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            "Hangfire schema initialization attempt {RetryCount} failed. Waiting {TimeSpan} seconds before next attempt.",
                            retryCount,
                            timeSpan.TotalSeconds);
                    });

            await retryPolicy.ExecuteAsync(async () =>
            {
                using var connection = new SqlConnection(hangfireConnectionString);
                await connection.OpenAsync(cancellationToken);

                // Install Hangfire schema if needed
                _logger.LogInformation("Installing Hangfire SQL objects if necessary...");

                var options = new SqlServerStorageOptions
                {
                    PrepareSchemaIfNecessary = true,
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(ApplicationConstants.Database.CommandBatchMaxTimeoutMinutes),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(ApplicationConstants.Database.SlidingInvisibilityTimeoutMinutes),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true,
                };

                var storage = new SqlServerStorage(hangfireConnectionString, options);

                // This will create the schema if it doesn't exist
                storage.GetConnection();

                _logger.LogInformation("Hangfire schema initialization completed successfully");
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Hangfire schema. Hangfire background jobs may not function properly.");
            throw;
        }
    }
}
