using Collectibles.Application.Interfaces;
using Collectibles.Domain.Configuration;
using Collectibles.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

namespace Collectibles.Web.Endpoints;

public static class LinkCacheEndpoints
{
    private const string RoutePrefix = "/api/link-caches";

    public static IEndpointRouteBuilder MapLinkCacheEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet($"{RoutePrefix}/{{id:long}}/content", GetCachedContent)
            .WithName("GetLinkCacheContent")
            .WithTags("LinkCaches")
            .RequireAuthorization()
            .Produces<FileResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        endpoints.MapGet($"{RoutePrefix}/{{id:long}}/screenshot", GetCachedScreenshot)
            .WithName("GetLinkCacheScreenshot")
            .WithTags("LinkCaches")
            .RequireAuthorization()
            .Produces<FileResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> GetCachedContent(
        long id,
        [FromServices] IApplicationDbContextFactory contextFactory,
        [FromServices] IFileStorage fileStorage,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IOptions<ExternalLinksOptions> externalLinksOptions)
    {
        try
        {
            if (!externalLinksOptions.Value.Enabled || !externalLinksOptions.Value.CachingEnabled)
            {
                return Results.NotFound();
            }

            await using var context = await contextFactory.CreateDbContextAsync();
            var cache = await context.LinkCaches.AsNoTracking()
                .Include(lc => lc.LinkInfo)
                .FirstOrDefaultAsync(lc => lc.Id == id);

            if (cache == null || string.IsNullOrEmpty(cache.CachedContentPath))
            {
                return Results.NotFound("Cache not found");
            }

            // Verify the current user owns the item this link belongs to
            var ownsItem = await context.CollectibleItems
                .Where(ci => ci.Id == cache.LinkInfo.CollectibleItemId)
                .SelectMany(ci => ci.Showcases)
                .AnyAsync(s => s.UserId == currentUserService.UserId);

            if (!ownsItem)
            {
                return Results.Forbid();
            }

            var stream = await fileStorage.GetFileStreamAsync(cache.CachedContentPath);
            if (stream == null)
            {
                return Results.NotFound("Cached file not found in storage");
            }

            return Results.File(stream, "text/html", $"cache-{id}.html");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error serving cached content for link cache {Id}", id);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> GetCachedScreenshot(
        long id,
        [FromServices] IApplicationDbContextFactory contextFactory,
        [FromServices] IFileStorage fileStorage,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IOptions<ExternalLinksOptions> externalLinksOptions)
    {
        try
        {
            if (!externalLinksOptions.Value.Enabled || !externalLinksOptions.Value.CachingEnabled)
            {
                return Results.NotFound();
            }

            await using var context = await contextFactory.CreateDbContextAsync();
            var cache = await context.LinkCaches.AsNoTracking()
                .Include(lc => lc.LinkInfo)
                .FirstOrDefaultAsync(lc => lc.Id == id);

            if (cache == null || string.IsNullOrEmpty(cache.ScreenshotPath))
            {
                return Results.NotFound("Cache not found");
            }

            // Verify the current user owns the item this link belongs to
            var ownsItem = await context.CollectibleItems
                .Where(ci => ci.Id == cache.LinkInfo.CollectibleItemId)
                .SelectMany(ci => ci.Showcases)
                .AnyAsync(s => s.UserId == currentUserService.UserId);

            if (!ownsItem)
            {
                return Results.Forbid();
            }

            var stream = await fileStorage.GetFileStreamAsync(cache.ScreenshotPath);
            if (stream == null)
            {
                return Results.NotFound("Screenshot file not found in storage");
            }

            return Results.File(stream, "image/png", $"screenshot-{id}.png");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error serving screenshot for link cache {Id}", id);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
