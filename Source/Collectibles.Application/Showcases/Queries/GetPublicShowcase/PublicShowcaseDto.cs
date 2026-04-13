namespace Collectibles.Application.Showcases.Queries.GetPublicShowcase;

public class PublicShowcaseDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PreviewImageUrl { get; set; }
    public List<PublicCollectibleItemDto> CollectibleItems { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

public class PublicCollectibleItemDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PreviewImageUrl { get; set; }
    public List<PublicAttachmentDto> Attachments { get; set; } = new();
}

public class PublicAttachmentDto
{
    public long Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? ThumbnailUrl { get; set; }
}
