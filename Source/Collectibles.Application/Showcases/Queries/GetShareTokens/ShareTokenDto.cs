namespace Collectibles.Application.Showcases.Queries.GetShareTokens;

/// <summary>
/// Describes an issued share link for management purposes.
/// </summary>
/// <remarks>
/// The token itself is deliberately absent. Only its hash is stored, so the link cannot be
/// reconstructed after creation - it is shown to its creator once, at generation. Listing exists to
/// review and revoke links, not to retrieve them.
/// </remarks>
public class ShareTokenDto
{
    public long Id { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int ViewCount { get; set; }
    public DateTime? LastViewedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
    public string? Note { get; set; }
    public DateTime Created { get; set; }
}
