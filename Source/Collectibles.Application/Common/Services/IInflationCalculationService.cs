namespace Collectibles.Application.Common.Services;

/// <summary>
/// Service for calculating inflation-adjusted prices.
/// </summary>
public interface IInflationCalculationService
{
    /// <summary>
    /// Calculates the inflation-adjusted price from a historical year to the current year.
    /// </summary>
    /// <param name="originalPrice">The original price in the historical year.</param>
    /// <param name="year">The historical year of the original price.</param>
    /// <returns>The inflation-adjusted price in current year dollars.</returns>
    decimal CalculateAdjustedPrice(decimal originalPrice, int year);

    /// <summary>
    /// Calculates the inflation-adjusted price from a historical year to a target year.
    /// </summary>
    /// <param name="originalPrice">The original price in the historical year.</param>
    /// <param name="fromYear">The historical year of the original price.</param>
    /// <param name="toYear">The target year to adjust the price to.</param>
    /// <returns>The inflation-adjusted price in the target year dollars.</returns>
    decimal CalculateAdjustedPrice(decimal originalPrice, int fromYear, int toYear);

    /// <summary>
    /// Gets the total inflation rate between two years.
    /// </summary>
    /// <param name="fromYear">The starting year.</param>
    /// <param name="toYear">The ending year.</param>
    /// <returns>The total inflation rate as a percentage.</returns>
    double GetInflationRate(int fromYear, int toYear);

    /// <summary>
    /// Validates if a year is within the supported range for CPI data.
    /// </summary>
    /// <param name="year">The year to validate.</param>
    /// <returns>True if the year is valid, false otherwise.</returns>
    bool IsValidYear(int year);
}
