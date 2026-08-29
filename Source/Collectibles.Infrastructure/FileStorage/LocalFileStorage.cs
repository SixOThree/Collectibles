using Collectibles.Domain.Configuration.Storage;
using Collectibles.Domain.Interfaces;

using Microsoft.Extensions.Options;

namespace Collectibles.Infrastructure.FileStorage;

public class LocalFileStorage : IFileStorage
{
    private readonly LocalFileSystemSettings _settings;
    private readonly string _basePath;

    public LocalFileStorage(IOptions<StorageSettings> storageOptions)
    {
        _settings = storageOptions.Value.LocalFileSystem
            ?? throw new ArgumentException("Local file system settings are not configured");

        _basePath = _settings.UseAbsolutePath
            ? _settings.BasePath
            : Path.Combine(Directory.GetCurrentDirectory(), _settings.BasePath);

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> SaveFileAsync(byte[] fileContent, string fileName, string contentType, long? showcaseId = null, CancellationToken cancellationToken = default)
    {
        var (relativePath, fullPath) = PrepareTarget(fileName, showcaseId);

        await File.WriteAllBytesAsync(fullPath, fileContent, cancellationToken);

        return relativePath;
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType, long? showcaseId = null, CancellationToken cancellationToken = default)
    {
        var (relativePath, fullPath) = PrepareTarget(fileName, showcaseId);

        using (var fileStreamOutput = new FileStream(fullPath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fileStreamOutput, cancellationToken);
        }

        return relativePath;
    }

    /// <summary>
    /// Resolves the storage-relative and canonical full paths for a save, creating the
    /// target directory. Throws if the caller-supplied name would escape the storage root.
    /// </summary>
    private (string RelativePath, string FullPath) PrepareTarget(string fileName, long? showcaseId)
    {
        var relativePath = StoragePathBuilder.BuildRelativePath(fileName, showcaseId, Path.DirectorySeparatorChar);
        var fullPath = StoragePathBuilder.ResolveContainedPath(_basePath, relativePath);

        var fullDirectory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(fullDirectory) && !Directory.Exists(fullDirectory))
        {
            Directory.CreateDirectory(fullDirectory);
        }

        return (relativePath, fullPath);
    }

    public async Task<byte[]?> GetFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = StoragePathBuilder.TryResolveContainedPath(_basePath, filePath);

            if (fullPath is null || !File.Exists(fullPath))
            {
                return null;
            }

            return await File.ReadAllBytesAsync(fullPath, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = StoragePathBuilder.TryResolveContainedPath(_basePath, filePath);

            if (fullPath is not null && File.Exists(fullPath))
            {
                await Task.Run(() => File.Delete(fullPath), cancellationToken);
            }
        }
        catch
        {
            // Silently handle deletion errors
        }
    }

    public async Task<Stream?> GetFileStreamAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(
            () =>
        {
            try
            {
                var fullPath = StoragePathBuilder.TryResolveContainedPath(_basePath, filePath);

                if (fullPath is null || !File.Exists(fullPath))
                {
                    return null;
                }

                return File.OpenRead(fullPath);
            }
            catch
            {
                return null;
            }
        }, cancellationToken);
    }

    public async Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = StoragePathBuilder.TryResolveContainedPath(_basePath, filePath);
            return await Task.FromResult(fullPath is not null && File.Exists(fullPath));
        }
        catch
        {
            return false;
        }
    }

    public async Task<long?> GetFileSizeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = StoragePathBuilder.TryResolveContainedPath(_basePath, filePath);

            if (fullPath is null || !File.Exists(fullPath))
            {
                return null;
            }

            var fileInfo = new FileInfo(fullPath);
            return await Task.FromResult(fileInfo.Length);
        }
        catch
        {
            return null;
        }
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
        throw new NotSupportedException("Local file storage does not support direct uploads. Files must be uploaded through the server.");
    }

    /// <inheritdoc />
    public string GenerateUploadSasUrl(string blobName, TimeSpan expiry, string contentType)
    {
        throw new NotSupportedException("Local file storage does not support direct uploads. Files must be uploaded through the server.");
    }
}
