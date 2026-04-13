namespace Collectibles.Domain.Configuration.Email;

public class SendGridSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public bool SandboxMode { get; set; }
}
