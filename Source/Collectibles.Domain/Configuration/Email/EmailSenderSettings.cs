namespace Collectibles.Domain.Configuration.Email;

public class EmailSenderSettings
{
    public string DefaultFromEmail { get; set; } = string.Empty;
    public string DefaultFromName { get; set; } = string.Empty;
    public string ReplyToEmail { get; set; } = string.Empty;
    public string ReplyToName { get; set; } = string.Empty;
}
