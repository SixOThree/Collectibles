namespace Collectibles.Domain.Entities;

public class AttachmentContent : BaseEntity
{
    public byte[]? Content { get; set; }

    public virtual Attachment Attachment { get; set; } = null!;
}
