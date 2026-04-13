namespace Collectibles.Domain.Entities;

public class RequestLog : BaseEntity
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? QueryString { get; set; }
    public int StatusCode { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public string? RequestId { get; set; }
    public string? CorrelationId { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Scheme { get; set; }
    public string? Host { get; set; }
    public string? ContentType { get; set; }
    public long? ContentLength { get; set; }
    public string? ResponseContentType { get; set; }
    public long? ResponseContentLength { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }
}
