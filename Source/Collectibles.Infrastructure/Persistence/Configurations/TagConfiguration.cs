namespace Collectibles.Infrastructure.Persistence.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.Ignore(e => e.DomainEvents);

        builder.Property(t => t.Name).HasMaxLength(50).IsRequired();

        builder.HasMany(t => t.CollectibleItemTags)
            .WithOne(cit => cit.Tag)
            .HasForeignKey(cit => cit.TagId);

        builder.HasMany(t => t.ShowcaseTags)
            .WithOne(st => st.Tag)
            .HasForeignKey(st => st.TagId);
    }
}
