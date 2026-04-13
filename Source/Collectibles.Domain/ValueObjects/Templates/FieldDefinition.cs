namespace Collectibles.Domain.ValueObjects.Templates;

/// <summary>
/// Represents a field definition in a content template.
/// </summary>
public class FieldDefinition
{
    /// <summary>
    /// Gets or sets the unique name of the field (used as the key for storing values).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display label for the field.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the field.
    /// </summary>
    public FieldType FieldType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether this field is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets the display order of the field.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the placeholder text for the field.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Gets or sets the default value for the field.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the help text to display for the field.
    /// </summary>
    public string? HelpText { get; set; }

    /// <summary>
    /// Gets or sets the validation rules for the field.
    /// </summary>
    public FieldValidationRules ValidationRules { get; set; } = new();

    /// <summary>
    /// Gets or sets additional options specific to the field type (e.g., number format, date format).
    /// </summary>
    public Dictionary<string, object> Options { get; set; } = new();
}
