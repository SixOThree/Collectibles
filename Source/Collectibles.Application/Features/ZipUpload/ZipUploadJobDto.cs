using Collectibles.Domain.Common.Enums;

namespace Collectibles.Application.Features.ZipUpload;

public class ZipUploadJobDto
{
    public long Id { get; set; }
    public long ShowcaseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public JobStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int FoldersCreated { get; set; }
    public int FilesAttached { get; set; }
    public int ErrorCount { get; set; }
    public string? CurrentItemName { get; set; }
    public string? ErrorDetails { get; set; }
    public int ProgressPercentage { get; set; }
}
