namespace Collectibles.Application.Tests.Common.TestDataBuilders;

public class AttachmentBuilder
{
    private long _id = 1;
    private string _name = "Test Attachment";
    private string? _originalFilename;
    private string? _fileType;
    private AttachmentType? _attachmentType;
    private byte[]? _content;
    private byte[]? _previewThumbnail;
    private DateTime? _created;
    private string? _createdBy;
    private DateTime? _lastModified;
    private string? _lastModifiedBy;
    private DateTime? _deleted;
    private string? _deletedBy;

    public AttachmentBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public AttachmentBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public AttachmentBuilder WithOriginalFilename(string? originalFilename)
    {
        _originalFilename = originalFilename;
        return this;
    }

    public AttachmentBuilder WithFileType(string? fileType)
    {
        _fileType = fileType;
        return this;
    }

    public AttachmentBuilder WithAttachmentType(AttachmentType? attachmentType)
    {
        _attachmentType = attachmentType;
        return this;
    }

    public AttachmentBuilder WithContent(byte[]? content)
    {
        _content = content;
        return this;
    }

    public AttachmentBuilder WithPreviewThumbnail(byte[]? previewThumbnail)
    {
        _previewThumbnail = previewThumbnail;
        return this;
    }

    public AttachmentBuilder WithAuditInfo(DateTime? created = null, string? createdBy = null,
        DateTime? lastModified = null, string? lastModifiedBy = null)
    {
        _created = created;
        _createdBy = createdBy;
        _lastModified = lastModified;
        _lastModifiedBy = lastModifiedBy;
        return this;
    }

    public AttachmentBuilder WithSoftDelete(DateTime? deleted = null, string? deletedBy = null)
    {
        _deleted = deleted;
        _deletedBy = deletedBy;
        return this;
    }

    public Attachment Build()
    {
        var attachment = new Attachment
        {
            Id = _id,
            Name = _name,
            OriginalFilename = _originalFilename,
            FileType = _fileType,
            AttachmentType = _attachmentType,
            Created = _created,
            CreatedBy = _createdBy,
            LastModified = _lastModified,
            LastModifiedBy = _lastModifiedBy,
            Deleted = _deleted,
            DeletedBy = _deletedBy,
        };

        if (_content != null)
        {
            attachment.AttachmentContent = new AttachmentContent
            {
                Id = _id,
                Content = _content,
                Attachment = attachment,
            };
        }

        if (_previewThumbnail != null)
        {
            attachment.AttachmentPreview = new AttachmentPreview
            {
                Id = _id,
                PreviewThumbnail = _previewThumbnail,
                Attachment = attachment,
            };
        }

        return attachment;
    }
}
