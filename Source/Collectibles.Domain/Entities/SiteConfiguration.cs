namespace Collectibles.Domain.Entities;

public class SiteConfiguration
{
    public long Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Gets or sets the optimistic-concurrency token. Without it, two editors of the same
    /// aggregate silently last-write-wins.
    /// </summary>
    public byte[]? RowVersion { get; set; }
}
