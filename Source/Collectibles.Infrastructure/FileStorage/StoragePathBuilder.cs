using System.Globalization;
using System.Text.RegularExpressions;

namespace Collectibles.Infrastructure.FileStorage;

/// <summary>
/// Shared path assembly and containment validation for the file storage providers.
/// The GUID-detection plus directory-preservation rules used to be copy-pasted into
/// every provider overload; centralizing them means a containment fix applies everywhere.
/// </summary>
internal static class StoragePathBuilder
{
    private static readonly Regex GuidBasedNamePattern = new(
        @"^[a-f0-9]{32}(_preview)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromSeconds(1));

    /// <summary>
    /// Builds the storage-relative path for a newly saved file, preserving any directory
    /// structure the caller supplied while rejecting anything that could escape the root.
    /// </summary>
    /// <param name="fileName">Caller-supplied name, optionally including a relative directory.</param>
    /// <param name="showcaseId">Optional showcase folder to nest the file under.</param>
    /// <param name="separator">Separator to join the resulting segments with.</param>
    /// <returns>A relative path containing no traversal or rooted segments.</returns>
    /// <exception cref="ArgumentException">The name is empty or contains traversal/rooted segments.</exception>
    public static string BuildRelativePath(string fileName, long? showcaseId, char separator)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name must be provided.", nameof(fileName));
        }

        var segments = new List<string>();

        if (showcaseId.HasValue)
        {
            segments.Add(showcaseId.Value.ToString(CultureInfo.InvariantCulture));
        }

        segments.AddRange(SafeDirectorySegments(Path.GetDirectoryName(fileName), nameof(fileName)));
        segments.Add(SafeLeafName(fileName));

        return string.Join(separator, segments);
    }

    /// <summary>
    /// Produces the stored name for a file, keeping an already-randomized name as-is and
    /// otherwise replacing the caller-supplied name with a server-generated one.
    /// </summary>
    public static string SafeLeafName(string fileName)
    {
        var leaf = Path.GetFileName(fileName);

        if (string.IsNullOrWhiteSpace(leaf))
        {
            throw new ArgumentException("File name must include a file component.", nameof(fileName));
        }

        var withoutExtension = Path.GetFileNameWithoutExtension(leaf);

        return GuidBasedNamePattern.IsMatch(withoutExtension)
            ? leaf
            : GenerateRandomFileName(leaf);
    }

    /// <summary>
    /// Validates a caller-supplied relative directory, returning its individual segments.
    /// </summary>
    /// <exception cref="ArgumentException">A segment is a traversal marker or the path is rooted.</exception>
    public static IReadOnlyList<string> SafeDirectorySegments(string? directory, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return [];
        }

        if (Path.IsPathRooted(directory) || directory.Contains(':', StringComparison.Ordinal))
        {
            throw new ArgumentException($"Rooted paths are not allowed: '{directory}'.", parameterName);
        }

        var segments = directory
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var segment in segments)
        {
            if (segment is "." or "..")
            {
                throw new ArgumentException($"Relative traversal is not allowed: '{directory}'.", parameterName);
            }
        }

        return segments;
    }

    /// <summary>
    /// Resolves a storage-relative path against a provider root and verifies the result
    /// stays inside that root after canonicalization.
    /// </summary>
    /// <returns>The canonical full path, or <c>null</c> if it would escape the root.</returns>
    public static string? TryResolveContainedPath(string basePath, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        try
        {
            var canonicalBase = Path.TrimEndingDirectorySeparator(Path.GetFullPath(basePath));
            var fullPath = Path.GetFullPath(Path.Combine(canonicalBase, relativePath));

            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            return fullPath.StartsWith(canonicalBase + Path.DirectorySeparatorChar, comparison)
                ? fullPath
                : null;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    /// <summary>
    /// Resolves a storage-relative path against a provider root, throwing when it escapes.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">The path resolves outside the root.</exception>
    public static string ResolveContainedPath(string basePath, string relativePath)
    {
        return TryResolveContainedPath(basePath, relativePath)
            ?? throw new UnauthorizedAccessException(
                $"The resolved path escapes the configured storage root: '{relativePath}'.");
    }

    private static string GenerateRandomFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var randomName = Guid.NewGuid().ToString("N");
        return $"{randomName}{extension}";
    }
}
