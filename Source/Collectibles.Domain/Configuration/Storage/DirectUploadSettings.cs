namespace Collectibles.Domain.Configuration.Storage;

/// <summary>
/// Settings for direct-to-storage uploads (bypassing the server for large files).
/// </summary>
public class DirectUploadSettings
{
    /// <summary>
    /// Whether direct upload is enabled. Requires Azure Blob Storage.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Files larger than this threshold (in bytes) will use direct upload.
    /// Files smaller will use the traditional server-side upload.
    /// Default: 50MB (52428800 bytes)
    /// </summary>
    public long ThresholdBytes { get; set; } = 50 * 1024 * 1024;

    /// <summary>
    /// How long the SAS URL should be valid (in minutes).
    /// Default: 30 minutes
    /// </summary>
    public int SasExpiryMinutes { get; set; } = 30;
}
