namespace Collectibles.Domain.Entities;

/// <summary>
/// Represents a tag that can be associated with collectible items and showcases.
/// </summary>
public class Tag : BaseAuditableSoftDeleteEntity
{
    public string Name { get; set; }
    public ICollection<CollectibleItemTag> CollectibleItemTags { get; set; }
    public ICollection<CollectibleItemRelatedTag> CollectibleItemRelatedTags { get; set; }
    public ICollection<ShowcaseTag> ShowcaseTags { get; set; }

    public Tag()
    {
        Name = string.Empty;
        CollectibleItemTags = new List<CollectibleItemTag>();
        CollectibleItemRelatedTags = new List<CollectibleItemRelatedTag>();
        ShowcaseTags = new List<ShowcaseTag>();
    }
}
