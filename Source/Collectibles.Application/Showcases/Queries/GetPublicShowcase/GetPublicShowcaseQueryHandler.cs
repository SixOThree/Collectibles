using Collectibles.Application.Services;
using Collectibles.Domain.Constants;
using Collectibles.Domain.Interfaces;

using MediatR;

namespace Collectibles.Application.Showcases.Queries.GetPublicShowcase;

public class GetPublicShowcaseQueryHandler : IRequestHandler<GetPublicShowcaseQuery, PublicShowcaseDto?>
{
    private readonly IShowcaseShareTokenRepository _shareTokenRepository;
    private readonly IHashIdsService _hashIdsService;

    public GetPublicShowcaseQueryHandler(
        IShowcaseShareTokenRepository shareTokenRepository,
        IHashIdsService hashIdsService)
    {
        _shareTokenRepository = shareTokenRepository;
        _hashIdsService = hashIdsService;
    }

    public async Task<PublicShowcaseDto?> Handle(GetPublicShowcaseQuery request, CancellationToken cancellationToken)
    {
        // Get the active token with showcase data
        var shareToken = await _shareTokenRepository.GetActiveTokenAsync(request.Token, cancellationToken);

        if (shareToken == null || shareToken.Showcase == null)
        {
            return null;
        }

        // Increment view count
        await _shareTokenRepository.IncrementViewCountAsync(request.Token, cancellationToken);

        var showcase = shareToken.Showcase;

        // Map to DTO
        var dto = new PublicShowcaseDto
        {
            HashId = _hashIdsService.Encode(showcase.Id),
            Name = showcase.Name,
            Description = showcase.Description,
            PreviewImageUrl = showcase.PreviewImage != null
                ? $"{ApplicationConstants.ApiRoutes.PublicAttachmentUrlPath}{_hashIdsService.Encode(showcase.PreviewImage.Id)}/preview/{request.Token}"
                : null,
            Tags = showcase.ShowcaseTags?.Select(st => st.Tag?.Name ?? string.Empty).Where(t => !string.IsNullOrEmpty(t)).ToList() ?? new List<string>(),
        };

        // Map collectible items
        if (showcase.CollectibleItems != null)
        {
            dto.CollectibleItems = showcase.CollectibleItems.Select(item => new PublicCollectibleItemDto
            {
                HashId = _hashIdsService.Encode(item.Id),
                Name = item.Name,
                Description = item.DetailedDescription,
                PreviewImageUrl = item.PreviewImage != null
                    ? $"{ApplicationConstants.ApiRoutes.PublicAttachmentUrlPath}{_hashIdsService.Encode(item.PreviewImage.Id)}/preview/{request.Token}"
                    : null,
                Attachments = item.CollectibleItemAttachments?.Select(cia => new PublicAttachmentDto
                {
                    HashId = _hashIdsService.Encode(cia.Attachment.Id),
                    FileName = cia.Attachment.OriginalFilename ?? cia.Attachment.Name,
                    Description = cia.Attachment.Name,
                    ContentType = cia.Attachment.FileType,
                    FileSize = cia.Attachment.FileSize,
                    ThumbnailUrl = !string.IsNullOrEmpty(cia.Attachment.PreviewPath)
                        ? $"{ApplicationConstants.ApiRoutes.PublicAttachmentUrlPath}{_hashIdsService.Encode(cia.Attachment.Id)}/preview/{request.Token}"
                        : null,
                }).ToList() ?? new List<PublicAttachmentDto>(),
            }).ToList();
        }

        return dto;
    }
}
