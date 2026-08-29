// Copyright (c) Collectibles. All rights reserved.

namespace Collectibles.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="TaxonomyTerm"/> to the TaxonomyTerms table.
/// </summary>
/// <remarks>
/// The two taxonomy configuration classes were swapped between files, so the physical
/// tables were inverted: terms lived in TaxonomyVocabularies and vice versa. EF was
/// internally consistent, so the app worked, but any raw SQL or DBA operation hit the
/// wrong table. Each class now lives in its correctly named file and maps its own table.
/// </remarks>
public class TaxonomyTermConfiguration : IEntityTypeConfiguration<TaxonomyTerm>
{
    public void Configure(EntityTypeBuilder<TaxonomyTerm> builder)
    {
        builder.ToTable("TaxonomyTerms");
        builder.Ignore(e => e.DomainEvents);

        builder.Property(t => t.Name).HasMaxLength(200);
        builder.Property(t => t.CreatedBy).HasMaxLength(450);
        builder.Property(t => t.LastModifiedBy).HasMaxLength(450);
    }
}
