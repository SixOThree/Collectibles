// Copyright (c) Collectibles. All rights reserved.

namespace Collectibles.Infrastructure.Persistence.Configurations;

public class TaxonomyTermConfiguration : IEntityTypeConfiguration<TaxonomyTerm>
{
    public void Configure(EntityTypeBuilder<TaxonomyTerm> builder)
    {
        builder.ToTable("TaxonomyVocabularies");
        builder.Ignore(e => e.DomainEvents);

        builder.Property(t => t.Name).HasMaxLength(200);
        builder.Property(t => t.CreatedBy).HasMaxLength(450);
        builder.Property(t => t.LastModifiedBy).HasMaxLength(450);
    }
}
