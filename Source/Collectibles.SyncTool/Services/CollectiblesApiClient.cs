using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Collectibles.Application.Features.Attachments.Commands;
using Collectibles.Application.Features.Sync.Queries;
using Collectibles.SyncTool.Models;

namespace Collectibles.SyncTool.Services;

public class CollectiblesApiClient
{
    private const int BlockSize = 8 * 1024 * 1024; // 8 MB
    private const long BlockUploadThreshold = 200 * 1024 * 1024; // 200 MB

    private readonly HttpClient _httpClient;
    private readonly HttpClient _azureClient; // Separate client for Azure (no API key header)
    private string _baseUrl = string.Empty;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private static readonly Dictionary<string, string> ContentTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".bmp"] = "image/bmp",
        [".tiff"] = "image/tiff",
        [".tif"] = "image/tiff",
        [".svg"] = "image/svg+xml",
        [".mp4"] = "video/mp4",
        [".mov"] = "video/quicktime",
        [".avi"] = "video/x-msvideo",
        [".wmv"] = "video/x-ms-wmv",
        [".mkv"] = "video/x-matroska",
        [".mp3"] = "audio/mpeg",
        [".wav"] = "audio/wav",
        [".flac"] = "audio/flac",
        [".pdf"] = "application/pdf",
        [".zip"] = "application/zip",
        [".rar"] = "application/x-rar-compressed",
        [".7z"] = "application/x-7z-compressed",
    };

    public CollectiblesApiClient(HttpClient httpClient, HttpClient azureClient)
    {
        _httpClient = httpClient;
        _azureClient = azureClient;
    }

    public void Configure(string baseUrl, string apiKey)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient.DefaultRequestHeaders.Remove("X-Api-Key");
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }

    private string Url(string path) => $"{_baseUrl}{path}";

    public async Task<List<ShowcaseManifestItemDto>> GetManifestAsync(
        string showcaseHashId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync(Url($"/api/sync/manifest/{showcaseHashId}"), ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<ShowcaseManifestItemDto>>(JsonOptions, ct);
        return result ?? [];
    }

    public async Task<DirectUploadInitiation> InitiateUploadAsync(
        InitiateUploadRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync(Url("/api/attachments/initiate-upload"), request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<DirectUploadInitiation>(JsonOptions, ct);
        return result ?? throw new InvalidOperationException("Empty response from initiate-upload.");
    }

    public async Task<long> CompleteUploadAsync(
        CompleteUploadRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync(Url("/api/attachments/complete-upload"), request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CompleteUploadResponse>(JsonOptions, ct);
        return result?.AttachmentId ?? 0;
    }

    public async Task<AttachmentContextResponse?> GetAttachmentContextAsync(
        string attachmentHashId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync(Url($"/api/attachments/{attachmentHashId}/context"), ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AttachmentContextResponse>(JsonOptions, ct);
    }

    public async Task<bool> DeleteCollectibleItemAsync(
        string itemHashId, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsync(Url($"/api/collectible-items/{itemHashId}/delete"), null, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task DeleteAttachmentAsync(string attachmentHashId, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsync(Url($"/api/attachments/{attachmentHashId}/delete"), null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task MoveAttachmentAsync(
        string attachmentHashId, string relativePath, string showcaseHashId, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            Url($"/api/sync/attachments/{attachmentHashId}/move"),
            new { relativePath, showcaseHashId },
            JsonOptions,
            ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<byte[]?> GetAttachmentThumbnailAsync(
        string attachmentHashId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                Url($"/api/attachments/{attachmentHashId}/thumbnail"), ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch
        {
            return null;
        }
    }

    public async Task<byte[]?> GetAttachmentDownloadAsync(
        string attachmentHashId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                Url($"/api/attachments/{attachmentHashId}/download"), ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch
        {
            return null;
        }
    }

    public async Task<SyncUploadInitiationResult> InitiateSyncUploadAsync(
        string showcaseHashId, string relativePath, string contentHash,
        long fileSize, string contentType, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/sync/upload",
            new
            {
                ShowcaseHashId = showcaseHashId,
                RelativePath = relativePath,
                ContentHash = contentHash,
                FileSize = fileSize,
                ContentType = contentType
            }, ct);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SyncUploadInitiationResult>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Failed to deserialize sync upload response.");
    }

    public async Task<long> CompleteSyncUploadAsync(
        string uploadId, string blobName, string originalFileName,
        string contentType, long fileSize, long targetItemId,
        string showcaseHashId, string? contentHash,
        string? attachmentType, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/sync/upload/complete",
            new
            {
                UploadId = uploadId,
                BlobName = blobName,
                OriginalFileName = originalFileName,
                ContentType = contentType,
                FileSize = fileSize,
                TargetItemId = targetItemId,
                ShowcaseHashId = showcaseHashId,
                ContentHash = contentHash,
                AttachmentTypeString = attachmentType
            }, ct);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return result.GetProperty("attachmentId").GetInt64();
    }

    /// <summary>
    /// Uploads a file to Azure Blob Storage via SAS URL.
    /// Uses block upload for files larger than 200MB.
    /// </summary>
    public async Task UploadToAzureAsync(
        string sasUrl,
        string filePath,
        string contentType,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var fileInfo = new FileInfo(filePath);

        if (fileInfo.Length > BlockUploadThreshold)
        {
            await BlockUploadAsync(sasUrl, filePath, contentType, fileInfo.Length, progress, ct);
        }
        else
        {
            await SingleUploadAsync(sasUrl, filePath, contentType, fileInfo.Length, progress, ct);
        }
    }

    /// <summary>
    /// Full upload flow: initiate, upload to Azure, complete.
    /// </summary>
    public async Task UploadFileAsync(
        string filePath,
        string showcaseHashId,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var fileName = Path.GetFileName(filePath);
        var fileInfo = new FileInfo(filePath);
        var contentType = GetContentType(filePath);
        var attachmentType = GetAttachmentType(filePath);

        // Step 1: Initiate
        var initiation = await InitiateUploadAsync(new InitiateUploadRequest
        {
            FileName = fileName,
            FileSize = fileInfo.Length,
            ContentType = contentType,
            ShowcaseHashId = showcaseHashId,
        }, ct);

        // Step 2: Upload to Azure
        await UploadToAzureAsync(initiation.SasUrl, filePath, contentType, progress, ct);

        // Step 3: Complete
        await CompleteUploadAsync(new CompleteUploadRequest
        {
            UploadId = initiation.UploadId,
            BlobName = initiation.BlobName,
            OriginalFileName = fileName,
            ContentType = contentType,
            FileSize = fileInfo.Length,
            AttachmentType = attachmentType,
            ShowcaseHashId = showcaseHashId,
        }, ct);
    }

    private async Task SingleUploadAsync(
        string sasUrl, string filePath, string contentType, long fileSize,
        IProgress<double>? progress, CancellationToken ct)
    {
        var fileBytes = await File.ReadAllBytesAsync(filePath, ct);
        using var content = new ByteArrayContent(fileBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Headers.Add("x-ms-blob-type", "BlockBlob");

        var response = await _azureClient.PutAsync(sasUrl, content, ct);
        response.EnsureSuccessStatusCode();
        progress?.Report(1.0);
    }

    private async Task BlockUploadAsync(
        string sasUrl, string filePath, string contentType, long fileSize,
        IProgress<double>? progress, CancellationToken ct)
    {
        var blockIds = new List<string>();
        var blockIndex = 0;
        long totalUploaded = 0;

        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var buffer = new byte[BlockSize];

        while (true)
        {
            var bytesRead = await fileStream.ReadAsync(buffer, ct);
            if (bytesRead == 0)
            {
                break;
            }

            var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(blockIndex.ToString("D6")));
            blockIds.Add(blockId);

            var blockUrl = $"{sasUrl}&comp=block&blockid={Uri.EscapeDataString(blockId)}";
            using var blockContent = new ByteArrayContent(buffer, 0, bytesRead);
            blockContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var response = await _azureClient.PutAsync(blockUrl, blockContent, ct);
            response.EnsureSuccessStatusCode();

            totalUploaded += bytesRead;
            blockIndex++;
            progress?.Report((double)totalUploaded / fileSize);
        }

        // Commit block list
        var blockListXml = new XElement("BlockList",
            blockIds.Select(id => new XElement("Latest", id)));

        var commitUrl = $"{sasUrl}&comp=blocklist";
        using var commitContent = new StringContent(blockListXml.ToString(), Encoding.UTF8, "application/xml");
        commitContent.Headers.Add("x-ms-blob-content-type", contentType);

        var commitResponse = await _azureClient.PutAsync(commitUrl, commitContent, ct);
        commitResponse.EnsureSuccessStatusCode();

        progress?.Report(1.0);
    }

    public static string GetContentType(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return ContentTypeMap.GetValueOrDefault(ext, "application/octet-stream");
    }

    public static Domain.Common.Enums.AttachmentType GetAttachmentType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" or ".tiff" or ".tif" or ".svg"
                => Domain.Common.Enums.AttachmentType.Image,
            ".mp4" or ".mov" or ".avi" or ".wmv" or ".mkv"
                => Domain.Common.Enums.AttachmentType.Video,
            ".mp3" or ".wav" or ".flac"
                => Domain.Common.Enums.AttachmentType.Audio,
            ".pdf"
                => Domain.Common.Enums.AttachmentType.Document,
            ".zip" or ".rar" or ".7z"
                => Domain.Common.Enums.AttachmentType.Archive,
            _ => Domain.Common.Enums.AttachmentType.File,
        };
    }
}

public class SyncUploadInitiationResult
{
    public bool Skipped { get; set; }
    public long? AttachmentId { get; set; }
    public string? UploadId { get; set; }
    public string? SasUrl { get; set; }
    public string? BlobName { get; set; }
    public long? TargetItemId { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
