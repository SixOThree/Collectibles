using System.Reflection;
using Collectibles.Domain.Constants;
using Collectibles.Domain.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace Collectibles.Infrastructure.Services;

/// <summary>
/// Service for retrieving application version information.
/// </summary>
public class VersionService : IVersionService
{
    private readonly IWebHostEnvironment _environment;
    private readonly Assembly _assembly;
    private readonly DateTime _buildTimestamp;

    public VersionService(IWebHostEnvironment environment)
    {
        _environment = environment;
        _assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

        // Calculate build timestamp from assembly file
        var assemblyPath = _assembly.Location;
        if (!string.IsNullOrEmpty(assemblyPath) && File.Exists(assemblyPath))
        {
            _buildTimestamp = File.GetLastWriteTime(assemblyPath);
        }
        else
        {
            _buildTimestamp = DateTime.UtcNow;
        }
    }

    /// <inheritdoc />
    public string GetAssemblyVersion()
    {
        var version = _assembly.GetName().Version;
        return version != null
            ? $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}"
            : ApplicationConstants.Version.DefaultVersion;
    }

    /// <inheritdoc />
    public string GetFileVersion()
    {
        var fileVersionAttribute = _assembly
            .GetCustomAttribute<AssemblyFileVersionAttribute>();

        return fileVersionAttribute?.Version ?? GetAssemblyVersion();
    }

    /// <inheritdoc />
    public DateTime GetBuildTimestamp()
    {
        return _buildTimestamp;
    }

    /// <inheritdoc />
    public string GetEnvironmentName()
    {
        return _environment.EnvironmentName ?? ApplicationConstants.Version.UnknownEnvironment;
    }

    /// <inheritdoc />
    public string GetFormattedVersionInfo()
    {
        var version = GetAssemblyVersion();
        var buildDateTime = GetBuildTimestamp().ToUniversalTime();
        var buildDateTimeFormatted = buildDateTime.ToString("MMM dd, yyyy HH:mm") + " UTC";
        var environment = GetEnvironmentName();

        return $"v{version} | Built: {buildDateTimeFormatted} | {environment}";
    }
}
