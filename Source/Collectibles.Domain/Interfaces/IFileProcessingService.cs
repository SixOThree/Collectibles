namespace Collectibles.Domain.Interfaces;

/// <summary>
/// Service for processing files and generating previews.
/// </summary>
public interface IFileProcessingService
{
    Task<byte[]?> GeneratePreviewAsync(byte[] fileContent, string contentType, CancellationToken cancellationToken = default);

    Task<byte[]?> GenerateCollagePreviewAsync(List<byte[]> imageContents, CancellationToken cancellationToken = default);

    Task<byte[]?> RotateImageAsync(byte[] imageContent, int degrees, CancellationToken cancellationToken = default);
}
