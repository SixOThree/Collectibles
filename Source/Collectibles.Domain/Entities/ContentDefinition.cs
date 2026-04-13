using Collectibles.Domain.ValueObjects.Templates;

namespace Collectibles.Domain.Entities;

/// <summary>
/// Represents a content definition that can be used to define the structure of collectible items.
/// </summary>
public class ContentDefinition : BaseAuditableEntity
{
    public string? Name { get; set; }
    public string? DefinitionJson { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets the border color (hex) to apply to item cards using this template.
    /// </summary>
    public string? BorderColor { get; set; }

    /// <summary>
    /// Gets or sets the Bootstrap Icon class name to display on item cards using this template.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether items using this template should hide their attachments
    /// on the detail page, showing only related items. A preview image can still be set in edit mode.
    /// </summary>
    public bool HideAttachments { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether indicates whether this template is globally available to all showcases.
    /// If false, the template is only available to the associated showcase.
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// Gets or sets the ID of the showcase this template belongs to (if not global).
    /// </summary>
    public long? ShowcaseId { get; set; }

    /// <summary>
    /// Gets or sets navigation property to the associated showcase (if not global).
    /// </summary>
    public Showcase? Showcase { get; set; }

    /// <summary>
    /// Gets the template definition from the stored JSON.
    /// </summary>
    /// <returns></returns>
    public TemplateDefinition? GetTemplateDefinition()
    {
        return TemplateDefinition.FromJson(DefinitionJson);
    }

    /// <summary>
    /// Sets the template definition by serializing it to JSON.
    /// </summary>
    public void SetTemplateDefinition(TemplateDefinition templateDefinition)
    {
        DefinitionJson = templateDefinition.ToJson();
        Name = templateDefinition.Name;
        Description = templateDefinition.Description;
    }

    /// <summary>
    /// Gets or sets navigation property for collectible items using this content definition.
    /// </summary>
    public ICollection<CollectibleItem> CollectibleItems { get; set; } = new List<CollectibleItem>();
}
