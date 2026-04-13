using System.Text.Json;

using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Interfaces;
using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Features.ZipUpload.Commands;

public class CreateZipUploadJobCommand : IRequest<long>
{
    public long ShowcaseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Base64Content { get; set; } = string.Empty;
    public string? UserId { get; set; } // Optional UserId to handle Blazor context issues
}

public class CreateZipUploadJobCommandHandler : IRequestHandler<CreateZipUploadJobCommand, long>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<CreateZipUploadJobCommandHandler> _logger;
    private readonly IEventLogService _eventLogService;

    public CreateZipUploadJobCommandHandler(
        IApplicationDbContextFactory contextFactory,
        ICurrentUserService currentUserService,
        IFileStorage fileStorage,
        ILogger<CreateZipUploadJobCommandHandler> logger,
        IEventLogService eventLogService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _fileStorage = fileStorage;
        _logger = logger;
        _eventLogService = eventLogService;
    }

    public async Task<long> Handle(CreateZipUploadJobCommand request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Use the provided UserId if available, otherwise fall back to CurrentUserService
        var userId = request.UserId ?? _currentUserService.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User context not available. Please ensure you are logged in.");
        }

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

        // Store the zip file for background processing
        var fileBytes = Convert.FromBase64String(request.Base64Content);
        var requestedPath = $"zip-uploads/{job.Id}/{request.FileName}";

        _logger.LogInformation("Saving zip file for job {JobId} with requested path: {RequestedPath}", job.Id, requestedPath);

        var actualStoragePath = await _fileStorage.SaveFileAsync(fileBytes, requestedPath, "application/zip", null, cancellationToken);

        _logger.LogInformation("Zip file saved for job {JobId} with actual storage path: {ActualPath}", job.Id, actualStoragePath);

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
}
