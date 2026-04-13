namespace Collectibles.Domain.Configuration.Storage;

public class AzureBlobStorageSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "collectibles-attachments";
    public bool CreateContainerIfNotExists { get; set; } = true;
    public string? SubfolderPath { get; set; }
}
