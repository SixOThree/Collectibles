namespace Collectibles.Domain.Configuration.Storage;

public class LocalFileSystemSettings
{
    public string BasePath { get; set; } = "Uploads";
    public bool UseAbsolutePath { get; set; }
}
