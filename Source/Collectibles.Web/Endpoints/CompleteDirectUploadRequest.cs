using System.Text.Json.Serialization;
using Collectibles.Domain.Common.Enums;

namespace Collectibles.Web.Endpoints;

public record CompleteDirectUploadRequest
{
    [JsonPropertyName("uploadId")]
    public required string UploadId { get; init; }

    [JsonPropertyName("blobName")]
    public required string BlobName { get; init; }

    [JsonPropertyName("originalFileName")]
    public required string OriginalFileName { get; init; }

    [JsonPropertyName("contentType")]
    public required string ContentType { get; init; }

    [JsonPropertyName("fileSize")]
    public required long FileSize { get; init; }

    [JsonPropertyName("attachmentType")]
    public AttachmentType? AttachmentType { get; init; }

    [JsonPropertyName("showcaseId")]
    public long? ShowcaseId { get; init; }

    [JsonPropertyName("showcaseHashId")]
    public string? ShowcaseHashId { get; init; }
}
