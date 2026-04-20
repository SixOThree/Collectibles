using Collectibles.Application.Features.ContentDefinitions.Commands;

namespace Collectibles.Application.Features.ContentDefinitions;

public class ContentDefinitionDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool HideAttachments { get; set; }
    public int? ItemDetailPreviewHeight { get; set; }
    public bool IsGlobal { get; set; }
    public long? ShowcaseId { get; set; }
    public string? ShowcaseName { get; set; }
    public string? BorderColor { get; set; }
    public string? Icon { get; set; }
    public bool AllowMultipleEntries { get; set; }
    public List<FieldDefinitionDto> Fields { get; set; } = new();
    public int ItemCount { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? LastModified { get; set; }
}

public class ContentDefinitionListDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public bool HideAttachments { get; set; }
    public int? ItemDetailPreviewHeight { get; set; }
    public bool IsGlobal { get; set; }
    public long? ShowcaseId { get; set; }
    public string? ShowcaseName { get; set; }
    public string? BorderColor { get; set; }
    public string? Icon { get; set; }
    public bool AllowMultipleEntries { get; set; }
    public int FieldCount { get; set; }
    public int ItemCount { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? LastModified { get; set; }
}
