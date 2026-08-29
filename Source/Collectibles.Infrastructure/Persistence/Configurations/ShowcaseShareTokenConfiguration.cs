using Collectibles.Domain.Constants;

namespace Collectibles.Infrastructure.Persistence.Configurations;

/// <summary>
/// The share token is the credential anonymous visitors present, so it is looked up on
/// every public share view. Without a configuration it was an unindexed
/// <c>nvarchar(max)</c>, table-scanned on each visit, and its uniqueness rested on an
/// application-level check-then-insert rather than a constraint.
/// </summary>
public class ShowcaseShareTokenConfiguration : IEntityTypeConfiguration<ShowcaseShareToken>
{
    public void Configure(EntityTypeBuilder<ShowcaseShareToken> builder)
    {
        builder.ToTable("ShowcaseShareTokens");
        builder.Ignore(e => e.DomainEvents);

        builder.Property(t => t.Token)
            .HasMaxLength(ApplicationConstants.ValidationLengths.ShareTokenMaxLength)
            .IsRequired();

        builder.Property(t => t.Note)
            .HasMaxLength(ApplicationConstants.ValidationLengths.DescriptionMaxLength);

        builder.HasIndex(t => t.Token)
            .IsUnique()
            .HasDatabaseName("IX_ShowcaseShareTokens_Token");
    }
}
