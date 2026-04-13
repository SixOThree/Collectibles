namespace Collectibles.Domain.Entities;

public class EventLog : BaseAuditableEntity
{
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public EventAction Action { get; set; }
    public string? EntityType { get; set; }
    public long? EntityId { get; set; }
    public string? EntityName { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? AdditionalData { get; set; }
    public string? SessionId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
