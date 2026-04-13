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
        // Preserve directory structure if provided
        var directory = Path.GetDirectoryName(fileName);
        var file = Path.GetFileName(fileName);

        // Check if the filename already looks like a GUID-based name (32 hex chars followed by extension or _preview suffix)
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
        var isGuidBased = fileNameWithoutExt != null &&
                         (System.Text.RegularExpressions.Regex.IsMatch(fileNameWithoutExt, @"^[a-f0-9]{32}$") ||
                          System.Text.RegularExpressions.Regex.IsMatch(fileNameWithoutExt, @"^[a-f0-9]{32}_preview$"));

        // Only generate a new name if the provided name isn't already GUID-based
        var uniqueFileName = isGuidBased ? file : GenerateRandomFileName(file);

        string relativePath;
        string fullPath;

        // If showcase ID is provided, create a subfolder for it
        if (showcaseId.HasValue)
        {
            var showcaseFolder = showcaseId.Value.ToString();
            if (!string.IsNullOrEmpty(directory))
            {
                var fullDirectory = Path.Combine(_basePath, showcaseFolder, directory);
                if (!Directory.Exists(fullDirectory))
                {
                    Directory.CreateDirectory(fullDirectory);
                }

                relativePath = Path.Combine(showcaseFolder, directory, uniqueFileName);
                fullPath = Path.Combine(fullDirectory, uniqueFileName);
            }
            else
            {
                var fullDirectory = Path.Combine(_basePath, showcaseFolder);
                if (!Directory.Exists(fullDirectory))
                {
                    Directory.CreateDirectory(fullDirectory);
                }

                relativePath = Path.Combine(showcaseFolder, uniqueFileName);
                fullPath = Path.Combine(fullDirectory, uniqueFileName);
            }
        }
        else if (!string.IsNullOrEmpty(directory))
        {
            var fullDirectory = Path.Combine(_basePath, directory);
            if (!Directory.Exists(fullDirectory))
            {
                Directory.CreateDirectory(fullDirectory);
            }

            relativePath = Path.Combine(directory, uniqueFileName);
            fullPath = Path.Combine(fullDirectory, uniqueFileName);
        }
        else
        {
            relativePath = uniqueFileName;
            fullPath = Path.Combine(_basePath, uniqueFileName);
        }

        await File.WriteAllBytesAsync(fullPath, fileContent, cancellationToken);

        return relativePath;
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType, long? showcaseId = null, CancellationToken cancellationToken = default)
    {
        // Preserve directory structure if provided
        var directory = Path.GetDirectoryName(fileName);
        var file = Path.GetFileName(fileName);

        // Check if the filename already looks like a GUID-based name (32 hex chars followed by extension or _preview suffix)
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
        var isGuidBased = fileNameWithoutExt != null &&
                         (System.Text.RegularExpressions.Regex.IsMatch(fileNameWithoutExt, @"^[a-f0-9]{32}$") ||
                          System.Text.RegularExpressions.Regex.IsMatch(fileNameWithoutExt, @"^[a-f0-9]{32}_preview$"));

        // Only generate a new name if the provided name isn't already GUID-based
        var uniqueFileName = isGuidBased ? file : GenerateRandomFileName(file);

        string relativePath;
        string fullPath;

        // If showcase ID is provided, create a subfolder for it
        if (showcaseId.HasValue)
        {
            var showcaseFolder = showcaseId.Value.ToString();
            if (!string.IsNullOrEmpty(directory))
            {
                var fullDirectory = Path.Combine(_basePath, showcaseFolder, directory);
                if (!Directory.Exists(fullDirectory))
                {
                    Directory.CreateDirectory(fullDirectory);
                }

                relativePath = Path.Combine(showcaseFolder, directory, uniqueFileName);
                fullPath = Path.Combine(fullDirectory, uniqueFileName);
            }
            else
            {
                var fullDirectory = Path.Combine(_basePath, showcaseFolder);
                if (!Directory.Exists(fullDirectory))
                {
                    Directory.CreateDirectory(fullDirectory);
                }

                relativePath = Path.Combine(showcaseFolder, uniqueFileName);
                fullPath = Path.Combine(fullDirectory, uniqueFileName);
            }
        }
        else if (!string.IsNullOrEmpty(directory))
        {
            var fullDirectory = Path.Combine(_basePath, directory);
            if (!Directory.Exists(fullDirectory))
            {
                Directory.CreateDirectory(fullDirectory);
            }

            relativePath = Path.Combine(directory, uniqueFileName);
            fullPath = Path.Combine(fullDirectory, uniqueFileName);
        }
        else
        {
            relativePath = uniqueFileName;
            fullPath = Path.Combine(_basePath, uniqueFileName);
        }

        using (var fileStreamOutput = new FileStream(fullPath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fileStreamOutput, cancellationToken);
        }

        return relativePath;
    }

    public async Task<byte[]?> GetFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, filePath);

            if (!File.Exists(fullPath))
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
            var fullPath = Path.Combine(_basePath, filePath);

            if (File.Exists(fullPath))
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
                var fullPath = Path.Combine(_basePath, filePath);

                if (!File.Exists(fullPath))
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
            var fullPath = Path.Combine(_basePath, filePath);
            return await Task.FromResult(File.Exists(fullPath));
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
            var fullPath = Path.Combine(_basePath, filePath);

            if (!File.Exists(fullPath))
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

    private static string GenerateRandomFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var randomName = Guid.NewGuid().ToString("N");
        return $"{randomName}{extension}";
    }
}
