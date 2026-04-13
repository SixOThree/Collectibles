using Collectibles.Application.Features.Sync.Queries;
using Collectibles.SyncTool.Models;

namespace Collectibles.SyncTool.Services;

public class SyncComparisonService
{
    /// <summary>
    /// Compares local files against the server manifest and classifies each entry.
    /// </summary>
    public List<SyncItem> Compare(
        Dictionary<string, (string Hash, long Size, string FullPath)> localFiles,
        List<ShowcaseManifestItemDto> serverManifest)
    {
        var results = new List<SyncItem>();
        var matchedServerEntries = new HashSet<string>();

        // Build server lookup by hash (may have multiple entries per hash now)
        var serverByHash = serverManifest
            .Where(s => !string.IsNullOrEmpty(s.ContentHash))
            .GroupBy(s => s.ContentHash!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        // Fallback: by name+size for entries with no hash
        var serverByNameSize = serverManifest
            .Where(s => string.IsNullOrEmpty(s.ContentHash) && !string.IsNullOrEmpty(s.OriginalFilename))
            .ToDictionary(s => $"{s.OriginalFilename}|{s.FileSize}", StringComparer.OrdinalIgnoreCase);

        foreach (var (relativePath, (hash, size, fullPath)) in localFiles)
        {
            var normalizedPath = relativePath.Replace('\\', '/');
            var pathParts = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var fileName = pathParts[^1];
            var folderSegments = pathParts.Length > 1 ? pathParts[..^1] : Array.Empty<string>();

            if (serverByHash.TryGetValue(hash, out var hashMatches))
            {
                // Check for exact path match first
                var exactMatch = hashMatches.FirstOrDefault(s =>
                    s.ItemPathSegments != null
                    && s.ItemPathSegments.Length == folderSegments.Length
                    && s.ItemPathSegments.Zip(folderSegments).All(pair =>
                        string.Equals(pair.First, pair.Second, StringComparison.OrdinalIgnoreCase)));

                if (exactMatch != null)
                {
                    results.Add(new SyncItem
                    {
                        LocalFilePath = fullPath,
                        LocalFileName = relativePath,
                        LocalContentHash = hash,
                        LocalFileSize = size,
                        ServerFileName = exactMatch.OriginalFilename,
                        ServerContentHash = exactMatch.ContentHash,
                        ServerFileSize = exactMatch.FileSize,
                        ItemPath = exactMatch.ItemPath,
                        AttachmentHashId = exactMatch.AttachmentHashId,
                        AttachmentType = exactMatch.AttachmentType,
                        Status = SyncStatus.Matched
                    });
                    matchedServerEntries.Add(exactMatch.AttachmentHashId ?? "");
                }
                else
                {
                    var firstMatch = hashMatches.First();
                    results.Add(new SyncItem
                    {
                        LocalFilePath = fullPath,
                        LocalFileName = relativePath,
                        LocalContentHash = hash,
                        LocalFileSize = size,
                        ServerFileName = firstMatch.OriginalFilename,
                        ServerContentHash = firstMatch.ContentHash,
                        ServerFileSize = firstMatch.FileSize,
                        ItemPath = firstMatch.ItemPath,
                        AttachmentHashId = firstMatch.AttachmentHashId,
                        AttachmentType = firstMatch.AttachmentType,
                        Status = SyncStatus.MovedCopied
                    });
                    matchedServerEntries.Add(firstMatch.AttachmentHashId ?? "");
                }
            }
            else if (serverByNameSize.TryGetValue($"{fileName}|{size}", out var nameMatch))
            {
                results.Add(new SyncItem
                {
                    LocalFilePath = fullPath,
                    LocalFileName = relativePath,
                    LocalContentHash = hash,
                    LocalFileSize = size,
                    ServerFileName = nameMatch.OriginalFilename,
                    ServerContentHash = nameMatch.ContentHash,
                    ServerFileSize = nameMatch.FileSize,
                    ItemPath = nameMatch.ItemPath,
                    AttachmentHashId = nameMatch.AttachmentHashId,
                    AttachmentType = nameMatch.AttachmentType,
                    Status = SyncStatus.Matched
                });
                matchedServerEntries.Add(nameMatch.AttachmentHashId ?? "");
            }
            else
            {
                results.Add(new SyncItem
                {
                    LocalFilePath = fullPath,
                    LocalFileName = relativePath,
                    LocalContentHash = hash,
                    LocalFileSize = size,
                    Status = SyncStatus.ToUpload
                });
            }
        }

        // Server-only entries
        foreach (var entry in serverManifest)
        {
            if (!matchedServerEntries.Contains(entry.AttachmentHashId ?? ""))
            {
                results.Add(new SyncItem
                {
                    ServerFileName = entry.OriginalFilename,
                    ServerContentHash = entry.ContentHash,
                    ServerFileSize = entry.FileSize,
                    ItemPath = entry.ItemPath,
                    AttachmentHashId = entry.AttachmentHashId,
                    AttachmentType = entry.AttachmentType,
                    Status = SyncStatus.ServerOnly
                });
            }
        }

        return results;
    }
}
