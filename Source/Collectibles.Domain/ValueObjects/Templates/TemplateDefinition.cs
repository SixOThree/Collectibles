using System.Text.Json;

namespace Collectibles.Domain.ValueObjects.Templates;

/// <summary>
/// Represents a complete template definition with its fields.
/// </summary>
public class TemplateDefinition
{
    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the collection of field definitions.
    /// </summary>
    public List<FieldDefinition> Fields { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether items using this template can store multiple entries
    /// (rows of field values) instead of a single set of field values.
    /// For example, a magazine template could store multiple issues within one item.
    /// </summary>
    public bool AllowMultipleEntries { get; set; }

    /// <summary>
    /// Gets or sets the template version for future compatibility.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Serializes this template definition to JSON.
    /// </summary>
    /// <returns>JSON representation of the template.</returns>
    public string ToJson()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        return JsonSerializer.Serialize(this, options);
    }

    /// <summary>
    /// Deserializes a template definition from JSON.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A TemplateDefinition instance.</returns>
    public static TemplateDefinition? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        return JsonSerializer.Deserialize<TemplateDefinition>(json, options);
    }
}
