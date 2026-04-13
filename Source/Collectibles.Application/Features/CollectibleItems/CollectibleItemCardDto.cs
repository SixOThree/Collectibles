namespace Collectibles.Application.Features.CollectibleItems;

/// <summary>
/// Lightweight DTO for displaying collectible items in cards/lists.
/// Used consistently across showcases, search results, and child item displays.
/// </summary>
public class CollectibleItemCardDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PreviewImageUrl { get; set; }
    public int AttachmentCount { get; set; }
    public int ChildItemCount { get; set; }
    public List<TagSummaryDto> Tags { get; set; } = new();
    public DateTime CreatedDate { get; set; }

    // Optional: Include when needed for navigation
    public long? ParentId { get; set; }
    public string? ParentName { get; set; }

    // Optional: Include for showcase context
    public long? ShowcaseId { get; set; }
    public string? ShowcaseName { get; set; }

    // Template display properties
    public long? ContentDefinitionId { get; set; }
    public string? TemplateName { get; set; }
    public string? TemplateBorderColor { get; set; }
    public string? TemplateIcon { get; set; }
}

public class TagSummaryDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
}