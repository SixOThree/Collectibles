using System.Text.Json;

using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Features.ZipUpload.Commands;

public class InitializeChunkedUploadCommand : IRequest<long>
{
    public long ShowcaseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? UserId { get; set; } // Optional UserId to handle Blazor context issues
}

public class InitializeChunkedUploadCommandHandler : IRequestHandler<InitializeChunkedUploadCommand, long>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<InitializeChunkedUploadCommandHandler> _logger;
    private readonly IEventLogService _eventLogService;

    public InitializeChunkedUploadCommandHandler(
        IApplicationDbContextFactory contextFactory,
        ICurrentUserService currentUserService,
        ILogger<InitializeChunkedUploadCommandHandler> logger,
        IEventLogService eventLogService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _logger = logger;
        _eventLogService = eventLogService;
    }

    public async Task<long> Handle(InitializeChunkedUploadCommand request, CancellationToken cancellationToken)
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

        _logger.LogInformation(
            "Initialized chunked upload job {JobId} for file {FileName} ({FileSize} bytes)",
            job.Id, request.FileName, request.FileSize);

        await _eventLogService.LogEventAsync(
            EventAction.Upload,
            entityType: "ZipUploadJob",
            entityId: job.Id,
            entityName: request.FileName,
            additionalData: JsonSerializer.Serialize(new { Action = "ChunkedUploadInitialized", ShowcaseId = request.ShowcaseId, FileSize = request.FileSize }),
            cancellationToken: cancellationToken);

        return job.Id;
    }
}
