using Collectibles.Domain.Constants;

namespace Collectibles.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuration keys are a get-or-create lookup; the uniqueness the repository assumed
/// is now enforced by the database instead of by an exists-then-insert race.
/// </summary>
public class SiteConfigurationConfiguration : IEntityTypeConfiguration<SiteConfiguration>
{
    public void Configure(EntityTypeBuilder<SiteConfiguration> builder)
    {
        builder.ToTable("SiteConfigurations");

        builder.Property(c => c.Key)
            .HasMaxLength(ApplicationConstants.ValidationLengths.ConfigurationKeyMaxLength)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(ApplicationConstants.ValidationLengths.DescriptionMaxLength);

        builder.Property(c => c.RowVersion).IsRowVersion();

        builder.HasIndex(c => c.Key)
            .IsUnique()
            .HasDatabaseName("IX_SiteConfigurations_Key");
    }
}
