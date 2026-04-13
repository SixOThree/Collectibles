namespace Collectibles.Domain.Entities;

/// <summary>
/// Represents a zip file upload job that processes folders into collectible items.
/// </summary>
public class ZipUploadJob : BaseAuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public long ShowcaseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int FoldersCreated { get; set; }
    public int FilesAttached { get; set; }
    public int ErrorCount { get; set; }
    public string? CurrentItemName { get; set; }
    public string? ErrorDetails { get; set; }
    public string? ProcessingData { get; set; } // JSON serialized data for resuming
    public string? StoragePath { get; set; } // Path where the zip file is stored
}
