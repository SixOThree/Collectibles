namespace Collectibles.Domain.ValueObjects.Templates;

/// <summary>
/// Defines the types of fields that can be used in content templates.
/// </summary>
public enum FieldType
{
    /// <summary>
    /// Single-line text input field.
    /// </summary>
    Text,

    /// <summary>
    /// Multi-line text input field for longer content.
    /// </summary>
    MultilineText,

    /// <summary>
    /// Large multi-line text area for extensive content.
    /// </summary>
    LargeText,

    /// <summary>
    /// Date-only picker field.
    /// </summary>
    Date,

    /// <summary>
    /// Numeric input field for integers or decimals.
    /// </summary>
    Number,

    /// <summary>
    /// Boolean checkbox field.
    /// </summary>
    Boolean,

    /// <summary>
    /// Date and time picker field.
    /// </summary>
    DateTime,

    /// <summary>
    /// Inflation-adjusted price field that stores a price and year,
    /// and displays the inflation-adjusted value.
    /// </summary>
    InflationAdjustedPrice,

    /// <summary>
    /// Dropdown selection field with predefined options.
    /// Allows for an unset/empty state.
    /// </summary>
    Dropdown,
}
