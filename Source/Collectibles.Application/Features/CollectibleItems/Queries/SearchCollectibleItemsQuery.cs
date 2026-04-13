using Collectibles.Application.Common.Models;
using Collectibles.Application.Interfaces;
using Collectibles.Application.Mappings.Explicit;
using Collectibles.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.CollectibleItems.Queries;

public class SearchCollectibleItemsQuery : IRequest<PaginatedList<CollectibleItemDto>>
{
    public string? SearchTerm { get; set; }
    public long? ExcludeId { get; set; }
    public List<long>? ShowcaseIds { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class SearchCollectibleItemsQueryHandler : IRequestHandler<SearchCollectibleItemsQuery, PaginatedList<CollectibleItemDto>>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEventLogService _eventLogService;
    private readonly ICollectibleItemMappingService _collectibleItemMappingService;

    public SearchCollectibleItemsQueryHandler(
        IApplicationDbContextFactory contextFactory,
        ICurrentUserService currentUserService,
        IEventLogService eventLogService,
        ICollectibleItemMappingService collectibleItemMappingService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _eventLogService = eventLogService;
        _collectibleItemMappingService = collectibleItemMappingService;
    }

    public async Task<PaginatedList<CollectibleItemDto>> Handle(SearchCollectibleItemsQuery request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.CollectibleItems
            .AsNoTracking() // Read-only search query
            .Include(ci => ci.PreviewImage)
                .ThenInclude(pi => pi!.AttachmentPreview)
            .Include(ci => ci.PreviewImage) // Include for PreviewPath
            .Include(ci => ci.Parent)
            .Include(ci => ci.Showcases)
            .Include(ci => ci.ContentType)
            .Include(ci => ci.CollectibleItemTags)
            .Include(ci => ci.CollectibleItemAttachments)
            .Where(ci => ci.Deleted == null);

        // Only include items from showcases the user has access to
        if (!string.IsNullOrEmpty(_currentUserService.UserId))
        {
            query = query.Where(ci => ci.Showcases.Any(s =>
                s.UserId == _currentUserService.UserId || !s.IsPrivate));
        }
        else
        {
            // Anonymous users can only see public showcases
            query = query.Where(ci => ci.Showcases.Any(s => !s.IsPrivate));
        }

        // Filter to items in specific showcases (e.g. parent selection should only show same-showcase items)
        if (request.ShowcaseIds is { Count: > 0 })
        {
            query = query.Where(ci => ci.Showcases.Any(s => request.ShowcaseIds.Contains(s.Id)));
        }

        // Exclude specific item if requested (useful when selecting parent to avoid circular references)
        if (request.ExcludeId.HasValue)
        {
            query = query.Where(ci => ci.Id != request.ExcludeId.Value);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();
            query = query.Where(ci =>
                (ci.Name != null && ci.Name.Contains(searchTerm)) ||
                (ci.DetailedDescription != null && ci.DetailedDescription.Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Fetch entities from database
        var entities = await query
            .OrderBy(ci => ci.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Use explicit mapping service to map entities to DTOs with preview URLs
        var items = _collectibleItemMappingService.MapManyWithPreviewUrls(entities);

        // Note: ParentName and ContentDefinitionName are already set by the mapping extensions

        // Log the search event
        await _eventLogService.LogEventAsync(
            EventAction.Search,
            nameof(CollectibleItem),
            null,
            null,
            null,
            null,
            System.Text.Json.JsonSerializer.Serialize(new
            {
                SearchTerm = request.SearchTerm,
                ExcludeId = request.ExcludeId,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                ResultCount = items.Count,
                TotalCount = totalCount,
            }),
            cancellationToken);

        return new PaginatedList<CollectibleItemDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
