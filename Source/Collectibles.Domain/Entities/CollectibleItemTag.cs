namespace Collectibles.Domain.Entities;

/// <summary>
/// Represents a many-to-many relationship between collectible items and tags.
/// </summary>
public class CollectibleItemTag : BaseAuditableSoftDeleteEntity
{
    public long CollectibleItemId { get; set; }
    public CollectibleItem CollectibleItem { get; set; } = null!;
    public long TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
