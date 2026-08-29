using Collectibles.Domain.ValueObjects.Templates;

namespace Collectibles.Domain.Entities;

/// <summary>
/// Represents a collectible item that can be part of showcases or collections.
/// </summary>
public class CollectibleItem : BaseAuditableSoftDeleteEntity
{
    public string? Name { get; set; }
    public string? DetailedDescription { get; set; }
    public long? PreviewImageId { get; set; }
    public Attachment? PreviewImage { get; set; }
    public long? ParentId { get; set; }
    public CollectibleItem? Parent { get; set; }
    public ICollection<CollectibleItem> Children { get; set; }
    public List<Showcase> Showcases { get; set; }
    public ICollection<CollectibleItem> ComponentOfItem { get; set; }
    public long? ContentDefinitionId { get; set; }
    public ContentDefinition? ContentType { get; set; }
    public string? ContentValue { get; set; }
    public ICollection<CollectibleItemAttachment> CollectibleItemAttachments { get; set; }
    public ICollection<CollectibleItemTag> CollectibleItemTags { get; set; }
    public ICollection<CollectibleItemRelatedTag> CollectibleItemRelatedTags { get; set; }
    public ICollection<LinkInfo> ExternalReferences { get; set; }

    /// <summary>
    /// Gets or sets the QR code assigned to this item, if any.
    /// </summary>
    /// <remarks>
    /// The relationship is owned by <see cref="Entities.QRCode.CollectibleItemId"/>. A
    /// second scalar <c>QRCodeId</c> used to mirror it with no foreign key, no index, and
    /// two separate saves, so the sides could disagree and a deleted item left its QR code
    /// permanently unassignable.
    /// </remarks>
    public QRCode? QRCode { get; set; }

    public bool ShowRelatedItemsFirst { get; set; }

    public CollectibleItem()
    {
        Showcases = new List<Showcase>();
        Children = new List<CollectibleItem>();
        ComponentOfItem = new List<CollectibleItem>();
        CollectibleItemAttachments = new List<CollectibleItemAttachment>();
        CollectibleItemTags = new List<CollectibleItemTag>();
        ExternalReferences = new List<LinkInfo>();
        CollectibleItemRelatedTags = new List<CollectibleItemRelatedTag>();
    }

    /// <summary>
    /// Gets the field values from the stored JSON.
    /// </summary>
    /// <returns></returns>
    public FieldValueCollection GetFieldValues()
    {
        return FieldValueCollection.FromJson(ContentValue);
    }

    /// <summary>
    /// Sets the field values by serializing them to JSON.
    /// </summary>
    public void SetFieldValues(FieldValueCollection fieldValues)
    {
        ContentValue = fieldValues.ToJson();
    }

    /// <summary>
    /// Gets the field value entries from the stored JSON (for multi-entry templates).
    /// </summary>
    /// <returns></returns>
    public FieldValueEntryCollection GetFieldValueEntries()
    {
        return FieldValueEntryCollection.FromJson(ContentValue);
    }

    /// <summary>
    /// Sets the field value entries by serializing them to JSON (for multi-entry templates).
    /// </summary>
    public void SetFieldValueEntries(FieldValueEntryCollection entries)
    {
        ContentValue = entries.ToJson();
    }

    /// <summary>
    /// Gets or sets the optimistic-concurrency token. Without it, two editors of the same
    /// aggregate silently last-write-wins.
    /// </summary>
    public byte[]? RowVersion { get; set; }
}
