namespace Collectibles.Domain.Entities;

/// <summary>
/// Represents a many-to-many relationship between showcases and tags.
/// </summary>
public class ShowcaseTag : BaseAuditableSoftDeleteEntity
{
    public long TagId { get; set; }
    public Tag Tag { get; set; }

    public long ShowcaseId { get; set; }
    public Showcase Showcase { get; set; }

    public ShowcaseTag()
    {
        Tag = null!;
        Showcase = null!;
    }
}
