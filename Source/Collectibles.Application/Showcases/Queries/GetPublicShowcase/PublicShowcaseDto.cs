namespace Collectibles.Application.Showcases.Queries.GetPublicShowcase;

/// <summary>
/// A showcase as served to anonymous share-link visitors.
/// </summary>
/// <remarks>
/// Identifiers on this contract are HashIds, not database keys. These DTOs are the one
/// place the application serves data to unauthenticated callers, and they previously
/// carried the raw sequential primary keys — inviting enumeration and defeating the
/// HashIds obfuscation the routes themselves already use.
/// </remarks>
public class PublicShowcaseDto
{
    /// <summary>Gets or sets the HashId of the showcase.</summary>
    public string HashId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PreviewImageUrl { get; set; }
    public List<PublicCollectibleItemDto> CollectibleItems { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// A collectible item as served to anonymous share-link visitors.
/// </summary>
public class PublicCollectibleItemDto
{
    /// <summary>Gets or sets the HashId of the item.</summary>
    public string HashId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PreviewImageUrl { get; set; }
    public List<PublicAttachmentDto> Attachments { get; set; } = new();
}

/// <summary>
/// An attachment as served to anonymous share-link visitors.
/// </summary>
public class PublicAttachmentDto
{
    /// <summary>Gets or sets the HashId of the attachment.</summary>
    public string HashId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? ThumbnailUrl { get; set; }
}
