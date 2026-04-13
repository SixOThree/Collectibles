using Collectibles.Application.Features.Attachments.Queries;
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
    private const string CacheControlHeader = ApplicationConstants.HttpCache.PublicAttachmentCacheHeader;

    /// <summary>
    /// Maps all public API endpoints.
    /// </summary>
    /// <returns></returns>
    public static IEndpointRouteBuilder MapPublicEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Public preview endpoint - no authentication required
        endpoints.MapGet($"{RoutePrefix}/attachments/{{hash}}/preview/{{token}}", GetPublicAttachmentPreview)
            .AllowAnonymous()
            .WithName("GetPublicAttachmentPreview")
            .WithTags("Public")
            .Produces<FileResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        // Public thumbnail endpoint - no authentication required
        endpoints.MapGet($"{RoutePrefix}/attachments/{{hash}}/thumbnail/{{token}}", GetPublicAttachmentThumbnail)
            .AllowAnonymous()
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
        [FromServices] IHttpContextAccessor httpContextAccessor)
    {
        try
        {
            // Validate and get attachment
            var validationResult = await ValidatePublicAttachmentAccessAsync(
                hash, token, hashIdsService, mediator);

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
            var imageResult = ParseBase64Image(attachment.Base64PreviewThumbnail, attachment.FileType);

            // Set cache headers
            SetCacheHeaders(httpContextAccessor.HttpContext, hash);

            return Results.File(imageResult.ImageBytes, imageResult.ContentType);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error serving public preview for attachment {Hash} with token {Token}", hash, token);
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
        [FromServices] IHttpContextAccessor httpContextAccessor)
    {
        try
        {
            // Validate and get attachment
            var validationResult = await ValidatePublicAttachmentAccessAsync(
                hash, token, hashIdsService, mediator);

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
            var imageResult = ParseBase64Image(attachment.Base64PreviewThumbnail, attachment.FileType);

            // Set cache headers
            SetCacheHeaders(httpContextAccessor.HttpContext, hash);

            return Results.File(imageResult.ImageBytes, imageResult.ContentType);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error serving public thumbnail for attachment {Hash} with token {Token}", hash, token);
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
        IMediator mediator)
    {
        // Decode the hash to get the attachment ID
        var attachmentId = hashIdsService.Decode(hash);
        if (attachmentId == 0)
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
                    if (item.Attachments.Any(a => hashIdsService.Encode(a.Id) == hash))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Parses a base64 image string and returns the image bytes and content type.
    /// </summary>
    private static (byte[] ImageBytes, string ContentType) ParseBase64Image(string base64Data, string? fileType)
    {
        // Parse the base64 data URI to get the actual image bytes
        if (base64Data.Contains(','))
        {
            base64Data = base64Data.Split(',')[1];
        }

        var imageBytes = Convert.FromBase64String(base64Data);
        var contentType = fileType ?? "image/jpeg";

        return (imageBytes, contentType);
    }

    /// <summary>
    /// Sets cache headers for the HTTP response.
    /// </summary>
    private static void SetCacheHeaders(HttpContext? httpContext, string hash)
    {
        if (httpContext != null)
        {
            httpContext.Response.Headers.CacheControl = CacheControlHeader;
            httpContext.Response.Headers.ETag = $"\"{hash}\"";
        }
    }

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
