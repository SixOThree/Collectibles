namespace Collectibles.Application.Features.Attachments.Dtos;

public class RollbackError
{
    public long AttachmentId { get; set; }
    public string AttachmentName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public RollbackErrorType ErrorType { get; set; }
}

public enum RollbackErrorType
{
    StorageDeletionFailed,
    DatabaseUpdateFailed,
    FileNotFound,
    Other,
}
