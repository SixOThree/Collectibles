namespace Collectibles.Application.Features.Attachments.Dtos;

public class MigrationResult
{
    public bool Success { get; set; }
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<MigrationError> Errors { get; set; } = new List<MigrationError>();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
}
