namespace Collectibles.Application.Tests.Helpers;

/// <summary>
/// Minimal, genuinely well-formed image bytes for tests.
///
/// Preview thumbnails are validated by signature, because they are served inline and arbitrary
/// bytes wearing an image's name would be rendered in the application's own origin. Tests that
/// exercise the happy path therefore need real image headers rather than arbitrary filler.
/// </summary>
public static class TestImages
{
    /// <summary>
    /// A one-pixel GIF: the smallest complete image that satisfies signature detection.
    /// </summary>
    public static byte[] Gif() =>
    [
        0x47, 0x49, 0x46, 0x38, 0x39, 0x61, // "GIF89a"
        0x01, 0x00, 0x01, 0x00,             // 1x1
        0x80, 0x00, 0x00,
        0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF,
        0x21, 0xF9, 0x04, 0x01, 0x00, 0x00, 0x00, 0x00,
        0x2C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00,
        0x02, 0x02, 0x44, 0x01, 0x00, 0x3B,
    ];

    /// <summary>
    /// A JPEG header, sufficient for signature-based content-type detection.
    /// </summary>
    public static byte[] Jpeg() =>
    [
        0xFF, 0xD8, 0xFF, 0xE0,
        0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
        0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00,
    ];

    /// <summary>
    /// A PNG signature followed by an IHDR chunk header.
    /// </summary>
    public static byte[] Png() =>
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
    ];

    /// <summary>
    /// Base64 of <see cref="Gif"/>, for commands that take preview data as a string.
    /// </summary>
    public static string GifBase64() => Convert.ToBase64String(Gif());

    /// <summary>
    /// Base64 of <see cref="Jpeg"/>.
    /// </summary>
    public static string JpegBase64() => Convert.ToBase64String(Jpeg());
}
