using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

using Collectibles.Domain.Configuration.Storage;
using Collectibles.Domain.Interfaces;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Collectibles.Infrastructure.FileStorage;

public class AzureBlobFileStorage : IFileStorage
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobContainerClient _containerClient;
    private readonly AzureBlobStorageSettings _settings;
    private readonly ILogger<AzureBlobFileStorage> _logger;

    public AzureBlobFileStorage(IOptions<StorageSettings> storageOptions, ILogger<AzureBlobFileStorage> logger)
    {
        _logger = logger;
        _settings = storageOptions.Value.AzureBlobStorage
            ?? throw new ArgumentException("Azure Blob Storage settings are not configured");

        ValidateConfiguration();

        _blobServiceClient = new BlobServiceClient(_settings.ConnectionString);
        _containerClient = _blobServiceClient.GetBlobContainerClient(_settings.ContainerName);

        if (_settings.CreateContainerIfNotExists)
        {
            // This is a blocking network round-trip in a constructor. It is acceptable
            // only because FileStorageFactory caches a single provider instance for the
            // process: when this type was rebuilt per DI scope it ran once per request
            // that touched storage.
            _containerClient.CreateIfNotExists(PublicAccessType.None);
        }
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_settings.ConnectionString))
        {
            throw new ArgumentException("Azure Blob Storage connection string is not configured");
        }

        if (string.IsNullOrWhiteSpace(_settings.ContainerName))
        {
            throw new ArgumentException("Azure Blob Storage container name is not configured");
        }

        if (!IsValidContainerName(_settings.ContainerName))
        {
            throw new ArgumentException($"Invalid container name: '{_settings.ContainerName}'. Container names must be 3-63 characters, lowercase letters, numbers, and hyphens only, cannot start or end with a hyphen, and cannot have consecutive hyphens.");
        }
    }

    private static bool IsValidContainerName(string containerName)
    {
        if (containerName.Length < 3 || containerName.Length > 63)
        {
            return false;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(containerName, @"^[a-z0-9]([a-z0-9-]*[a-z0-9])?$"))
        {
            return false;
        }

        if (containerName.Contains("--"))
        {
            return false;
        }

        return true;
    }

    public async Task<string> SaveFileAsync(byte[] fileContent, string fileName, string contentType, long? showcaseId = null, CancellationToken cancellationToken = default)
    {
        // Preserve directory structure if provided, rejecting traversal segments
        var blobName = BuildBlobPath(StoragePathBuilder.BuildRelativePath(fileName, showcaseId, '/'));

        var blobClient = _containerClient.GetBlobClient(blobName);

        using var stream = new MemoryStream(fileContent);
        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },

            // Configure transfer options for improved performance
            TransferOptions = new Azure.Storage.StorageTransferOptions
            {
                // Initial transfer size - start with 8MB chunks (increased from default 4MB)
                InitialTransferSize = 8 * 1024 * 1024,

                // Maximum transfer size - use 100MB chunks for large files (increased from default 4MB)
                MaximumTransferSize = 100 * 1024 * 1024,

                // Maximum concurrency - allow up to 8 parallel operations (increased from default 1)
                MaximumConcurrency = 8,
            },

            // Set transfer validation options to reduce overhead for large files
            TransferValidation = new Azure.Storage.UploadTransferValidationOptions
            {
                ChecksumAlgorithm = Azure.Storage.StorageChecksumAlgorithm.None, // Skip checksum for performance
            },
        };

        await blobClient.UploadAsync(stream, uploadOptions, cancellationToken);

        return blobName;
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType, long? showcaseId = null, CancellationToken cancellationToken = default)
    {
        // Preserve directory structure if provided, rejecting traversal segments
        var blobName = BuildBlobPath(StoragePathBuilder.BuildRelativePath(fileName, showcaseId, '/'));

        var blobClient = _containerClient.GetBlobClient(blobName);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },

            // Configure transfer options for improved performance
            TransferOptions = new Azure.Storage.StorageTransferOptions
            {
                // Initial transfer size - start with 8MB chunks (increased from default 4MB)
                InitialTransferSize = 8 * 1024 * 1024,

                // Maximum transfer size - use 100MB chunks for large files (increased from default 4MB)
                MaximumTransferSize = 100 * 1024 * 1024,

                // Maximum concurrency - allow up to 8 parallel operations (increased from default 1)
                MaximumConcurrency = 8,
            },

            // Set transfer validation options to reduce overhead for large files
            TransferValidation = new Azure.Storage.UploadTransferValidationOptions
            {
                ChecksumAlgorithm = Azure.Storage.StorageChecksumAlgorithm.None, // Skip checksum for performance
            },
        };

        await blobClient.UploadAsync(fileStream, uploadOptions, cancellationToken);

        return blobName;
    }

    public async Task<byte[]?> GetFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // Normalize the path to include subfolder if configured
        var normalizedPath = NormalizeBlobPath(filePath);

        try
        {
            var blobClient = _containerClient.GetBlobClient(normalizedPath);

            // Get properties to check existence and determine content length upfront.
            // This replaces ExistsAsync and ensures we always know the exact size.
            long contentLength;
            try
            {
                var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
                contentLength = properties.Value.ContentLength;
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("File not found at path: {Path}", normalizedPath);
                return null;
            }

            if (contentLength > int.MaxValue)
            {
                _logger.LogWarning("File too large to load into memory at path: {Path} ({ContentLength} bytes). Use GetFileStreamAsync instead.", normalizedPath, contentLength);
                return null;
            }

            // Use DownloadStreamingAsync and read directly into a pre-allocated byte array
            // to avoid MemoryStream buffer-doubling that causes "Stream was too long".
            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            using var stream = response.Value.Content;

            var buffer = new byte[contentLength];
            var totalRead = 0;
            while (totalRead < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                totalRead += read;
            }

            return buffer;
        }
        catch (Exception ex)
        {
            // Log the exception for debugging
            _logger.LogError(ex, "Error retrieving file at path: {Path}", normalizedPath);
            return null;
        }
    }

    public async Task<Stream?> GetFileStreamAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // Normalize the path to include subfolder if configured
        var normalizedPath = NormalizeBlobPath(filePath);

        try
        {
            var blobClient = _containerClient.GetBlobClient(normalizedPath);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                // Log for debugging
                _logger.LogWarning("File not found at path: {Path}", normalizedPath);
                return null;
            }

            // For downloads, the SDK handles streaming efficiently by default
            // The streaming download automatically uses chunked transfer internally
            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            return response.Value.Content;
        }
        catch (Exception ex)
        {
            // Log the exception for debugging
            _logger.LogError(ex, "Error retrieving file stream at path: {Path}", normalizedPath);
            return null;
        }
    }

    public async Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            // Normalize the path to include subfolder if configured
            var normalizedPath = NormalizeBlobPath(filePath);
            var blobClient = _containerClient.GetBlobClient(normalizedPath);
            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
        catch
        {
            // Silently handle deletion errors
        }
    }

    public string GenerateSasUrl(string filePath, TimeSpan expiry, BlobSasPermissions permissions = BlobSasPermissions.Read)
    {
        // Normalize the path to include subfolder if configured
        var normalizedPath = NormalizeBlobPath(filePath);
        var blobClient = _containerClient.GetBlobClient(normalizedPath);

        if (!blobClient.CanGenerateSasUri)
        {
            throw new NotSupportedException("Cannot generate SAS URI. Ensure the BlobServiceClient was created with credentials that support SAS generation.");
        }

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerClient.Name,
            BlobName = normalizedPath,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry),
        };

        sasBuilder.SetPermissions(permissions);

        return blobClient.GenerateSasUri(sasBuilder).ToString();
    }

    public async Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            // Normalize the path to include subfolder if configured
            var normalizedPath = NormalizeBlobPath(filePath);
            var blobClient = _containerClient.GetBlobClient(normalizedPath);
            return await blobClient.ExistsAsync(cancellationToken);
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
            // Normalize the path to include subfolder if configured
            var normalizedPath = NormalizeBlobPath(filePath);
            var blobClient = _containerClient.GetBlobClient(normalizedPath);
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            return properties.Value.ContentLength;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<StorageBlobInfo>> ListBlobsAsync(CancellationToken cancellationToken = default)
    {
        var blobs = new List<StorageBlobInfo>();

        await foreach (var blobItem in _containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            blobs.Add(new StorageBlobInfo(blobItem.Name, blobItem.Properties.ContentLength ?? 0));
        }

        return blobs;
    }

    /// <inheritdoc />
    public bool SupportsDirectUpload => true;

    /// <inheritdoc />
    public string GenerateBlobName(string fileName, long? showcaseId = null)
    {
        // Direct uploads always get a server-generated leaf name; the caller-supplied
        // directory portion is intentionally discarded here.
        var leaf = Path.GetFileName(fileName);
        var blobName = StoragePathBuilder.BuildRelativePath(Guid.NewGuid().ToString("N") + Path.GetExtension(leaf).ToLowerInvariant(), showcaseId, '/');

        // Apply subfolder if configured
        return BuildBlobPath(blobName);
    }

    /// <inheritdoc />
    public string GenerateUploadSasUrl(string blobName, TimeSpan expiry, string contentType)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);

        if (!blobClient.CanGenerateSasUri)
        {
            throw new NotSupportedException("Cannot generate SAS URI. Ensure the BlobServiceClient was created with credentials that support SAS generation.");
        }

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerClient.Name,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry),
            ContentType = contentType,
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

        return blobClient.GenerateSasUri(sasBuilder).ToString();
    }

    private string BuildBlobPath(string relativePath)
    {
        // If subfolder is configured, prepend it to the path
        if (!string.IsNullOrWhiteSpace(_settings.SubfolderPath))
        {
            var subfolder = _settings.SubfolderPath.Trim('/');
            return string.IsNullOrEmpty(relativePath)
                ? subfolder
                : $"{subfolder}/{relativePath}";
        }

        return relativePath;
    }

    private string NormalizeBlobPath(string filePath)
    {
        // If subfolder is configured and the path doesn't already start with it, prepend it
        if (!string.IsNullOrWhiteSpace(_settings.SubfolderPath))
        {
            var subfolder = _settings.SubfolderPath.Trim('/');

            // Check if the path already starts with the subfolder
            if (!filePath.StartsWith(subfolder + "/", StringComparison.OrdinalIgnoreCase) &&
                !filePath.Equals(subfolder, StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrEmpty(filePath)
                    ? subfolder
                    : $"{subfolder}/{filePath}";
            }
        }

        return filePath;
    }
}
