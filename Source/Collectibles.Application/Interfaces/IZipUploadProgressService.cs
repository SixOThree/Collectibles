using Collectibles.Application.Features.ZipUpload;

namespace Collectibles.Application.Interfaces;

public interface IZipUploadProgressService
{
    Task SendProgressUpdate(long jobId, ZipUploadJobDto jobDto);
    Task SendJobCompleted(long jobId, ZipUploadJobDto jobDto);
    Task SendJobFailed(long jobId, string error);
}
