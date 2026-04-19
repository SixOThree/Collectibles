using Collectibles.Application.Features.CollectibleItems;
using Collectibles.Application.Features.Attachments;

namespace Collectibles.Web.Components.Pages.PrerenderState;

public sealed class CollectibleItemDetailPrerenderState
{
    public CollectibleItemDetailDto? Item { get; set; }

    public bool AccessDenied { get; set; }

    public bool CanEdit { get; set; }

    public bool CanView { get; set; }

    public string? ShowcaseHash { get; set; }

    public ShowcaseBriefDto? ContextShowcase { get; set; }

    public string? TemplateName { get; set; }

    public bool HideAttachments { get; set; }

    public List<CollectibleItemCardDto> ChildItemCards { get; set; } = new();

    public List<AttachmentBriefDto> RelatedAttachments { get; set; } = new();

    public string ShowcaseOwnerName { get; set; } = "Unknown";
}
