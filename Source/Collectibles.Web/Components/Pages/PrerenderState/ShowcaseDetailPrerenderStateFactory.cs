using Collectibles.Application.Features.CollectibleItems;
using Collectibles.Application.Features.Showcases;
using Collectibles.Application.Features.Tags;

namespace Collectibles.Web.Components.Pages.PrerenderState;

internal static class ShowcaseDetailPrerenderStateFactory
{
    public static ShowcaseDetailDto? CreatePersistableShowcase(ShowcaseDetailDto? showcase)
    {
        if (showcase == null)
        {
            return null;
        }

        return new ShowcaseDetailDto
        {
            Id = showcase.Id,
            Name = showcase.Name,
            Description = showcase.Description,
            IsPrivate = showcase.IsPrivate,
            SortOrder = showcase.SortOrder,
            CreatedDate = showcase.CreatedDate,
            LastModifiedDate = showcase.LastModifiedDate,
            TotalItemCount = showcase.TotalItemCount,
            TotalAttachmentCount = showcase.TotalAttachmentCount,
            ItemsWithPreviewCount = showcase.ItemsWithPreviewCount,
            Tags = showcase.Tags.Select(CreatePersistableTag).ToList(),
            ItemCards = showcase.ItemCards.Select(CreatePersistableItemCard).ToList(),
            Items = showcase.Items.Select(CreatePersistableItem).ToList(),
        };
    }

    private static Collectibles.Application.Features.Showcases.CollectibleItemDto CreatePersistableItem(Collectibles.Application.Features.Showcases.CollectibleItemDto item)
    {
        return new Collectibles.Application.Features.Showcases.CollectibleItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            CreatedDate = item.CreatedDate,
            ChildItemCount = item.ChildItemCount,
            Tags = item.Tags.Select(CreatePersistableTag).ToList(),
        };
    }

    private static CollectibleItemCardDto CreatePersistableItemCard(CollectibleItemCardDto itemCard)
    {
        return new CollectibleItemCardDto
        {
            Id = itemCard.Id,
            Name = itemCard.Name,
            Description = itemCard.Description,
            PreviewImageUrl = itemCard.PreviewImageUrl,
            AttachmentCount = itemCard.AttachmentCount,
            ChildItemCount = itemCard.ChildItemCount,
            CreatedDate = itemCard.CreatedDate,
            ContentDefinitionId = itemCard.ContentDefinitionId,
            TemplateName = itemCard.TemplateName,
            TemplateBorderColor = itemCard.TemplateBorderColor,
            TemplateIcon = itemCard.TemplateIcon,
            Tags = itemCard.Tags.Select(CreatePersistableTagSummary).ToList(),
        };
    }

    private static TagDto CreatePersistableTag(TagDto tag)
    {
        return new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
        };
    }

    private static TagSummaryDto CreatePersistableTagSummary(TagSummaryDto tag)
    {
        return new TagSummaryDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Color = tag.Color,
        };
    }
}
