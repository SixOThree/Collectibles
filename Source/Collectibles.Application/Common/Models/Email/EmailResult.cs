namespace Collectibles.Application.Common.Models.Email;

public class EmailResult
{
    public bool IsSuccess { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public string? Provider { get; set; }

    public static EmailResult Success(string? messageId = null, string? provider = null)
    {
        return new EmailResult
        {
            IsSuccess = true,
            MessageId = messageId,
            SentAt = DateTime.UtcNow,
            Provider = provider,
        };
    }

    public static EmailResult Failure(string errorMessage, string? provider = null)
    {
        return new EmailResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Provider = provider,
        };
    }
}
