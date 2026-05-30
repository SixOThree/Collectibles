using Collectibles.Application.Features.CollectibleItems;
using Collectibles.Application.Features.CollectibleItems.Queries;
using Collectibles.Domain.Entities;

namespace Collectibles.Application.Mappings.Explicit;

/// <summary>
/// Extension methods for mapping CollectibleItem entities to DTOs.
/// </summary>
public static class CollectibleItemMappingExtensions
{
    /// <summary>
    /// Maps a CollectibleItem entity to a basic DTO.
    /// </summary>
    /// <returns></returns>
    public static CollectibleItemDto ToDto(this CollectibleItem entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new CollectibleItemDto
        {
            Id = entity.Id,
            Name = entity.Name,
            DetailedDescription = entity.DetailedDescription,
            PreviewImageId = entity.PreviewImageId,
            PreviewImageUrl = null, // Set by service with IHashIdsService
            ParentId = entity.ParentId,
            ParentName = entity.Parent?.Name,
            TagCount = entity.CollectibleItemTags?.Count ?? 0,
            AttachmentCount = entity.CollectibleItemAttachments?.Count ?? 0,
            ChildItemCount = entity.Children?.Count(c => c.Deleted == null) ?? 0,
            Created = entity.Created ?? DateTime.MinValue,
            CreatedBy = entity.CreatedBy,
            LastModified = entity.LastModified,
            LastModifiedBy = entity.LastModifiedBy,
            ContentDefinitionId = entity.ContentDefinitionId,
            ContentDefinitionName = entity.ContentType?.Name,
            TemplateBorderColor = entity.ContentType?.BorderColor,
            TemplateIcon = entity.ContentType?.Icon,
            FieldValues = GetFieldValuesAsDictionary(entity),
            EntryCount = GetEntryCount(entity),
        };
    }

    /// <summary>
    /// Maps a CollectibleItem entity to a detailed DTO (without navigation properties).
    /// </summary>
    /// <returns></returns>
    public static CollectibleItemDetailDto ToDetailDto(this CollectibleItem entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new CollectibleItemDetailDto
        {
            Id = entity.Id,
            Name = entity.Name,
            DetailedDescription = entity.DetailedDescription,
            PreviewImageId = entity.PreviewImageId,
            PreviewImage = null, // Set separately if needed
            PreviewImageUrl = null, // Set by service with IHashIdsService
            ParentId = entity.ParentId,
            ParentName = entity.Parent?.Name,
            ParentHierarchy = new List<ParentInfo>(), // Built separately
            Children = new List<CollectibleItemDto>(), // Mapped separately
            ContentDefinitionId = entity.ContentDefinitionId,
            ItemDetailPreviewHeight = entity.ContentType?.ItemDetailPreviewHeight,
            FieldValues = GetFieldValuesAsDictionary(entity),
            FieldValueEntries = GetFieldValueEntries(entity),
            AllowMultipleEntries = GetAllowMultipleEntries(entity),
            ContentType = entity.ContentType?.Name,
            ContentValue = entity.ContentValue,
            Attachments = new List<Features.Attachments.AttachmentBriefDto>(), // Mapped separately
            Tags = new List<Features.Tags.TagDto>(), // Mapped separately
            RelatedTags = new List<Features.Tags.TagDto>(), // Mapped separately
            ExternalReferences = new List<LinkInfoDto>(), // Mapped separately
            Showcases = new List<ShowcaseBriefDto>(), // Mapped separately
            QRCodeId = entity.QRCodeId,
            ShowRelatedItemsFirst = entity.ShowRelatedItemsFirst,
            Created = entity.Created ?? DateTime.MinValue,
            CreatedBy = entity.CreatedBy,
            LastModified = entity.LastModified,
            LastModifiedBy = entity.LastModifiedBy,
            IsDeleted = entity.Deleted != null,
        };
    }

    /// <summary>
    /// Maps a LinkInfo entity to DTO.
    /// </summary>
    /// <returns></returns>
    public static LinkInfoDto ToDto(this LinkInfo entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new LinkInfoDto
        {
            Id = entity.Id,
            Url = entity.Url,
            Title = entity.Title,
            Description = null, // LinkInfo doesn't have Description property
        };
    }

    /// <summary>
    /// Maps a Showcase entity to brief DTO.
    /// </summary>
    /// <returns></returns>
    public static ShowcaseBriefDto ToBriefDto(this Showcase entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ShowcaseBriefDto
        {
            Id = entity.Id,
            Title = entity.Name, // Showcase uses Name, not Title
            Description = entity.Description,
            UserId = entity.UserId,
            IsPrivate = entity.IsPrivate,
            SortOrder = entity.SortOrder,
        };
    }

    /// <summary>
    /// Helper method to extract field values as dictionary.
    /// For multi-entry items, returns the first entry's values for backward compatibility.
    /// </summary>
    private static Dictionary<string, object?> GetFieldValuesAsDictionary(CollectibleItem entity)
    {
        if (IsMultiEntryContent(entity.ContentValue))
        {
            // For multi-entry items, return the first entry's values for backward-compat display
            var entries = entity.GetFieldValueEntries();
            if (entries.Count > 0)
            {
                return new Dictionary<string, object?>(entries.Entries[0].Values);
            }

            return new Dictionary<string, object?>();
        }

        var fieldValues = entity.GetFieldValues();
        if (fieldValues == null)
        {
            return new Dictionary<string, object?>();
        }

        var result = new Dictionary<string, object?>();
        foreach (var kvp in fieldValues.Values)
        {
            result[kvp.Key] = kvp.Value.Value;
        }

        return result;
    }

    /// <summary>
    /// Gets the list of field value entries for multi-entry items. Returns null for single-entry items.
    /// </summary>
    private static List<Dictionary<string, object?>>? GetFieldValueEntries(CollectibleItem entity)
    {
        if (!IsMultiEntryContent(entity.ContentValue))
        {
            return null;
        }

        var entries = entity.GetFieldValueEntries();
        return entries.ToDictionaryList();
    }

    /// <summary>
    /// Gets the entry count for multi-entry items. Returns null for single-entry items.
    /// </summary>
    private static int? GetEntryCount(CollectibleItem entity)
    {
        if (!IsMultiEntryContent(entity.ContentValue))
        {
            return null;
        }

        var entries = entity.GetFieldValueEntries();
        return entries.Count;
    }

    /// <summary>
    /// Checks if the ContentValue contains multi-entry JSON (array format).
    /// </summary>
    private static bool IsMultiEntryContent(string? contentValue)
    {
        return !string.IsNullOrWhiteSpace(contentValue) && contentValue.TrimStart().StartsWith('[');
    }

    /// <summary>
    /// Checks if the item's template allows multiple entries.
    /// </summary>
    private static bool GetAllowMultipleEntries(CollectibleItem entity)
    {
        if (entity.ContentType == null)
        {
            return false;
        }

        var templateDef = entity.ContentType.GetTemplateDefinition();
        return templateDef?.AllowMultipleEntries ?? false;
    }

    /// <summary>
    /// Maps a LinkCache entity to DTO.
    /// </summary>
    /// <returns></returns>
    public static LinkCacheDto ToDto(this LinkCache entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new LinkCacheDto
        {
            Id = entity.Id,
            CachedDate = entity.CachedDate,
            Status = entity.Status,
            CachedContentPath = entity.CachedContentPath,
            ScreenshotPath = entity.ScreenshotPath,
            FailureReason = entity.FailureReason,
        };
    }
}
