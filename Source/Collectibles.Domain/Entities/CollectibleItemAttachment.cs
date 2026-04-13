namespace Collectibles.Domain.Entities;

/// <summary>
/// Junction entity for the many-to-many relationship between CollectibleItem and Attachment.
/// Allows additional properties like IsFeatured to be added to the relationship.
/// </summary>
public class CollectibleItemAttachment
{
    public long CollectibleItemId { get; set; }
    public CollectibleItem CollectibleItem { get; set; } = null!;

    public long AttachmentId { get; set; }
    public Attachment Attachment { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether indicates whether this attachment is featured for the collectible item.
    /// Featured attachments are displayed prominently in a separate section.
    /// </summary>
    public bool IsFeatured { get; set; }

    /// <summary>
    /// Gets or sets the date when this attachment was marked as featured.
    /// Null if the attachment has never been featured.
    /// </summary>
    public DateTime? FeaturedDate { get; set; }

    /// <summary>
    /// Gets or sets display order for featured attachments.
    /// Lower numbers appear first.
    /// </summary>
    public int DisplayOrder { get; set; }
}
