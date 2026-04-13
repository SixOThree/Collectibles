namespace Collectibles.Application.Features.Attachments;

/// <summary>
/// Indicates the storage mode used for an attachment.
/// </summary>
public enum StorageMode
{
    /// <summary>
    /// Storage mode could not be determined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Attachment content is stored in the database (legacy approach).
    /// </summary>
    Database = 1,

    /// <summary>
    /// Attachment content is stored in external storage (modern approach).
    /// </summary>
    External = 2,
}
