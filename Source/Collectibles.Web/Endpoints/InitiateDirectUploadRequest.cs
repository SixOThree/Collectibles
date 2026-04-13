using System.Text.Json.Serialization;

namespace Collectibles.Web.Endpoints;

public record InitiateDirectUploadRequest
{
    [JsonPropertyName("fileName")]
    public required string FileName { get; init; }

    [JsonPropertyName("fileSize")]
    public required long FileSize { get; init; }

    [JsonPropertyName("contentType")]
    public required string ContentType { get; init; }

    [JsonPropertyName("showcaseId")]
    public long? ShowcaseId { get; init; }

    [JsonPropertyName("showcaseHashId")]
    public string? ShowcaseHashId { get; init; }
}
