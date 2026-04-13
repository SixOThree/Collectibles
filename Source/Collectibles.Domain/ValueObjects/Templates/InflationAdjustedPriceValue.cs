using Collectibles.Domain.Constants;

namespace Collectibles.Domain.ValueObjects.Templates;

/// <summary>
/// Represents a price value with year for inflation adjustment.
/// </summary>
public class InflationAdjustedPriceValue
{
    /// <summary>
    /// Gets or sets the original price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the year of the price.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InflationAdjustedPriceValue"/> class.
    /// Creates a new instance of InflationAdjustedPriceValue.
    /// </summary>
    public InflationAdjustedPriceValue()
    {
        Year = Math.Min(DateTime.UtcNow.Year, CpiData.MaxYear);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InflationAdjustedPriceValue"/> class.
    /// Creates a new instance of InflationAdjustedPriceValue with specified values.
    /// </summary>
    /// <param name="price">The price value.</param>
    /// <param name="year">The year of the price.</param>
    public InflationAdjustedPriceValue(decimal price, int year)
    {
        Price = price;
        Year = year;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"${Price:N2} ({Year})";
    }
}
