using Collectibles.Application.Interfaces;
using Collectibles.Application.Mappings.Explicit;
using Collectibles.Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Showcases.Queries;

public record GetShowcaseByIdQuery(long Id) : IRequest<ShowcaseDetailDto?>;

public class GetShowcaseByIdQueryHandler : IRequestHandler<GetShowcaseByIdQuery, ShowcaseDetailDto?>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly IEventLogService _eventLogService;
    private readonly IShowcaseMappingService _showcaseMappingService;
    private readonly ICurrentUserService _currentUserService;

    public GetShowcaseByIdQueryHandler(
        IApplicationDbContextFactory contextFactory,
        IEventLogService eventLogService,
        IShowcaseMappingService showcaseMappingService,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _eventLogService = eventLogService;
        _showcaseMappingService = showcaseMappingService;
        _currentUserService = currentUserService;
    }

    public async Task<ShowcaseDetailDto?> Handle(GetShowcaseByIdQuery request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Use AsNoTracking for read-only query
        // Split query behavior is configured globally in DbContext to prevent cartesian explosion
        var showcase = await context.Showcases
            .AsNoTracking() // Read-only query - no change tracking needed
            .Include(s => s.PreviewImage)
                .ThenInclude(p => p!.AttachmentContent)
            .Include(s => s.PreviewImage)
                .ThenInclude(p => p!.AttachmentPreview)
            .Include(s => s.ShowcaseTags)
                .ThenInclude(st => st.Tag)

            // Item cards render from the thumbnail only.
            .Include(s => s.CollectibleItems.Where(ci => ci.ParentId == null))
                .ThenInclude(ci => ci.PreviewImage)
                    .ThenInclude(p => p!.AttachmentPreview)

            // Attachment originals are served by the attachment endpoints, never inlined
            // into this DTO, so only the thumbnail is loaded here.
            .Include(s => s.CollectibleItems.Where(ci => ci.ParentId == null))
                .ThenInclude(ci => ci.CollectibleItemAttachments)
                    .ThenInclude(cia => cia.Attachment)
                        .ThenInclude(a => a!.AttachmentPreview)
            .Include(s => s.CollectibleItems.Where(ci => ci.ParentId == null))
                .ThenInclude(ci => ci.CollectibleItemTags)
                    .ThenInclude(cit => cit.Tag)
            .Include(s => s.CollectibleItems.Where(ci => ci.ParentId == null))
                .ThenInclude(ci => ci.Children)
            .Include(s => s.CollectibleItems.Where(ci => ci.ParentId == null))
                .ThenInclude(ci => ci.ContentType)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (showcase == null)
        {
            return null;
        }

        // Visibility check: private showcases are only visible to the owner or administrators
        if (showcase.IsPrivate && showcase.UserId != _currentUserService.UserId && !_currentUserService.IsAdministrator)
        {
            return null;
        }

        // Log the view event
        await _eventLogService.LogEventAsync(
            EventAction.View,
            nameof(Showcase),
            showcase.Id,
            showcase.Name,
            cancellationToken: cancellationToken);

        // Use the mapping service to map the showcase to DetailDto
        var dto = await _showcaseMappingService.MapToDetailDtoAsync(showcase, cancellationToken);

        // Compute recursive statistics across all items in this showcase (not just root)
        var allItemIds = await context.CollectibleItems
            .Where(ci => ci.Showcases.Any(s => s.Id == request.Id))
            .Select(ci => ci.Id)
            .ToListAsync(cancellationToken);

        dto.TotalItemCount = allItemIds.Count;
        dto.TotalAttachmentCount = await context.CollectibleItemAttachments
            .Where(cia => allItemIds.Contains(cia.CollectibleItemId))
            .CountAsync(cancellationToken);
        dto.ItemsWithPreviewCount = await context.CollectibleItems
            .Where(ci => allItemIds.Contains(ci.Id) && ci.PreviewImageId != null)
            .CountAsync(cancellationToken);

        return dto;
    }
}
