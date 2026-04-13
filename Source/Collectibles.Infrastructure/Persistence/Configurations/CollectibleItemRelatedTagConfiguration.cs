namespace Collectibles.Infrastructure.Persistence.Configurations;

public class CollectibleItemRelatedTagConfiguration : IEntityTypeConfiguration<CollectibleItemRelatedTag>
{
    public void Configure(EntityTypeBuilder<CollectibleItemRelatedTag> builder)
    {
        builder.HasKey(t => new { t.CollectibleItemId, t.TagId });

        builder.HasOne(t => t.CollectibleItem)
            .WithMany(e => e.CollectibleItemRelatedTags)
            .HasForeignKey(t => t.CollectibleItemId);

        builder.HasOne(t => t.Tag)
            .WithMany(e => e.CollectibleItemRelatedTags)
            .HasForeignKey(t => t.TagId);
    }
}
