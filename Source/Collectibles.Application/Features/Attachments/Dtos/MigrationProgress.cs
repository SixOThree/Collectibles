namespace Collectibles.Application.Features.Attachments.Dtos;

public class MigrationProgress
{
    public int TotalAttachments { get; set; }
    public int ProcessedAttachments { get; set; }
    public int SuccessfulMigrations { get; set; }
    public int FailedMigrations { get; set; }
    public int CurrentBatch { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public string CurrentAttachmentName { get; set; } = string.Empty;
}
