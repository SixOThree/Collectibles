namespace Collectibles.Infrastructure.Persistence.Configurations;

public class CollectibleItemAttachmentConfiguration : IEntityTypeConfiguration<CollectibleItemAttachment>
{
    public void Configure(EntityTypeBuilder<CollectibleItemAttachment> builder)
    {
        builder.ToTable("CollectibleItemAttachments");

        // Composite primary key
        builder.HasKey(cia => new { cia.CollectibleItemId, cia.AttachmentId });

        // Relationships
        builder.HasOne(cia => cia.CollectibleItem)
            .WithMany(ci => ci.CollectibleItemAttachments)
            .HasForeignKey(cia => cia.CollectibleItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cia => cia.Attachment)
            .WithMany(a => a.CollectibleItemAttachments)
            .HasForeignKey(cia => cia.AttachmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Properties
        builder.Property(cia => cia.IsFeatured)
            .HasDefaultValue(false);

        builder.Property(cia => cia.DisplayOrder)
            .HasDefaultValue(0);

        // Indexes
        builder.HasIndex(cia => cia.IsFeatured);
        builder.HasIndex(cia => new { cia.CollectibleItemId, cia.IsFeatured });
    }
}
