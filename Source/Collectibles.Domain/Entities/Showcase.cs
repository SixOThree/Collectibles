using Collectibles.Domain.Enums;

namespace Collectibles.Domain.Entities;

/// <summary>
/// Showcases encompass collections of collectible items.
/// </summary>
public class Showcase : BaseAuditableSoftDeleteEntity
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? UserId { get; set; }
    public Attachment? PreviewImage { get; set; }
    public bool IsPrivate { get; set; } = true;
    public ShowcaseSortOrder SortOrder { get; set; } = ShowcaseSortOrder.Alphabetical;
    public ICollection<ShowcaseTag> ShowcaseTags { get; set; }
    public List<CollectibleItem> CollectibleItems { get; set; }
    public ICollection<ShowcaseShareToken> ShareTokens { get; set; }

    public Showcase()
    {
        Name = string.Empty;
        ShowcaseTags = new List<ShowcaseTag>();
        CollectibleItems = new List<CollectibleItem>();
        ShareTokens = new List<ShowcaseShareToken>();
    }

    /// <summary>
    /// Gets or sets the optimistic-concurrency token. Without it, two editors of the same
    /// aggregate silently last-write-wins.
    /// </summary>
    public byte[]? RowVersion { get; set; }
}
