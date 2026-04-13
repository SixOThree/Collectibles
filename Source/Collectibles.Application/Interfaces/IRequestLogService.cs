using Collectibles.Domain.Entities;

namespace Collectibles.Application.Interfaces;

public interface IRequestLogService
{
    Task LogRequestAsync(
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
        CancellationToken cancellationToken = default);

    Task<IEnumerable<RequestLog>> GetRequestLogsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? userId = null,
        string? path = null,
        int? minStatusCode = null,
        int? maxStatusCode = null,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    Task<int> GetRequestLogCountAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? userId = null,
        string? path = null,
        int? minStatusCode = null,
        int? maxStatusCode = null,
        CancellationToken cancellationToken = default);

    Task CleanupOldLogsAsync(int daysToKeep = 365, CancellationToken cancellationToken = default);
}
