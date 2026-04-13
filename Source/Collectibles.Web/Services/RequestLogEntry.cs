namespace Collectibles.Web.Services;

/// <summary>
/// Represents a request log entry to be persisted to the database.
/// </summary>
public class RequestLogEntry
{
    public required string Method { get; init; }
    public required string Path { get; init; }
    public required string QueryString { get; init; }
    public required int StatusCode { get; init; }
    public required long ElapsedMilliseconds { get; init; }
    public required string RequestId { get; init; }
    public required string CorrelationId { get; init; }
    public string? UserId { get; init; }
    public string? UserName { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public required string Scheme { get; init; }
    public required string Host { get; init; }
    public string? ContentType { get; init; }
    public long? ContentLength { get; init; }
    public string? ResponseContentType { get; init; }
    public long? ResponseContentLength { get; init; }
    public Exception? Exception { get; init; }
}
