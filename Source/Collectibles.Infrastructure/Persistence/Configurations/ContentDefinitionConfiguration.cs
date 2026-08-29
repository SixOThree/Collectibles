namespace Collectibles.Infrastructure.Persistence.Configurations;

public class ContentDefinitionConfiguration : BaseEntityConfiguration<ContentDefinition>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ContentDefinition> builder)
    {
        builder.ToTable("ContentDefinitions");

        builder.Property(cd => cd.RowVersion).IsRowVersion();

        builder.Property(cd => cd.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(cd => cd.Description)
            .HasMaxLength(500);

        builder.Property(cd => cd.DefinitionJson)
            .IsRequired();

        builder.Property(cd => cd.IsActive)
            .HasDefaultValue(true);

        builder.Property(cd => cd.HideAttachments)
            .HasDefaultValue(false);

        builder.Property(cd => cd.IsGlobal)
            .HasDefaultValue(false);

        builder.Property(cd => cd.BorderColor)
            .HasMaxLength(7);

        builder.Property(cd => cd.Icon)
            .HasMaxLength(50);

        builder.Property(cd => cd.CreatedBy)
            .HasMaxLength(450);

        builder.Property(cd => cd.LastModifiedBy)
            .HasMaxLength(450);

        builder.HasMany(cd => cd.CollectibleItems)
            .WithOne(ci => ci.ContentType)
            .HasForeignKey(ci => ci.ContentDefinitionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(cd => cd.Showcase)
            .WithMany()
            .HasForeignKey(cd => cd.ShowcaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(cd => new { cd.Name, cd.ShowcaseId })
            .IsUnique();

        builder.HasIndex(cd => cd.IsActive);
        builder.HasIndex(cd => cd.IsGlobal);
        builder.HasIndex(cd => cd.ShowcaseId);
    }
}
