using Collectibles.Application.Features.Showcases;

namespace Collectibles.Web.Components.Pages.PrerenderState;

public sealed class ShowcaseDetailPrerenderState
{
    public ShowcaseDetailDto? Showcase { get; set; }

    public bool AccessDenied { get; set; }

    public bool CanEdit { get; set; }

    public string SearchTerm { get; set; } = string.Empty;

    public List<long> SelectedTagIds { get; set; } = new();

    public int CardImageHeight { get; set; } = 200;

    public int CardMinWidth { get; set; } = 280;
}
