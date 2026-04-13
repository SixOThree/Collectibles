namespace Collectibles.Infrastructure.Persistence.Configurations;

public class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.ToTable("EmailLogs");
        builder.Ignore(e => e.DomainEvents);

        builder.Property(e => e.ToEmail).HasMaxLength(256).IsRequired();
        builder.Property(e => e.ToName).HasMaxLength(256);
        builder.Property(e => e.CcEmails).HasMaxLength(1000);
        builder.Property(e => e.BccEmails).HasMaxLength(1000);
        builder.Property(e => e.FromEmail).HasMaxLength(256).IsRequired();
        builder.Property(e => e.FromName).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Subject).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Body).HasMaxLength(int.MaxValue);
        builder.Property(e => e.Provider).HasMaxLength(50);
        builder.Property(e => e.Status).HasConversion<int>();
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);
        builder.Property(e => e.MessageId).HasMaxLength(256);
        builder.Property(e => e.TemplateName).HasMaxLength(100);
        builder.Property(e => e.TemplateData).HasMaxLength(int.MaxValue);
        builder.Property(e => e.CreatedBy).HasMaxLength(450);
        builder.Property(e => e.LastModifiedBy).HasMaxLength(450);

        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.ToEmail);
        builder.HasIndex(e => e.SentAt);
        builder.HasIndex(e => e.ScheduledFor);
        builder.HasIndex(e => new { e.Status, e.ScheduledFor, e.Priority, e.Created });
    }
}
