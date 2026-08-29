namespace Collectibles.Domain.Interfaces;

/// <summary>
/// Represents a blob/file in storage.
/// </summary>
public record StorageBlobInfo(string Name, long SizeBytes);

/// <summary>
/// Interface for file storage operations.
/// </summary>
public interface IFileStorage
{
    Task<string> SaveFileAsync(byte[] fileContent, string fileName, string contentType, long? showcaseId = null, CancellationToken cancellationToken = default);
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType, long? showcaseId = null, CancellationToken cancellationToken = default);
    Task<byte[]?> GetFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<Stream?> GetFileStreamAsync(string filePath, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<long?> GetFileSizeAsync(string filePath, CancellationToken cancellationToken = default);
    Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all blob/file names in storage. Returns an empty list if the provider does not support listing.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task<List<StorageBlobInfo>> ListBlobsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether indicates whether this storage provider supports direct client uploads via SAS URLs.
    /// </summary>
    bool SupportsDirectUpload { get; }

    /// <summary>
    /// Generates a blob name/path for a file without actually uploading it.
    /// Used for direct upload scenarios where the client uploads directly to storage.
    /// </summary>
    /// <param name="fileName">The original file name.</param>
    /// <param name="showcaseId">Optional showcase ID for folder organization.</param>
    /// <returns>The generated blob path that can be used for direct upload.</returns>
    string GenerateBlobName(string fileName, long? showcaseId = null);

    /// <summary>
    /// Generates a SAS URL with write permissions for direct client upload.
    /// </summary>
    /// <param name="blobName">The blob name/path to upload to (from GenerateBlobName).</param>
    /// <param name="expiry">How long the SAS URL should be valid.</param>
    /// <param name="contentType">The expected content type of the file.</param>
    /// <returns>A fully qualified URL with SAS token for direct upload.</returns>
    string GenerateUploadSasUrl(string blobName, TimeSpan expiry, string contentType);
}
