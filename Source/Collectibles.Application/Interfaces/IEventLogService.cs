using Collectibles.Domain.Entities;

namespace Collectibles.Application.Interfaces;

public interface IEventLogService
{
    Task LogEventAsync(
        EventAction action,
        string? entityType = null,
        long? entityId = null,
        string? entityName = null,
        object? oldValues = null,
        object? newValues = null,
        string? additionalData = null,
        CancellationToken cancellationToken = default);

    // Overload with explicit context information for Blazor Interactive components
    Task LogEventAsync(
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
        CancellationToken cancellationToken = default);

    Task LogUserActivityAsync(
        EventAction action,
        string? additionalData = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<EventLog>> GetEventLogsAsync(
        string? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        EventAction? action = null,
        string? entityType = null,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    Task<int> GetEventLogCountAsync(
        string? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        EventAction? action = null,
        string? entityType = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<UserSession>> GetUserSessionsAsync(
        string? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<EventLog>> GetEventLogsBySessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}

public class UserSession
{
    public string SessionId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int EventCount { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public List<EventAction> UniqueActions { get; set; } = new();
}
