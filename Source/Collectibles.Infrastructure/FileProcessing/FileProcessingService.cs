using Collectibles.Domain.Configuration;
using Collectibles.Domain.Constants;
using Collectibles.Domain.Interfaces;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

using FFMpegCore;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PDFtoImage;

using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Collectibles.Infrastructure.FileProcessing;

public class FileProcessingService : IFileProcessingService
{
    private const int ThumbnailWidth = ApplicationConstants.Media.ThumbnailWidth;
    private const int ThumbnailHeight = ApplicationConstants.Media.ThumbnailHeight;
    private static readonly Color BackgroundColor = Color.White;
    private static readonly Color TextColor = Color.Black;
    private readonly ILogger<FileProcessingService> _logger;
    private readonly PreviewGenerationSettings _previewSettings;

    public FileProcessingService(
        ILogger<FileProcessingService> logger,
        IOptions<PreviewGenerationSettings> previewSettings)
    {
        _logger = logger;
        _previewSettings = previewSettings.Value;
    }

    public async Task<byte[]?> GeneratePreviewAsync(byte[] fileContent, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return _previewSettings.Images
                    ? await GenerateImageThumbnailAsync(fileContent, cancellationToken)
                    : null;
            }
            else if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                return _previewSettings.Pdf
                    ? await GeneratePdfThumbnailAsync(fileContent, cancellationToken)
                    : null;
            }
            else if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                return _previewSettings.Video
                    ? await GenerateVideoThumbnailAsync(fileContent, contentType, cancellationToken)
                    : null;
            }
            else if (IsWordDocument(contentType))
            {
                return _previewSettings.Word
                    ? await GenerateWordThumbnailAsync(fileContent, cancellationToken)
                    : null;
            }
            else if (IsPowerPointDocument(contentType))
            {
                return _previewSettings.PowerPoint
                    ? await GeneratePowerPointThumbnailAsync(fileContent, cancellationToken)
                    : null;
            }

            return await GenerateGenericThumbnailAsync(contentType, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating preview");
            return await GenerateErrorThumbnailAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Reads only the image header and rejects declared dimensions that would force an
    /// unreasonable allocation on decode (width x height x 4 bytes).
    /// </summary>
    /// <remarks>
    /// A few hundred bytes of PNG can declare 40,000 x 40,000, so the size of the uploaded
    /// file is no guard at all. Checking the header first keeps a decompression bomb from
    /// exhausting server memory.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The image is too large to process.</exception>
    private static void EnsureImageWithinLimits(byte[] imageContent)
    {
        ImageInfo info;
        try
        {
            info = Image.Identify(imageContent);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException)
        {
            throw new InvalidOperationException("The file is not a readable image.", ex);
        }

        if (info.Width > ApplicationConstants.Media.MaxImageDimension
            || info.Height > ApplicationConstants.Media.MaxImageDimension
            || (long)info.Width * info.Height > ApplicationConstants.Media.MaxImagePixels)
        {
            throw new InvalidOperationException(
                $"Image dimensions {info.Width}x{info.Height} exceed the supported maximum.");
        }
    }

    private static async Task<byte[]> GenerateImageThumbnailAsync(byte[] imageContent, CancellationToken cancellationToken)
    {
        EnsureImageWithinLimits(imageContent);

        using var input = new MemoryStream(imageContent);
        using var output = new MemoryStream();

        using (var image = await Image.LoadAsync(input, cancellationToken))
        {
            image.Mutate(x => x
                .Resize(new ResizeOptions
                {
                    Size = new Size(ThumbnailWidth, ThumbnailHeight),
                    Mode = ResizeMode.Max,
                })
                .BackgroundColor(BackgroundColor));

            await image.SaveAsPngAsync(output, cancellationToken);
        }

        return output.ToArray();
    }

    private static async Task<byte[]?> GeneratePdfThumbnailAsync(byte[] pdfContent, CancellationToken cancellationToken)
    {
        try
        {
            // Convert first page to PNG using byte array directly (no temp files needed)
            var renderOptions = new RenderOptions { Dpi = ApplicationConstants.Media.PdfRenderDpi };
            using var pngStream = new MemoryStream();
            await Task.Run(() => Conversion.SavePng(pngStream, pdfContent, 0, options: renderOptions), cancellationToken);

            var pngBytes = pngStream.ToArray();
            if (pngBytes.Length > 0)
            {
                // Generate thumbnail from the rendered PDF page image
                return await GenerateImageThumbnailAsync(pngBytes, cancellationToken);
            }

            return await GenerateGenericThumbnailAsync("application/pdf", cancellationToken);
        }
        catch
        {
            return await GenerateGenericThumbnailAsync("application/pdf", cancellationToken);
        }
    }

    private static async Task<byte[]?> GenerateVideoThumbnailAsync(byte[] videoContent, string contentType, CancellationToken cancellationToken)
    {
        try
        {
            // Save video to temporary file with correct extension for FFMpeg.
            //
            // Path.GetTempFileName() creates a zero-byte tmpXXXX.tmp file and then a
            // *different* path (with the extension appended) was used, so the original
            // file was never deleted - leaking two temp files per video processed.
            var videoExtension = GetVideoExtension(contentType);
            var tempDirectory = System.IO.Path.GetTempPath();
            var tempStem = Guid.NewGuid().ToString("N");
            var tempVideoPath = System.IO.Path.Combine(tempDirectory, tempStem + videoExtension);
            var tempImagePath = System.IO.Path.Combine(tempDirectory, tempStem + ".png");

            try
            {
                await File.WriteAllBytesAsync(tempVideoPath, videoContent, cancellationToken);

                // A malformed or hostile video can hold an ffmpeg child process open
                // indefinitely, so the snapshot runs under an explicit timeout that also
                // observes the caller's cancellation.
                using var snapshotCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                snapshotCts.CancelAfter(TimeSpan.FromSeconds(ApplicationConstants.Media.VideoThumbnailTimeoutSeconds));

                // Extract frame at 1 second mark (or beginning if video is shorter)
                await FFMpegArguments
                    .FromFileInput(tempVideoPath, verifyExists: true)
                    .OutputToFile(tempImagePath, overwrite: true, options => options
                        .Seek(TimeSpan.FromSeconds(ApplicationConstants.Media.VideoThumbnailCaptureSeconds))
                        .WithFrameOutputCount(1)
                        .Resize(ThumbnailWidth, ThumbnailHeight))
                    .CancellableThrough(snapshotCts.Token)
                    .ProcessAsynchronously();

                if (File.Exists(tempImagePath))
                {
                    var imageBytes = await File.ReadAllBytesAsync(tempImagePath, cancellationToken);
                    return imageBytes;
                }
            }
            finally
            {
                // Clean up temp files
                if (File.Exists(tempVideoPath))
                {
                    File.Delete(tempVideoPath);
                }

                if (File.Exists(tempImagePath))
                {
                    File.Delete(tempImagePath);
                }
            }

            return await GenerateGenericThumbnailAsync("video/*", cancellationToken);
        }
        catch
        {
            return await GenerateGenericThumbnailAsync("video/*", cancellationToken);
        }
    }

    private static async Task<byte[]?> GenerateWordThumbnailAsync(byte[] wordContent, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = new MemoryStream(wordContent);
            using var wordDoc = WordprocessingDocument.Open(stream, false);

            var body = wordDoc.MainDocumentPart?.Document?.Body;
            if (body == null)
            {
                return await GenerateGenericThumbnailAsync("Word Document", cancellationToken);
            }

            // Extract first few paragraphs of text
            var paragraphs = body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>()
                .Take(ApplicationConstants.Media.WordDocumentMaxParagraphs)
                .Select(p => p.InnerText)
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToList();

            return await GenerateTextThumbnailAsync("Word Document", paragraphs, cancellationToken);
        }
        catch
        {
            return await GenerateGenericThumbnailAsync("Word Document", cancellationToken);
        }
    }

    private static async Task<byte[]?> GeneratePowerPointThumbnailAsync(byte[] pptContent, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = new MemoryStream(pptContent);
            using var pptDoc = PresentationDocument.Open(stream, false);

            var presentation = pptDoc.PresentationPart?.Presentation;
            if (presentation == null)
            {
                return await GenerateGenericThumbnailAsync("PowerPoint", cancellationToken);
            }

            // Try to get the first slide
            var slideId = presentation.SlideIdList?.ChildElements.FirstOrDefault() as SlideId;
            if (slideId == null)
            {
                return await GenerateGenericThumbnailAsync("PowerPoint", cancellationToken);
            }

            var slidePart = pptDoc.PresentationPart?.GetPartById(slideId.RelationshipId!) as SlidePart;
            if (slidePart?.Slide == null)
            {
                return await GenerateGenericThumbnailAsync("PowerPoint", cancellationToken);
            }

            // Extract text from first slide
            var texts = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>()
                .Select(t => t.Text)
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Take(ApplicationConstants.Media.PowerPointMaxTextElements)
                .ToList();

            return await GenerateTextThumbnailAsync("PowerPoint", texts, cancellationToken);
        }
        catch
        {
            return await GenerateGenericThumbnailAsync("PowerPoint", cancellationToken);
        }
    }

    private static async Task<byte[]> GenerateTextThumbnailAsync(string title, List<string> textLines, CancellationToken cancellationToken)
    {
        using var image = new Image<Rgba32>(ThumbnailWidth, ThumbnailHeight);

        image.Mutate(ctx =>
        {
            // Fill background
            ctx.Fill(BackgroundColor);

            // Draw border
            var borderPath = new PathBuilder()
                .AddLine(0, 0, ThumbnailWidth - 1, 0)
                .AddLine(ThumbnailWidth - 1, 0, ThumbnailWidth - 1, ThumbnailHeight - 1)
                .AddLine(ThumbnailWidth - 1, ThumbnailHeight - 1, 0, ThumbnailHeight - 1)
                .AddLine(0, ThumbnailHeight - 1, 0, 0)
                .Build();

            ctx.Draw(Color.LightGray, 2, borderPath);

            // Try to use system font or fallback
            SixLabors.Fonts.Font titleFont;
            SixLabors.Fonts.Font textFont;
            try
            {
                var fontFamily = SystemFonts.Get("Arial");
                titleFont = fontFamily.CreateFont(20, FontStyle.Bold);
                textFont = fontFamily.CreateFont(14);
            }
            catch
            {
                // Fallback to default font if Arial is not available
                titleFont = SystemFonts.CreateFont("Arial", 20, FontStyle.Bold);
                textFont = SystemFonts.CreateFont("Arial", 14);
            }

            // Draw title
            ctx.DrawText(title, titleFont, TextColor, new PointF(20, 20));

            // Draw text lines
            var y = 60f;
            foreach (var line in textLines)
            {
                if (y > ThumbnailHeight - 40)
                {
                    break;
                }

                var displayText = line.Length > 50 ? string.Concat(line.AsSpan(0, 50), "...") : line;
                ctx.DrawText(displayText, textFont, Color.DarkGray, new PointF(20, y));
                y += 25;
            }
        });

        using var output = new MemoryStream();
        await image.SaveAsPngAsync(output, cancellationToken);
        return output.ToArray();
    }

    private static async Task<byte[]> GenerateGenericThumbnailAsync(string fileType, CancellationToken cancellationToken)
    {
        using var image = new Image<Rgba32>(ThumbnailWidth, ThumbnailHeight);

        image.Mutate(ctx =>
        {
            // Fill background
            ctx.Fill(Color.WhiteSmoke);

            // Draw border
            var borderPath = new PathBuilder()
                .AddLine(0, 0, ThumbnailWidth - 1, 0)
                .AddLine(ThumbnailWidth - 1, 0, ThumbnailWidth - 1, ThumbnailHeight - 1)
                .AddLine(ThumbnailWidth - 1, ThumbnailHeight - 1, 0, ThumbnailHeight - 1)
                .AddLine(0, ThumbnailHeight - 1, 0, 0)
                .Build();

            ctx.Draw(Color.Gray, 2, borderPath);

            // Try to use system font or fallback
            SixLabors.Fonts.Font font;
            try
            {
                var fontFamily = SystemFonts.Get("Arial");
                font = fontFamily.CreateFont(36, FontStyle.Bold);
            }
            catch
            {
                font = SystemFonts.CreateFont("Arial", 36, FontStyle.Bold);
            }

            // Draw file type icon/text
            var text = GetFileTypeDisplay(fileType);

            var textOptions = new RichTextOptions(font)
            {
                Origin = new PointF(ThumbnailWidth / 2, ThumbnailHeight / 2),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            ctx.DrawText(textOptions, text, Color.DarkGray);
        });

        using var output = new MemoryStream();
        await image.SaveAsPngAsync(output, cancellationToken);
        return output.ToArray();
    }

    private static async Task<byte[]> GenerateErrorThumbnailAsync(CancellationToken cancellationToken)
    {
        using var image = new Image<Rgba32>(ThumbnailWidth, ThumbnailHeight);

        image.Mutate(ctx =>
        {
            ctx.Fill(Color.LightGray);

            // Try to use system font or fallback
            SixLabors.Fonts.Font font;
            try
            {
                var fontFamily = SystemFonts.Get("Arial");
                font = fontFamily.CreateFont(24);
            }
            catch
            {
                font = SystemFonts.CreateFont("Arial", 24);
            }

            var text = "Preview Unavailable";

            var textOptions = new RichTextOptions(font)
            {
                Origin = new PointF(ThumbnailWidth / 2, ThumbnailHeight / 2),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            ctx.DrawText(textOptions, text, Color.DarkGray);
        });

        using var output = new MemoryStream();
        await image.SaveAsPngAsync(output, cancellationToken);
        return output.ToArray();
    }

    private static bool IsWordDocument(string contentType)
    {
        return
            contentType.Equals("application/vnd.openxmlformats-officedocument.wordprocessingml.document", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/msword", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPowerPointDocument(string contentType)
    {
        return
            contentType.Equals("application/vnd.openxmlformats-officedocument.presentationml.presentation", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/vnd.ms-powerpoint", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<byte[]?> GenerateCollagePreviewAsync(List<byte[]> imageContents, CancellationToken cancellationToken = default)
    {
        if (imageContents == null || imageContents.Count == 0)
        {
            return null;
        }

        // Limit to first few images for collage
        var imagesToUse = imageContents.Take(ApplicationConstants.Media.CollageImageCount).ToList();
        var imageCount = imagesToUse.Count;

        // Create a square collage matching thumbnail dimensions
        using var collageImage = new Image<Rgba32>(ThumbnailWidth, ThumbnailHeight);

        // Calculate tile dimensions based on number of images
        int tileWidth, tileHeight;
        int padding = 4; // Small padding between images

        if (imageCount == 1)
        {
            // Single image - use full size with padding
            tileWidth = ThumbnailWidth - (padding * 2);
            tileHeight = ThumbnailHeight - (padding * 2);
        }
        else
        {
            // Multiple images - create grid (2x2 for 2-4 images)
            tileWidth = (ThumbnailWidth - (padding * 3)) / 2;
            tileHeight = (ThumbnailHeight - (padding * 3)) / 2;
        }

        // Fill background with a light gray
        collageImage.Mutate(ctx => ctx.Fill(Color.FromRgb(240, 240, 240)));

        // Process and place each image
        for (int i = 0; i < imageCount; i++)
        {
            try
            {
                EnsureImageWithinLimits(imageContents[i]);
                using var sourceImage = Image.Load(imageContents[i]);

                // Resize to fit tile while maintaining aspect ratio
                sourceImage.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = new Size(tileWidth, tileHeight),
                    Mode = ResizeMode.Max,
                }));

                // Calculate position based on index
                int x, y;

                if (imageCount == 1)
                {
                    // Center single image
                    x = padding;
                    y = padding;
                }
                else if (imageCount == 2)
                {
                    // Side by side
                    x = ((i % 2) * (tileWidth + padding)) + padding;
                    y = (ThumbnailHeight - sourceImage.Height) / 2;
                }
                else if (imageCount == 3)
                {
                    if (i < 2)
                    {
                        // First two on top row
                        x = ((i % 2) * (tileWidth + padding)) + padding;
                        y = padding;
                    }
                    else
                    {
                        // Third centered on bottom
                        x = (ThumbnailWidth - sourceImage.Width) / 2;
                        y = tileHeight + (padding * 2);
                    }
                }
                else
                {
                    // 2x2 grid for 4 images
                    x = ((i % 2) * (tileWidth + padding)) + padding;
                    y = (i / 2 * (tileHeight + padding)) + padding;
                }

                // Center the image within its tile if it's smaller
                if (sourceImage.Width < tileWidth)
                {
                    x += (tileWidth - sourceImage.Width) / 2;
                }

                if (sourceImage.Height < tileHeight)
                {
                    y += (tileHeight - sourceImage.Height) / 2;
                }

                // Draw the image onto the collage
                collageImage.Mutate(ctx => ctx.DrawImage(sourceImage, new Point(x, y), 1f));
            }
            catch
            {
                // Skip images that fail to load
                continue;
            }
        }

        // Add a subtle border around the entire collage
        collageImage.Mutate(ctx => ctx.Draw(Color.FromRgb(200, 200, 200), 2,
            new RectangleF(0, 0, ThumbnailWidth - 1, ThumbnailHeight - 1)));

        // Convert to JPEG bytes with good quality
        using var ms = new MemoryStream();
        await collageImage.SaveAsJpegAsync(ms, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
        {
            Quality = 85,
        }, cancellationToken);

        return ms.ToArray();
    }

    public async Task<byte[]?> RotateImageAsync(byte[] imageContent, int degrees, CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureImageWithinLimits(imageContent);

            using var input = new MemoryStream(imageContent);
            using var output = new MemoryStream();

            using (var image = await Image.LoadAsync(input, cancellationToken))
            {
                // Normalize degrees to 0-360 range
                degrees = degrees % ApplicationConstants.Media.RotationNormalizeDegrees;
                if (degrees < 0)
                {
                    degrees += ApplicationConstants.Media.RotationNormalizeDegrees;
                }

                // Apply rotation
                if (degrees != 0)
                {
                    image.Mutate(x => x.Rotate(degrees));
                }

                // Save the rotated image in the same format as input
                // Try to detect the format from the input
                var format = await Image.DetectFormatAsync(new MemoryStream(imageContent), cancellationToken);

                if (format != null)
                {
                    await image.SaveAsync(output, format, cancellationToken);
                }
                else
                {
                    // Default to PNG if format cannot be detected
                    await image.SaveAsPngAsync(output, cancellationToken);
                }
            }

            return output.ToArray();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string GetVideoExtension(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "video/mp4" => ".mp4",
            "video/x-msvideo" => ".avi",
            "video/avi" => ".avi",
            "video/quicktime" => ".mov",
            "video/mov" => ".mov",
            "video/x-ms-wmv" => ".wmv",
            "video/webm" => ".webm",
            "video/mpeg" => ".mpeg",
            _ => ".mp4",
        };
    }

    private static string GetFileTypeDisplay(string fileType)
    {
        return fileType switch
        {
            "application/pdf" => "PDF",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => "DOCX",
            "application/msword" => "DOC",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation" => "PPTX",
            "application/vnd.ms-powerpoint" => "PPT",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => "XLSX",
            "application/vnd.ms-excel" => "XLS",
            "application/zip" => "ZIP",
            "application/x-rar-compressed" => "RAR",
            "application/x-7z-compressed" => "7Z",
            var s when s.StartsWith("video/") => "VIDEO",
            var s when s.StartsWith("audio/") => "AUDIO",
            var s when s.StartsWith("text/") => "TEXT",
            _ => "FILE",
        };
    }
}
