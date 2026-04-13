using System.Text.Json;

using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Interfaces;
using FluentValidation;
using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Features.ZipUpload.Commands;

/// <summary>
/// Command to complete a zip direct upload after the client has uploaded to Azure.
/// Creates the ZipUploadJob entity, validates the zip, and enqueues processing.
/// </summary>
public record CompleteZipDirectUploadCommand : IRequest<long>
{
    public long ShowcaseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }

    /// <summary>
    /// The blob name/path where the zip was uploaded via SAS URL.
    /// </summary>
    public string BlobName { get; set; } = string.Empty;

    /// <summary>
    /// Optional UserId to handle Blazor context issues.
    /// </summary>
    public string? UserId { get; set; }
}

public class CompleteZipDirectUploadCommandValidator : AbstractValidator<CompleteZipDirectUploadCommand>
{
    public CompleteZipDirectUploadCommandValidator()
    {
        RuleFor(v => v.ShowcaseId)
            .GreaterThan(0);

        RuleFor(v => v.FileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(v => v.FileSize)
            .GreaterThan(0);

        RuleFor(v => v.BlobName)
            .NotEmpty()
            .MaximumLength(1024);
    }
}

public class CompleteZipDirectUploadCommandHandler : IRequestHandler<CompleteZipDirectUploadCommand, long>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<CompleteZipDirectUploadCommandHandler> _logger;
    private readonly IEventLogService _eventLogService;

    public CompleteZipDirectUploadCommandHandler(
        IApplicationDbContextFactory contextFactory,
        ICurrentUserService currentUserService,
        IFileStorage fileStorage,
        ILogger<CompleteZipDirectUploadCommandHandler> logger,
        IEventLogService eventLogService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _fileStorage = fileStorage;
        _logger = logger;
        _eventLogService = eventLogService;
    }

    public async Task<long> Handle(CompleteZipDirectUploadCommand request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var userId = request.UserId ?? _currentUserService.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User context not available. Please ensure you are logged in.");
        }

        // Verify the blob exists in storage
        var fileExists = await _fileStorage.FileExistsAsync(request.BlobName, cancellationToken);
        if (!fileExists)
        {
            throw new InvalidOperationException(
                $"The zip file was not found in storage. The upload may have failed or the SAS URL expired. " +
                $"Blob: {request.BlobName}");
        }

        // Verify the file size matches
        var actualSize = await _fileStorage.GetFileSizeAsync(request.BlobName, cancellationToken);
        if (actualSize.HasValue && actualSize.Value != request.FileSize)
        {
            _logger.LogError(
                "File size mismatch. Expected: {ExpectedSize}, Actual: {ActualSize}, Blob: {BlobName}",
                request.FileSize, actualSize.Value, request.BlobName);

            throw new InvalidOperationException(
                $"Upload incomplete. Expected {request.FileSize} bytes but received {actualSize.Value} bytes.");
        }

        // Create the job entity
        var job = new ZipUploadJob
        {
            UserId = userId,
            ShowcaseId = request.ShowcaseId,
            FileName = request.FileName,
            FileSize = request.FileSize,
            Status = Domain.Common.Enums.JobStatus.NotStart,
            CreatedBy = userId,
            Created = DateTime.UtcNow,
        };

        context.ZipUploadJobs.Add(job);
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            // Validate that the file is a valid ZIP archive
            await ValidateZipFile(request.BlobName, cancellationToken);

            // Update job with storage path and mark as ready for processing
            job.StoragePath = request.BlobName;
            job.Status = Domain.Common.Enums.JobStatus.Pending;
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Job {JobId} marked as Pending (direct upload) and ready for processing", job.Id);

            await _eventLogService.LogEventAsync(
                EventAction.Upload,
                entityType: "ZipUploadJob",
                entityId: job.Id,
                entityName: request.FileName,
                additionalData: JsonSerializer.Serialize(new { Action = "DirectUploadCompleted", ShowcaseId = request.ShowcaseId, FileSize = request.FileSize }),
                cancellationToken: cancellationToken);

            // Enqueue the job for processing with Hangfire
            BackgroundJob.Enqueue<IZipUploadJobService>(service => service.ProcessJobAsync(job.Id));

            _logger.LogInformation("Job {JobId} enqueued for background processing (direct upload)", job.Id);

            return job.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete zip direct upload for job {JobId}", job.Id);
            await CleanupFailedUpload(request.BlobName, job, context, ex.Message, cancellationToken);
            throw new InvalidOperationException($"Failed to process uploaded zip file: {ex.Message}", ex);
        }
    }

    private async Task ValidateZipFile(string storagePath, CancellationToken cancellationToken)
    {
        // Lightweight validation: check the ZIP magic number (PK\x03\x04) from the first 4 bytes.
        // We avoid downloading the entire file into memory because ZipArchive in Read mode copies
        // the stream into a MemoryStream internally, which fails for files over 2 GB.
        // Full zip integrity is validated later when the background job processes the archive.
        var fileStream = await _fileStorage.GetFileStreamAsync(storagePath, cancellationToken);
        if (fileStream == null)
        {
            throw new InvalidOperationException("Failed to retrieve uploaded file for validation");
        }

        using (fileStream)
        {
            var header = new byte[4];
            var bytesRead = 0;
            while (bytesRead < 4)
            {
                var read = await fileStream.ReadAsync(header, bytesRead, 4 - bytesRead, cancellationToken);
                if (read == 0)
                {
                    break;
                }

                bytesRead += read;
            }

            // ZIP files start with PK\x03\x04 (local file header signature)
            if (bytesRead < 4 || header[0] != 0x50 || header[1] != 0x4B || header[2] != 0x03 || header[3] != 0x04)
            {
                throw new InvalidOperationException("Uploaded file is not a valid ZIP archive");
            }

            _logger.LogInformation("ZIP file header validation successful for {StoragePath}", storagePath);
        }
    }

    private async Task CleanupFailedUpload(string? storagePath, ZipUploadJob job, IApplicationDbContext context, string errorMessage, CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrEmpty(storagePath))
            {
                await _fileStorage.DeleteFileAsync(storagePath, cancellationToken);
                _logger.LogInformation("Cleaned up failed upload file at {StoragePath} for job {JobId}", storagePath, job.Id);
            }

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
