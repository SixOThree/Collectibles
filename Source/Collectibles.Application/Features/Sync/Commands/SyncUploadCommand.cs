using Collectibles.Application.Features.Attachments.Commands;
using Collectibles.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Features.Sync.Commands;

public record SyncUploadResult(
    bool Skipped,
    long? AttachmentId,
    string? UploadId,
    string? SasUrl,
    string? BlobName,
    long? TargetItemId,
    DateTime? ExpiresAt);

public record SyncUploadCommand : IRequest<SyncUploadResult>
{
    public required long ShowcaseId { get; init; }
    public required string RelativePath { get; init; }
    public required string ContentHash { get; init; }
    public required long FileSize { get; init; }
    public required string ContentType { get; init; }
    public string? UserId { get; init; }
}

public class SyncUploadCommandHandler : IRequestHandler<SyncUploadCommand, SyncUploadResult>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IItemHierarchyService _hierarchyService;
    private readonly IMediator _mediator;
    private readonly ILogger<SyncUploadCommandHandler> _logger;

    public SyncUploadCommandHandler(
        IApplicationDbContextFactory contextFactory,
        ICurrentUserService currentUserService,
        IItemHierarchyService hierarchyService,
        IMediator mediator,
        ILogger<SyncUploadCommandHandler> logger)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _hierarchyService = hierarchyService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<SyncUploadResult> Handle(SyncUploadCommand request, CancellationToken ct)
    {
        // Parse path
        var normalizedPath = request.RelativePath.Replace('\\', '/');
        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length < 2)
        {
            throw new ArgumentException(
                "Files must be inside at least one folder. Root-level files are not supported.");
        }

        var fileName = segments[^1];
        var folderSegments = segments[..^1];
        var effectiveUserId = await EnsureAuthorizedShowcaseAccessAsync(
            request.ShowcaseId,
            request.UserId,
            ct);

        // Resolve or create hierarchy
        var targetItemId = await _hierarchyService.ResolveOrCreateHierarchyAsync(
            request.ShowcaseId,
            folderSegments,
            effectiveUserId,
            ct);

        // Check for duplicate
        if (!string.IsNullOrEmpty(request.ContentHash))
        {
            var existingId = await _hierarchyService.FindDuplicateAttachmentAsync(
                targetItemId, request.ContentHash, ct);

            if (existingId.HasValue)
            {
                _logger.LogInformation(
                    "Skipping duplicate: {Path} already exists as attachment {Id}",
                    request.RelativePath, existingId.Value);

                return new SyncUploadResult(
                    Skipped: true,
                    AttachmentId: existingId.Value,
                    UploadId: null,
                    SasUrl: null,
                    BlobName: null,
                    TargetItemId: targetItemId,
                    ExpiresAt: null);
            }
        }

        // Initiate upload — adapt property names to match InitiateDirectUploadCommand
        var initiation = await _mediator.Send(new InitiateDirectUploadCommand
        {
            FileName = fileName,
            FileSize = request.FileSize,
            ContentType = request.ContentType,
            ShowcaseId = request.ShowcaseId
        }, ct);

        return new SyncUploadResult(
            Skipped: false,
            AttachmentId: null,
            UploadId: initiation.UploadId,
            SasUrl: initiation.SasUrl,
            BlobName: initiation.BlobName,
            TargetItemId: targetItemId,
            ExpiresAt: initiation.ExpiresAt);
    }

    private async Task<string> EnsureAuthorizedShowcaseAccessAsync(
        long showcaseId,
        string? requestUserId,
        CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var showcaseOwnerId = await context.Showcases
            .Where(s => s.Id == showcaseId && s.Deleted == null)
            .Select(s => s.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (showcaseOwnerId == null)
        {
            throw new InvalidOperationException($"Showcase {showcaseId} not found.");
        }

        var effectiveUserId = requestUserId ?? _currentUserService.UserId;
        if (string.IsNullOrEmpty(effectiveUserId) || showcaseOwnerId != effectiveUserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to upload files to this showcase.");
        }

        return effectiveUserId;
    }
}
