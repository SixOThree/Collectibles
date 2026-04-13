namespace Collectibles.Domain.ValueObjects.Templates;

/// <summary>
/// Represents validation rules for a field in a content template.
/// </summary>
public class FieldValidationRules
{
    /// <summary>
    /// Gets or sets the minimum length for text fields.
    /// </summary>
    public int? MinLength { get; set; }

    /// <summary>
    /// Gets or sets the maximum length for text fields.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the regular expression pattern for validation.
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Gets or sets the custom validation error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the minimum value for numeric fields.
    /// </summary>
    public decimal? MinValue { get; set; }

    /// <summary>
    /// Gets or sets the maximum value for numeric fields.
    /// </summary>
    public decimal? MaxValue { get; set; }

    /// <summary>
    /// Gets or sets the minimum date for date fields.
    /// </summary>
    public DateTime? MinDate { get; set; }

    /// <summary>
    /// Gets or sets the maximum date for date fields.
    /// </summary>
    public DateTime? MaxDate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether to allow decimal values for number fields.
    /// </summary>
    public bool AllowDecimals { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of decimal places allowed.
    /// </summary>
    public int? DecimalPlaces { get; set; }
}
