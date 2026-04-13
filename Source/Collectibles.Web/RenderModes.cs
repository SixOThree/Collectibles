using Microsoft.AspNetCore.Components.Web;

namespace Collectibles.Web;

public static class RenderModes
{
    public static readonly InteractiveServerRenderMode InteractiveServerWithoutPrerender =
        new InteractiveServerRenderMode(prerender: false);
}
