namespace Collectibles.Application.Interfaces;

public interface IZipUploadJobService
{
    Task ProcessJobAsync(long jobId);
    Task CleanupOrphanedJobsAsync();
}
