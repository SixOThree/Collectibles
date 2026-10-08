using Collectibles.Application.Features.Attachments;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Interfaces;

using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Mappings.Explicit;

/// <summary>
/// Service for complex attachment mapping operations that require external dependencies.
/// Handles async operations like loading content from file storage.
/// </summary>
public interface IAttachmentMappingService : IAsyncMappingService<Attachment, AttachmentDto>
{
    /// <summary>
    /// Maps an attachment with its full content loaded from storage.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<AttachmentDto> MapWithContentAsync(Attachment entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Maps an attachment with only its preview loaded from storage.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<AttachmentDto> MapWithPreviewAsync(Attachment entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Maps an attachment brief DTO with preview loaded from storage.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<AttachmentBriefDto> MapToBriefWithPreviewAsync(Attachment entity, bool isFeatured = false, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of attachment mapping service with file storage support.
/// </summary>
public class AttachmentMappingService : IAttachmentMappingService
{
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<AttachmentMappingService> _logger;

    public AttachmentMappingService(IFileStorage fileStorage, ILogger<AttachmentMappingService> logger)
    {
        _fileStorage = fileStorage;
        _logger = logger;
    }

    /// <summary>
    /// Maps an attachment to DTO with content and preview loaded from storage.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<AttachmentDto> MapAsync(Attachment source, CancellationToken cancellationToken = default)
    {
        return await MapWithContentAsync(source, cancellationToken);
    }

    /// <summary>
    /// Maps multiple attachments to DTOs in parallel.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<IEnumerable<AttachmentDto>> MapManyAsync(IEnumerable<Attachment> sources, CancellationToken cancellationToken = default)
    {
        return await sources.MapInParallelAsync(
            async (attachment, ct) => await MapAsync(attachment, ct),
            maxDegreeOfParallelism: 5,
            cancellationToken);
    }

    /// <summary>
    /// Maps an attachment with its full content loaded from storage.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<AttachmentDto> MapWithContentAsync(Attachment entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var dto = entity.ToDto();

        // Load content based on storage location
        await LoadContentAsync(entity, dto, cancellationToken);

        // Load preview based on storage location
        await LoadPreviewAsync(entity, dto, cancellationToken);

        return dto;
    }

    /// <summary>
    /// Maps an attachment with only its preview loaded from storage.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<AttachmentDto> MapWithPreviewAsync(Attachment entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var dto = entity.ToDto();

        // Only load preview, not full content
        await LoadPreviewAsync(entity, dto, cancellationToken);

        return dto;
    }

    /// <summary>
    /// Maps an attachment to brief DTO with preview loaded from storage.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<AttachmentBriefDto> MapToBriefWithPreviewAsync(Attachment entity, bool isFeatured = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var dto = entity.ToBriefDto();
        dto.IsFeatured = isFeatured;

        // Load preview thumbnail
        dto.Base64PreviewThumbnail = await LoadPreviewThumbnailAsync(entity, cancellationToken);

        return dto;
    }

    /// <summary>
    /// Loads content from storage (database or external) into the DTO.
    /// </summary>
    private async Task LoadContentAsync(Attachment entity, AttachmentDto dto, CancellationToken cancellationToken)
    {
        try
        {
            // Prefer external storage when a file path is available
            if (!string.IsNullOrEmpty(entity.FilePath))
            {
                try
                {
                    var content = await _fileStorage.GetFileAsync(entity.FilePath, cancellationToken);
                    if (content != null && content.Length > 0)
                    {
                        dto.Base64Content = content.ToBase64String();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to load content from external storage for attachment {AttachmentId} with path {FilePath}",
                        entity.Id, entity.FilePath);
                }
            }

            // Fall back to database content
            if (entity.AttachmentContent?.Content != null)
            {
                dto.Base64Content = entity.AttachmentContent.Content.ToBase64String();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading content for attachment {AttachmentId}", entity.Id);
        }
    }

    /// <summary>
    /// Loads preview from storage (database or external) into the DTO.
    /// </summary>
    private async Task LoadPreviewAsync(Attachment entity, AttachmentDto dto, CancellationToken cancellationToken)
    {
        dto.Base64PreviewThumbnail = await LoadPreviewThumbnailAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Loads preview thumbnail from storage (database or external).
    /// </summary>
    private async Task<string?> LoadPreviewThumbnailAsync(Attachment entity, CancellationToken cancellationToken)
    {
        try
        {
            // Prefer external storage when a preview path is available
            if (!string.IsNullOrEmpty(entity.PreviewPath))
            {
                try
                {
                    var preview = await _fileStorage.GetFileAsync(entity.PreviewPath, cancellationToken);
                    if (preview != null && preview.Length > 0)
                    {
                        return preview.ToBase64String();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to load preview from external storage for attachment {AttachmentId} with path {PreviewPath}",
                        entity.Id, entity.PreviewPath);
                }
            }

            // Fall back to database preview
            if (entity.AttachmentPreview?.PreviewThumbnail != null)
            {
                return entity.AttachmentPreview.PreviewThumbnail.ToBase64String();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading preview for attachment {AttachmentId}", entity.Id);
        }

        return null;
    }
}
