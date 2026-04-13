using System.Text.Json.Serialization;
using Collectibles.Domain.Common.Enums;

namespace Collectibles.SyncTool.Models;

public record InitiateUploadRequest
{
    [JsonPropertyName("fileName")]
    public required string FileName { get; init; }

    [JsonPropertyName("fileSize")]
    public required long FileSize { get; init; }

    [JsonPropertyName("contentType")]
    public required string ContentType { get; init; }

    [JsonPropertyName("showcaseHashId")]
    public string? ShowcaseHashId { get; init; }
}

public record CompleteUploadRequest
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

    [JsonPropertyName("showcaseHashId")]
    public string? ShowcaseHashId { get; init; }
}

public record CompleteUploadResponse
{
    [JsonPropertyName("attachmentId")]
    public long AttachmentId { get; init; }
}

public record AttachmentContextResponse
{
    [JsonPropertyName("attachmentHashId")]
    public string? AttachmentHashId { get; init; }

    [JsonPropertyName("collectibleItemHashId")]
    public string? CollectibleItemHashId { get; init; }

    [JsonPropertyName("itemName")]
    public string? ItemName { get; init; }

    [JsonPropertyName("itemPath")]
    public string? ItemPath { get; init; }

    [JsonPropertyName("otherAttachmentCount")]
    public int OtherAttachmentCount { get; init; }

    [JsonPropertyName("childItemCount")]
    public int ChildItemCount { get; init; }

    [JsonPropertyName("hasDescription")]
    public bool HasDescription { get; init; }

    [JsonPropertyName("hasCustomFields")]
    public bool HasCustomFields { get; init; }

    [JsonPropertyName("hasTags")]
    public bool HasTags { get; init; }

    [JsonPropertyName("hasExternalLinks")]
    public bool HasExternalLinks { get; init; }

    [JsonPropertyName("hasQrCode")]
    public bool HasQrCode { get; init; }

    [JsonPropertyName("hasAdditionalData")]
    public bool HasAdditionalData { get; init; }
}
