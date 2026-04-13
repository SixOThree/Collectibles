namespace Collectibles.Domain.Configuration.Email;

public class EmailSettings
{
    public const string ConfigurationSection = "EmailSettings";

    public string Provider { get; set; } = "SMTP";
    public SmtpSettings Smtp { get; set; } = new();
    public SendGridSettings SendGrid { get; set; } = new();
    public AzureCommunicationSettings AzureCommunication { get; set; } = new();
    public EmailSenderSettings Sender { get; set; } = new();
    public EmailRetrySettings Retry { get; set; } = new();
    public bool EnableEmailLogging { get; set; } = true;
    public int LogRetentionDays { get; set; } = 365;
}
