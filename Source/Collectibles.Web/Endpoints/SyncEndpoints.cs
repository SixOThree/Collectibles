using Collectibles.Application.Features.Attachments.Commands;
using Collectibles.Application.Features.Sync.Commands;
using Collectibles.Application.Features.Sync.Queries;
using Collectibles.Application.Services;
using Collectibles.Domain.Common.Enums;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using Serilog;

namespace Collectibles.Web.Endpoints;

/// <summary>
/// Defines API endpoints for local-to-server file sync operations.
/// </summary>
public static class SyncEndpoints
{
    private const string RoutePrefix = "/api/sync";

    /// <summary>
    /// Maps all sync-related endpoints.
    /// </summary>
    /// <returns></returns>
    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet($"{RoutePrefix}/manifest/{{showcaseHashId}}", GetShowcaseManifest)
            .WithName("GetShowcaseManifest")
            .WithTags("Sync")
            .RequireAuthorization("ApiKeyOnly")
            .RequireRateLimiting("ApiEndpoints")
            .DisableAntiforgery()
            .Produces<List<ShowcaseManifestItemDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        endpoints.MapPost($"{RoutePrefix}/attachments/{{hash}}/move", MoveAttachment)
            .WithName("MoveAttachment")
            .WithTags("Sync")
            .RequireAuthorization("ApiKeyOnly")
            .RequireRateLimiting("ApiEndpoints")
            .DisableAntiforgery()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        endpoints.MapPost($"{RoutePrefix}/upload", InitiateSyncUpload)
            .RequireAuthorization("ApiKeyOnly")
            .RequireRateLimiting("ApiEndpoints")
            .DisableAntiforgery()
            .Produces<SyncUploadResult>(200)
            .Produces(400)
            .Produces(401);

        endpoints.MapPost($"{RoutePrefix}/upload/complete", CompleteSyncUpload)
            .RequireAuthorization("ApiKeyOnly")
            .RequireRateLimiting("ApiEndpoints")
            .DisableAntiforgery()
            .Produces<long>(200)
            .Produces(400)
            .Produces(401);

        return endpoints;
    }

    /// <summary>
    /// Moves an attachment to a new path within a showcase.
    /// Creates parent items for folder structure and renames the attachment.
    /// </summary>
    private static async Task<IResult> MoveAttachment(
        string hash,
        [FromBody] MoveAttachmentRequest request,
        [FromServices] IHashIdsService hashIdsService,
        [FromServices] IMediator mediator)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RelativePath))
            {
                return Results.BadRequest(new { error = "Relative path is required." });
            }

            if (!hashIdsService.TryDecode(hash, out var attachmentId))
            {
                return Results.NotFound("Invalid attachment identifier.");
            }

            if (!hashIdsService.TryDecode(request.ShowcaseHashId, out var showcaseId))
            {
                return Results.BadRequest(new { error = "Invalid showcase identifier." });
            }

            await mediator.Send(new MoveAttachmentCommand
            {
                AttachmentId = attachmentId,
                RelativePath = request.RelativePath,
                ShowcaseId = showcaseId,
            });

            return Results.NoContent();
        }
        catch (ArgumentException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error moving attachment {Hash}", hash);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Returns a manifest of all attachments in a showcase for sync comparison.
    /// </summary>
    private static async Task<IResult> GetShowcaseManifest(
        string showcaseHashId,
        [FromServices] IHashIdsService hashIdsService,
        [FromServices] IMediator mediator)
    {
        try
        {
            if (!hashIdsService.TryDecode(showcaseHashId, out var showcaseId))
            {
                return Results.NotFound("Invalid showcase identifier.");
            }

            var query = new GetShowcaseManifestQuery(showcaseId);
            var manifest = await mediator.Send(query);

            return Results.Ok(manifest);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving manifest for showcase {ShowcaseHashId}", showcaseHashId);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> InitiateSyncUpload(
        [FromBody] SyncUploadRequest request,
        [FromServices] IHashIdsService hashIdsService,
        [FromServices] IMediator mediator,
        HttpContext httpContext)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RelativePath))
            {
                return Results.BadRequest(new { error = "RelativePath is required." });
            }

            long? showcaseId = null;
            if (!string.IsNullOrWhiteSpace(request.ShowcaseHashId))
            {
                if (!hashIdsService.TryDecode(request.ShowcaseHashId, out var decodedShowcaseId))
                {
                    return Results.NotFound(new { error = "Invalid showcase ID." });
                }

                showcaseId = decodedShowcaseId;
            }

            if (!showcaseId.HasValue)
            {
                return Results.BadRequest(new { error = "ShowcaseHashId is required." });
            }

            var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var result = await mediator.Send(new SyncUploadCommand
            {
                ShowcaseId = showcaseId.Value,
                RelativePath = request.RelativePath,
                ContentHash = request.ContentHash,
                FileSize = request.FileSize,
                ContentType = request.ContentType,
            });

            return Results.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error initiating sync upload for {Path}", request.RelativePath);
            return Results.Problem("An error occurred during sync upload initiation.");
        }
    }

    private static async Task<IResult> CompleteSyncUpload(
        [FromBody] SyncUploadCompleteRequest request,
        [FromServices] IHashIdsService hashIdsService,
        [FromServices] IMediator mediator)
    {
        try
        {
            long? showcaseId = null;
            if (!string.IsNullOrWhiteSpace(request.ShowcaseHashId))
            {
                showcaseId = hashIdsService.Decode(request.ShowcaseHashId);
            }

            AttachmentType? attachmentType = null;
            if (!string.IsNullOrEmpty(request.AttachmentTypeString)
                && Enum.TryParse<AttachmentType>(request.AttachmentTypeString, true, out var parsed))
            {
                attachmentType = parsed;
            }

            var attachmentId = await mediator.Send(new CompleteSyncUploadCommand
            {
                UploadId = request.UploadId,
                BlobName = request.BlobName,
                OriginalFileName = request.OriginalFileName,
                ContentType = request.ContentType,
                FileSize = request.FileSize,
                TargetItemHashId = request.TargetItemHashId,
                ShowcaseId = showcaseId,
                ContentHash = request.ContentHash,
                AttachmentType = attachmentType,
            });

            return Results.Ok(new { attachmentId });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error completing sync upload for {BlobName}", request.BlobName);
            return Results.Problem("An error occurred during sync upload completion.");
        }
    }
}

public record MoveAttachmentRequest(string RelativePath, string ShowcaseHashId);

public record SyncUploadRequest(
    string ShowcaseHashId,
    string RelativePath,
    string ContentHash,
    long FileSize,
    string ContentType);

public record SyncUploadCompleteRequest(
    string UploadId,
    string BlobName,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    string TargetItemHashId,
    string ShowcaseHashId,
    string? ContentHash,
    string? AttachmentTypeString);
