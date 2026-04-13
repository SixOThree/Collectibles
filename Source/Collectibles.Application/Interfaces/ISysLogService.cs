using Collectibles.Domain.Entities;

namespace Collectibles.Application.Interfaces;

public interface ISysLogService
{
    Task LogAsync(
        LogLevel level,
        string message,
        Exception? exception = null,
        string? source = null,
        string? category = null,
        string? correlationId = null,
        Dictionary<string, object>? properties = null,
        CancellationToken cancellationToken = default);

    // Overload with explicit context information for Blazor Interactive components
    Task LogAsync(
        LogLevel level,
        string message,
        Exception? exception,
        string? source,
        string? category,
        string? correlationId,
        Dictionary<string, object>? properties,
        string? userId,
        string? requestPath,
        string? requestMethod,
        CancellationToken cancellationToken = default);

    Task LogTraceAsync(string message, string? category = null, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default);
    Task LogDebugAsync(string message, string? category = null, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default);
    Task LogInformationAsync(string message, string? category = null, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default);
    Task LogWarningAsync(string message, string? category = null, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default);
    Task LogErrorAsync(string message, Exception? exception = null, string? category = null, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default);
    Task LogCriticalAsync(string message, Exception? exception = null, string? category = null, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default);

    Task<IEnumerable<SysLog>> GetSysLogsAsync(
        LogLevel? minLevel = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? category = null,
        string? correlationId = null,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    Task<int> GetSysLogCountAsync(
        LogLevel? minLevel = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? category = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    Task CleanupOldLogsAsync(int daysToKeep = 30, CancellationToken cancellationToken = default);
}
