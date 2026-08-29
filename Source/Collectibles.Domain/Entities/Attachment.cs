namespace Collectibles.Domain.Entities;

/// <summary>
/// Represents an attachment that can be associated with various entities.
/// </summary>
public class Attachment : BaseAuditableSoftDeleteEntity
{
    public required string Name { get; set; }
    public string? OriginalFilename { get; set; }
    public string? FileType { get; set; }
    public AttachmentType? AttachmentType { get; set; }
    public long FileSize { get; set; }

    // Storage paths for external file storage
    public string? FilePath { get; set; }
    public string? PreviewPath { get; set; }

    // Legacy database storage (kept for backward compatibility)
    public virtual AttachmentContent? AttachmentContent { get; set; }
    public virtual AttachmentPreview? AttachmentPreview { get; set; }
    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public virtual ICollection<CollectibleItemAttachment> CollectibleItemAttachments { get; set; } = new List<CollectibleItemAttachment>();

    // Migration tracking fields
    public bool IsMigrated { get; set; }
    public DateTime? MigrationDate { get; set; }

    // Content hash for duplicate detection
    public string? ContentHash { get; set; }
    public DateTime? HashComputedAt { get; set; }

    /// <summary>
    /// Gets or sets when preview generation was last attempted for this attachment.
    /// Lets the preview worker back off rows that keep failing instead of re-selecting
    /// them on every run and starving newer uploads.
    /// </summary>
    public DateTime? PreviewAttemptedAt { get; set; }

    /// <summary>
    /// Gets or sets the optimistic-concurrency token. Without it, two editors of the same
    /// aggregate silently last-write-wins.
    /// </summary>
    public byte[]? RowVersion { get; set; }
}
