namespace Collectibles.Domain.Configuration.Email;

public class AzureCommunicationSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string SenderAddress { get; set; } = string.Empty;
    public int PollingIntervalSeconds { get; set; } = 1;
    public int TimeoutSeconds { get; set; } = 120;
}
