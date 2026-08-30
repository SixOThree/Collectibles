using Collectibles.Domain.Common;

namespace Collectibles.Application.Common;

/// <summary>
/// Validation predicates shared by the attachment upload commands.
///
/// Upload metadata is caller-supplied, and the content type in particular decides how a browser
/// later interprets the stored bytes. These rules keep that decision on the server: a type a
/// browser would execute is refused outright, and a preview thumbnail must actually be an image
/// rather than arbitrary bytes wearing an image's name.
/// </summary>
public static class AttachmentContentRules
{
    /// <summary>
    /// Message shown when a caller declares a content type the application will not store.
    /// </summary>
    public const string UnsupportedContentTypeMessage =
        "File type is not supported. Markup, script, and vector-image content types are not accepted.";

    /// <summary>
    /// Message shown when supplied preview bytes are not a recognised image.
    /// </summary>
    public const string PreviewNotAnImageMessage =
        "Preview thumbnail must be a JPEG, PNG, GIF, BMP, or WebP image.";

    /// <summary>
    /// Determines whether a caller-supplied content type may be stored.
    /// </summary>
    /// <param name="contentType">The declared content type; may be null or empty.</param>
    /// <returns><c>true</c> when the value is absent or benign.</returns>
    public static bool BeAnAcceptableContentType(string? contentType) =>
        FileContentType.IsAcceptableDeclaredType(contentType);

    /// <summary>
    /// Determines whether supplied preview data decodes to a recognised raster image.
    /// </summary>
    /// <param name="base64Preview">The base64-encoded preview; may be null or empty.</param>
    /// <returns><c>true</c> when absent, or present and identifiable as an image.</returns>
    public static bool BeARecognisedImage(string? base64Preview)
    {
        if (string.IsNullOrEmpty(base64Preview))
        {
            return true;
        }

        try
        {
            var payload = base64Preview;
            var separator = payload.IndexOf(',');
            if (separator >= 0)
            {
                payload = payload[(separator + 1)..];
            }

            var bytes = Convert.FromBase64String(payload);
            return FileContentType.TryResolveImageType(bytes, out _);
        }
        catch (FormatException)
        {
            // Malformed base64 is reported by the separate base64 rule.
            return true;
        }
    }
}
