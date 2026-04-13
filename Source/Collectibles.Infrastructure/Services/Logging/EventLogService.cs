using System.Text.Json;

using Collectibles.Application.Interfaces;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Collectibles.Infrastructure.Services.Logging;

public class EventLogService : IEventLogService
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<EventLogService> _logger;

    public EventLogService(
        IApplicationDbContextFactory contextFactory,
        ICurrentUserService currentUserService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<EventLogService> logger)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
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

        // Try to get HTTP context, but handle gracefully if it's null (common in Blazor Server)
        var httpContext = _httpContextAccessor?.HttpContext;

        // Get user info from CurrentUserService which now handles null HttpContext gracefully
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

    // Overload with explicit context information for Blazor Interactive components
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
            query = query.Where(e => e.UserId == userId);
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
            query = query.Where(e => e.UserId == userId);
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

    private static string? TryGetSessionId(HttpContext? httpContext)
    {
        if (httpContext == null)
        {
            return null;
        }

        try
        {
            // Primary: Try to get tracking cookie
            const string trackingCookieName = "CollectiblesTrackingId";
            if (httpContext.Request.Cookies.TryGetValue(trackingCookieName, out var trackingId) &&
                !string.IsNullOrEmpty(trackingId))
            {
                return $"session_{trackingId}";
            }

            // Fallback: Try to get connection ID
            var connectionId = httpContext.Connection?.Id;
            if (!string.IsNullOrEmpty(connectionId))
            {
                return $"connection_{connectionId}";
            }

            return null;
        }
        catch
        {
            // If cookie access fails for any reason, just return null
            return null;
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

        // Group events by session or by user and time proximity
        var allEvents = await query.OrderBy(e => e.Timestamp).ToListAsync(cancellationToken);

        var sessions = new List<UserSession>();
        var processedEvents = new HashSet<long>();

        foreach (var evt in allEvents)
        {
            if (processedEvents.Contains(evt.Id))
            {
                continue;
            }

            var session = new UserSession
            {
                SessionId = evt.SessionId ?? $"synthetic_{evt.UserId ?? "anonymous"}_{evt.Timestamp:yyyyMMddHHmmss}",
                UserId = evt.UserId,
                UserEmail = evt.UserEmail,
                StartTime = evt.Timestamp,
                EndTime = evt.Timestamp,
                EventCount = 1,
                UniqueActions = new List<EventAction> { evt.Action },
            };

            // Find all events in the same session
            var sessionEvents = new List<EventLog>();

            if (!string.IsNullOrEmpty(evt.SessionId))
            {
                // If we have a session ID, use it
                sessionEvents = allEvents
                    .Where(e => e.SessionId == evt.SessionId && !processedEvents.Contains(e.Id))
                    .ToList();
            }
            else
            {
                // Group by user and time proximity (within 30 minutes of each other)
                var currentUser = evt.UserId;
                var currentTime = evt.Timestamp;

                sessionEvents = allEvents
                    .Where(e => e.UserId == currentUser &&
                               !processedEvents.Contains(e.Id) &&
                               Math.Abs((e.Timestamp - currentTime).TotalMinutes) <= 30)
                    .OrderBy(e => e.Timestamp)
                    .ToList();

                // Refine the group to ensure continuity
                var refinedEvents = new List<EventLog>();
                DateTime? lastEventTime = null;

                foreach (var e in sessionEvents)
                {
                    if (lastEventTime == null || (e.Timestamp - lastEventTime.Value).TotalMinutes <= 30)
                    {
                        refinedEvents.Add(e);
                        lastEventTime = e.Timestamp;
                    }
                    else
                    {
                        break; // Gap too large, end this session
                    }
                }

                sessionEvents = refinedEvents;
            }

            if (sessionEvents.Count != 0)
            {
                foreach (var sessionEvent in sessionEvents)
                {
                    processedEvents.Add(sessionEvent.Id);

                    if (sessionEvent.Timestamp < session.StartTime)
                    {
                        session.StartTime = sessionEvent.Timestamp;
                    }

                    if (sessionEvent.Timestamp > session.EndTime)
                    {
                        session.EndTime = sessionEvent.Timestamp;
                    }

                    if (!session.UniqueActions.Contains(sessionEvent.Action))
                    {
                        session.UniqueActions.Add(sessionEvent.Action);
                    }
                }

                session.EventCount = sessionEvents.Count;
                sessions.Add(session);

                _logger.LogDebug("Created session {SessionId} with {EventCount} events for user {User}", session.SessionId, session.EventCount, session.UserEmail ?? session.UserId ?? "anonymous");
            }
        }

        _logger.LogDebug("Total sessions created: {Count}", sessions.Count);

        // Apply pagination
        return sessions
            .OrderByDescending(s => s.StartTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public async Task<IEnumerable<EventLog>> GetEventLogsBySessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // First try to find events by actual session ID
        var events = await context.EventLogs
            .Where(e => e.SessionId == sessionId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync(cancellationToken);

        if (events.Count != 0)
        {
            _logger.LogDebug("Found {Count} events by SessionId: {SessionId}", events.Count, sessionId);
            return events;
        }

        // If no events found by session ID, try to parse the synthetic session ID
        // Formats: synthetic_{userId}_{timestamp}, session_{trackingId}, connection_{connectionId}
        if (sessionId.StartsWith("synthetic_") || sessionId.StartsWith("session_") || sessionId.StartsWith("connection_"))
        {
            _logger.LogDebug("Parsing synthetic session ID: {SessionId}", sessionId);
            var parts = sessionId.Split('_');
            if (parts.Length >= 3)
            {
                var userId = parts[1];
                var timestampStr = parts[parts.Length - 1]; // Get last part as timestamp

                // Reconstruct userId if it contained underscores
                if (parts.Length > 3)
                {
                    userId = string.Join("_", parts.Skip(1).Take(parts.Length - 2));
                }

                _logger.LogDebug("Extracted userId: {UserId}, timestamp: {Timestamp}", userId, timestampStr);

                if (DateTime.TryParseExact(timestampStr, "yyyyMMddHHmmss", null,
                    System.Globalization.DateTimeStyles.None, out var baseTime))
                {
                    // Find events for this user around this time (within 30 minutes)
                    var startTime = baseTime.AddMinutes(-30);
                    var endTime = baseTime.AddMinutes(30);

                    if (userId == "anonymous")
                    {
                        // For anonymous users, match by time range and null userId
                        events = await context.EventLogs
                            .Where(e => e.UserId == null &&
                                       e.Timestamp >= startTime &&
                                       e.Timestamp <= endTime)
                            .OrderBy(e => e.Timestamp)
                            .ToListAsync(cancellationToken);
                    }
                    else
                    {
                        events = await context.EventLogs
                            .Where(e => e.UserId == userId &&
                                       e.Timestamp >= startTime &&
                                       e.Timestamp <= endTime)
                            .OrderBy(e => e.Timestamp)
                            .ToListAsync(cancellationToken);
                    }

                    // Refine to ensure continuity
                    var refinedEvents = new List<EventLog>();
                    DateTime? lastEventTime = null;

                    foreach (var evt in events)
                    {
                        if (lastEventTime == null || (evt.Timestamp - lastEventTime.Value).TotalMinutes <= 30)
                        {
                            refinedEvents.Add(evt);
                            lastEventTime = evt.Timestamp;
                        }
                        else if (refinedEvents.Count != 0)
                        {
                            break; // Gap too large, end this session
                        }
                    }

                    _logger.LogDebug("Found {Count} refined events for synthetic session", refinedEvents.Count);
                    return refinedEvents;
                }
            }
        }

        _logger.LogDebug("No events found for session: {SessionId}", sessionId);
        return events;
    }
}
