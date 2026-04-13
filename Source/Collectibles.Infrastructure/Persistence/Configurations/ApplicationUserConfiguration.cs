namespace Collectibles.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName).HasMaxLength(100);
        builder.Property(u => u.LastName).HasMaxLength(100);
        builder.Property(u => u.ProfilePictureUrl).HasMaxLength(500);
        builder.Property(u => u.IsActive).HasDefaultValue(true);
        builder.Property(u => u.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(u => u.CreatedBy).HasMaxLength(450);
        builder.Property(u => u.ModifiedBy).HasMaxLength(450);

        builder.Ignore(u => u.FullName);
    }
}
