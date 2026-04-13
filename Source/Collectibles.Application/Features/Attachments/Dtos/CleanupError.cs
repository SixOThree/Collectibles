namespace Collectibles.Application.Features.Attachments.Dtos;

public class CleanupError
{
    public long AttachmentId { get; set; }
    public string AttachmentName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public CleanupErrorType ErrorType { get; set; }
}

public enum CleanupErrorType
{
    NotVerified,
    TooRecent,
    DatabaseError,
    Other,
}
