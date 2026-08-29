namespace Collectibles.Infrastructure.Persistence.Configurations;
#nullable disable

public class CollectibleItemConfiguration : BaseEntityConfiguration<CollectibleItem>
{
    protected override void ConfigureEntity(EntityTypeBuilder<CollectibleItem> builder)
    {
        builder.ToTable("CollectibleItems");

        builder.Property(ci => ci.Name).HasMaxLength(200);
        builder.Property(ci => ci.CreatedBy).HasMaxLength(450);
        builder.Property(ci => ci.DeletedBy).HasMaxLength(450);

        builder.HasOne(ci => ci.PreviewImage)
            .WithMany()
            .HasForeignKey(ci => ci.PreviewImageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(ci => ci.Parent)
            .WithMany(ci => ci.Children)
            .HasForeignKey(ci => ci.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(ci => ci.ComponentOfItem)
            .WithMany();

        builder.HasMany(ci => ci.CollectibleItemAttachments)
            .WithOne(cia => cia.CollectibleItem)
            .HasForeignKey(cia => cia.CollectibleItemId);

        builder.HasMany(ci => ci.CollectibleItemTags)
            .WithOne(cit => cit.CollectibleItem)
            .HasForeignKey(cit => cit.CollectibleItemId);

        builder.HasMany(ci => ci.ExternalReferences)
            .WithOne(li => li.CollectibleItem)
            .HasForeignKey(li => li.CollectibleItemId);

        builder.HasOne(ci => ci.ContentType)
            .WithMany(cd => cd.CollectibleItems)
            .HasForeignKey(ci => ci.ContentDefinitionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(ci => ci.ContentValue);

        // Blazor Server is inherently multi-circuit: without a concurrency token two
        // editors of the same item silently last-write-wins, including whole-document
        // overwrites of the ContentValue JSON.
        builder.Property(ci => ci.RowVersion).IsRowVersion();
    }
}
