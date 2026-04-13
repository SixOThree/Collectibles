namespace Collectibles.Application.Features.Attachments.Dtos;

public class VerificationResult
{
    public int TotalMigratedAttachments { get; set; }
    public int VerifiedCount { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
    public List<VerificationError> VerificationErrors { get; set; } = new List<VerificationError>();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success => FailedCount == 0 && VerifiedCount == TotalMigratedAttachments;
}
