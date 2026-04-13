namespace Collectibles.Infrastructure.Persistence.Configurations;

public class LinkCacheConfiguration : IEntityTypeConfiguration<LinkCache>
{
    public void Configure(EntityTypeBuilder<LinkCache> builder)
    {
        builder.ToTable("LinkCaches");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CachedContentPath)
            .HasMaxLength(1024);

        builder.Property(e => e.ScreenshotPath)
            .HasMaxLength(1024);

        builder.Property(e => e.FailureReason)
            .HasMaxLength(4000);

        builder.HasOne(e => e.LinkInfo)
            .WithMany(e => e.Caches)
            .HasForeignKey(e => e.LinkInfoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
