using Collectibles.Application.Interfaces;
using Collectibles.Domain.Constants;

namespace Collectibles.Web.Services;

/// <summary>
/// Background service that processes request log entries from the queue
/// and persists them to the database in batches for optimal performance.
/// </summary>
public class RequestLogBackgroundService : BackgroundService
{
    private readonly RequestLogQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RequestLogBackgroundService> _logger;
    private const int BatchSize = ApplicationConstants.BatchProcessing.EmailBatchSize;
    private const int BatchDelayMilliseconds = ApplicationConstants.BatchProcessing.RequestLogBatchDelayMs;

    public RequestLogBackgroundService(
        RequestLogQueue queue,
        IServiceProvider serviceProvider,
        ILogger<RequestLogBackgroundService> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RequestLogBackgroundService started");

        await foreach (var entry in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                // Process entry immediately (can be batched later for better performance)
                await ProcessLogEntryAsync(entry, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request log entry for {Method} {Path}", entry.Method, entry.Path);
                // Continue processing - don't let one failure stop the service
            }
        }

        _logger.LogInformation("RequestLogBackgroundService stopped");
    }

    private async Task ProcessLogEntryAsync(RequestLogEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            // Create a new scope for this operation
            await using var scope = _serviceProvider.CreateAsyncScope();
            var requestLogService = scope.ServiceProvider.GetRequiredService<IRequestLogService>();

            await requestLogService.LogRequestAsync(
                method: entry.Method,
                path: entry.Path,
                queryString: entry.QueryString,
                statusCode: entry.StatusCode,
                elapsedMilliseconds: entry.ElapsedMilliseconds,
                requestId: entry.RequestId,
                correlationId: entry.CorrelationId,
                userId: entry.UserId,
                userName: entry.UserName,
                ipAddress: entry.IpAddress,
                userAgent: entry.UserAgent,
                scheme: entry.Scheme,
                host: entry.Host,
                contentType: entry.ContentType,
                contentLength: entry.ContentLength,
                responseContentType: entry.ResponseContentType,
                responseContentLength: entry.ResponseContentLength,
                exception: entry.Exception);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist request log entry to database for {Method} {Path}", entry.Method, entry.Path);
        }
    }
}
