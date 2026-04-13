using Collectibles.Application.Features.Attachments;
using Collectibles.Domain.Entities;

namespace Collectibles.Application.Mappings.Explicit;

/// <summary>
/// Extension methods for mapping Attachment entities to DTOs.
/// For async operations involving file storage, use AttachmentMappingService instead.
/// </summary>
public static class AttachmentMappingExtensions
{
    /// <summary>
    /// Maps an Attachment entity to an AttachmentDto without loading content.
    /// Content and preview should be loaded separately using AttachmentMappingService.
    /// </summary>
    /// <param name="entity">The Attachment entity to map.</param>
    /// <returns>The mapped AttachmentDto.</returns>
    public static AttachmentDto ToDto(this Attachment entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new AttachmentDto
        {
            Id = entity.Id,
            Name = entity.Name,
            OriginalFilename = entity.OriginalFilename,
            FileType = entity.FileType,
            FileSize = entity.FileSize,
            AttachmentType = entity.AttachmentType,
            Created = entity.Created.MapDateTimeOrMin(),
            CreatedBy = entity.CreatedBy,
            LastModified = entity.LastModified,
            LastModifiedBy = entity.LastModifiedBy,
            IsMigrated = entity.IsMigrated,
            MigrationDate = entity.MigrationDate,
            StorageMode = entity.GetStorageMode(),
            Tags = entity.Tags?.ToAttachmentTagDtos() ?? new List<Features.Attachments.TagDto>(),
            Base64Content = null, // Content should be loaded separately if needed
            Base64PreviewThumbnail = null, // Preview should be loaded separately if needed
        };
    }

    /// <summary>
    /// Maps an Attachment entity to an AttachmentBriefDto for list displays.
    /// </summary>
    /// <param name="entity">The Attachment entity to map.</param>
    /// <returns>The mapped AttachmentBriefDto.</returns>
    public static AttachmentBriefDto ToBriefDto(this Attachment entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new AttachmentBriefDto
        {
            Id = entity.Id,
            Name = entity.Name,
            OriginalFilename = entity.OriginalFilename,
            FileType = entity.FileType,
            AttachmentType = entity.AttachmentType,
            Created = entity.Created.MapDateTimeOrMin(),
            IsFeatured = false, // This should be set from the context (CollectibleItemAttachment)
            Base64PreviewThumbnail = null, // Preview should be loaded separately if needed
        };
    }

    /// <summary>
    /// Maps an Attachment entity to an AttachmentDto with inline database content.
    /// Only use this when content is already loaded from database.
    /// </summary>
    /// <param name="entity">The Attachment entity with loaded content.</param>
    /// <returns>The mapped AttachmentDto with content.</returns>
    public static AttachmentDto ToDtoWithDatabaseContent(this Attachment entity)
    {
        var dto = entity.ToDto();

        // Add database content if available
        if (entity.AttachmentContent?.Content != null)
        {
            dto.Base64Content = entity.AttachmentContent.Content.ToBase64String();
        }

        // Add database preview if available
        if (entity.AttachmentPreview?.PreviewThumbnail != null)
        {
            dto.Base64PreviewThumbnail = entity.AttachmentPreview.PreviewThumbnail.ToBase64String();
        }

        return dto;
    }

    /// <summary>
    /// Maps an Attachment entity to an AttachmentBriefDto with database preview.
    /// Only use this when preview is already loaded from database.
    /// </summary>
    /// <param name="entity">The Attachment entity with loaded preview.</param>
    /// <param name="isFeatured">Whether this attachment is featured.</param>
    /// <returns>The mapped AttachmentBriefDto with preview.</returns>
    public static AttachmentBriefDto ToBriefDtoWithDatabasePreview(this Attachment entity, bool isFeatured = false)
    {
        var dto = entity.ToBriefDto();
        dto.IsFeatured = isFeatured;

        // Add database preview if available
        if (entity.AttachmentPreview?.PreviewThumbnail != null)
        {
            dto.Base64PreviewThumbnail = entity.AttachmentPreview.PreviewThumbnail.ToBase64String();
        }

        return dto;
    }

    /// <summary>
    /// Maps a collection of Attachment entities to AttachmentDtos.
    /// </summary>
    /// <param name="entities">The Attachment entities to map.</param>
    /// <returns>A list of mapped AttachmentDtos.</returns>
    public static List<AttachmentDto> ToDtos(this IEnumerable<Attachment> entities)
    {
        return entities?.Select(e => e.ToDto()).ToList() ?? new List<AttachmentDto>();
    }

    /// <summary>
    /// Maps a collection of Attachment entities to AttachmentBriefDtos.
    /// </summary>
    /// <param name="entities">The Attachment entities to map.</param>
    /// <returns>A list of mapped AttachmentBriefDtos.</returns>
    public static List<AttachmentBriefDto> ToBriefDtos(this IEnumerable<Attachment> entities)
    {
        return entities?.Select(e => e.ToBriefDto()).ToList() ?? new List<AttachmentBriefDto>();
    }
}
