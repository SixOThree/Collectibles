using Collectibles.Domain.Constants;

namespace Collectibles.Application.Common.Services;

/// <summary>
/// Implementation of inflation calculation service using CPI data.
/// </summary>
public class InflationCalculationService : IInflationCalculationService
{
    /// <inheritdoc/>
    public decimal CalculateAdjustedPrice(decimal originalPrice, int year)
    {
        var currentYear = DateTime.UtcNow.Year;
        if (currentYear > CpiData.MaxYear)
        {
            currentYear = CpiData.MaxYear;
        }

        return CalculateAdjustedPrice(originalPrice, year, currentYear);
    }

    /// <inheritdoc/>
    public decimal CalculateAdjustedPrice(decimal originalPrice, int fromYear, int toYear)
    {
        if (!IsValidYear(fromYear))
        {
            throw new ArgumentOutOfRangeException(
                nameof(fromYear),
                $"Year must be between {CpiData.MinYear} and {CpiData.MaxYear}");
        }

        if (!IsValidYear(toYear))
        {
            throw new ArgumentOutOfRangeException(
                nameof(toYear),
                $"Year must be between {CpiData.MinYear} and {CpiData.MaxYear}");
        }

        if (fromYear == toYear)
        {
            return originalPrice;
        }

        var fromCpi = CpiData.UsCpiData[fromYear];
        var toCpi = CpiData.UsCpiData[toYear];

        return originalPrice * (decimal)(toCpi / fromCpi);
    }

    /// <inheritdoc/>
    public double GetInflationRate(int fromYear, int toYear)
    {
        if (!IsValidYear(fromYear))
        {
            throw new ArgumentOutOfRangeException(
                nameof(fromYear),
                $"Year must be between {CpiData.MinYear} and {CpiData.MaxYear}");
        }

        if (!IsValidYear(toYear))
        {
            throw new ArgumentOutOfRangeException(
                nameof(toYear),
                $"Year must be between {CpiData.MinYear} and {CpiData.MaxYear}");
        }

        if (fromYear == toYear)
        {
            return 0;
        }

        var fromCpi = CpiData.UsCpiData[fromYear];
        var toCpi = CpiData.UsCpiData[toYear];

        return ((toCpi - fromCpi) / fromCpi) * 100;
    }

    /// <inheritdoc/>
    public bool IsValidYear(int year)
    {
        return year >= CpiData.MinYear && year <= CpiData.MaxYear;
    }
}
