using System.Diagnostics;
using System.Text.Json;

using Collectibles.Application.Interfaces;

using Microsoft.AspNetCore.Http;

namespace Collectibles.Infrastructure.Services.Logging;

public class SysLogService : ISysLogService
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentUserService _currentUserService;

    public SysLogService(
        IApplicationDbContextFactory contextFactory,
        IHttpContextAccessor httpContextAccessor,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _httpContextAccessor = httpContextAccessor;
        _currentUserService = currentUserService;
    }

    public async Task LogAsync(
        LogLevel level,
        string message,
        Exception? exception = null,
        string? source = null,
        string? category = null,
        string? correlationId = null,
        Dictionary<string, object>? properties = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Try to get HTTP context, but handle gracefully if it's null (common in Blazor Server)
        var httpContext = _httpContextAccessor?.HttpContext;
        var process = Process.GetCurrentProcess();

        // Get user info from CurrentUserService which now handles null HttpContext gracefully
        var userId = _currentUserService?.UserId;

        var sysLog = new SysLog
        {
            Level = level,
            Message = TruncateMessage(message, 4000),
            Exception = exception?.ToString(),
            StackTrace = exception?.StackTrace,
            Source = source ?? exception?.Source,
            MachineName = Environment.MachineName,
            ProcessName = process.ProcessName,
            ThreadId = Environment.CurrentManagedThreadId,
            Properties = properties != null ? JsonSerializer.Serialize(properties) : null,
            Timestamp = DateTime.UtcNow,
            Category = category,
            CorrelationId = correlationId ?? Activity.Current?.Id,
            UserId = userId,
            RequestPath = httpContext?.Request?.Path,
            RequestMethod = httpContext?.Request?.Method,
        };

        context.SysLogs.Add(sysLog);
        await context.SaveChangesAsync(cancellationToken);
    }

    // Overload with explicit context information for Blazor Interactive components
    public async Task LogAsync(
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
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var process = Process.GetCurrentProcess();

        var sysLog = new SysLog
        {
            Level = level,
            Message = TruncateMessage(message, 4000),
            Exception = exception?.ToString(),
            StackTrace = exception?.StackTrace,
            Source = source ?? exception?.Source,
            MachineName = Environment.MachineName,
            ProcessName = process.ProcessName,
            ThreadId = Environment.CurrentManagedThreadId,
            Properties = properties != null ? JsonSerializer.Serialize(properties) : null,
            Timestamp = DateTime.UtcNow,
            Category = category,
            CorrelationId = correlationId ?? Activity.Current?.Id,
            UserId = userId ?? _currentUserService.UserId,
            RequestPath = requestPath ?? _httpContextAccessor.HttpContext?.Request.Path,
            RequestMethod = requestMethod ?? _httpContextAccessor.HttpContext?.Request.Method,
        };

        context.SysLogs.Add(sysLog);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task LogTraceAsync(string message, string? category = null, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        return LogAsync(LogLevel.Trace, message, null, null, category, null, properties, cancellationToken);
    }

    public Task LogDebugAsync(string message, string? category = null, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        return LogAsync(LogLevel.Debug, message, null, null, category, null, properties, cancellationToken);
    }

    public Task LogInformationAsync(string message, string? category = null, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        return LogAsync(LogLevel.Information, message, null, null, category, null, properties, cancellationToken);
    }

    public Task LogWarningAsync(string message, string? category = null, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        return LogAsync(LogLevel.Warning, message, null, null, category, null, properties, cancellationToken);
    }

    public Task LogErrorAsync(string message, Exception? exception = null, string? category = null, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        return LogAsync(LogLevel.Error, message, exception, null, category, null, properties, cancellationToken);
    }

    public Task LogCriticalAsync(string message, Exception? exception = null, string? category = null, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        return LogAsync(LogLevel.Critical, message, exception, null, category, null, properties, cancellationToken);
    }

    public async Task<IEnumerable<SysLog>> GetSysLogsAsync(
        LogLevel? minLevel = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? category = null,
        string? correlationId = null,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.SysLogs.AsQueryable();

        if (minLevel.HasValue)
        {
            query = query.Where(s => s.Level >= minLevel.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(s => s.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.Timestamp <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(s => s.Category == category);
        }

        if (!string.IsNullOrEmpty(correlationId))
        {
            query = query.Where(s => s.CorrelationId == correlationId);
        }

        return await query
            .OrderByDescending(s => s.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetSysLogCountAsync(
        LogLevel? minLevel = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? category = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.SysLogs.AsQueryable();

        if (minLevel.HasValue)
        {
            query = query.Where(s => s.Level >= minLevel.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(s => s.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.Timestamp <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(s => s.Category == category);
        }

        if (!string.IsNullOrEmpty(correlationId))
        {
            query = query.Where(s => s.CorrelationId == correlationId);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task CleanupOldLogsAsync(int daysToKeep = 30, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);

        await context.SysLogs
            .Where(s => s.Timestamp < cutoffDate)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static string TruncateMessage(string message, int maxLength)
    {
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        return message.Length <= maxLength ? message : message.Substring(0, maxLength);
    }
}
