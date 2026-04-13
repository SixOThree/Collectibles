using Collectibles.Domain.Enums;

namespace Collectibles.Domain.Entities;

public class LinkCache : BaseAuditableEntity
{
    public long LinkInfoId { get; set; }
    public LinkInfo LinkInfo { get; set; } = null!;
    public DateTime CachedDate { get; set; }
    public LinkCacheStatus Status { get; set; }
    public string? CachedContentPath { get; set; }
    public string? ScreenshotPath { get; set; }
    public string? FailureReason { get; set; }
}
