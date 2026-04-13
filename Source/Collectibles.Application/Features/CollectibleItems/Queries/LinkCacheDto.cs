using Collectibles.Domain.Enums;

namespace Collectibles.Application.Features.CollectibleItems.Queries;

public class LinkCacheDto
{
    public long Id { get; set; }
    public DateTime CachedDate { get; set; }
    public LinkCacheStatus Status { get; set; }
    public string? CachedContentPath { get; set; }
    public string? ScreenshotPath { get; set; }
    public string? FailureReason { get; set; }
}
