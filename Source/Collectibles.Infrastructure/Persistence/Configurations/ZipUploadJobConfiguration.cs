using Collectibles.Domain.Constants;

namespace Collectibles.Infrastructure.Persistence.Configurations;

/// <summary>
/// The job's progress counters are written by the Hangfire worker while the UI polls them,
/// so it needs a concurrency token like the other mutable aggregates. The status column is
/// also the target of the atomic claim in <c>ZipUploadJobService</c>.
/// </summary>
public class ZipUploadJobConfiguration : BaseEntityConfiguration<ZipUploadJob>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ZipUploadJob> builder)
    {
        builder.ToTable("ZipUploadJobs");

        builder.Property(j => j.UserId).HasMaxLength(450).IsRequired();
        builder.Property(j => j.FileName).HasMaxLength(ApplicationConstants.ValidationLengths.FileNameMaxLength).IsRequired();
        builder.Property(j => j.CurrentItemName).HasMaxLength(ApplicationConstants.ValidationLengths.NameMaxLength);

        builder.Property(j => j.RowVersion).IsRowVersion();

        builder.HasIndex(j => new { j.UserId, j.Created })
            .HasDatabaseName("IX_ZipUploadJobs_UserId_Created");

        builder.HasIndex(j => j.Status)
            .HasDatabaseName("IX_ZipUploadJobs_Status");
    }
}
