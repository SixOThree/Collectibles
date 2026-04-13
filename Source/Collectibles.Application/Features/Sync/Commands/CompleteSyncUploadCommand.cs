using Collectibles.Application.Features.Attachments.Commands;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Common.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Features.Sync.Commands;

public record CompleteSyncUploadCommand : IRequest<long>
{
    public required string UploadId { get; init; }
    public required string BlobName { get; init; }
    public required string OriginalFileName { get; init; }
    public required string ContentType { get; init; }
    public required long FileSize { get; init; }
    public required long TargetItemId { get; init; }
    public long? ShowcaseId { get; init; }
    public string? ContentHash { get; init; }
    public AttachmentType? AttachmentType { get; init; }
}

public class CompleteSyncUploadCommandHandler : IRequestHandler<CompleteSyncUploadCommand, long>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IItemHierarchyService _hierarchyService;
    private readonly IMediator _mediator;
    private readonly ILogger<CompleteSyncUploadCommandHandler> _logger;

    public CompleteSyncUploadCommandHandler(
        IApplicationDbContextFactory contextFactory,
        ICurrentUserService currentUserService,
        IItemHierarchyService hierarchyService,
        IMediator mediator,
        ILogger<CompleteSyncUploadCommandHandler> logger)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _hierarchyService = hierarchyService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<long> Handle(CompleteSyncUploadCommand request, CancellationToken ct)
    {
        var authorizedShowcaseId = await ResolveAuthorizedShowcaseIdAsync(
            request.TargetItemId,
            request.ShowcaseId,
            ct);

        // Create the attachment via existing command — adapt property names
        var attachmentId = await _mediator.Send(new CompleteDirectUploadCommand
        {
            UploadId = request.UploadId,
            BlobName = request.BlobName,
            OriginalFileName = request.OriginalFileName,
            ContentType = request.ContentType,
            FileSize = request.FileSize,
            AttachmentType = request.AttachmentType,
            ShowcaseId = authorizedShowcaseId
        }, ct);

        // Link attachment to the target item
        await _hierarchyService.LinkAttachmentAsync(request.TargetItemId, attachmentId, ct);

        _logger.LogInformation(
            "Sync upload complete: attachment {AttachmentId} linked to item {ItemId}",
            attachmentId, request.TargetItemId);

        return attachmentId;
    }

    private async Task<long> ResolveAuthorizedShowcaseIdAsync(
        long targetItemId,
        long? requestedShowcaseId,
        CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var effectiveUserId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(effectiveUserId))
        {
            throw new UnauthorizedAccessException("You are not authorized to complete uploads for this item.");
        }

        var showcaseMemberships = await context.CollectibleItems
            .Where(ci => ci.Id == targetItemId && ci.Deleted == null)
            .SelectMany(
                ci => ci.Showcases.Where(s => s.Deleted == null),
                (ci, showcase) => new
                {
                    ShowcaseId = showcase.Id,
                    showcase.UserId,
                })
            .ToListAsync(cancellationToken);

        if (showcaseMemberships.Count == 0)
        {
            throw new InvalidOperationException($"Collectible item {targetItemId} not found.");
        }

        var authorizedShowcase = requestedShowcaseId.HasValue
            ? showcaseMemberships.FirstOrDefault(membership =>
                membership.ShowcaseId == requestedShowcaseId.Value &&
                membership.UserId == effectiveUserId)
            : showcaseMemberships.FirstOrDefault(membership => membership.UserId == effectiveUserId);

        if (authorizedShowcase == null)
        {
            throw new UnauthorizedAccessException("You are not authorized to complete uploads for this item.");
        }

        return authorizedShowcase.ShowcaseId;
    }
}
