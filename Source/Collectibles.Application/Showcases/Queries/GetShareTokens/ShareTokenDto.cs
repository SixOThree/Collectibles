namespace Collectibles.Application.Showcases.Queries.GetShareTokens;

public class ShareTokenDto
{
    public long Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string ShareUrl { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public int ViewCount { get; set; }
    public DateTime? LastViewedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
    public string? Note { get; set; }
    public DateTime Created { get; set; }
}
