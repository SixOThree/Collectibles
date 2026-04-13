namespace Collectibles.Application.Features.Attachments.Dtos;

public class VerificationError
{
    public long AttachmentId { get; set; }
    public string AttachmentName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public VerificationErrorType ErrorType { get; set; }
    public string ErrorDetails { get; set; } = string.Empty;
    public long? ExpectedSize { get; set; }
    public long? ActualSize { get; set; }
}

public enum VerificationErrorType
{
    FileNotFound,
    SizeMismatch,
    PreviewNotFound,
    Other,
}
