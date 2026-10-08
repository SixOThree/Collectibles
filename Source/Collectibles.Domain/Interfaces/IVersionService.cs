namespace Collectibles.Domain.Interfaces;

/// <summary>
/// Service interface for retrieving application version information.
/// </summary>
public interface IVersionService
{
    /// <summary>
    /// Gets the application assembly version.
    /// </summary>
    /// <returns></returns>
    string GetAssemblyVersion();

    /// <summary>
    /// Gets the application file version.
    /// </summary>
    /// <returns></returns>
    string GetFileVersion();

    /// <summary>
    /// Gets the application build timestamp.
    /// </summary>
    /// <returns></returns>
    DateTime GetBuildTimestamp();

    /// <summary>
    /// Gets the current environment name.
    /// </summary>
    /// <returns></returns>
    string GetEnvironmentName();

    /// <summary>
    /// Gets a formatted version string combining multiple version attributes.
    /// </summary>
    /// <returns></returns>
    string GetFormattedVersionInfo();
}
