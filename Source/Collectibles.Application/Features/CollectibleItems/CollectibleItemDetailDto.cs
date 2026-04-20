using Collectibles.Application.Features.Attachments;

namespace Collectibles.Application.Features.CollectibleItems;

public class CollectibleItemDetailDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DetailedDescription { get; set; }
    public long? PreviewImageId { get; set; }
    public AttachmentBriefDto? PreviewImage { get; set; }
    public string? PreviewImageUrl { get; set; }
    public long? ParentId { get; set; }
    public string? ParentName { get; set; }
    public List<ParentInfo> ParentHierarchy { get; set; } = new();
    public List<CollectibleItemDto> Children { get; set; } = new();
    public long? ContentDefinitionId { get; set; }
    public int? ItemDetailPreviewHeight { get; set; }
    public Dictionary<string, object?> FieldValues { get; set; } = new();
    public List<Dictionary<string, object?>>? FieldValueEntries { get; set; }
    public bool AllowMultipleEntries { get; set; }
    public string? ContentType { get; set; }
    public string? ContentValue { get; set; }
    public List<AttachmentBriefDto> Attachments { get; set; } = new();
    public List<Collectibles.Application.Features.Tags.TagDto> Tags { get; set; } = new();
    public List<Collectibles.Application.Features.Tags.TagDto> RelatedTags { get; set; } = new();
    public List<LinkInfoDto> ExternalReferences { get; set; } = new();
    public List<ShowcaseBriefDto> Showcases { get; set; } = new();
    public long? QRCodeId { get; set; }
    public bool ShowRelatedItemsFirst { get; set; }
    public DateTime Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
}

public class LinkInfoDto
{
    public long Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
}

public class ShowcaseBriefDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? UserId { get; set; }
    public string? OwnerDisplayName { get; set; }
    public bool IsPrivate { get; set; }
    public Domain.Enums.ShowcaseSortOrder SortOrder { get; set; }
}

public class ParentInfo
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
}
