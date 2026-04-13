using Collectibles.Domain.Entities;

namespace Collectibles.Application.Mappings.Explicit;

/// <summary>
/// Extension methods for mapping Tag entities to DTOs.
/// </summary>
public static class TagMappingExtensions
{
    /// <summary>
    /// Maps a Tag entity to a TagDto.
    /// </summary>
    /// <param name="entity">The Tag entity to map.</param>
    /// <returns>The mapped TagDto.</returns>
    public static Features.Tags.TagDto ToDto(this Tag entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new Features.Tags.TagDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = null, // Set separately if needed
            UsageCount = 0, // Set separately if needed
        };
    }

    /// <summary>
    /// Maps a Tag entity to an Attachments.TagDto (simplified version used in attachments).
    /// </summary>
    /// <param name="entity">The Tag entity to map.</param>
    /// <returns>The mapped TagDto for attachments.</returns>
    public static Features.Attachments.TagDto ToAttachmentTagDto(this Tag entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new Features.Attachments.TagDto
        {
            Id = entity.Id,
            Name = entity.Name,
        };
    }

    /// <summary>
    /// Maps a collection of Tag entities to TagDtos.
    /// </summary>
    /// <param name="entities">The Tag entities to map.</param>
    /// <returns>A list of mapped TagDtos.</returns>
    public static List<Features.Tags.TagDto> ToDtos(this IEnumerable<Tag> entities)
    {
        return entities?.Select(e => e.ToDto()).ToList() ?? new List<Features.Tags.TagDto>();
    }

    /// <summary>
    /// Maps a collection of Tag entities to Attachment TagDtos.
    /// </summary>
    /// <param name="entities">The Tag entities to map.</param>
    /// <returns>A list of mapped Attachment TagDtos.</returns>
    public static List<Features.Attachments.TagDto> ToAttachmentTagDtos(this IEnumerable<Tag> entities)
    {
        return entities?.Select(e => e.ToAttachmentTagDto()).ToList() ?? new List<Features.Attachments.TagDto>();
    }

    /// <summary>
    /// Maps a Tag entity to a TagDto with usage count calculated from provided data.
    /// </summary>
    /// <param name="entity">The Tag entity to map.</param>
    /// <param name="usageCount">The calculated usage count.</param>
    /// <returns>The mapped TagDto with usage count.</returns>
    public static Features.Tags.TagDto ToDtoWithUsage(this Tag entity, int usageCount)
    {
        var dto = entity.ToDto();
        dto.UsageCount = usageCount;
        return dto;
    }

    /// <summary>
    /// Maps a Tag entity to a TagDto with description.
    /// </summary>
    /// <param name="entity">The Tag entity to map.</param>
    /// <param name="description">The description to include.</param>
    /// <returns>The mapped TagDto with description.</returns>
    public static Features.Tags.TagDto ToDtoWithDescription(this Tag entity, string? description)
    {
        var dto = entity.ToDto();
        dto.Description = description;
        return dto;
    }
}
