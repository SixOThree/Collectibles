namespace Collectibles.Application.Features.Attachments.Dtos;

public class CleanupResult
{
    public int TotalEligible { get; set; }
    public int CleanedCount { get; set; }
    public int SkippedCount { get; set; }
    public long SpaceReclaimed { get; set; } // in bytes
    public List<CleanupError> Errors { get; set; } = new List<CleanupError>();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
