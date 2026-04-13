using Collectibles.Application.Features.ZipUpload;
using Collectibles.Application.Interfaces;

namespace Collectibles.Web.Services;

/// <summary>
/// Simple no-op implementation of IZipUploadProgressService.
/// Progress is tracked via database polling instead of real-time updates.
/// </summary>
public class ZipUploadProgressService : IZipUploadProgressService
{
    public Task SendProgressUpdate(long jobId, ZipUploadJobDto jobDto)
    {
        // No-op: Progress is tracked via database polling
        return Task.CompletedTask;
    }

    public Task SendJobCompleted(long jobId, ZipUploadJobDto jobDto)
    {
        // No-op: Completion status is tracked via database polling
        return Task.CompletedTask;
    }

    public Task SendJobFailed(long jobId, string error)
    {
        // No-op: Failure status is tracked via database polling
        return Task.CompletedTask;
    }
}
