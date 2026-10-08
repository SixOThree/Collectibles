using Collectibles.Application.Features.Attachments.Queries;
using Collectibles.Application.Interfaces;
using Collectibles.Application.Services;
using Collectibles.Application.Showcases.Queries.GetPublicShowcase;
using Collectibles.Domain.Constants;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using Serilog;

namespace Collectibles.Web.Endpoints;

/// <summary>
/// Defines public API endpoints that don't require authentication.
/// These endpoints are used for sharing showcases and their attachments publicly.
/// </summary>
public static class PublicEndpoints
{
    private const string RoutePrefix = ApplicationConstants.ApiRoutes.PublicApiBase;

    /// <summary>
    /// Maps all public API endpoints.
    /// </summary>
    /// <returns></returns>
    public static IEndpointRouteBuilder MapPublicEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Public preview endpoint - no authentication required
        endpoints.MapGet($"{RoutePrefix}/attachments/{{hash}}/preview/{{token}}", GetPublicAttachmentPreview)
            .AllowAnonymous()
            .RequireRateLimiting("PublicEndpoints")
            .WithName("GetPublicAttachmentPreview")
            .WithTags("Public")
            .Produces<FileResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        // Public thumbnail endpoint - no authentication required
        endpoints.MapGet($"{RoutePrefix}/attachments/{{hash}}/thumbnail/{{token}}", GetPublicAttachmentThumbnail)
            .AllowAnonymous()
            .RequireRateLimiting("PublicEndpoints")
            .WithName("GetPublicAttachmentThumbnail")
            .WithTags("Public")
            .Produces<FileResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    /// <summary>
    /// Gets the preview image for an attachment in a public showcase.
    /// </summary>
    private static async Task<IResult> GetPublicAttachmentPreview(
        string hash,
        string token,
        [FromServices] IHashIdsService hashIdsService,
        [FromServices] IMediator mediator,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IShareAccessContext shareAccessContext)
    {
        try
        {
            // Validate and get attachment
            var validationResult = await ValidatePublicAttachmentAccessAsync(
                hash, token, hashIdsService, mediator, shareAccessContext);

            if (!validationResult.IsValid)
            {
                return Results.NotFound(validationResult.ErrorMessage);
            }

            // Get the attachment with preview data
            var query = new GetAttachmentForPreviewQuery(validationResult.AttachmentId);
            var attachment = await mediator.Send(query);

            if (attachment == null)
            {
                return Results.NotFound("Attachment not found");
            }

            // If there's no preview content, return 404
            if (string.IsNullOrEmpty(attachment.Base64PreviewThumbnail))
            {
                return Results.NotFound("Preview not available");
            }

            // Parse and return the image
            return ServeImage(httpContextAccessor.HttpContext, hash, attachment.Base64PreviewThumbnail);
        }
        catch (UnauthorizedAccessException)
        {
            // The share token was valid but the resource authorization denied the request.
            // Report it as absent rather than as a server fault: this is an access decision,
            // and 404 avoids confirming to an anonymous caller that the attachment exists.
            return Results.NotFound("Preview not available");
        }
        catch (Exception ex)
        {
            // The token is a bearer credential; it must never reach durable log storage.
            Log.Error(ex, "Error serving public preview for attachment {Hash}", hash);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Gets the thumbnail image for an attachment in a public showcase.
    /// </summary>
    private static async Task<IResult> GetPublicAttachmentThumbnail(
        string hash,
        string token,
        [FromServices] IHashIdsService hashIdsService,
        [FromServices] IMediator mediator,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IShareAccessContext shareAccessContext)
    {
        try
        {
            // Validate and get attachment
            var validationResult = await ValidatePublicAttachmentAccessAsync(
                hash, token, hashIdsService, mediator, shareAccessContext);

            if (!validationResult.IsValid)
            {
                return Results.NotFound(validationResult.ErrorMessage);
            }

            // Get the attachment with preview data
            var query = new GetAttachmentForPreviewQuery(validationResult.AttachmentId);
            var attachment = await mediator.Send(query);

            if (attachment == null)
            {
                return Results.NotFound("Attachment not found");
            }

            // If there's no preview content, return 404
            if (string.IsNullOrEmpty(attachment.Base64PreviewThumbnail))
            {
                return Results.NotFound("Thumbnail not available");
            }

            // Parse and return the image
            return ServeImage(httpContextAccessor.HttpContext, hash, attachment.Base64PreviewThumbnail);
        }
        catch (UnauthorizedAccessException)
        {
            // See GetPublicAttachmentPreview: an access decision, not a server fault.
            return Results.NotFound("Thumbnail not available");
        }
        catch (Exception ex)
        {
            // The token is a bearer credential; it must never reach durable log storage.
            Log.Error(ex, "Error serving public thumbnail for attachment {Hash}", hash);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Validates that an attachment can be accessed with a public share token.
    /// </summary>
    private static async Task<AttachmentValidationResult> ValidatePublicAttachmentAccessAsync(
        string hash,
        string token,
        IHashIdsService hashIdsService,
        IMediator mediator,
        IShareAccessContext shareAccessContext)
    {
        // Decode the hash to get the attachment ID
        if (!hashIdsService.TryDecode(hash, out var attachmentId))
        {
            return new AttachmentValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid attachment identifier",
            };
        }

        // Verify the token is valid and the attachment belongs to a public showcase
        var shareTokenQuery = new GetPublicShowcaseQuery { Token = token };
        var publicShowcase = await mediator.Send(shareTokenQuery);

        if (publicShowcase == null)
        {
            return new AttachmentValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid or expired share token",
            };
        }

        // Verify the attachment belongs to this showcase
        var attachmentBelongsToShowcase = CheckAttachmentBelongsToShowcase(
            publicShowcase, hash, hashIdsService);

        if (!attachmentBelongsToShowcase)
        {
            return new AttachmentValidationResult
            {
                IsValid = false,
                ErrorMessage = "Attachment not found in this showcase",
            };
        }

        // The token has now been proven for this showcase. Record it so resource authorization,
        // which runs later and would otherwise see only an anonymous caller, can honour the grant
        // instead of denying a legitimate share link to a private showcase.
        if (hashIdsService.TryDecode(publicShowcase.HashId, out var showcaseId))
        {
            shareAccessContext.GrantShowcaseAccess(showcaseId);
        }

        return new AttachmentValidationResult
        {
            IsValid = true,
            AttachmentId = attachmentId,
        };
    }

    /// <summary>
    /// Checks if an attachment belongs to a specific showcase.
    /// </summary>
    private static bool CheckAttachmentBelongsToShowcase(
        PublicShowcaseDto publicShowcase,
        string hash,
        IHashIdsService hashIdsService)
    {
        // Check showcase preview image
        if (publicShowcase.PreviewImageUrl?.Contains($"/{hash}/") == true)
        {
            return true;
        }

        // Check collectible items and their attachments
        if (publicShowcase.CollectibleItems != null)
        {
            foreach (var item in publicShowcase.CollectibleItems)
            {
                // Check item preview
                if (item.PreviewImageUrl?.Contains($"/{hash}/") == true)
                {
                    return true;
                }

                // Check item attachments
                if (item.Attachments != null)
                {
                    if (item.Attachments.Any(a => a.HashId == hash))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Serves a stored preview image under a signature-derived content type.
    /// </summary>
    private static IResult ServeImage(HttpContext? httpContext, string hash, string base64Data) =>
        AttachmentImageResults.ServeImage(httpContext, hash, base64Data);

    /// <summary>
    /// Result of attachment validation.
    /// </summary>
    private class AttachmentValidationResult
    {
        public bool IsValid { get; set; }
        public long AttachmentId { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
