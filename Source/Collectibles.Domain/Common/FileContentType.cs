namespace Collectibles.Domain.Common;

/// <summary>
/// Decides what content type the application is willing to <em>declare</em> for stored bytes.
///
/// A caller-supplied MIME type is metadata, not evidence: echoing it back on retrieval lets an
/// uploader choose how the browser interprets their bytes, which turns any file store into a
/// same-origin script host. Everything here therefore derives the type from the leading bytes of
/// the content itself, and treats the declared value as a hint that must survive a check before it
/// is used at all.
/// </summary>
public static class FileContentType
{
    /// <summary>
    /// Neutral type used whenever content cannot be positively identified. Browsers download
    /// rather than render this, which is the outcome we want for anything unrecognised.
    /// </summary>
    public const string Fallback = "application/octet-stream";

    /// <summary>
    /// Default type for a generated preview thumbnail.
    /// </summary>
    public const string DefaultPreview = "image/jpeg";

    /// <summary>
    /// Types that are never accepted from a caller because a browser will execute or actively
    /// interpret them in the origin that serves them. SVG is included deliberately: it is an image
    /// format that can carry script.
    /// </summary>
    private static readonly HashSet<string> ActivelyDangerousTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/html",
        "application/xhtml+xml",
        "image/svg+xml",
        "text/javascript",
        "application/javascript",
        "application/x-javascript",
        "text/ecmascript",
        "application/ecmascript",
        "application/xml",
        "text/xml",
        "application/xslt+xml",
        "text/vbscript",
        "application/x-shockwave-flash",
    };

    /// <summary>
    /// Image types the application is willing to serve inline. Anything outside this set is served
    /// as a download or not at all.
    /// </summary>
    private static readonly HashSet<string> InlineImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/bmp",
        "image/webp",
    };

    /// <summary>
    /// Determines whether a caller-supplied content type may be stored at all.
    /// </summary>
    /// <param name="declaredContentType">The content type supplied by the caller; may be null.</param>
    /// <returns><c>true</c> when the value is absent or benign; <c>false</c> when it is a type a browser would execute.</returns>
    public static bool IsAcceptableDeclaredType(string? declaredContentType)
    {
        if (string.IsNullOrWhiteSpace(declaredContentType))
        {
            return true;
        }

        // Compare the bare type, ignoring any parameters such as "; charset=utf-8".
        var bare = declaredContentType.Split(';')[0].Trim();

        return !ActivelyDangerousTypes.Contains(bare);
    }

    /// <summary>
    /// Produces the content type to persist for uploaded bytes. The signature wins where the
    /// content is recognisable; a benign declared value is kept otherwise; a dangerous one is
    /// replaced with <see cref="Fallback"/>.
    /// </summary>
    /// <param name="content">The uploaded bytes; may be empty when content is streamed.</param>
    /// <param name="declaredContentType">The content type supplied by the caller; may be null.</param>
    /// <returns>
    /// A content type safe to persist and later declare, or <c>null</c> when the caller declared
    /// nothing and the content is unrecognised. Absence is preserved rather than replaced with a
    /// invented value; responses substitute <see cref="Fallback"/> at the point of use.
    /// </returns>
    public static string? ResolveStoredType(ReadOnlySpan<byte> content, string? declaredContentType)
    {
        if (TryResolveImageType(content, out var signatureType))
        {
            return signatureType;
        }

        if (string.IsNullOrWhiteSpace(declaredContentType))
        {
            return null;
        }

        return IsAcceptableDeclaredType(declaredContentType) ? declaredContentType : Fallback;
    }

    /// <summary>
    /// Identifies image content from its leading bytes.
    /// </summary>
    /// <param name="content">The bytes to inspect.</param>
    /// <param name="contentType">The identified image content type when this returns <c>true</c>.</param>
    /// <returns><c>true</c> when the content is a recognised raster image format.</returns>
    public static bool TryResolveImageType(ReadOnlySpan<byte> content, out string contentType)
    {
        // JPEG: FF D8 FF
        if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
        {
            contentType = "image/jpeg";
            return true;
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (content.Length >= 8
            && content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47
            && content[4] == 0x0D && content[5] == 0x0A && content[6] == 0x1A && content[7] == 0x0A)
        {
            contentType = "image/png";
            return true;
        }

        // GIF: "GIF87a" or "GIF89a"
        if (content.Length >= 6
            && content[0] == (byte)'G' && content[1] == (byte)'I' && content[2] == (byte)'F'
            && content[3] == (byte)'8' && (content[4] == (byte)'7' || content[4] == (byte)'9')
            && content[5] == (byte)'a')
        {
            contentType = "image/gif";
            return true;
        }

        // BMP: "BM"
        if (content.Length >= 2 && content[0] == (byte)'B' && content[1] == (byte)'M')
        {
            contentType = "image/bmp";
            return true;
        }

        // WebP: "RIFF" .... "WEBP"
        if (content.Length >= 12
            && content[0] == (byte)'R' && content[1] == (byte)'I' && content[2] == (byte)'F' && content[3] == (byte)'F'
            && content[8] == (byte)'W' && content[9] == (byte)'E' && content[10] == (byte)'B' && content[11] == (byte)'P')
        {
            contentType = "image/webp";
            return true;
        }

        contentType = Fallback;
        return false;
    }

    /// <summary>
    /// Determines whether a content type may be sent without a download disposition.
    /// </summary>
    /// <param name="contentType">The content type to test.</param>
    /// <returns><c>true</c> for raster image types the application serves inline.</returns>
    public static bool IsInlineImageType(string? contentType) =>
        !string.IsNullOrWhiteSpace(contentType) && InlineImageTypes.Contains(contentType);
}
