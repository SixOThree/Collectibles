namespace Collectibles.Application.Features.CollectibleItems;

public class CollectibleItemDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DetailedDescription { get; set; }
    public long? PreviewImageId { get; set; }
    public string? PreviewImageUrl { get; set; }
    public long? ParentId { get; set; }
    public string? ParentName { get; set; }
    public int TagCount { get; set; }
    public int AttachmentCount { get; set; }
    public DateTime Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
    public long? ContentDefinitionId { get; set; }
    public string? ContentDefinitionName { get; set; }
    public string? TemplateBorderColor { get; set; }
    public string? TemplateIcon { get; set; }
    public Dictionary<string, object?> FieldValues { get; set; } = new();
    public int? EntryCount { get; set; }
}
