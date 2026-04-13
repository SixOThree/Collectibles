using Collectibles.Application.Features.Attachments;
using Collectibles.Application.Features.CollectibleItems;
using Collectibles.Domain.Enums;

namespace Collectibles.Application.Features.Showcases;

public class ShowcaseDetailDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? UserId { get; set; }
    public AttachmentDto? PreviewImage { get; set; }
    public bool IsPrivate { get; set; }
    public ShowcaseSortOrder SortOrder { get; set; }
    public List<CollectibleItemDto> Items { get; set; } = new();
    public List<CollectibleItemCardDto> ItemCards { get; set; } = new();
    public List<Collectibles.Application.Features.Tags.TagDto> Tags { get; set; } = new();
    public DateTime CreatedDate { get; set; }
    public DateTime? LastModifiedDate { get; set; }

    // Recursive statistics (includes all nested children)
    public int TotalItemCount { get; set; }
    public int TotalAttachmentCount { get; set; }
    public int ItemsWithPreviewCount { get; set; }
}

public class CollectibleItemDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AttachmentDto? PreviewImage { get; set; }
    public List<AttachmentDto> Attachments { get; set; } = new();
    public List<Collectibles.Application.Features.Tags.TagDto> Tags { get; set; } = new();
    public DateTime CreatedDate { get; set; }
    public int ChildItemCount { get; set; }
}
