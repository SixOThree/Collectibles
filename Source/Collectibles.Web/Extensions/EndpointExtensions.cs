using Collectibles.Domain.Configuration;
using Collectibles.Web.Endpoints;

namespace Collectibles.Web.Extensions;

/// <summary>
/// Extension methods for configuring API endpoints.
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Maps all API endpoints for the application.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder MapApiEndpoints(this WebApplication app)
    {
        // Map public endpoints (no authentication required)
        app.MapPublicEndpoints();

        // Map attachment endpoints (authentication handled internally)
        app.MapAttachmentEndpoints();

        // Map sync endpoints (API key or cookie auth) - conditional on SyncTool enabled
        var syncToolSettings = app.Configuration.GetSection("SyncTool").Get<SyncToolSettings>();
        if (syncToolSettings?.Enabled == true)
        {
            app.MapSyncEndpoints();
        }

        // Map collectible item endpoints (API key or cookie auth)
        app.MapCollectibleItemEndpoints();

        // Map link cache endpoints (serves cached HTML/screenshots)
        app.MapLinkCacheEndpoints();

        return app;
    }
}
