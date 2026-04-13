using Collectibles.Domain.Common.Enums;

namespace Collectibles.Application.Features.Attachments;

public class TagDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AttachmentDto
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string? OriginalFilename { get; set; }
    public string? FileType { get; set; }
    public long FileSize { get; set; }
    public AttachmentType? AttachmentType { get; set; }
    public string? Base64Content { get; set; }
    public string? Base64PreviewThumbnail { get; set; }
    public DateTime Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
    public ICollection<TagDto> Tags { get; set; } = new List<TagDto>();
    public bool IsMigrated { get; set; }
    public DateTime? MigrationDate { get; set; }
    public StorageMode? StorageMode { get; set; }
}

public class AttachmentBriefDto
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string? OriginalFilename { get; set; }
    public string? FileType { get; set; }
    public AttachmentType? AttachmentType { get; set; }
    public string? Base64PreviewThumbnail { get; set; }
    public DateTime Created { get; set; }
    public bool IsFeatured { get; set; }
}
