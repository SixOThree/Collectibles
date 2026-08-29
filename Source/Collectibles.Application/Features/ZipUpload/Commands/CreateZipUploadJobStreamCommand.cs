using System.IO.Compression;
using System.Text.Json;

using Collectibles.Application.Interfaces;
using Collectibles.Domain.Common;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Interfaces;

using Hangfire;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Features.ZipUpload.Commands;

public class CreateZipUploadJobStreamCommand : IRequest<long>
{
    public long ShowcaseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public Stream FileStream { get; set; } = null!;
}

public class CreateZipUploadJobStreamCommandHandler : IRequestHandler<CreateZipUploadJobStreamCommand, long>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<CreateZipUploadJobStreamCommandHandler> _logger;
    private readonly IEventLogService _eventLogService;

    public CreateZipUploadJobStreamCommandHandler(
        IApplicationDbContextFactory contextFactory,
        ICurrentUserService currentUserService,
        IFileStorage fileStorage,
        ILogger<CreateZipUploadJobStreamCommandHandler> logger,
        IEventLogService eventLogService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _fileStorage = fileStorage;
        _logger = logger;
        _eventLogService = eventLogService;
    }

    public async Task<long> Handle(CreateZipUploadJobStreamCommand request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Identity always comes from the authenticated principal, never the request.
        var userId = _currentUserService.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User context not available. Please ensure you are logged in.");
        }

        await ZipUploadAuthorization.EnsureShowcaseOwnedAsync(context, request.ShowcaseId, userId, cancellationToken);

        var job = new ZipUploadJob
        {
            UserId = userId,
            ShowcaseId = request.ShowcaseId,
            FileName = request.FileName,
            FileSize = request.FileSize,
            Status = Domain.Common.Enums.JobStatus.NotStart,

            // Explicitly set audit fields when UserId is provided
            CreatedBy = userId,
            Created = DateTime.UtcNow,
        };

        context.ZipUploadJobs.Add(job);
        await context.SaveChangesAsync(cancellationToken);

        string? actualStoragePath = null;

        try
        {
            // Stream the zip file directly to storage for background processing
            var requestedPath = $"zip-uploads/{job.Id}/{SafeFileName.Sanitize(request.FileName)}";

            _logger.LogInformation(
                "Streaming zip file for job {JobId} with requested path: {RequestedPath}, size: {FileSize} bytes",
                job.Id, requestedPath, request.FileSize);

            actualStoragePath = await _fileStorage.SaveFileAsync(request.FileStream, requestedPath, "application/zip", null, cancellationToken);

            _logger.LogInformation("Zip file streamed for job {JobId} with actual storage path: {ActualPath}", job.Id, actualStoragePath);

            // Validate that the file was saved completely
            var savedFileSize = await _fileStorage.GetFileSizeAsync(actualStoragePath, cancellationToken);
            if (savedFileSize == null || savedFileSize != request.FileSize)
            {
                _logger.LogError(
                    "File size mismatch for job {JobId}. Expected: {ExpectedSize}, Actual: {ActualSize}",
                    job.Id, request.FileSize, savedFileSize ?? 0);

                throw new InvalidOperationException($"Upload incomplete. Expected {request.FileSize} bytes but received {savedFileSize ?? 0} bytes.");
            }

            // Validate that the file is a valid ZIP archive
            await ValidateZipFile(actualStoragePath, cancellationToken);

            // Update job with actual storage path
            job.StoragePath = actualStoragePath;
            job.Status = Domain.Common.Enums.JobStatus.Pending; // Now safe to mark as pending
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Job {JobId} marked as Pending and ready for processing", job.Id);

            await _eventLogService.LogEventAsync(
                EventAction.Upload,
                entityType: "ZipUploadJob",
                entityId: job.Id,
                entityName: request.FileName,
                additionalData: JsonSerializer.Serialize(new { ShowcaseId = request.ShowcaseId, FileSize = request.FileSize }),
                cancellationToken: cancellationToken);

            // Enqueue the job for processing with Hangfire
            BackgroundJob.Enqueue<IZipUploadJobService>(service => service.ProcessJobAsync(job.Id));

            _logger.LogInformation("Job {JobId} enqueued for background processing", job.Id);

            return job.Id;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Upload cancelled for job {JobId}", job.Id);
            await CleanupFailedUpload(actualStoragePath, job, context, "Upload was cancelled by user", cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload zip file for job {JobId}", job.Id);
            await CleanupFailedUpload(actualStoragePath, job, context, ex.Message, cancellationToken);
            throw new InvalidOperationException($"Failed to upload file: {ex.Message}", ex);
        }
    }

    private async Task ValidateZipFile(string storagePath, CancellationToken cancellationToken)
    {
        var fileStream = await _fileStorage.GetFileStreamAsync(storagePath, cancellationToken);
        if (fileStream == null)
        {
            throw new InvalidOperationException("Failed to retrieve uploaded file for validation");
        }

        using (fileStream)
        {
            try
            {
                using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: false);

                // If we can open it, it's a valid ZIP file
                _logger.LogInformation("ZIP file validation successful. Archive contains {EntryCount} entries", archive.Entries.Count);
            }
            catch (InvalidDataException ex)
            {
                throw new InvalidOperationException("Uploaded file is not a valid ZIP archive", ex);
            }
        }
    }

    private async Task CleanupFailedUpload(string? storagePath, ZipUploadJob job, IApplicationDbContext context, string errorMessage, CancellationToken cancellationToken)
    {
        try
        {
            // Delete the partial/failed file if it exists
            if (!string.IsNullOrEmpty(storagePath))
            {
                await _fileStorage.DeleteFileAsync(storagePath, cancellationToken);
                _logger.LogInformation("Cleaned up failed upload file at {StoragePath} for job {JobId}", storagePath, job.Id);
            }

            // Mark the job as failed
            job.Status = Domain.Common.Enums.JobStatus.Failed;
            job.ErrorDetails = errorMessage;
            job.CompletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Marked job {JobId} as failed: {ErrorMessage}", job.Id, errorMessage);
        }
        catch (Exception cleanupEx)
        {
            _logger.LogError(cleanupEx, "Failed to cleanup after failed upload for job {JobId}", job.Id);
        }
    }
}
