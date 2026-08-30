namespace Collectibles.Domain.Entities;

public class ShowcaseShareToken : BaseAuditableSoftDeleteEntity
{
    public long ShowcaseId { get; set; }
    public Showcase Showcase { get; set; }

    /// <summary>
    /// Gets or sets the one-way hash of the token, never the token itself. The plaintext exists
    /// only in the share URL and is shown to its creator once, at generation.
    /// </summary>
    public string TokenHash { get; set; }

    /// <summary>
    /// Gets or sets the moment this link stops working. Not nullable: a share link that never
    /// expires stays usable for as long as it leaks, so "perpetual" is not a representable state.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    public int ViewCount { get; set; }
    public DateTime? LastViewedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; } // Optional note to remember who/why it was shared

    public ShowcaseShareToken()
    {
        TokenHash = string.Empty;
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

        if (ExpiresAt < DateTime.UtcNow)
        {
            return true;
        }

        return false;
    }
}
