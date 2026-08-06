using Collectibles.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Collectibles.Infrastructure.Persistence.Configurations;

public class EventLogConfiguration : BaseEntityConfiguration<EventLog>
{
    protected override void ConfigureEntity(EntityTypeBuilder<EventLog> builder)
    {
        builder.ToTable("EventLogs");

        builder.Property(e => e.UserId)
            .HasMaxLength(450);

        builder.Property(e => e.UserEmail)
            .HasMaxLength(256);

        builder.Property(e => e.Action)
            .IsRequired();

        builder.Property(e => e.EntityType)
            .HasMaxLength(256);

        builder.Property(e => e.EntityName)
            .HasMaxLength(500);

        builder.Property(e => e.IPAddress)
            .HasMaxLength(64);

        builder.Property(e => e.UserAgent)
            .HasMaxLength(512);

        builder.Property(e => e.SessionId)
            .HasMaxLength(128);

        builder.Property(e => e.Timestamp)
            .IsRequired();

        builder.HasIndex(e => e.Action);
        builder.HasIndex(e => e.EntityType);
        builder.HasIndex(e => e.EntityId);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.UserEmail);
        builder.HasIndex(e => e.SessionId);
        builder.HasIndex(e => e.Timestamp);
        builder.HasIndex(e => new { e.EntityType, e.EntityId });
        builder.HasIndex(e => new { e.SessionId, e.Timestamp });
        builder.HasIndex(e => new { e.UserId, e.Timestamp });
    }
}
