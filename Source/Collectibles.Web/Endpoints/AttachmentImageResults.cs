using Collectibles.Domain.Common;

namespace Collectibles.Web.Endpoints;

/// <summary>
/// Builds responses for stored preview and thumbnail images.
///
/// Preview bytes can originate from a caller (upload commands accept a supplied thumbnail), so the
/// stored content type is not evidence of what the bytes actually are. These responses therefore
/// declare a type derived from the content's own signature and refuse to serve anything that is not
/// a recognised raster image, so an uploader cannot choose how a browser interprets their bytes.
/// </summary>
internal static class AttachmentImageResults
{
    /// <summary>
    /// Returns the decoded image under a signature-derived content type, or 404 when the content is
    /// not a recognised image.
    /// </summary>
    /// <param name="httpContext">The current context, used to set cache headers.</param>
    /// <param name="hash">The attachment hash, used as the ETag.</param>
    /// <param name="base64Data">The stored preview, optionally as a data URI.</param>
    /// <param name="notFoundMessage">Message returned when the content cannot be served.</param>
    /// <returns>A file result carrying the image, or a not-found result.</returns>
    public static IResult ServeImage(
        HttpContext? httpContext,
        string hash,
        string base64Data,
        string notFoundMessage = "Preview not available")
    {
        byte[] imageBytes;

        try
        {
            imageBytes = DecodeBase64(base64Data);
        }
        catch (FormatException)
        {
            return Results.NotFound(notFoundMessage);
        }

        // The signature decides the declared type. A stored type is never echoed back.
        if (!FileContentType.TryResolveImageType(imageBytes, out var contentType))
        {
            return Results.NotFound(notFoundMessage);
        }

        SetCacheHeaders(httpContext, hash);

        // Explicitly inline with a server-chosen name: the content type is now known-safe, and the
        // caller's original filename plays no part in the response.
        if (httpContext != null)
        {
            httpContext.Response.Headers.ContentDisposition = "inline; filename=\"preview\"";
        }

        return Results.File(imageBytes, contentType);
    }

    /// <summary>
    /// Decodes stored preview data, tolerating a leading data-URI prefix.
    /// </summary>
    private static byte[] DecodeBase64(string base64Data)
    {
        var payload = base64Data;

        var separator = payload.IndexOf(',');
        if (separator >= 0)
        {
            payload = payload[(separator + 1)..];
        }

        return Convert.FromBase64String(payload);
    }

    /// <summary>
    /// Sets cache headers for the HTTP response.
    /// </summary>
    private static void SetCacheHeaders(HttpContext? httpContext, string hash)
    {
        if (httpContext != null)
        {
            httpContext.Response.Headers.CacheControl = Domain.Constants.ApplicationConstants.HttpCache.PublicAttachmentCacheHeader;
            httpContext.Response.Headers.ETag = $"\"{hash}\"";
        }
    }
}
