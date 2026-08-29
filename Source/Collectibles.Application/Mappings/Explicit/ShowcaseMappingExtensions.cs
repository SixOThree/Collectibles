using Collectibles.Application.Features.Showcases;
using Collectibles.Application.Features.Tags;
using Collectibles.Domain.Constants;
using Collectibles.Domain.Entities;

namespace Collectibles.Application.Mappings.Explicit;

/// <summary>
/// Extension methods for mapping Showcase entities to DTOs.
/// </summary>
public static class ShowcaseMappingExtensions
{
    /// <summary>
    /// Maps a Showcase entity to a basic DTO.
    /// </summary>
    /// <returns></returns>
    public static ShowcaseDto ToDto(this Showcase entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ShowcaseDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            ItemCount = entity.CollectibleItems?.Count(ci => ci.Deleted == null) ?? 0,
            Tags = entity.ShowcaseTags?
                .Select(st => st.Tag.ToDto())
                .ToList() ?? new List<TagDto>(),
            CreatedDate = entity.Created ?? DateTime.MinValue,
            LastModifiedDate = entity.LastModified,
        };
    }

    /// <summary>
    /// Maps a Showcase entity to a card DTO (without async operations).
    /// </summary>
    /// <returns></returns>
    public static ShowcaseCardDto ToCardDto(this Showcase entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ShowcaseCardDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            UserId = entity.UserId,
            ItemCount = entity.CollectibleItems?.Count(ci => ci.ParentId == null) ?? 0,
            TopTags = entity.ShowcaseTags?
                .OrderBy(st => st.Tag.Name)
                .Take(ApplicationConstants.Pagination.ShowcasePreviewItemCount)
                .Select(st => st.Tag.Name)
                .ToList() ?? new List<string>(),
            OwnerDisplayName = null, // Set by service
            IsPrivate = entity.IsPrivate,
        };
    }

    /// <summary>
    /// Maps a Showcase entity to a detail DTO (without navigation properties).
    /// </summary>
    /// <returns></returns>
    public static ShowcaseDetailDto ToDetailDto(this Showcase entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ShowcaseDetailDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            UserId = entity.UserId,
            IsPrivate = entity.IsPrivate,
            SortOrder = entity.SortOrder,
            PreviewImage = null, // Set separately
            Items = new List<Features.Showcases.CollectibleItemDto>(), // Mapped separately
            Tags = entity.ShowcaseTags?
                .Select(st => st.Tag.ToDto())
                .ToList() ?? new List<TagDto>(),
            CreatedDate = entity.Created ?? DateTime.MinValue,
            LastModifiedDate = entity.LastModified,
        };
    }

    /// <summary>
    /// Maps a ShowcaseTag entity to DTO.
    /// </summary>
    /// <returns></returns>
    public static ShowcaseTagDto ToDto(this ShowcaseTag entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ShowcaseTagDto
        {
            Id = entity.TagId,
            Name = entity.Tag?.Name ?? string.Empty,
            ShowcaseCount = 0, // Set separately if needed
        };
    }
}
