namespace Collectibles.Infrastructure.Persistence.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments");
        builder.Ignore(e => e.DomainEvents);
        builder.Property(a => a.Name).HasMaxLength(200).IsRequired();
        builder.Property(a => a.OriginalFilename).HasMaxLength(256);
        builder.Property(a => a.FileType).HasMaxLength(64);
        builder.Property(a => a.CreatedBy).HasMaxLength(450);
        builder.Property(a => a.LastModifiedBy).HasMaxLength(450);
        builder.Property(a => a.ContentHash).HasMaxLength(64);

        builder.HasIndex(a => a.ContentHash);

        builder.HasMany(a => a.Tags)
            .WithMany()
            .UsingEntity(j => j.ToTable("AttachmentTags"));
    }
}
