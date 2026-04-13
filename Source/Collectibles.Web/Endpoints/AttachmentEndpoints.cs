using Collectibles.Application.Features.Attachments.Commands;
using Collectibles.Application.Features.Attachments.Queries;
using Collectibles.Application.Interfaces;
using Collectibles.Application.Services;
using Collectibles.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Collectibles.Web.Endpoints;

/// <summary>
/// Defines API endpoints for attachment operations including preview and thumbnail generation.
/// </summary>
public static class AttachmentEndpoints
{
    private const string RoutePrefix = ApplicationConstants.ApiRoutes.AttachmentApiBase;
    private const string CacheControlHeader = ApplicationConstants.HttpCache.PublicAttachmentCacheHeader;

    /// <summary>
    /// Maps all attachment-related endpoints.
    /// </summary>
    /// <returns></returns>
    public static IEndpointRouteBuilder MapAttachmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet($"{RoutePrefix}/{{hash}}/preview", GetAttachmentPreview)
            .WithName("GetAttachmentPreview")
            .WithTags("Attachments")
            .AllowAnonymous() // Allow anonymous - we handle authorization internally based on showcase visibility
            .Produces<FileResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        endpoints.MapGet($"{RoutePrefix}/{{hash}}/thumbnail", GetAttachmentThumbnail)
            .WithName("GetAttachmentThumbnail")
            .WithTags("Attachments")
            .AllowAnonymous() // Allow anonymous - we handle authorization internally based on showcase visibility
            .Produces<FileResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        endpoints.MapGet($"{RoutePrefix}/{{hash}}/download", DownloadAttachment)
            .WithName("DownloadAttachment")
            .WithTags("Attachments")
            .AllowAnonymous() // Allow anonymous - we handle authorization internally based on showcase visibility
            .Produces<FileResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        // Direct upload endpoints (bypass Cloudflare for large files)
        // DisableAntiforgery is required because these are called via HttpClient from Blazor components
        // ApiKeyOrCookie policy allows both browser (cookie) and script (API key) authentication
        endpoints.MapPost($"{RoutePrefix}/initiate-upload", InitiateDirectUpload)
            .WithName("InitiateDirectUpload")
            .WithTags("Attachments")
            .RequireAuthorization("ApiKeyOrCookie")
            .DisableAntiforgery()
            .Produces<DirectUploadInitiation>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        endpoints.MapPost($"{RoutePrefix}/complete-upload", CompleteDirectUpload)
            .WithName("CompleteDirectUpload")
            .WithTags("Attachments")
            .RequireAuthorization("ApiKeyOrCookie")
            .DisableAntiforgery()
            .Produces<long>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        endpoints.MapPost($"{RoutePrefix}/{{hash}}/delete", DeleteAttachment)
            .WithName("DeleteAttachment")
            .WithTags("Attachments")
            .RequireAuthorization("ApiKeyOrCookie")
            .DisableAntiforgery()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        endpoints.MapGet($"{RoutePrefix}/{{hash}}/context", GetAttachmentContext)
            .WithName("GetAttachmentContext")
            .WithTags("Attachments")
            .RequireAuthorization("ApiKeyOrCookie")
            .Produces<AttachmentContextDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    /// <summary>
    /// Gets the context of a collectible item owning an attachment.
    /// </summary>
    private static async Task<IResult> GetAttachmentContext(
        string hash,
        [FromServices] IHashIdsService hashIdsService,
        [FromServices] IMediator mediator)
    {
        var attachmentId = hashIdsService.Decode(hash);
        if (attachmentId == 0)
        {
            return Results.NotFound("Invalid attachment identifier");
        }

        var result = await mediator.Send(new GetAttachmentContextQuery(attachmentId));
        return result != null ? Results.Ok(result) : Results.NotFound("Attachment not found");
    }

    /// <summary>
    /// Gets the preview image for an attachment.
    /// </summary>
    private static async Task<IResult> GetAttachmentPreview(
        string hash,
        [FromServices] IHashIdsService hashIdsService,
        [FromServices] IMediator mediator,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IApplicationDbContextFactory contextFactory,
        [FromServices] ICurrentUserService currentUserService)
    {
        try
        {
            // Decode the hash to get the attachment ID
            var attachmentId = hashIdsService.Decode(hash);
            if (attachmentId == 0)
            {
                return Results.NotFound("Invalid attachment identifier");
            }

            // WORKAROUND: Get user ID directly from HttpContext due to service scoping issue
            // CurrentUserService doesn't always get the correct HttpContext in minimal API endpoints
            string? actualUserId = null;
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                actualUserId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var isAuthenticated = httpContext.User?.Identity?.IsAuthenticated ?? false;

                // Log for debugging
                if (!string.IsNullOrEmpty(actualUserId) && string.IsNullOrEmpty(currentUserService.UserId))
                {
                    Log.Debug(
                        "Using direct HttpContext user {ActualUserId} instead of CurrentUserService (which returned null)",
                        actualUserId);
                }
            }

            // Use the actual user ID from HttpContext if available, otherwise fall back to CurrentUserService
            var effectiveUserId = actualUserId ?? currentUserService.UserId;

            // Check authorization with the effective user ID
            var authorizationResult = await CheckAttachmentAuthorizationWithUserIdAsync(
                attachmentId,
                contextFactory,
                effectiveUserId);

            if (!authorizationResult.IsAuthorized)
            {
                // Log detailed authorization failure to syslog
                var endpoint = httpContextAccessor.HttpContext?.Request.Path.Value ?? "unknown";
                var userAgent = httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown";
                var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                Log.Warning(
                    "AUTHORIZATION FAILED - Preview Access Denied | " +
                    "Endpoint: {Endpoint} | " +
                    "AttachmentHash: {Hash} | " +
                    "AttachmentId: {AttachmentId} | " +
                    "StatusCode: {StatusCode} | " +
                    "UserId: {UserId} | " +
                    "IP: {IpAddress} | " +
                    "UserAgent: {UserAgent} | " +
                    "DebugInfo: {DebugInfo}",
                    endpoint,
                    hash,
                    attachmentId,
                    authorizationResult.IsAuthenticated ? 403 : 401,
                    effectiveUserId ?? "Anonymous",
                    ipAddress,
                    userAgent,
                    authorizationResult.DebugInfo);

                return authorizationResult.IsAuthenticated
                    ? Results.Forbid()
                    : Results.Unauthorized();
            }

            // Get the attachment with preview data
            var query = new GetAttachmentForPreviewQuery(attachmentId);
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
            Log.Error(ex, "Error serving preview for attachment {Hash}", hash);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Gets the thumbnail image for an attachment.
    /// </summary>
    private static async Task<IResult> GetAttachmentThumbnail(
        string hash,
        [FromServices] IHashIdsService hashIdsService,
        [FromServices] IMediator mediator,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IApplicationDbContextFactory contextFactory,
        [FromServices] ICurrentUserService currentUserService)
    {
        try
        {
            // Decode the hash to get the attachment ID
            var attachmentId = hashIdsService.Decode(hash);
            if (attachmentId == 0)
            {
                return Results.NotFound("Invalid attachment identifier");
            }

            // WORKAROUND: Get user ID directly from HttpContext due to service scoping issue
            // CurrentUserService doesn't always get the correct HttpContext in minimal API endpoints
            string? actualUserId = null;
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                actualUserId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                // Log for debugging
                if (!string.IsNullOrEmpty(actualUserId) && string.IsNullOrEmpty(currentUserService.UserId))
                {
                    Log.Debug(
                        "Using direct HttpContext user {ActualUserId} instead of CurrentUserService (which returned null)",
                        actualUserId);
                }
            }

            // Use the actual user ID from HttpContext if available, otherwise fall back to CurrentUserService
            var effectiveUserId = actualUserId ?? currentUserService.UserId;

            // Check authorization with the effective user ID
            var authorizationResult = await CheckAttachmentAuthorizationWithUserIdAsync(
                attachmentId,
                contextFactory,
                effectiveUserId);

            if (!authorizationResult.IsAuthorized)
            {
                // Log detailed authorization failure to syslog
                var endpoint = httpContextAccessor.HttpContext?.Request.Path.Value ?? "unknown";
                var userAgent = httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown";
                var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                Log.Warning(
                    "AUTHORIZATION FAILED - Thumbnail Access Denied | " +
                    "Endpoint: {Endpoint} | " +
                    "AttachmentHash: {Hash} | " +
                    "AttachmentId: {AttachmentId} | " +
                    "StatusCode: {StatusCode} | " +
                    "UserId: {UserId} | " +
                    "IP: {IpAddress} | " +
                    "UserAgent: {UserAgent} | " +
                    "DebugInfo: {DebugInfo}",
                    endpoint,
                    hash,
                    attachmentId,
                    authorizationResult.IsAuthenticated ? 403 : 401,
                    effectiveUserId ?? "Anonymous",
                    ipAddress,
                    userAgent,
                    authorizationResult.DebugInfo);

                return authorizationResult.IsAuthenticated
                    ? Results.Forbid()
                    : Results.Unauthorized();
            }

            // Get the attachment with preview data
            var query = new GetAttachmentForPreviewQuery(attachmentId);
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
            Log.Error(ex, "Error serving thumbnail for attachment {Hash}", hash);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Downloads an attachment file.
    /// </summary>
    private static async Task<IResult> DownloadAttachment(
        string hash,
        [FromServices] IHashIdsService hashIdsService,
        [FromServices] IMediator mediator,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IApplicationDbContextFactory contextFactory,
        [FromServices] ICurrentUserService currentUserService)
    {
        try
        {
            var attachmentId = hashIdsService.Decode(hash);
            if (attachmentId == 0)
            {
                return Results.NotFound("Invalid attachment identifier");
            }

            // Get user ID from HttpContext
            string? actualUserId = null;
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                actualUserId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            }

            var effectiveUserId = actualUserId ?? currentUserService.UserId;

            // Check authorization
            var authorizationResult = await CheckAttachmentAuthorizationWithUserIdAsync(
                attachmentId, contextFactory, effectiveUserId);

            if (!authorizationResult.IsAuthorized)
            {
                return authorizationResult.IsAuthenticated
                    ? Results.Forbid()
                    : Results.Unauthorized();
            }

            var downloadDto = await mediator.Send(new GetAttachmentForDownloadQuery(attachmentId));

            if (downloadDto.Content == null || downloadDto.Content.Length == 0)
            {
                return Results.NotFound("Attachment content not available");
            }

            var fileName = downloadDto.OriginalFilename ?? downloadDto.Name;
            var contentType = downloadDto.FileType ?? "application/octet-stream";

            return Results.File(downloadDto.Content, contentType, fileName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error downloading attachment {Hash}", hash);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Initiates a direct upload to Azure Blob Storage.
    /// Returns a SAS URL for the client to upload directly.
    /// </summary>
    private static async Task<IResult> InitiateDirectUpload(
        [FromBody] InitiateDirectUploadRequest request,
        [FromServices] IMediator mediator,
        [FromServices] IHashIdsService hashIdsService)
    {
        // Resolve ShowcaseHashId to ShowcaseId if provided
        var showcaseId = request.ShowcaseId;
        if (showcaseId == null && !string.IsNullOrWhiteSpace(request.ShowcaseHashId))
        {
            showcaseId = hashIdsService.Decode(request.ShowcaseHashId);
            if (showcaseId == 0)
            {
                return Results.BadRequest(new { error = "Invalid showcase identifier." });
            }
        }

        Log.Information("InitiateDirectUpload called: FileName={FileName}, FileSize={FileSize}, ContentType={ContentType}, ShowcaseId={ShowcaseId}",
            request.FileName, request.FileSize, request.ContentType, showcaseId);

        try
        {
            var command = new InitiateDirectUploadCommand
            {
                FileName = request.FileName,
                FileSize = request.FileSize,
                ContentType = request.ContentType,
                ShowcaseId = showcaseId,
            };

            var result = await mediator.Send(command);
            return Results.Ok(result);
        }
        catch (NotSupportedException ex)
        {
            Log.Warning(ex, "Direct upload not supported");
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            Log.Warning(ex, "Direct upload disabled or misconfigured");
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error initiating direct upload for file {FileName}", request.FileName);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Completes a direct upload after the client has uploaded to Azure.
    /// </summary>
    private static async Task<IResult> CompleteDirectUpload(
        [FromBody] CompleteDirectUploadRequest request,
        [FromServices] IMediator mediator,
        [FromServices] IHashIdsService hashIdsService)
    {
        try
        {
            // Resolve ShowcaseHashId to ShowcaseId if provided
            var showcaseId = request.ShowcaseId;
            if (showcaseId == null && !string.IsNullOrWhiteSpace(request.ShowcaseHashId))
            {
                showcaseId = hashIdsService.Decode(request.ShowcaseHashId);
                if (showcaseId == 0)
                {
                    return Results.BadRequest(new { error = "Invalid showcase identifier." });
                }
            }

            var command = new CompleteDirectUploadCommand
            {
                UploadId = request.UploadId,
                BlobName = request.BlobName,
                OriginalFileName = request.OriginalFileName,
                ContentType = request.ContentType,
                FileSize = request.FileSize,
                AttachmentType = request.AttachmentType,
                ShowcaseId = showcaseId,
            };

            var attachmentId = await mediator.Send(command);
            return Results.Ok(new { attachmentId });
        }
        catch (InvalidOperationException ex)
        {
            Log.Warning(ex, "Direct upload completion failed for upload {UploadId}", request.UploadId);
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error completing direct upload {UploadId}", request.UploadId);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Deletes an attachment by its hash ID.
    /// </summary>
    private static async Task<IResult> DeleteAttachment(
        string hash,
        [FromServices] IHashIdsService hashIdsService,
        [FromServices] IMediator mediator)
    {
        try
        {
            var attachmentId = hashIdsService.Decode(hash);
            if (attachmentId == 0)
            {
                return Results.NotFound("Invalid attachment identifier");
            }

            await mediator.Send(new DeleteAttachmentCommand(attachmentId));
            return Results.NoContent();
        }
        catch (ArgumentException ex)
        {
            Log.Warning(ex, "Attachment not found for deletion: {Hash}", hash);
            return Results.NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.Warning(ex, "Unauthorized attempt to delete attachment {Hash}", hash);
            return Results.Forbid();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting attachment {Hash}", hash);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Checks if the current user is authorized to access an attachment.
    /// </summary>
    private static async Task<(bool IsAuthorized, bool IsAuthenticated, string DebugInfo)> CheckAttachmentAuthorizationAsync(
        long attachmentId,
        IApplicationDbContextFactory contextFactory,
        ICurrentUserService currentUserService)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        // Build debug information for logging
        var debugInfo = new System.Text.StringBuilder();
        debugInfo.AppendLine($"Checking authorization for attachment ID: {attachmentId}");

        // Check if attachment belongs to any public showcase
        var belongsToPublicShowcase = await IsAttachmentInPublicShowcaseAsync(context, attachmentId);
        debugInfo.AppendLine($"Belongs to public showcase: {belongsToPublicShowcase}");

        if (belongsToPublicShowcase)
        {
            return (true, true, debugInfo.ToString());
        }

        // If not public, check if the current user has access
        var userId = currentUserService.UserId;
        debugInfo.AppendLine($"Current user ID: {(string.IsNullOrEmpty(userId) ? "Anonymous" : userId)}");

        if (string.IsNullOrEmpty(userId))
        {
            // User is not authenticated and showcase is not public
            debugInfo.AppendLine("Result: Unauthorized - User not authenticated and attachment not in public showcase");
            return (false, false, debugInfo.ToString());
        }

        // Check if user owns any showcase containing this attachment
        var userHasAccess = await UserOwnsAttachmentAsync(context, attachmentId, userId);
        debugInfo.AppendLine($"User owns attachment: {userHasAccess}");

        if (!userHasAccess)
        {
            // Additional debug: Check what type of attachment this is
            var attachmentInfo = await context.Attachments
                .Where(a => a.Id == attachmentId)
                .Select(a => new
                {
                    HasDirectItems = a.CollectibleItemAttachments.Any(),
                    IsChildItem = a.CollectibleItemAttachments.Any(cia => cia.CollectibleItem.ParentId != null),
                    ParentIds = a.CollectibleItemAttachments
                        .Where(cia => cia.CollectibleItem.ParentId != null)
                        .Select(cia => cia.CollectibleItem.ParentId!.Value)
                        .Distinct()
                        .ToList(),
                })
                .FirstOrDefaultAsync();

            if (attachmentInfo != null)
            {
                debugInfo.AppendLine($"Attachment has direct items: {attachmentInfo.HasDirectItems}");
                debugInfo.AppendLine($"Attachment belongs to child item: {attachmentInfo.IsChildItem}");
                if (attachmentInfo.ParentIds.Count != 0)
                {
                    debugInfo.AppendLine($"Parent item IDs: {string.Join(", ", attachmentInfo.ParentIds)}");
                }
            }

            debugInfo.AppendLine("Result: Forbidden - User authenticated but does not have access");
        }
        else
        {
            debugInfo.AppendLine("Result: Authorized");
        }

        return (userHasAccess, true, debugInfo.ToString());
    }

    /// <summary>
    /// Checks if a user (by ID) is authorized to access an attachment.
    /// This version accepts the user ID directly instead of using CurrentUserService.
    /// </summary>
    private static async Task<(bool IsAuthorized, bool IsAuthenticated, string DebugInfo)> CheckAttachmentAuthorizationWithUserIdAsync(
        long attachmentId,
        IApplicationDbContextFactory contextFactory,
        string? userId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        // Build debug information for logging
        var debugInfo = new System.Text.StringBuilder();
        debugInfo.AppendLine($"Checking authorization for attachment ID: {attachmentId}");

        // Check if attachment belongs to any public showcase
        var belongsToPublicShowcase = await IsAttachmentInPublicShowcaseAsync(context, attachmentId);
        debugInfo.AppendLine($"Belongs to public showcase: {belongsToPublicShowcase}");

        if (belongsToPublicShowcase)
        {
            return (true, true, debugInfo.ToString());
        }

        // If not public, check if the current user has access
        debugInfo.AppendLine($"Current user ID: {(string.IsNullOrEmpty(userId) ? "Anonymous" : userId)}");

        if (string.IsNullOrEmpty(userId))
        {
            // User is not authenticated and showcase is not public
            debugInfo.AppendLine("Result: Unauthorized - User not authenticated and attachment not in public showcase");
            return (false, false, debugInfo.ToString());
        }

        // Check if user owns any showcase containing this attachment
        var userHasAccess = await UserOwnsAttachmentAsync(context, attachmentId, userId);
        debugInfo.AppendLine($"User owns attachment: {userHasAccess}");

        if (!userHasAccess)
        {
            // Additional debug: Check what type of attachment this is
            var attachmentInfo = await context.Attachments
                .Where(a => a.Id == attachmentId)
                .Select(a => new
                {
                    HasDirectItems = a.CollectibleItemAttachments.Any(),
                    IsChildItem = a.CollectibleItemAttachments.Any(cia => cia.CollectibleItem.ParentId != null),
                    ParentIds = a.CollectibleItemAttachments
                        .Where(cia => cia.CollectibleItem.ParentId != null)
                        .Select(cia => cia.CollectibleItem.ParentId!.Value)
                        .Distinct()
                        .ToList(),
                })
                .FirstOrDefaultAsync();

            if (attachmentInfo != null)
            {
                debugInfo.AppendLine($"Attachment has direct items: {attachmentInfo.HasDirectItems}");
                debugInfo.AppendLine($"Attachment belongs to child item: {attachmentInfo.IsChildItem}");
                if (attachmentInfo.ParentIds.Count != 0)
                {
                    debugInfo.AppendLine($"Parent item IDs: {string.Join(", ", attachmentInfo.ParentIds)}");
                }
            }

            debugInfo.AppendLine("Result: Forbidden - User authenticated but does not have access");
        }
        else
        {
            debugInfo.AppendLine("Result: Authorized");
        }

        return (userHasAccess, true, debugInfo.ToString());
    }

    /// <summary>
    /// Checks if an attachment belongs to a public showcase.
    /// </summary>
    private static async Task<bool> IsAttachmentInPublicShowcaseAsync(
        IApplicationDbContext context,
        long attachmentId)
    {
        // Check if attachment belongs to any public showcase
        var belongsToPublicShowcase = await context.Attachments
            .Where(a => a.Id == attachmentId)
            .SelectMany(a => a.CollectibleItemAttachments)
            .SelectMany(cia => cia.CollectibleItem.Showcases)
            .AnyAsync(s => !s.IsPrivate);

        // Also check if it's a preview image for a public showcase
        if (!belongsToPublicShowcase)
        {
            belongsToPublicShowcase = await context.Showcases
                .Where(s => !s.IsPrivate && s.PreviewImage != null && s.PreviewImage.Id == attachmentId)
                .AnyAsync();
        }

        // Also check if it's a preview image for an item in a public showcase
        if (!belongsToPublicShowcase)
        {
            belongsToPublicShowcase = await context.CollectibleItems
                .Where(ci => ci.PreviewImageId == attachmentId)
                .SelectMany(ci => ci.Showcases)
                .AnyAsync(s => !s.IsPrivate);
        }

        // Also check if it's a preview image for a child item whose parent is in a public showcase
        if (!belongsToPublicShowcase)
        {
            belongsToPublicShowcase = await context.CollectibleItems
                .Where(ci => ci.PreviewImageId == attachmentId && ci.ParentId != null)
                .Select(ci => ci.Parent!)
                .SelectMany(parent => parent.Showcases)
                .AnyAsync(s => !s.IsPrivate);
        }

        // CRITICAL FIX: Check if attachment belongs to a child item whose parent is in a public showcase
        // This handles the case where child items use fallback attachments (not PreviewImageId)
        if (!belongsToPublicShowcase)
        {
            belongsToPublicShowcase = await context.Attachments
                .Where(a => a.Id == attachmentId)
                .SelectMany(a => a.CollectibleItemAttachments)
                .Where(cia => cia.CollectibleItem.ParentId != null)
                .Select(cia => cia.CollectibleItem.Parent!)
                .SelectMany(parent => parent.Showcases)
                .AnyAsync(s => !s.IsPrivate);
        }

        return belongsToPublicShowcase;
    }

    /// <summary>
    /// Checks if a user owns an attachment through their showcases.
    /// </summary>
    private static async Task<bool> UserOwnsAttachmentAsync(
        IApplicationDbContext context,
        long attachmentId,
        string userId)
    {
        // Check if user owns any showcase containing this attachment
        var userHasAccess = await context.Attachments
            .Where(a => a.Id == attachmentId)
            .SelectMany(a => a.CollectibleItemAttachments)
            .SelectMany(cia => cia.CollectibleItem.Showcases)
            .AnyAsync(s => s.UserId == userId);

        // Also check if user owns a showcase with this preview image
        if (!userHasAccess)
        {
            userHasAccess = await context.Showcases
                .Where(s => s.UserId == userId && s.PreviewImage != null && s.PreviewImage.Id == attachmentId)
                .AnyAsync();
        }

        // Also check if user owns a showcase with an item that has this preview image
        if (!userHasAccess)
        {
            userHasAccess = await context.CollectibleItems
                .Where(ci => ci.PreviewImageId == attachmentId)
                .SelectMany(ci => ci.Showcases)
                .AnyAsync(s => s.UserId == userId);
        }

        // Also check if user owns a showcase with a child item that has this preview image
        if (!userHasAccess)
        {
            userHasAccess = await context.CollectibleItems
                .Where(ci => ci.PreviewImageId == attachmentId && ci.ParentId != null)
                .Select(ci => ci.Parent!)
                .SelectMany(parent => parent.Showcases)
                .AnyAsync(s => s.UserId == userId);
        }

        // CRITICAL FIX: Check if user owns a showcase with a child item that uses this as a fallback attachment
        // This handles the case where child items use regular attachments (not PreviewImageId) for preview
        if (!userHasAccess)
        {
            userHasAccess = await context.Attachments
                .Where(a => a.Id == attachmentId)
                .SelectMany(a => a.CollectibleItemAttachments)
                .Where(cia => cia.CollectibleItem.ParentId != null)
                .Select(cia => cia.CollectibleItem.Parent!)
                .SelectMany(parent => parent.Showcases)
                .AnyAsync(s => s.UserId == userId);
        }

        return userHasAccess;
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
}
