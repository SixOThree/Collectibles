namespace Collectibles.Domain.Entities;

/// <summary>
/// Represents a taxonomy vocabulary that can contain multiple terms for categorizing collectible items and showcases.
/// </summary>
public class TaxonomyVocabulary : BaseAuditableSoftDeleteEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public virtual ICollection<TaxonomyTerm> Terms { get; set; }
    public TaxonomySortOrder SortOrder { get; set; } = TaxonomySortOrder.TermSorting;
    public bool IsEnabled { get; set; } = true;
    public bool IsPublic { get; set; }
    public bool IsLocked { get; set; }

    public TaxonomyVocabulary()
    {
        Terms = new List<TaxonomyTerm>();
    }
}
