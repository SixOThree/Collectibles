namespace Collectibles.Domain.Interfaces;

/// <summary>
/// Service interface for retrieving application version information.
/// </summary>
public interface IVersionService
{
    /// <summary>
    /// Gets the application assembly version.
    /// </summary>
    string GetAssemblyVersion();

    /// <summary>
    /// Gets the application file version.
    /// </summary>
    string GetFileVersion();

    /// <summary>
    /// Gets the application build timestamp.
    /// </summary>
    DateTime GetBuildTimestamp();

    /// <summary>
    /// Gets the current environment name.
    /// </summary>
    string GetEnvironmentName();

    /// <summary>
    /// Gets a formatted version string combining multiple version attributes.
    /// </summary>
    string GetFormattedVersionInfo();
}