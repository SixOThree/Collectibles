using Collectibles.Application.Features.CollectibleItems.Commands;
using Collectibles.Application.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Collectibles.Web.Endpoints;

public static class CollectibleItemEndpoints
{
    public static IEndpointRouteBuilder MapCollectibleItemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/collectible-items/{hash}/delete", DeleteCollectibleItem)
            .WithName("DeleteCollectibleItem")
            .WithTags("CollectibleItems")
            .RequireAuthorization("ApiKeyOrCookie")
            .DisableAntiforgery()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> DeleteCollectibleItem(
        string hash,
        [FromServices] IHashIdsService hashIdsService,
        [FromServices] IMediator mediator)
    {
        try
        {
            var itemId = hashIdsService.Decode(hash);
            if (itemId == 0)
            {
                return Results.NotFound("Invalid item identifier");
            }

            var result = await mediator.Send(new DeleteCollectibleItemCommand { Id = itemId });

            if (!result.Success)
            {
                return Results.BadRequest(new { error = result.ErrorMessage });
            }

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting collectible item {Hash}", hash);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
