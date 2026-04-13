namespace Collectibles.Infrastructure.Persistence.Configurations;

public class SysLogConfiguration : BaseEntityConfiguration<SysLog>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SysLog> builder)
    {
        builder.ToTable("SysLogs");

        builder.Property(e => e.Level)
            .IsRequired();

        builder.Property(e => e.Message)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(e => e.Exception)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.StackTrace)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.Source)
            .HasMaxLength(256);

        builder.Property(e => e.MachineName)
            .HasMaxLength(256);

        builder.Property(e => e.ProcessName)
            .HasMaxLength(256);

        builder.Property(e => e.ThreadId);

        builder.Property(e => e.Properties)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.Timestamp)
            .IsRequired();

        builder.Property(e => e.Category)
            .HasMaxLength(256);

        builder.Property(e => e.CorrelationId)
            .HasMaxLength(128);

        builder.Property(e => e.UserId)
            .HasMaxLength(450);

        builder.Property(e => e.RequestPath)
            .HasMaxLength(1000);

        builder.Property(e => e.RequestMethod)
            .HasMaxLength(10);

        builder.HasIndex(e => e.Level);
        builder.HasIndex(e => e.Timestamp);
        builder.HasIndex(e => e.Category);
        builder.HasIndex(e => e.CorrelationId);
        builder.HasIndex(e => new { e.Level, e.Timestamp });
    }
}
