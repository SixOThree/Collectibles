namespace Collectibles.Infrastructure.Persistence.Configurations;

public class CollectibleItemTagConfiguration : BaseEntityConfiguration<CollectibleItemTag>
{
    protected override void ConfigureEntity(EntityTypeBuilder<CollectibleItemTag> builder)
    {
        builder.ToTable("CollectibleItemTags");

        builder.HasOne(cit => cit.CollectibleItem)
            .WithMany(ci => ci.CollectibleItemTags)
            .HasForeignKey(cit => cit.CollectibleItemId);

        builder.HasOne(cit => cit.Tag)
            .WithMany(t => t.CollectibleItemTags)
            .HasForeignKey(cit => cit.TagId);
    }
}
