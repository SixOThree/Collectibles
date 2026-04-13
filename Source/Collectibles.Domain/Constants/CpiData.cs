namespace Collectibles.Domain.Constants;

/// <summary>
/// Contains Consumer Price Index (CPI) data for US inflation calculations.
/// </summary>
public static class CpiData
{
    /// <summary>
    /// CPI data from 1970 to 2025 (estimated for 2025 based on latest available data).
    /// Source: U.S. Bureau of Labor Statistics.
    /// </summary>
    public static readonly IReadOnlyDictionary<int, double> UsCpiData = new Dictionary<int, double>
    {
        { 1970, 38.8 },
        { 1971, 40.5 },
        { 1972, 41.8 },
        { 1973, 44.4 },
        { 1974, 49.3 },
        { 1975, 53.8 },
        { 1976, 56.9 },
        { 1977, 60.6 },
        { 1978, 65.2 },
        { 1979, 72.6 },
        { 1980, 82.4 },
        { 1981, 90.9 },
        { 1982, 96.5 },
        { 1983, 99.6 },
        { 1984, 103.9 },
        { 1985, 107.6 },
        { 1986, 109.6 },
        { 1987, 113.6 },
        { 1988, 118.3 },
        { 1989, 124.0 },
        { 1990, 130.7 },
        { 1991, 136.2 },
        { 1992, 140.3 },
        { 1993, 144.5 },
        { 1994, 148.2 },
        { 1995, 152.4 },
        { 1996, 156.9 },
        { 1997, 160.5 },
        { 1998, 163.0 },
        { 1999, 166.6 },
        { 2000, 172.2 },
        { 2001, 177.1 },
        { 2002, 179.9 },
        { 2003, 184.0 },
        { 2004, 188.9 },
        { 2005, 195.3 },
        { 2006, 201.6 },
        { 2007, 207.3 },
        { 2008, 215.3 },
        { 2009, 214.5 },
        { 2010, 218.1 },
        { 2011, 224.9 },
        { 2012, 229.6 },
        { 2013, 233.0 },
        { 2014, 236.7 },
        { 2015, 237.0 },
        { 2016, 240.0 },
        { 2017, 245.1 },
        { 2018, 251.1 },
        { 2019, 255.7 },
        { 2020, 258.8 },
        { 2021, 271.0 },
        { 2022, 292.7 },
        { 2023, 304.7 },
        { 2024, 314.4 },
        { 2025, 322.6 }, // Estimated based on June 2025 CPI data
    };

    /// <summary>
    /// Gets the minimum year available in the CPI data.
    /// </summary>
    public const int MinYear = 1970;

    /// <summary>
    /// Gets the maximum year available in the CPI data.
    /// </summary>
    public const int MaxYear = 2025;
}
