using System.Text.Json;

using Collectibles.Application.Interfaces;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Collectibles.Infrastructure.Services.Logging;

public class EventLogService : IEventLogService
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<EventLogService> _logger;
    private readonly ISessionTrackingService _sessionTrackingService;

    public EventLogService(
        IApplicationDbContextFactory contextFactory,
        ICurrentUserService currentUserService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<EventLogService> logger,
        ISessionTrackingService sessionTrackingService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _sessionTrackingService = sessionTrackingService;
    }

    public async Task LogEventAsync(
        EventAction action,
        string? entityType = null,
        long? entityId = null,
        string? entityName = null,
        object? oldValues = null,
        object? newValues = null,
        string? additionalData = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var httpContext = _httpContextAccessor?.HttpContext;
        var userId = _currentUserService?.UserId;
        var userName = _currentUserService?.UserName;

        var eventLog = new EventLog
        {
            UserId = userId,
            UserEmail = userName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            EntityName = entityName,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            IPAddress = GetIPAddress(httpContext),
            UserAgent = httpContext?.Request?.Headers?["User-Agent"].ToString(),
            AdditionalData = additionalData,
            SessionId = TryGetSessionId(httpContext),
            Timestamp = DateTime.UtcNow,
        };

        context.EventLogs.Add(eventLog);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task LogEventAsync(
        EventAction action,
        string? entityType,
        long? entityId,
        string? entityName,
        object? oldValues,
        object? newValues,
        string? additionalData,
        string? userId,
        string? userEmail,
        string? ipAddress,
        string? userAgent,
        string? sessionId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var eventLog = new EventLog
        {
            UserId = userId ?? _currentUserService.UserId,
            UserEmail = userEmail ?? _currentUserService.UserName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            EntityName = entityName,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            IPAddress = ipAddress ?? GetIPAddress(_httpContextAccessor.HttpContext),
            UserAgent = userAgent ?? _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString(),
            AdditionalData = additionalData,
            SessionId = sessionId ?? TryGetSessionId(_httpContextAccessor.HttpContext),
            Timestamp = DateTime.UtcNow,
        };

        context.EventLogs.Add(eventLog);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task LogUserActivityAsync(
        EventAction action,
        string? additionalData = null,
        CancellationToken cancellationToken = default)
    {
        await LogEventAsync(
            action,
            entityType: null,
            entityId: null,
            entityName: null,
            oldValues: null,
            newValues: null,
            additionalData: additionalData,
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<EventLog>> GetEventLogsAsync(
        string? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        EventAction? action = null,
        string? entityType = null,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.EventLogs.AsQueryable();

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(e => e.UserId == userId || e.UserEmail == userId);
        }

        if (startDate.HasValue)
        {
            query = query.Where(e => e.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.Timestamp <= endDate.Value);
        }

        if (action.HasValue)
        {
            query = query.Where(e => e.Action == action.Value);
        }

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(e => e.EntityType == entityType);
        }

        return await query
            .OrderByDescending(e => e.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetEventLogCountAsync(
        string? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        EventAction? action = null,
        string? entityType = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.EventLogs.AsQueryable();

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(e => e.UserId == userId || e.UserEmail == userId);
        }

        if (startDate.HasValue)
        {
            query = query.Where(e => e.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.Timestamp <= endDate.Value);
        }

        if (action.HasValue)
        {
            query = query.Where(e => e.Action == action.Value);
        }

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(e => e.EntityType == entityType);
        }

        return await query.CountAsync(cancellationToken);
    }

    private static string? GetIPAddress(HttpContext? httpContext)
    {
        if (httpContext == null)
        {
            return null;
        }

        var ipAddress = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(ipAddress))
        {
            var addresses = ipAddress.Split(',');
            if (addresses.Length > 0)
            {
                return addresses[0].Trim();
            }
        }

        ipAddress = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(ipAddress))
        {
            return ipAddress;
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private string? TryGetSessionId(HttpContext? httpContext)
    {
        try
        {
            if (httpContext != null)
            {
                const string trackingCookieName = "CollectiblesTrackingId";
                if (httpContext.Request.Cookies.TryGetValue(trackingCookieName, out var trackingId) &&
                    !string.IsNullOrEmpty(trackingId))
                {
                    return $"session_{trackingId}";
                }

                var connectionId = httpContext.Connection?.Id;
                if (!string.IsNullOrEmpty(connectionId))
                {
                    return $"connection_{connectionId}";
                }
            }

            return _sessionTrackingService.SessionId;
        }
        catch
        {
            return _sessionTrackingService.SessionId;
        }
    }

    public async Task<IEnumerable<UserSession>> GetUserSessionsAsync(
        string? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.EventLogs.AsQueryable();

        if (!string.IsNullOrEmpty(userId))
        {
            if (string.Equals(userId, "anonymous", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(e => e.UserId == null && e.UserEmail == null);
            }
            else
            {
                query = query.Where(e => e.UserId == userId || e.UserEmail == userId);
            }
        }

        if (startDate.HasValue)
        {
            query = query.Where(e => e.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.Timestamp <= endDate.Value);
        }

        // 1. Group events with a valid SessionId
        var trackedSessionGroups = query
            .Where(e => e.SessionId != null && e.SessionId != "")
            .GroupBy(e => e.SessionId!)
            .Select(g => new
            {
                SessionId = g.Key,
                StartTime = g.Min(e => e.Timestamp),
                EndTime = g.Max(e => e.Timestamp),
                EventCount = g.Count(),
            });

        // 2. Group legacy events with a null SessionId
        var legacySessionGroups = query
            .Where(e => e.SessionId == null || e.SessionId == "")
            .GroupBy(e => new { UserKey = e.UserId ?? e.UserEmail ?? "anonymous", DateBucket = e.Timestamp.Date })
            .Select(g => new
            {
                SessionId = "synthetic_" + g.Key.UserKey + "_" + g.Min(e => e.Timestamp).ToString("yyyyMMddHHmmss"),
                StartTime = g.Min(e => e.Timestamp),
                EndTime = g.Max(e => e.Timestamp),
                EventCount = g.Count(),
            });

        // Combine both in SQL and apply pagination
        var sessionGroups = await trackedSessionGroups
            .Concat(legacySessionGroups)
            .OrderByDescending(s => s.StartTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var sessionIds = sessionGroups.Select(s => s.SessionId).ToList();
        var sessionDetailsMap = new Dictionary<string, (string? UserId, string? UserEmail, List<EventAction> Actions)>();

        if (sessionIds.Count > 0)
        {
            var trackedSessionIds = sessionIds.Where(id => !id.StartsWith("synthetic_")).ToList();
            if (trackedSessionIds.Count > 0)
            {
                var eventsForSessions = await context.EventLogs
                    .Where(e => e.SessionId != null && trackedSessionIds.Contains(e.SessionId))
                    .OrderBy(e => e.Timestamp)
                    .ToListAsync(cancellationToken);

                var groupedEvents = eventsForSessions.GroupBy(e => e.SessionId!);

                foreach (var g in groupedEvents)
                {
                    var firstEvt = g.First();
                    var primaryUserId = firstEvt.UserId;

                    var userEmail = g.FirstOrDefault(e =>
                        (string.IsNullOrEmpty(primaryUserId) ? e.UserId == null : e.UserId == primaryUserId) &&
                        !string.IsNullOrEmpty(e.UserEmail))?.UserEmail ?? firstEvt.UserEmail;

                    var actions = g.Select(e => e.Action).Distinct().ToList();

                    sessionDetailsMap[g.Key] = (primaryUserId, userEmail, actions);
                }
            }
        }

        var result = new List<UserSession>();
        foreach (var sg in sessionGroups)
        {
            sessionDetailsMap.TryGetValue(sg.SessionId, out var details);

            var session = new UserSession
            {
                SessionId = sg.SessionId,
                UserId = details.UserId,
                UserEmail = details.UserEmail,
                StartTime = sg.StartTime,
                EndTime = sg.EndTime,
                EventCount = sg.EventCount,
                UniqueActions = details.Actions ?? new List<EventAction>(),
            };
            result.Add(session);
        }

        return result;
    }

    public async Task<IEnumerable<EventLog>> GetEventLogsBySessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var events = await context.EventLogs
            .Where(e => e.SessionId == sessionId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync(cancellationToken);

        if (events.Count != 0)
        {
            return events;
        }

        if (sessionId.StartsWith("synthetic_") || sessionId.StartsWith("session_") || sessionId.StartsWith("connection_"))
        {
            var parts = sessionId.Split('_');
            if (parts.Length >= 3)
            {
                var timestampStr = parts[parts.Length - 1];
                var userKey = string.Join("_", parts.Skip(1).Take(parts.Length - 2));

                if (DateTime.TryParseExact(timestampStr, "yyyyMMddHHmmss", null,
                    System.Globalization.DateTimeStyles.None, out var baseTime))
                {
                    var startTime = baseTime.AddMinutes(-30);
                    var endTime = baseTime.AddMinutes(30);

                    if (userKey == "anonymous")
                    {
                        events = await context.EventLogs
                            .Where(e => e.UserId == null && e.UserEmail == null &&
                                       e.Timestamp >= startTime && e.Timestamp <= endTime)
                            .OrderBy(e => e.Timestamp)
                            .ToListAsync(cancellationToken);
                    }
                    else
                    {
                        events = await context.EventLogs
                            .Where(e => (e.UserId == userKey || e.UserEmail == userKey) &&
                                       e.Timestamp >= startTime && e.Timestamp <= endTime)
                            .OrderBy(e => e.Timestamp)
                            .ToListAsync(cancellationToken);
                    }

                    return events;
                }
            }
        }

        return events;
    }
}
