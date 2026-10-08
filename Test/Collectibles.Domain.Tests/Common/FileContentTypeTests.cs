using System.Text;

using Collectibles.Domain.Common;

namespace Collectibles.Domain.Tests.Common;

/// <summary>
/// Covers the decision about what content type the application will declare for stored bytes.
/// Echoing a caller's declared type let an uploader choose how a browser interprets their content,
/// which turned the attachment store into a same-origin script host.
/// </summary>
public class FileContentTypeTests
{
    [Theory]
    [InlineData("text/html")]
    [InlineData("TEXT/HTML")]
    [InlineData("text/html; charset=utf-8")]
    [InlineData("image/svg+xml")]
    [InlineData("application/xhtml+xml")]
    [InlineData("text/javascript")]
    [InlineData("application/javascript")]
    [InlineData("application/xml")]
    public void IsAcceptableDeclaredTypeShouldRejectTypesABrowserExecutes(string contentType)
    {
        FileContentType.IsAcceptableDeclaredType(contentType).Should().BeFalse();
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("application/pdf")]
    [InlineData("video/mp4")]
    [InlineData("application/octet-stream")]
    [InlineData(null)]
    [InlineData("")]
    public void IsAcceptableDeclaredTypeShouldAllowBenignTypes(string? contentType)
    {
        FileContentType.IsAcceptableDeclaredType(contentType).Should().BeTrue();
    }

    [Fact]
    public void TryResolveImageTypeShouldIdentifyJpeg()
    {
        var identified = FileContentType.TryResolveImageType(
            [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10],
            out var contentType);

        identified.Should().BeTrue();
        contentType.Should().Be("image/jpeg");
    }

    [Fact]
    public void TryResolveImageTypeShouldIdentifyPng()
    {
        var identified = FileContentType.TryResolveImageType(
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
            out var contentType);

        identified.Should().BeTrue();
        contentType.Should().Be("image/png");
    }

    [Fact]
    public void TryResolveImageTypeShouldIdentifyGif()
    {
        var identified = FileContentType.TryResolveImageType(
            Encoding.ASCII.GetBytes("GIF89a....."),
            out var contentType);

        identified.Should().BeTrue();
        contentType.Should().Be("image/gif");
    }

    [Fact]
    public void TryResolveImageTypeShouldIdentifyWebp()
    {
        var bytes = new byte[12];
        Encoding.ASCII.GetBytes("RIFF").CopyTo(bytes, 0);
        Encoding.ASCII.GetBytes("WEBP").CopyTo(bytes, 8);

        var identified = FileContentType.TryResolveImageType(bytes, out var contentType);

        identified.Should().BeTrue();
        contentType.Should().Be("image/webp");
    }

    [Fact]
    public void TryResolveImageTypeShouldRejectMarkupThatClaimsToBeAnImage()
    {
        var identified = FileContentType.TryResolveImageType(
            Encoding.UTF8.GetBytes("<html><body>not an image</body></html>"),
            out var contentType);

        identified.Should().BeFalse();
        contentType.Should().Be(FileContentType.Fallback);
    }

    [Fact]
    public void TryResolveImageTypeShouldRejectEmptyContent()
    {
        FileContentType.TryResolveImageType([], out _).Should().BeFalse();
    }

    [Fact]
    public void ResolveStoredTypeShouldPreferTheSignatureOverTheDeclaredType()
    {
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        FileContentType.ResolveStoredType(pngBytes, "text/html").Should().Be("image/png");
    }

    [Fact]
    public void ResolveStoredTypeShouldNeutraliseADangerousDeclaredTypeForUnrecognisedContent()
    {
        var bytes = Encoding.UTF8.GetBytes("<script>alert(1)</script>");

        FileContentType.ResolveStoredType(bytes, "text/html").Should().Be(FileContentType.Fallback);
    }

    [Fact]
    public void ResolveStoredTypeShouldKeepABenignDeclaredTypeForUnrecognisedContent()
    {
        var bytes = Encoding.UTF8.GetBytes("plain text content");

        FileContentType.ResolveStoredType(bytes, "application/pdf").Should().Be("application/pdf");
    }

    [Fact]
    public void ResolveStoredTypeShouldPreserveAbsenceRatherThanInventingAType()
    {
        var bytes = Encoding.UTF8.GetBytes("plain text content");

        FileContentType.ResolveStoredType(bytes, null).Should().BeNull();
    }

    [Fact]
    public void IsInlineImageTypeShouldRejectSvgBecauseItCanCarryScript()
    {
        FileContentType.IsInlineImageType("image/svg+xml").Should().BeFalse();
    }

    [Fact]
    public void IsInlineImageTypeShouldAcceptRasterImages()
    {
        FileContentType.IsInlineImageType("image/jpeg").Should().BeTrue();
        FileContentType.IsInlineImageType("image/png").Should().BeTrue();
    }
}
