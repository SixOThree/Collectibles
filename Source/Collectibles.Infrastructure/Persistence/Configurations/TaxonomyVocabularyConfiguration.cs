// Copyright (c) Collectibles. All rights reserved.

namespace Collectibles.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="TaxonomyVocabulary"/> to the TaxonomyVocabularies table.
/// See <see cref="TaxonomyTermConfiguration"/> for why these two were previously swapped.
/// </summary>
public class TaxonomyVocabularyConfiguration : IEntityTypeConfiguration<TaxonomyVocabulary>
{
    public void Configure(EntityTypeBuilder<TaxonomyVocabulary> builder)
    {
        builder.ToTable("TaxonomyVocabularies");
        builder.Ignore(e => e.DomainEvents);

        builder.Property(t => t.Name).HasMaxLength(200);
        builder.Property(t => t.CreatedBy).HasMaxLength(450);
        builder.Property(t => t.LastModifiedBy).HasMaxLength(450);
    }
}
