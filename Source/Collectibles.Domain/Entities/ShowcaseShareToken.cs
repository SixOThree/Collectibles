namespace Collectibles.Domain.Entities;

public class ShowcaseShareToken : BaseAuditableSoftDeleteEntity
{
    public long ShowcaseId { get; set; }
    public Showcase Showcase { get; set; }
    public string Token { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int ViewCount { get; set; }
    public DateTime? LastViewedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; } // Optional note to remember who/why it was shared

    public ShowcaseShareToken()
    {
        Token = string.Empty;
        Showcase = null!;
    }

    public bool IsExpired()
    {
        if (!IsActive)
        {
            return true;
        }

        if (Deleted.HasValue)
        {
            return true;
        }

        if (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow)
        {
            return true;
        }

        return false;
    }
}
