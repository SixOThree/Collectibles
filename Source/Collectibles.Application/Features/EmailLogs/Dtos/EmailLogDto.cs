using Collectibles.Domain.Entities;

namespace Collectibles.Application.Features.EmailLogs.Dtos;

public class EmailLogDto
{
    public long Id { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string? ToName { get; set; }
    public string? CcEmails { get; set; }
    public string? BccEmails { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public EmailStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Provider { get; set; }
    public int Priority { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public string? TemplateName { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? LastModified { get; set; }
}
