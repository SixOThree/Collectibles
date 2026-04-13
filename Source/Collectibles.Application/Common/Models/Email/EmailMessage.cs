namespace Collectibles.Application.Common.Models.Email;

public class EmailMessage
{
    public string ToEmail { get; set; } = string.Empty;
    public string? ToName { get; set; }
    public List<string> CcEmails { get; set; } = new();
    public List<string> BccEmails { get; set; } = new();
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public List<EmailAttachment> Attachments { get; set; } = new();
    public Dictionary<string, string> Headers { get; set; } = new();
    public int Priority { get; set; }
    public DateTime? ScheduledFor { get; set; }
}

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
}
