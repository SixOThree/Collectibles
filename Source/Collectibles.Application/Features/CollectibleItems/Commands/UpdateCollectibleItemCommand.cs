using Collectibles.Application.Interfaces;
using Collectibles.Application.Services;
using Collectibles.Domain.Entities;
using Collectibles.Domain.ValueObjects.Templates;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.CollectibleItems.Commands;

public class UpdateCollectibleItemCommand : IRequest<Unit>
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? PreviewImageId { get; set; }
    public long? ParentId { get; set; }
    public List<long> AttachmentIds { get; set; } = new();
    public List<long> TagIds { get; set; } = new();
    public List<long> RelatedTagIds { get; set; } = new();
    public long? ContentDefinitionId { get; set; }
    public Dictionary<string, object?> FieldValues { get; set; } = new();
    public List<Dictionary<string, object?>>? FieldValueEntries { get; set; }
    public bool ShowRelatedItemsFirst { get; set; }
}

public class UpdateCollectibleItemCommandHandler : IRequestHandler<UpdateCollectibleItemCommand, Unit>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEventLogService _eventLogService;
    private readonly ICollectibleItemPreviewService _previewService;
    private readonly IBackgroundJobScheduler _backgroundJobScheduler;

    public UpdateCollectibleItemCommandHandler(
        IApplicationDbContextFactory contextFactory,
        ICurrentUserService currentUserService,
        IEventLogService eventLogService,
        ICollectibleItemPreviewService previewService,
        IBackgroundJobScheduler backgroundJobScheduler)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _eventLogService = eventLogService;
        _previewService = previewService;
        _backgroundJobScheduler = backgroundJobScheduler;
    }

    public async Task<Unit> Handle(UpdateCollectibleItemCommand request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var collectibleItem = await context.CollectibleItems
            .Include(ci => ci.CollectibleItemAttachments)
                .ThenInclude(cia => cia.Attachment)
            .Include(ci => ci.CollectibleItemTags)
            .Include(ci => ci.CollectibleItemRelatedTags)
            .FirstOrDefaultAsync(ci => ci.Id == request.Id, cancellationToken);

        if (collectibleItem == null)
        {
            throw new InvalidOperationException($"Collectible item with ID {request.Id} not found.");
        }

        // Check if the current user owns any of the showcases this item belongs to
        var showcases = await context.CollectibleItems
            .Where(ci => ci.Id == collectibleItem.Id)
            .SelectMany(ci => ci.Showcases)
            .ToListAsync(cancellationToken);

        if (!showcases.Any(s => s.UserId == _currentUserService.UserId))
        {
            throw new UnauthorizedAccessException("You don't have permission to update this item.");
        }

        // Capture old values for event logging
        var oldValues = new
        {
            Name = collectibleItem.Name,
            Description = collectibleItem.DetailedDescription,
            PreviewImageId = collectibleItem.PreviewImageId,
            ParentId = collectibleItem.ParentId,
            ContentDefinitionId = collectibleItem.ContentDefinitionId,
            AttachmentIds = collectibleItem.CollectibleItemAttachments.Select(a => a.AttachmentId).ToList(),
            TagIds = collectibleItem.CollectibleItemTags.Select(t => t.TagId).ToList(),
            RelatedTagIds = collectibleItem.CollectibleItemRelatedTags.Select(t => t.TagId).ToList(),
            FieldValues = !string.IsNullOrWhiteSpace(collectibleItem.ContentValue) && collectibleItem.ContentValue.TrimStart().StartsWith('[')
                    ? collectibleItem.GetFieldValueEntries()
                    : (object)collectibleItem.GetFieldValues(),
            ShowRelatedItemsFirst = collectibleItem.ShowRelatedItemsFirst,
        };

        collectibleItem.Name = request.Name;
        collectibleItem.DetailedDescription = request.Description;

        // Validate preview image if provided
        if (request.PreviewImageId.HasValue && request.PreviewImageId.Value > 0)
        {
            // Allow the item's current preview image (e.g. auto-generated collage) or any of its attachments
            var isValidPreview = request.AttachmentIds.Contains(request.PreviewImageId.Value)
                || collectibleItem.PreviewImageId == request.PreviewImageId.Value;
            if (!isValidPreview)
            {
                throw new InvalidOperationException($"Preview image ID {request.PreviewImageId.Value} must be one of the item's attachments.");
            }
        }

        collectibleItem.PreviewImageId = request.PreviewImageId;
        collectibleItem.ParentId = request.ParentId;
        collectibleItem.ContentDefinitionId = request.ContentDefinitionId;
        collectibleItem.ShowRelatedItemsFirst = request.ShowRelatedItemsFirst;

        // Update template and field values
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
        else if (!request.ContentDefinitionId.HasValue)
        {
            // Clear template values if no template is selected
            collectibleItem.ContentValue = null;
        }

        // Update attachments by diffing against the existing junction rows. A clear-and-
        // rebuild would discard the payload those rows carry (IsFeatured, FeaturedDate,
        // DisplayOrder), so curating a featured attachment and then renaming the item
        // silently wiped the curation.
        var requestedAttachmentIds = request.AttachmentIds.ToHashSet();

        var removedLinks = collectibleItem.CollectibleItemAttachments
            .Where(cia => !requestedAttachmentIds.Contains(cia.AttachmentId))
            .ToList();

        foreach (var removed in removedLinks)
        {
            collectibleItem.CollectibleItemAttachments.Remove(removed);
        }

        var retainedAttachmentIds = collectibleItem.CollectibleItemAttachments
            .Select(cia => cia.AttachmentId)
            .ToHashSet();

        var addedAttachmentIds = requestedAttachmentIds.Except(retainedAttachmentIds).ToList();

        if (addedAttachmentIds.Count != 0)
        {
            var attachments = await context.Attachments
                .Where(a => addedAttachmentIds.Contains(a.Id))
                .ToListAsync(cancellationToken);

            foreach (var attachment in attachments)
            {
                collectibleItem.CollectibleItemAttachments.Add(new CollectibleItemAttachment
                {
                    CollectibleItem = collectibleItem,
                    Attachment = attachment,
                });
            }
        }

        // If no preview image is set, use the first available image from the attachments
        if (collectibleItem.PreviewImageId is null or 0 && collectibleItem.CollectibleItemAttachments.Count != 0)
        {
            var firstImage = collectibleItem.CollectibleItemAttachments
                .Select(x => x.Attachment)
                .FirstOrDefault(x => x.FileType != null && x.FileType.StartsWith("image/"));

            if (firstImage != null)
            {
                collectibleItem.PreviewImageId = firstImage.Id;
            }
        }

        // Update tags
        collectibleItem.CollectibleItemTags.Clear();
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

        // Update related tags
        collectibleItem.CollectibleItemRelatedTags.Clear();
        if (request.RelatedTagIds.Count != 0)
        {
            var relatedTags = await context.Tags
                .Where(t => request.RelatedTagIds.Contains(t.Id))
                .ToListAsync(cancellationToken);

            foreach (var tag in relatedTags)
            {
                collectibleItem.CollectibleItemRelatedTags.Add(new CollectibleItemRelatedTag
                {
                    CollectibleItem = collectibleItem,
                    Tag = tag,
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        // Check if the parent item needs a collage preview
        // This happens when the item has no preview but has children with images
        if (collectibleItem.PreviewImageId == null && collectibleItem.ParentId == null)
        {
            // Check if this item has children and might benefit from a collage
            var hasChildrenWithImages = await _previewService.NeedsCollagePreviewAsync(collectibleItem.Id, cancellationToken);
            if (hasChildrenWithImages)
            {
                // Queue the collage generation so it runs in its own scope. Running it via
                // Task.Run captured this request's scoped preview service, whose DbContext
                // is disposed once the request ends.
                var itemId = collectibleItem.Id;
                _backgroundJobScheduler.Enqueue<ICollectibleItemPreviewService>(
                    service => service.GenerateCollagePreviewAsync(itemId, CancellationToken.None, false, null));
            }
        }

        // Log the update event
        var newValues = new
        {
            Name = request.Name,
            Description = request.Description,
            PreviewImageId = request.PreviewImageId,
            ParentId = request.ParentId,
            ContentDefinitionId = request.ContentDefinitionId,
            AttachmentIds = request.AttachmentIds,
            TagIds = request.TagIds,
            RelatedTagIds = request.RelatedTagIds,
            FieldValues = request.FieldValues,
            ShowRelatedItemsFirst = request.ShowRelatedItemsFirst,
        };

        await _eventLogService.LogEventAsync(
            EventAction.Update,
            nameof(CollectibleItem),
            collectibleItem.Id,
            collectibleItem.Name,
            oldValues,
            newValues,
            cancellationToken: cancellationToken);

        return Unit.Value;
    }
}
