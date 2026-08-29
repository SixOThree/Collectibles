namespace Collectibles.Domain.Common;

/// <summary>
/// Reduces a caller-supplied file name to a single, storage-safe path segment.
/// Upload commands run every client-supplied name through this before it reaches
/// storage so that directory separators and traversal markers can never influence
/// the path a provider writes to.
/// </summary>
public static class SafeFileName
{
    private const string Fallback = "file";

    /// <summary>
    /// Strips any directory component and replaces characters that are invalid in a
    /// file name (including separators and traversal markers).
    /// </summary>
    /// <param name="fileName">The client-supplied name; may be null, empty, or rooted.</param>
    /// <returns>A non-empty single path segment safe to combine with a storage root.</returns>
    public static string Sanitize(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Fallback;
        }

        // Take the leaf only: anything before the last separator is discarded.
        var leaf = fileName;
        var lastSeparator = leaf.LastIndexOfAny(['/', '\\', ':']);
        if (lastSeparator >= 0)
        {
            leaf = leaf[(lastSeparator + 1)..];
        }

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string([.. leaf.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c)]).Trim();

        // "." and ".." are legal file-name characters but never legal names.
        if (cleaned.Length == 0 || cleaned.All(c => c == '.'))
        {
            return Fallback;
        }

        return cleaned;
    }
}
