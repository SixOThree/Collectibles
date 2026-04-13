using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;

namespace Collectibles.SyncTool.Services;

public class FileHashService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".tiff", ".tif", ".svg",
        ".mp4", ".mov", ".avi", ".wmv", ".mkv",
        ".mp3", ".wav", ".flac",
        ".pdf", ".zip", ".rar", ".7z",
    };

    private readonly ConcurrentDictionary<string, (string Hash, long Size, DateTime LastWriteUtc)> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Scans a folder recursively and computes SHA256 hashes for all supported files.
    /// Uses a cache keyed by full path, size, and last-write time to skip unchanged files.
    /// </summary>
    public async Task<Dictionary<string, (string Hash, long Size, string FullPath)>> HashFilesAsync(
        string folderPath,
        IProgress<(int processed, int total)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var files = Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories)
            .Where(f => SupportedExtensions.Contains(Path.GetExtension(f)))
            .ToList();

        var results = new Dictionary<string, (string Hash, long Size, string FullPath)>(StringComparer.OrdinalIgnoreCase);
        var processed = 0;

        foreach (var filePath in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(folderPath, filePath);
            var fileInfo = new FileInfo(filePath);
            var hash = GetOrComputeHashAsync(filePath, fileInfo, cancellationToken);

            results[relativePath] = (await hash, fileInfo.Length, filePath);

            processed++;
            progress?.Report((processed, files.Count));
        }

        return results;
    }

    private async Task<string> GetOrComputeHashAsync(string filePath, FileInfo fileInfo, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(filePath, out var cached)
            && cached.Size == fileInfo.Length
            && cached.LastWriteUtc == fileInfo.LastWriteTimeUtc)
        {
            return cached.Hash;
        }

        var hash = await ComputeHashAsync(filePath, cancellationToken);
        _cache[filePath] = (hash, fileInfo.Length, fileInfo.LastWriteTimeUtc);
        return hash;
    }

    private static async Task<string> ComputeHashAsync(string filePath, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexStringLower(hashBytes);
    }
}
