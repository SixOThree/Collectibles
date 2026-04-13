namespace Collectibles.Infrastructure.Persistence.Configurations;
#nullable disable

public class ShowcaseConfiguration : IEntityTypeConfiguration<Showcase>
{
    public void Configure(EntityTypeBuilder<Showcase> builder)
    {
        builder.ToTable("Showcases");
        builder.Ignore(e => e.DomainEvents);

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.UserId).HasMaxLength(450);
        builder.Property(s => s.IsPrivate).HasDefaultValue(true);
        builder.Property(a => a.CreatedBy).HasMaxLength(450);
        builder.Property(a => a.LastModifiedBy).HasMaxLength(450);
        builder.Property(a => a.DeletedBy).HasMaxLength(450);

        builder.HasMany(s => s.ShowcaseTags)
            .WithOne(st => st.Showcase)
            .HasForeignKey(st => st.ShowcaseId);

        // Configure relationship with PreviewImage
        builder.HasOne(s => s.PreviewImage)
            .WithMany()
            .HasForeignKey("PreviewImageId")
            .OnDelete(DeleteBehavior.SetNull);
    }
}
