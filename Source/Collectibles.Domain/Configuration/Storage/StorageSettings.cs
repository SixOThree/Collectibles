using Collectibles.Domain.Enums;

namespace Collectibles.Domain.Configuration.Storage;

public class StorageSettings
{
    public const string SectionName = "Storage";

    public StorageProvider Provider { get; set; } = StorageProvider.Database;
    public AzureBlobStorageSettings? AzureBlobStorage { get; set; }
    public LocalFileSystemSettings? LocalFileSystem { get; set; }
    public DirectUploadSettings? DirectUpload { get; set; }
}
