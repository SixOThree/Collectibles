namespace Collectibles.Domain.Entities;

public class CollectibleItemRelatedTag : BaseEntity
{
    public long CollectibleItemId { get; set; }
    public CollectibleItem CollectibleItem { get; set; } = null!;

    public long TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
