namespace Collectibles.Domain.Entities;

public class SysLog : BaseEntity
{
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? StackTrace { get; set; }
    public string? Source { get; set; }
    public string? MachineName { get; set; }
    public string? ProcessName { get; set; }
    public int? ThreadId { get; set; }
    public string? Properties { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Category { get; set; }
    public string? CorrelationId { get; set; }
    public string? UserId { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }
}
