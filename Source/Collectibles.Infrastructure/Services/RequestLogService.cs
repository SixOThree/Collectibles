using Collectibles.Application.Interfaces;
using Collectibles.Domain.Constants;

using Microsoft.Extensions.Logging;

namespace Collectibles.Infrastructure.Services;

public class RequestLogService : IRequestLogService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<RequestLogService> _logger;

    public RequestLogService(IApplicationDbContext context, ILogger<RequestLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogRequestAsync(
        string method,
        string path,
        string? queryString,
        int statusCode,
        long elapsedMilliseconds,
        string? requestId,
        string? correlationId,
        string? userId,
        string? userName,
        string? ipAddress,
        string? userAgent,
        string? scheme,
        string? host,
        string? contentType,
        long? contentLength,
        string? responseContentType,
        long? responseContentLength,
        Exception? exception = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestLog = new RequestLog
            {
                Method = method,
                Path = path,
                QueryString = queryString,
                StatusCode = statusCode,
                ElapsedMilliseconds = elapsedMilliseconds,
                RequestId = requestId,
                CorrelationId = correlationId,
                UserId = userId,
                UserName = userName,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                Scheme = scheme,
                Host = host,
                ContentType = contentType,
                ContentLength = contentLength,
                ResponseContentType = responseContentType,
                ResponseContentLength = responseContentLength,
                Timestamp = DateTime.UtcNow,
            };

            if (exception != null)
            {
                requestLog.ExceptionType = exception.GetType().FullName;
                requestLog.ExceptionMessage = exception.Message;
            }

            _context.RequestLogs.Add(requestLog);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Log the error but don't throw - we don't want logging failures to break the application
            _logger.LogError(ex, "Failed to log request to database");
        }
    }

    public async Task<IEnumerable<RequestLog>> GetRequestLogsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? userId = null,
        string? path = null,
        int? minStatusCode = null,
        int? maxStatusCode = null,
        int pageNumber = 1,
        int pageSize = ApplicationConstants.Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.RequestLogs.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(l => l.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(l => l.Timestamp <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(l => l.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(path))
        {
            query = query.Where(l => l.Path.Contains(path));
        }

        if (minStatusCode.HasValue)
        {
            query = query.Where(l => l.StatusCode >= minStatusCode.Value);
        }

        if (maxStatusCode.HasValue)
        {
            query = query.Where(l => l.StatusCode <= maxStatusCode.Value);
        }

        return await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetRequestLogCountAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? userId = null,
        string? path = null,
        int? minStatusCode = null,
        int? maxStatusCode = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.RequestLogs.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(l => l.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(l => l.Timestamp <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(l => l.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(path))
        {
            query = query.Where(l => l.Path.Contains(path));
        }

        if (minStatusCode.HasValue)
        {
            query = query.Where(l => l.StatusCode >= minStatusCode.Value);
        }

        if (maxStatusCode.HasValue)
        {
            query = query.Where(l => l.StatusCode <= maxStatusCode.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task CleanupOldLogsAsync(int daysToKeep = 365, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            var oldLogs = await _context.RequestLogs
                .Where(l => l.Timestamp < cutoffDate)
                .ToListAsync(cancellationToken);

            if (oldLogs.Count != 0)
            {
                _context.RequestLogs.RemoveRange(oldLogs);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation($"Cleaned up {oldLogs.Count} request logs older than {daysToKeep} days");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old request logs");
            throw;
        }
    }
}
