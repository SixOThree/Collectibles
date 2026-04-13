using Collectibles.Application.Interfaces;
using Collectibles.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Features.ZipUpload.Commands;

public class UploadZipChunkCommand : IRequest<UploadZipChunkResult>
{
    public long JobId { get; set; }
    public int ChunkIndex { get; set; }
    public int TotalChunks { get; set; }
    public byte[] ChunkData { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public long TotalFileSize { get; set; }
}

public class UploadZipChunkResult
{
    public bool Success { get; set; }
    public int ChunkIndex { get; set; }
    public string? ErrorMessage { get; set; }
}

public class UploadZipChunkCommandHandler : IRequestHandler<UploadZipChunkCommand, UploadZipChunkResult>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<UploadZipChunkCommandHandler> _logger;
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "collectibles-chunks");

    public UploadZipChunkCommandHandler(
        IApplicationDbContextFactory contextFactory,
        IFileStorage fileStorage,
        ILogger<UploadZipChunkCommandHandler> logger)
    {
        _contextFactory = contextFactory;
        _fileStorage = fileStorage;
        _logger = logger;

        // Ensure temp directory exists
        if (!Directory.Exists(_tempDirectory))
        {
            Directory.CreateDirectory(_tempDirectory);
        }
    }

    public async Task<UploadZipChunkResult> Handle(UploadZipChunkCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            // Get the job
            var job = await context.ZipUploadJobs.FindAsync(new object[] { request.JobId }, cancellationToken);
            if (job == null)
            {
                return new UploadZipChunkResult
                {
                    Success = false,
                    ChunkIndex = request.ChunkIndex,
                    ErrorMessage = "Job not found",
                };
            }

            // Create temp file path for this job
            var tempFilePath = Path.Combine(_tempDirectory, $"job_{request.JobId}.zip.tmp");

            // Write chunk to temp file
            using (var fileStream = new FileStream(
                tempFilePath,
                request.ChunkIndex == 0 ? FileMode.Create : FileMode.Append,
                FileAccess.Write,
                FileShare.None))
            {
                await fileStream.WriteAsync(request.ChunkData.AsMemory(0, request.ChunkData.Length), cancellationToken);
            }

            _logger.LogInformation(
                "Received chunk {ChunkIndex}/{TotalChunks} for job {JobId}, size: {ChunkSize} bytes",
                request.ChunkIndex + 1, request.TotalChunks, request.JobId, request.ChunkData.Length);

            // If this is the last chunk, finalize the upload
            if (request.ChunkIndex == request.TotalChunks - 1)
            {
                _logger.LogInformation("All chunks received for job {JobId}, finalizing upload", request.JobId);

                // Verify file size
                var fileInfo = new FileInfo(tempFilePath);
                if (fileInfo.Length != request.TotalFileSize)
                {
                    _logger.LogError(
                        "File size mismatch for job {JobId}. Expected: {ExpectedSize}, Actual: {ActualSize}",
                        request.JobId, request.TotalFileSize, fileInfo.Length);

                    // Clean up temp file
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch
                    {
                    }

                    return new UploadZipChunkResult
                    {
                        Success = false,
                        ChunkIndex = request.ChunkIndex,
                        ErrorMessage = $"File size mismatch. Expected {request.TotalFileSize} bytes but received {fileInfo.Length} bytes.",
                    };
                }

                // Upload to storage
                using (var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var requestedPath = $"zip-uploads/{request.JobId}/{request.FileName}";
                    var actualStoragePath = await _fileStorage.SaveFileAsync(fileStream, requestedPath, "application/zip", null, cancellationToken);

                    // Update job with storage path and mark as pending
                    job.StoragePath = actualStoragePath;
                    job.Status = Domain.Common.Enums.JobStatus.Pending;
                    await context.SaveChangesAsync(cancellationToken);

                    // Enqueue Hangfire job directly instead of relying on background service polling
                    Hangfire.BackgroundJob.Enqueue<IZipUploadJobService>(
                        s => s.ProcessJobAsync(job.Id));

                    _logger.LogInformation("Job {JobId} upload completed and marked as Pending", request.JobId);
                }

                // Clean up temp file
                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temp file for job {JobId}", request.JobId);
                }
            }

            return new UploadZipChunkResult
            {
                Success = true,
                ChunkIndex = request.ChunkIndex,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chunk {ChunkIndex} for job {JobId}", request.ChunkIndex, request.JobId);

            return new UploadZipChunkResult
            {
                Success = false,
                ChunkIndex = request.ChunkIndex,
                ErrorMessage = ex.Message,
            };
        }
    }
}
