using Collectibles.Application.Interfaces;
using Collectibles.Application.Mappings.Explicit;
using Collectibles.Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.CollectibleItems.Queries;

public class GetCollectibleItemByIdQuery : IRequest<CollectibleItemDetailDto?>
{
    public long Id { get; set; }
}

public class GetCollectibleItemByIdQueryHandler : IRequestHandler<GetCollectibleItemByIdQuery, CollectibleItemDetailDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICollectibleItemMappingService _collectibleItemMappingService;
    private readonly IEventLogService _eventLogService;
    private readonly ICurrentUserService _currentUserService;

    public GetCollectibleItemByIdQueryHandler(
        IApplicationDbContext context,
        ICollectibleItemMappingService collectibleItemMappingService,
        IEventLogService eventLogService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _collectibleItemMappingService = collectibleItemMappingService;
        _eventLogService = eventLogService;
        _currentUserService = currentUserService;
    }

    public async Task<CollectibleItemDetailDto?> Handle(GetCollectibleItemByIdQuery request, CancellationToken cancellationToken)
    {
        // Use AsNoTracking for read-only query
        // Split query behavior is configured globally in DbContext to prevent cartesian explosion
        var collectibleItem = await _context.CollectibleItems
            .AsNoTracking() // Read-only query - no change tracking needed
            .Include(ci => ci.PreviewImage)
                .ThenInclude(a => a!.AttachmentPreview)
            .Include(ci => ci.Parent)
            .Include(ci => ci.Children)
                .ThenInclude(child => child.PreviewImage)
                    .ThenInclude(a => a!.AttachmentPreview)
            .Include(ci => ci.Children)
                .ThenInclude(child => child.CollectibleItemAttachments)
                    .ThenInclude(cia => cia.Attachment)
                        .ThenInclude(a => a!.AttachmentPreview)
            .Include(ci => ci.Children)
                .ThenInclude(child => child.CollectibleItemTags)
                    .ThenInclude(cit => cit.Tag)
            .Include(ci => ci.Children)
                .ThenInclude(child => child.ContentType)
            .Include(ci => ci.Children)
                .ThenInclude(child => child.Children)

            // Attachment originals are served by the attachment endpoints, never inlined
            // into this DTO, so only the thumbnail is loaded here.
            .Include(ci => ci.CollectibleItemAttachments)
                .ThenInclude(cia => cia.Attachment)
                .ThenInclude(a => a.AttachmentPreview)
            .Include(ci => ci.CollectibleItemTags)
                .ThenInclude(cit => cit.Tag)
            .Include(ci => ci.CollectibleItemRelatedTags)
                .ThenInclude(cirt => cirt.Tag)
            .Include(ci => ci.ExternalReferences)
            .Include(ci => ci.Showcases)
            .Include(ci => ci.ContentType)
            .FirstOrDefaultAsync(ci => ci.Id == request.Id, cancellationToken);

        if (collectibleItem == null)
        {
            return null;
        }

        // Visibility check: item must be in a showcase the user owns or a public showcase
        var hasAccess = collectibleItem.Showcases.Any(s =>
            s.UserId == _currentUserService.UserId || !s.IsPrivate);

        if (!hasAccess && collectibleItem.Showcases.Count > 0)
        {
            return null;
        }

        var dto = _collectibleItemMappingService.MapDetailWithPreviewUrl(collectibleItem);

        // Map the attachments with their featured status
        dto.Attachments = collectibleItem.CollectibleItemAttachments
            .Select(cia => cia.Attachment!.ToBriefDtoWithDatabasePreview(cia.IsFeatured))
            .ToList();

        // Build parent hierarchy
        dto.ParentHierarchy = await BuildParentHierarchy(collectibleItem.ParentId, cancellationToken);

        // Log the view event
        await _eventLogService.LogEventAsync(
            EventAction.View,
            nameof(CollectibleItem),
            collectibleItem.Id,
            collectibleItem.Name,
            cancellationToken: cancellationToken);

        return dto;
    }

    private async Task<List<ParentInfo>> BuildParentHierarchy(long? parentId, CancellationToken cancellationToken)
    {
        var hierarchy = new List<ParentInfo>();

        if (!parentId.HasValue)
        {
            return hierarchy;
        }

        var visitedIds = new HashSet<long>();
        var currentParentId = parentId;
        var level = 0;

        while (currentParentId.HasValue && !visitedIds.Contains(currentParentId.Value))
        {
            visitedIds.Add(currentParentId.Value);

            var parent = await _context.CollectibleItems
                .Where(ci => ci.Id == currentParentId.Value)
                .Select(ci => new { ci.Id, ci.Name, ci.ParentId })
                .FirstOrDefaultAsync(cancellationToken);

            if (parent == null)
            {
                break;
            }

            hierarchy.Insert(0, new ParentInfo
            {
                Id = parent.Id,
                Name = parent.Name,
                Level = level++,
            });

            currentParentId = parent.ParentId;
        }

        return hierarchy;
    }
}
