namespace Collectibles.Domain.Entities;

public class AttachmentPreview : BaseEntity
{
    public byte[]? PreviewThumbnail { get; set; }

    public virtual Attachment Attachment { get; set; } = null!;
}
