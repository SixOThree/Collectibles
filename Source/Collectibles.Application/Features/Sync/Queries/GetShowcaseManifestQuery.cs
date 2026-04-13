using Collectibles.Application.Interfaces;
using Collectibles.Application.Services;
using Collectibles.Domain.Common.Enums;
using Collectibles.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Sync.Queries;

/// <summary>
/// DTO representing a single attachment in the showcase manifest.
/// </summary>
public record ShowcaseManifestItemDto
{
    public string? AttachmentHashId { get; init; }
    public string? OriginalFilename { get; init; }
    public string? ContentHash { get; init; }
    public long FileSize { get; init; }
    public AttachmentType? AttachmentType { get; init; }
    public string? ItemPath { get; init; }
    public string[] ItemPathSegments { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Query to retrieve a manifest of all attachments in a showcase.
/// Used for sync comparison with local files.
/// </summary>
public record GetShowcaseManifestQuery(long ShowcaseId) : IRequest<List<ShowcaseManifestItemDto>>;

public class GetShowcaseManifestQueryHandler : IRequestHandler<GetShowcaseManifestQuery, List<ShowcaseManifestItemDto>>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHashIdsService _hashIdsService;
    private readonly IEventLogService _eventLogService;

    public GetShowcaseManifestQueryHandler(
        IApplicationDbContextFactory contextFactory,
        IHashIdsService hashIdsService,
        IEventLogService eventLogService,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _hashIdsService = hashIdsService;
        _eventLogService = eventLogService;
        _currentUserService = currentUserService;
    }

    public async Task<List<ShowcaseManifestItemDto>> Handle(
        GetShowcaseManifestQuery request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var showcaseOwnerId = await context.Showcases
            .Where(s => s.Id == request.ShowcaseId && s.Deleted == null)
            .Select(s => s.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (showcaseOwnerId == null)
        {
            return new List<ShowcaseManifestItemDto>();
        }

        if (string.IsNullOrEmpty(_currentUserService.UserId) || showcaseOwnerId != _currentUserService.UserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to access this showcase manifest.");
        }

        // Step 1: Collect root item IDs in the showcase
        var rootItemIds = await context.CollectibleItems
            .Where(i => !i.Deleted.HasValue && i.Showcases.Any(s => s.Id == request.ShowcaseId))
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        if (rootItemIds.Count == 0)
        {
            return new List<ShowcaseManifestItemDto>();
        }

        // Step 2: Recursively collect all descendant IDs (max 10 levels)
        var allItemIds = new HashSet<long>(rootItemIds);
        var frontier = new HashSet<long>(rootItemIds);
        var depth = 0;
        const int maxDepth = 10;

        while (frontier.Count > 0 && depth < maxDepth)
        {
            var childIds = await context.CollectibleItems
                .Where(i => !i.Deleted.HasValue && i.ParentId.HasValue
                          && frontier.Contains(i.ParentId.Value))
                .Select(i => i.Id)
                .ToListAsync(cancellationToken);

            frontier = childIds.Where(id => allItemIds.Add(id)).ToHashSet();
            depth++;
        }

        // Step 3: Load all items with their ParentId for path construction
        var itemLookup = await context.CollectibleItems
            .Where(i => allItemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Name, i.ParentId })
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        // Step 4: Query all attachments for these items
        var attachments = await context.CollectibleItemAttachments
            .Where(cia => allItemIds.Contains(cia.CollectibleItemId))
            .Join(context.Attachments,
                cia => cia.AttachmentId,
                a => a.Id,
                (cia, a) => new
                {
                    cia.CollectibleItemId,
                    a.Id,
                    a.OriginalFilename,
                    a.ContentHash,
                    a.FileSize,
                    a.AttachmentType
                })
            .ToListAsync(cancellationToken);

        // Step 5: Build DTOs with full path segments
        var result = new List<ShowcaseManifestItemDto>();

        foreach (var att in attachments)
        {
            // Build path segments by walking parent chain
            var segments = new List<string>();
            var currentId = att.CollectibleItemId;

            while (itemLookup.TryGetValue(currentId, out var item))
            {
                segments.Add(item.Name);
                if (!item.ParentId.HasValue || !allItemIds.Contains(item.ParentId.Value))
                {
                    break;
                }

                currentId = item.ParentId.Value;
            }

            segments.Reverse();
            var pathSegments = segments.ToArray();
            var itemPath = string.Join(" > ", pathSegments);

            result.Add(new ShowcaseManifestItemDto
            {
                AttachmentHashId = _hashIdsService.Encode(att.Id),
                OriginalFilename = att.OriginalFilename,
                ContentHash = att.ContentHash,
                FileSize = att.FileSize,
                AttachmentType = att.AttachmentType,
                ItemPath = itemPath,
                ItemPathSegments = pathSegments
            });
        }

        // Log the manifest export event (matches existing pattern)
        await _eventLogService.LogEventAsync(
            EventAction.Export,
            "Showcase",
            request.ShowcaseId,
            null,
            null,
            new
            {
                AttachmentCount = result.Count,
                Source = "SyncTool",
            },
            cancellationToken: cancellationToken);

        return result;
    }
}
