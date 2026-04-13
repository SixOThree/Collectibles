namespace Collectibles.Infrastructure.Persistence.Configurations;

public class AttachmentContentConfiguration : BaseEntityConfiguration<AttachmentContent>
{
    protected override void ConfigureEntity(EntityTypeBuilder<AttachmentContent> builder)
    {
        builder.ToTable("AttachmentContents");

        builder.Property(e => e.Content)
            .HasColumnType("varbinary(max)");

        builder.HasOne(e => e.Attachment)
            .WithOne(a => a.AttachmentContent)
            .HasForeignKey<AttachmentContent>(e => e.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
