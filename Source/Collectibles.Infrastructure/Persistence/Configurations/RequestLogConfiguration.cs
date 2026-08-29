namespace Collectibles.Infrastructure.Persistence.Configurations;

/// <summary>
/// A high-volume append-and-query table that had no configuration at all: every string
/// column was <c>nvarchar(max)</c> and there were no indexes, so both the admin queries
/// and the retention delete degraded to full scans as volume grew.
/// </summary>
public class RequestLogConfiguration : BaseEntityConfiguration<RequestLog>
{
    protected override void ConfigureEntity(EntityTypeBuilder<RequestLog> builder)
    {
        builder.ToTable("RequestLogs");

        builder.Property(r => r.Method).HasMaxLength(16).IsRequired();
        builder.Property(r => r.Path).HasMaxLength(2048).IsRequired();
        builder.Property(r => r.QueryString).HasMaxLength(2048);
        builder.Property(r => r.RequestId).HasMaxLength(128);
        builder.Property(r => r.CorrelationId).HasMaxLength(128);
        builder.Property(r => r.UserId).HasMaxLength(450);
        builder.Property(r => r.UserName).HasMaxLength(256);
        builder.Property(r => r.IPAddress).HasMaxLength(45);
        builder.Property(r => r.UserAgent).HasMaxLength(512);
        builder.Property(r => r.Scheme).HasMaxLength(16);
        builder.Property(r => r.Host).HasMaxLength(256);
        builder.Property(r => r.ContentType).HasMaxLength(256);
        builder.Property(r => r.ResponseContentType).HasMaxLength(256);
        builder.Property(r => r.ExceptionType).HasMaxLength(512);

        // Matches the retention delete and the descending "recent requests" listing.
        builder.HasIndex(r => r.Timestamp)
            .HasDatabaseName("IX_RequestLogs_Timestamp");

        builder.HasIndex(r => new { r.UserId, r.Timestamp })
            .HasDatabaseName("IX_RequestLogs_UserId_Timestamp");

        builder.HasIndex(r => new { r.StatusCode, r.Timestamp })
            .HasDatabaseName("IX_RequestLogs_StatusCode_Timestamp");
    }
}
