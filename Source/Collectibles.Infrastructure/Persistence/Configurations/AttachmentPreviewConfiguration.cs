namespace Collectibles.Infrastructure.Persistence.Configurations;

public class AttachmentPreviewConfiguration : BaseEntityConfiguration<AttachmentPreview>
{
    protected override void ConfigureEntity(EntityTypeBuilder<AttachmentPreview> builder)
    {
        builder.ToTable("AttachmentPreviews");

        builder.Property(e => e.PreviewThumbnail)
            .HasColumnType("varbinary(max)");

        builder.HasOne(e => e.Attachment)
            .WithOne(a => a.AttachmentPreview)
            .HasForeignKey<AttachmentPreview>(e => e.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
