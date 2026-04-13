using Collectibles.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Collectibles.Infrastructure.FileStorage;

public class ScopedDatabaseFileStorage : IFileStorage
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ScopedDatabaseFileStorage(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<string> SaveFileAsync(byte[] fileContent, string fileName, string contentType, long? showcaseId = null, CancellationToken cancellationToken = default)
    {
        // For database storage, we don't actually store anything here
        // The CreateAttachmentCommand handles the database storage directly
        // We just return a placeholder value that indicates database storage
        return await Task.FromResult("db-storage");
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType, long? showcaseId = null, CancellationToken cancellationToken = default)
    {
        // For database storage, we don't actually store anything here
        // The CreateAttachmentCommand handles the database storage directly
        // We just return a placeholder value that indicates database storage
        return await Task.FromResult("db-storage");
    }

    public async Task<byte[]?> GetFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // For database storage, the filePath is "db-storage" and we need the attachment ID from context
        // This method is typically called from GetAttachmentForDownloadQuery which has the attachment ID
        // Since we can't get the ID from filePath alone, this implementation won't work properly
        // The proper solution would be to refactor the storage interface to include attachment ID
        return await Task.FromResult<byte[]?>(null);
    }

    public async Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // For database storage, deletion is handled when the Attachment entity is deleted
        // due to cascade delete configuration
        await Task.CompletedTask;
    }

    public async Task<long?> GetFileSizeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // For database storage, we can't determine file size from just the path
        // The actual file size is stored in the Attachment entity's FileSize property
        // This would require the attachment ID which isn't available in the filePath
        // Return null to indicate the file wasn't found in external storage
        return await Task.FromResult<long?>(null);
    }

    public async Task<Stream?> GetFileStreamAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // For database storage, streaming isn't supported in the same way
        // Return null as this storage type doesn't support external file streaming
        return await Task.FromResult<Stream?>(null);
    }

    public async Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // For database storage, we can't check file existence from just the path
        // Return false as this storage type doesn't support external file checking
        return await Task.FromResult(false);
    }

    public Task<List<StorageBlobInfo>> ListBlobsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<StorageBlobInfo>());
    }

    /// <inheritdoc />
    public bool SupportsDirectUpload => false;

    /// <inheritdoc />
    public string GenerateBlobName(string fileName, long? showcaseId = null)
    {
        throw new NotSupportedException("Database storage does not support direct uploads. Files must be uploaded through the server.");
    }

    /// <inheritdoc />
    public string GenerateUploadSasUrl(string blobName, TimeSpan expiry, string contentType)
    {
        throw new NotSupportedException("Database storage does not support direct uploads. Files must be uploaded through the server.");
    }
}
