namespace Collectibles.Infrastructure.Persistence.Configurations;

public class QRCodeConfiguration : IEntityTypeConfiguration<QRCode>
{
    public void Configure(EntityTypeBuilder<QRCode> builder)
    {
        builder.ToTable("QRCodes");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(q => q.Code)
            .IsUnique()
            .HasDatabaseName("IX_QRCodes_Code");

        builder.Property(q => q.Status)
            .IsRequired();

        builder.Property(q => q.RevokedReason)
            .HasMaxLength(500);

        builder.Property(q => q.ScanCount)
            .HasDefaultValue(0);

        builder.HasOne(q => q.CollectibleItem)
            .WithOne(c => c.QRCode)
            .HasForeignKey<QRCode>(q => q.CollectibleItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(q => q.Status)
            .HasDatabaseName("IX_QRCodes_Status");

        builder.HasIndex(q => q.CreatedBy)
            .HasDatabaseName("IX_QRCodes_CreatedBy");

        builder.HasIndex(q => new { q.Status, q.CreatedBy })
            .HasDatabaseName("IX_QRCodes_Status_CreatedBy");
    }
}
