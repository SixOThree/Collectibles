using Collectibles.Domain.Security;

namespace Collectibles.Infrastructure.Persistence.Configurations;

/// <summary>
/// Scanned on every password change. <c>UserId</c> was an unindexed <c>nvarchar(max)</c>
/// with no foreign key, so the reuse check scanned the whole table and rows outlived
/// their user.
/// </summary>
public class PasswordHistoryConfiguration : BaseEntityConfiguration<PasswordHistory>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PasswordHistory> builder)
    {
        builder.ToTable("PasswordHistories");

        builder.Property(p => p.UserId).HasMaxLength(450).IsRequired();
        builder.Property(p => p.PasswordHash).HasMaxLength(512).IsRequired();

        builder.HasIndex(p => new { p.UserId, p.CreatedAt })
            .HasDatabaseName("IX_PasswordHistories_UserId_CreatedAt");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
