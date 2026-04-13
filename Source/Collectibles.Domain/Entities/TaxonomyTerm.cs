namespace Collectibles.Domain.Entities;

/// <summary>
/// Represents a taxonomy term that can be used to categorize collectible items and showcases.
/// </summary>
public class TaxonomyTerm : BaseAuditableSoftDeleteEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int? SortOrder { get; set; } = int.MaxValue;
    public virtual TaxonomyTerm? Parent { get; set; }
    public virtual ICollection<TaxonomyTerm> Children { get; set; }
    public virtual TaxonomyVocabulary Vocabulary { get; set; }

    public TaxonomyTerm()
    {
        Children = new List<TaxonomyTerm>();
        Vocabulary = null!;
    }
}
