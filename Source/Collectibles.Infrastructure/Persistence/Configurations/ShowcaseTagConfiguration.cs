namespace Collectibles.Infrastructure.Persistence.Configurations;

public class ShowcaseTagConfiguration : BaseEntityConfiguration<ShowcaseTag>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ShowcaseTag> builder)
    {
        builder.ToTable("ShowcaseTags");

        builder.Property(a => a.CreatedBy).HasMaxLength(450);
        builder.Property(a => a.LastModifiedBy).HasMaxLength(450);

        builder.HasOne(st => st.Showcase)
            .WithMany(s => s.ShowcaseTags)
            .HasForeignKey(st => st.ShowcaseId);

        builder.HasOne(st => st.Tag)
            .WithMany(t => t.ShowcaseTags)
            .HasForeignKey(st => st.TagId);
    }
}
