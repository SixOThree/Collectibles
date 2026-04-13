using Collectibles.Domain.Entities;

namespace Collectibles.Application.Features.Attachments;

public static class AttachmentExtensions
{
    /// <summary>
    /// Determines if the attachment is using external storage (modern approach).
    /// </summary>
    /// <returns></returns>
    public static bool IsUsingExternalStorage(this Attachment attachment)
    {
        return !string.IsNullOrEmpty(attachment.FilePath);
    }

    /// <summary>
    /// Determines if the attachment has content in the database (legacy approach).
    /// </summary>
    /// <returns></returns>
    public static bool HasDatabaseContent(this Attachment attachment)
    {
        return attachment.AttachmentContent?.Content != null && attachment.AttachmentContent.Content.Length > 0;
    }

    /// <summary>
    /// Determines if the attachment has preview content in external storage.
    /// </summary>
    /// <returns></returns>
    public static bool HasExternalPreview(this Attachment attachment)
    {
        return !string.IsNullOrEmpty(attachment.PreviewPath);
    }

    /// <summary>
    /// Determines if the attachment has preview content in the database.
    /// </summary>
    /// <returns></returns>
    public static bool HasDatabasePreview(this Attachment attachment)
    {
        return attachment.AttachmentPreview?.PreviewThumbnail != null && attachment.AttachmentPreview.PreviewThumbnail.Length > 0;
    }

    /// <summary>
    /// Gets the storage mode for the attachment.
    /// </summary>
    /// <returns></returns>
    public static StorageMode GetStorageMode(this Attachment attachment)
    {
        if (attachment.IsUsingExternalStorage())
        {
            return StorageMode.External;
        }
        else if (attachment.HasDatabaseContent())
        {
            return StorageMode.Database;
        }
        else
        {
            return StorageMode.Unknown;
        }
    }
}
