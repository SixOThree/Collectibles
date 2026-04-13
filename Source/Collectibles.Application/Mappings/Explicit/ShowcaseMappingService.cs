using Collectibles.Application.Features.Showcases;
using Collectibles.Application.Interfaces;
using Collectibles.Application.Services;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Interfaces;

namespace Collectibles.Application.Mappings.Explicit;

/// <summary>
/// Service interface for complex Showcase mappings requiring dependency injection.
/// </summary>
public interface IShowcaseMappingService
{
    Task<ShowcaseCardDto> MapToCardDtoAsync(Showcase entity, CancellationToken cancellationToken = default);
    Task<List<ShowcaseCardDto>> MapManyToCardDtoAsync(IEnumerable<Showcase> entities, CancellationToken cancellationToken = default);
    Task<ShowcaseDetailDto> MapToDetailDtoAsync(Showcase entity, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of showcase mapping service for complex mappings requiring dependency injection.
/// </summary>
public class ShowcaseMappingService : IShowcaseMappingService
{
    private readonly IFileStorage _fileStorage;
    private readonly IUserManagementService _userManagementService;
    private readonly IAttachmentMappingService _attachmentMappingService;
    private readonly ICollectibleItemPreviewResolver _previewResolver;
    private readonly IHashIdsService _hashIdsService;

    public ShowcaseMappingService(
        IFileStorage fileStorage,
        IUserManagementService userManagementService,
        IAttachmentMappingService attachmentMappingService,
        ICollectibleItemPreviewResolver previewResolver,
        IHashIdsService hashIdsService)
    {
        _fileStorage = fileStorage;
        _userManagementService = userManagementService;
        _attachmentMappingService = attachmentMappingService;
        _previewResolver = previewResolver;
        _hashIdsService = hashIdsService;
    }

    public async Task<ShowcaseCardDto> MapToCardDtoAsync(Showcase entity, CancellationToken cancellationToken = default)
    {
        var dto = entity.ToCardDto();

        // Load preview image
        if (entity.PreviewImage != null)
        {
            dto.ImageUrl = await GetPreviewImageUrlAsync(entity.PreviewImage, cancellationToken);
        }

        // Load owner display name
        if (!string.IsNullOrEmpty(dto.UserId))
        {
            var user = await _userManagementService.GetUserByIdAsync(dto.UserId, cancellationToken);
            if (user != null)
            {
                dto.OwnerDisplayName = !string.IsNullOrWhiteSpace(user.DisplayName)
                    ? user.DisplayName
                    : !string.IsNullOrWhiteSpace(user.FullName)
                        ? user.FullName
                        : user.Email;
            }
        }

        return dto;
    }

    public async Task<List<ShowcaseCardDto>> MapManyToCardDtoAsync(IEnumerable<Showcase> entities, CancellationToken cancellationToken = default)
    {
        var showcases = entities.ToList();
        var dtos = new List<ShowcaseCardDto>();

        // First, map all showcases without async operations
        foreach (var showcase in showcases)
        {
            dtos.Add(showcase.ToCardDto());
        }

        // Load all preview images in parallel
        var imageLoadTasks = new List<Task>();
        foreach (var dto in dtos)
        {
            var showcase = showcases.First(s => s.Id == dto.Id);
            if (showcase.PreviewImage != null)
            {
                imageLoadTasks.Add(Task.Run(
                    async () =>
                {
                    dto.ImageUrl = await GetPreviewImageUrlAsync(showcase.PreviewImage, cancellationToken);
                }, cancellationToken));
            }
        }

        await Task.WhenAll(imageLoadTasks);

        // Load user display names
        foreach (var dto in dtos)
        {
            if (!string.IsNullOrEmpty(dto.UserId))
            {
                try
                {
                    var user = await _userManagementService.GetUserByIdAsync(dto.UserId, cancellationToken);
                    dto.OwnerDisplayName = !string.IsNullOrWhiteSpace(user.DisplayName)
                        ? user.DisplayName
                        : !string.IsNullOrWhiteSpace(user.FullName)
                            ? user.FullName
                            : user.Email;
                }
                catch
                {
                    // User not found or error loading - leave OwnerDisplayName as null
                    dto.OwnerDisplayName = null;
                }
            }
        }

        return dtos;
    }

    public async Task<ShowcaseDetailDto> MapToDetailDtoAsync(Showcase entity, CancellationToken cancellationToken = default)
    {
        var dto = entity.ToDetailDto();

        // Map preview image if available
        if (entity.PreviewImage != null)
        {
            dto.PreviewImage = await _attachmentMappingService.MapWithContentAsync(entity.PreviewImage, cancellationToken);
        }

        // Map collectible items using optimized card DTOs
        if (entity.CollectibleItems?.Any() == true)
        {
            dto.ItemCards = new List<Features.CollectibleItems.CollectibleItemCardDto>();

            // Get all item IDs for batch preview resolution
            var itemIds = entity.CollectibleItems
                .Where(ci => ci.Deleted == null)
                .Select(ci => ci.Id)
                .ToList();

            // Batch get preview URLs
            var previewUrls = await _previewResolver.GetPreviewUrlsAsync(itemIds, cancellationToken);

            foreach (var item in entity.CollectibleItems.Where(ci => ci.Deleted == null))
            {
                var itemDto = new Features.CollectibleItems.CollectibleItemCardDto
                {
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.DetailedDescription,
                    CreatedDate = item.Created ?? DateTime.MinValue,
                    ChildItemCount = item.Children?.Count(c => c.Deleted == null) ?? 0,
                    AttachmentCount = item.CollectibleItemAttachments?.Count ?? 0,
                    PreviewImageUrl = previewUrls.GetValueOrDefault(item.Id),
                    Tags = item.CollectibleItemTags?
                        .Select(cit => new Features.CollectibleItems.TagSummaryDto
                        {
                            Id = cit.Tag.Id,
                            Name = cit.Tag.Name,
                            Color = null, // Tag doesn't have Color property yet
                        })
                        .ToList() ?? new List<Features.CollectibleItems.TagSummaryDto>(),
                    ShowcaseId = entity.Id,
                    ShowcaseName = entity.Name,
                    ContentDefinitionId = item.ContentDefinitionId,
                    TemplateName = item.ContentType?.Name,
                    TemplateBorderColor = item.ContentType?.BorderColor,
                    TemplateIcon = item.ContentType?.Icon,
                };

                dto.ItemCards.Add(itemDto);
            }

            // Keep backward compatibility with existing Items property if needed
            // This can be removed once all consumers are updated to use ItemCards
            dto.Items = dto.ItemCards.Select(card => new Features.Showcases.CollectibleItemDto
            {
                Id = card.Id,
                Name = card.Name,
                Description = card.Description,
                CreatedDate = card.CreatedDate,
                ChildItemCount = card.ChildItemCount,
                Tags = card.Tags.Select(t => new Features.Tags.TagDto
                {
                    Id = t.Id,
                    Name = t.Name,
                }).ToList(),
                Attachments = new List<Features.Attachments.AttachmentDto>(), // Empty for performance
                PreviewImage = null, // Will use PreviewImageUrl instead
            }).ToList();
        }

        return dto;
    }

    private async Task<string?> GetPreviewImageUrlAsync(Attachment previewImage, CancellationToken cancellationToken)
    {
        byte[]? imageData = null;

        // First try to get the full-size image from storage (disk or Azure)
        if (!string.IsNullOrEmpty(previewImage.FilePath))
        {
            try
            {
                var fullImageContent = await _fileStorage.GetFileAsync(previewImage.FilePath, cancellationToken);
                if (fullImageContent != null && fullImageContent.Length > 0)
                {
                    imageData = fullImageContent;
                }
            }
            catch
            {
                // If full image fails, fall back to preview
            }
        }

        // If full image not available, check if full image is in database
        if (imageData == null && previewImage.AttachmentContent?.Content != null)
        {
            imageData = previewImage.AttachmentContent.Content;
        }

        // Fall back to preview thumbnail if full image not available
        if (imageData == null)
        {
            // Check if preview is in database
            if (previewImage.AttachmentPreview?.PreviewThumbnail != null)
            {
                imageData = previewImage.AttachmentPreview.PreviewThumbnail;
            }

            // If preview path exists, load from external storage
            else if (!string.IsNullOrEmpty(previewImage.PreviewPath))
            {
                try
                {
                    var previewContent = await _fileStorage.GetFileAsync(previewImage.PreviewPath, cancellationToken);
                    if (previewContent != null && previewContent.Length > 0)
                    {
                        imageData = previewContent;
                    }
                }
                catch
                {
                    // Fail silently for preview thumbnails since they're not critical
                }
            }
        }

        if (imageData != null)
        {
            return $"data:{previewImage.FileType ?? "image/jpeg"};base64,{Convert.ToBase64String(imageData)}";
        }

        return null;
    }
}
