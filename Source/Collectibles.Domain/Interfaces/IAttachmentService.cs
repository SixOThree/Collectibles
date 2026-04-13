using Collectibles.Domain.Entities;

namespace Collectibles.Domain.Interfaces;

/// <summary>
/// Domain service interface for attachment-related operations.
/// </summary>
public interface IAttachmentService
{
    Task<Attachment> CreateAttachmentAsync(
        byte[] content,
        string fileName,
        string contentType,
        AttachmentType type,
        long? collectibleItemId = null,
        CancellationToken cancellationToken = default);

    Task<byte[]?> GetAttachmentContentAsync(long attachmentId, CancellationToken cancellationToken = default);

    Task<Attachment?> GetAttachmentAsync(long attachmentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Attachment>> GetAttachmentsForCollectibleAsync(
        long collectibleItemId,
        CancellationToken cancellationToken = default);

    Task DeleteAttachmentAsync(long attachmentId, CancellationToken cancellationToken = default);
}
