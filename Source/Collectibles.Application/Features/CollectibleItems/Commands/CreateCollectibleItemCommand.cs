using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using Collectibles.Domain.ValueObjects.Templates;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.CollectibleItems.Commands;

public class CreateCollectibleItemCommand : IRequest<long>
{
    public long ShowcaseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? PreviewImageId { get; set; }
    public long? ParentId { get; set; }
    public List<long> AttachmentIds { get; set; } = new();
    public List<long> TagIds { get; set; } = new();
    public long? ContentDefinitionId { get; set; }
    public Dictionary<string, object?> FieldValues { get; set; } = new();
    public List<Dictionary<string, object?>>? FieldValueEntries { get; set; }
    public string? UserId { get; set; } // Optional UserId to handle Blazor context issues
}

public class CreateCollectibleItemCommandHandler(
    IApplicationDbContextFactory contextFactory,
    IEventLogService eventLogService,
    ICurrentUserService currentUserService) : IRequestHandler<CreateCollectibleItemCommand, long>
{
    private readonly IApplicationDbContextFactory _contextFactory = contextFactory;
    private readonly IEventLogService _eventLogService = eventLogService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<long> Handle(CreateCollectibleItemCommand request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Use the provided UserId if available, otherwise fall back to CurrentUserService
        var userId = request.UserId ?? _currentUserService.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User context not available. Please ensure you are logged in.");
        }

        var showcase = await context.Showcases
            .FirstOrDefaultAsync(s => s.Id == request.ShowcaseId, cancellationToken);

        if (showcase == null)
        {
            throw new InvalidOperationException($"Showcase with ID {request.ShowcaseId} not found.");
        }

        // Validate preview image if provided
        if (request.PreviewImageId.HasValue && request.PreviewImageId.Value > 0)
        {
            // Ensure the preview image is one of the provided attachments
            if (!request.AttachmentIds.Contains(request.PreviewImageId.Value))
            {
                throw new InvalidOperationException($"Preview image ID {request.PreviewImageId.Value} must be one of the item's attachments.");
            }
        }

        var collectibleItem = new CollectibleItem
        {
            Name = request.Name,
            DetailedDescription = request.Description,
            PreviewImageId = request.PreviewImageId,
            ParentId = request.ParentId,
            ContentDefinitionId = request.ContentDefinitionId,

            // Explicitly set audit fields when UserId is provided
            CreatedBy = userId,
            Created = DateTime.UtcNow,
        };

        // If a template is specified, validate and store field values
        if (request.ContentDefinitionId.HasValue)
        {
            var contentDefinition = await context.ContentDefinitions
                .FirstOrDefaultAsync(cd => cd.Id == request.ContentDefinitionId.Value, cancellationToken);

            if (contentDefinition != null)
            {
                if (request.FieldValueEntries is { Count: > 0 })
                {
                    // Multi-entry mode: store as JSON array
                    var entryCollection = FieldValueEntryCollection.FromDictionaryList(request.FieldValueEntries);
                    collectibleItem.SetFieldValueEntries(entryCollection);
                }
                else if (request.FieldValues.Count != 0)
                {
                    // Single-entry mode: store as JSON object
                    var fieldValueCollection = new FieldValueCollection();
                    foreach (var kvp in request.FieldValues)
                    {
                        fieldValueCollection.SetValue(kvp.Key, kvp.Value);
                    }

                    collectibleItem.SetFieldValues(fieldValueCollection);
                }
            }
        }

        collectibleItem.Showcases.Add(showcase);

        if (request.AttachmentIds.Count != 0)
        {
            var attachments = await context.Attachments
                .Where(a => request.AttachmentIds.Contains(a.Id))
                .ToListAsync(cancellationToken);

            foreach (var attachment in attachments)
            {
                collectibleItem.CollectibleItemAttachments.Add(new CollectibleItemAttachment
                {
                    CollectibleItem = collectibleItem,
                    Attachment = attachment,
                    IsFeatured = false,
                    DisplayOrder = 0,
                });
            }

            // If no preview image is set, use the first available image from the attachments
            if (collectibleItem.PreviewImageId is null or 0)
            {
                var firstImage = attachments
                    .FirstOrDefault(a => a.FileType != null && a.FileType.StartsWith("image/"));

                if (firstImage != null)
                {
                    collectibleItem.PreviewImageId = firstImage.Id;
                }
            }
        }

        if (request.TagIds.Count != 0)
        {
            var tags = await context.Tags
                .Where(t => request.TagIds.Contains(t.Id))
                .ToListAsync(cancellationToken);

            foreach (var tag in tags)
            {
                collectibleItem.CollectibleItemTags.Add(new CollectibleItemTag
                {
                    CollectibleItem = collectibleItem,
                    Tag = tag,
                });
            }
        }

        context.CollectibleItems.Add(collectibleItem);
        await context.SaveChangesAsync(cancellationToken);

        // Check if this is a parent item that might benefit from a collage preview
        // Only generate if no preview was explicitly set and this is not a child item
        if (collectibleItem.PreviewImageId == null && collectibleItem.ParentId == null)
        {
            // Check if item has children with images (will be checked by the service)
            // This is a deferred operation - the service will handle it appropriately
            // Note: Actual collage generation would need to be triggered separately
            // as children might be added after the parent is created
        }

        // Log the creation event
        await _eventLogService.LogEventAsync(
            EventAction.Create,
            nameof(CollectibleItem),
            collectibleItem.Id,
            collectibleItem.Name,
            null,
            new
            {
                ShowcaseId = request.ShowcaseId,
                Name = request.Name,
                Description = request.Description,
                PreviewImageId = request.PreviewImageId,
                ParentId = request.ParentId,
                AttachmentCount = request.AttachmentIds.Count,
                TagCount = request.TagIds.Count,
                ContentDefinitionId = request.ContentDefinitionId,
            },
            cancellationToken: cancellationToken);

        return collectibleItem.Id;
    }
}
