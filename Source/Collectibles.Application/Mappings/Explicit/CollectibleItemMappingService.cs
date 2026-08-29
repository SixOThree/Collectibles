using Collectibles.Application.Features.CollectibleItems;
using Collectibles.Application.Services;
using Collectibles.Domain.Constants;
using Collectibles.Domain.Entities;

using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Mappings.Explicit;

/// <summary>
/// Service for complex CollectibleItem mappings requiring dependency injection.
/// </summary>
public interface ICollectibleItemMappingService
{
    CollectibleItemDto MapWithPreviewUrl(CollectibleItem entity);
    CollectibleItemDetailDto MapDetailWithPreviewUrl(CollectibleItem entity);
    List<CollectibleItemDto> MapManyWithPreviewUrls(IEnumerable<CollectibleItem> entities);
}

public class CollectibleItemMappingService : ICollectibleItemMappingService
{
    private readonly IHashIdsService _hashIdsService;
    private readonly ILogger<CollectibleItemMappingService> _logger;

    public CollectibleItemMappingService(
        IHashIdsService hashIdsService,
        ILogger<CollectibleItemMappingService> logger)
    {
        _hashIdsService = hashIdsService;
        _logger = logger;
    }

    public CollectibleItemDto MapWithPreviewUrl(CollectibleItem entity)
    {
        var dto = entity.ToDto();
        dto.PreviewImageUrl = GeneratePreviewUrl(entity);
        return dto;
    }

    public CollectibleItemDetailDto MapDetailWithPreviewUrl(CollectibleItem entity)
    {
        var dto = entity.ToDetailDto();
        dto.PreviewImageUrl = GeneratePreviewUrl(entity);

        // Map preview image if loaded
        if (entity.PreviewImage != null)
        {
            dto.PreviewImage = entity.PreviewImage.ToBriefDtoWithDatabasePreview();
        }

        // Map children with preview URLs
        if (entity.Children != null)
        {
            dto.Children = entity.Children
                .Select(MapWithPreviewUrl)
                .ToList();
        }

        // Map tags
        if (entity.CollectibleItemTags != null)
        {
            dto.Tags = entity.CollectibleItemTags
                .Where(cit => cit.Tag != null)
                .Select(cit => cit.Tag!.ToDto())
                .ToList();
        }

        // Map related tags
        if (entity.CollectibleItemRelatedTags != null)
        {
            dto.RelatedTags = entity.CollectibleItemRelatedTags
                .Where(cirt => cirt.Tag != null)
                .Select(cirt => cirt.Tag!.ToDto())
                .ToList();
        }

        // Map external references
        if (entity.ExternalReferences != null)
        {
            dto.ExternalReferences = entity.ExternalReferences
                .Select(er => er.ToDto())
                .ToList();
        }

        // Map showcases
        if (entity.Showcases != null)
        {
            dto.Showcases = entity.Showcases
                .Select(s => s.ToBriefDto())
                .ToList();
        }

        return dto;
    }

    public List<CollectibleItemDto> MapManyWithPreviewUrls(IEnumerable<CollectibleItem> entities)
    {
        return entities.Select(MapWithPreviewUrl).ToList();
    }

    private string? GeneratePreviewUrl(CollectibleItem entity)
    {
        try
        {
            // Log for debugging child item preview issues
            if (entity.ParentId.HasValue)
            {
                _logger.LogDebug(
                    "Resolving preview for child item {ItemId} with parent {ParentId}. PreviewImage: {HasPreview}, AttachmentCount: {AttachmentCount}",
                    entity.Id,
                    entity.ParentId.Value,
                    entity.PreviewImage != null,
                    entity.CollectibleItemAttachments?.Count ?? 0);
            }

            // If the item has a preview image, return the API endpoint URL
            if (entity.PreviewImage != null)
            {
                var url = $"{ApplicationConstants.ApiRoutes.AttachmentUrlPath}{_hashIdsService.Encode(entity.PreviewImage.Id)}/preview";
                _logger.LogDebug("Generated preview URL for item {ItemId}: {Url}", entity.Id, url);
                return url;
            }

            // If this is a child item with no preview image, use the first image attachment as fallback
            // Only attempt this if the collection is initialized and loaded
            if (entity.ParentId.HasValue && entity.CollectibleItemAttachments != null)
            {
                try
                {
                    _logger.LogDebug(
                        "Attempting fallback preview for child item {ItemId}. Attachments loaded: {AttachmentCount}",
                        entity.Id,
                        entity.CollectibleItemAttachments.Count);

                    var firstImageAttachment = entity.CollectibleItemAttachments
                        .Where(cia => cia?.Attachment?.FileType != null &&
                                    cia.Attachment.FileType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                        .Select(cia => cia.Attachment)
                        .FirstOrDefault();

                    if (firstImageAttachment != null)
                    {
                        var url = $"{ApplicationConstants.ApiRoutes.AttachmentUrlPath}{_hashIdsService.Encode(firstImageAttachment.Id)}/preview";
                        _logger.LogDebug("Generated fallback preview URL for child item {ItemId}: {Url}", entity.Id, url);
                        return url;
                    }
                    else
                    {
                        _logger.LogDebug("No image attachments found for fallback preview for child item {ItemId}", entity.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Exception during attachment access for child item {ItemId}", entity.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during preview URL resolution for item {ItemId}", entity.Id);
        }

        return null;
    }
}
