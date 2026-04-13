namespace Collectibles.Infrastructure.Persistence.Configurations;

public class LinkInfoConfiguration : IEntityTypeConfiguration<LinkInfo>
{
    public void Configure(EntityTypeBuilder<LinkInfo> builder)
    {
        builder.ToTable("LinkInfos");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Url)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(e => e.Title)
            .HasMaxLength(256);

        builder.HasMany(e => e.Caches)
            .WithOne(e => e.LinkInfo)
            .HasForeignKey(e => e.LinkInfoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
