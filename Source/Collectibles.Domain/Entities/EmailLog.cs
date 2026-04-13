namespace Collectibles.Domain.Entities;

public class EmailLog : BaseAuditableEntity
{
    public string ToEmail { get; set; } = string.Empty;
    public string? ToName { get; set; }
    public string? CcEmails { get; set; }
    public string? BccEmails { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? Body { get; set; }
    public bool IsHtml { get; set; } = true;
    public string? Provider { get; set; }
    public EmailStatus Status { get; set; } = EmailStatus.Pending;
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MessageId { get; set; }
    public string? TemplateName { get; set; }
    public string? TemplateData { get; set; }
    public int Priority { get; set; }
    public DateTime? ScheduledFor { get; set; }
}
