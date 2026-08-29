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

    /// <summary>
    /// The database copy of the content was cleaned up after migration, so rolling back
    /// would leave no durable copy anywhere. The rollback is refused.
    /// </summary>
    MissingDatabaseCopy,
    Other,
}
