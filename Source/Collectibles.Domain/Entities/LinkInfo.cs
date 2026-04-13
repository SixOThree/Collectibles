namespace Collectibles.Domain.Entities;

public class LinkInfo : BaseAuditableEntity
{
    public long CollectibleItemId { get; set; }
    public CollectibleItem CollectibleItem { get; set; } = null!;
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public virtual ICollection<LinkCache> Caches { get; set; } = new List<LinkCache>();
}
