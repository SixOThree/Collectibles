namespace Collectibles.Domain.Tests.Entities;

public class AttachmentPreviewTests
{
    [Fact]
    public void AttachmentPreviewShouldCreateWithProperties()
    {
        // Arrange
        var thumbnail = new byte[] { 6, 7, 8, 9, 10 };
        var attachment = new Attachment { Name = "image.jpg" };

        // Act
        var attachmentPreview = new AttachmentPreview
        {
            Id = 456,
            PreviewThumbnail = thumbnail,
            Attachment = attachment,
        };

        // Assert
        attachmentPreview.Should().NotBeNull();
        attachmentPreview.Id.Should().Be(456);
        attachmentPreview.PreviewThumbnail.Should().BeEquivalentTo(thumbnail);
        attachmentPreview.Attachment.Should().Be(attachment);
    }

    [Fact]
    public void AttachmentPreviewShouldInheritFromBaseEntity()
    {
        // Arrange & Act
        var attachmentPreview = new AttachmentPreview();

        // Assert
        attachmentPreview.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void AttachmentPreviewShouldAllowNullPreviewThumbnail()
    {
        // Arrange & Act
        var attachmentPreview = new AttachmentPreview
        {
            PreviewThumbnail = null,
        };

        // Assert
        attachmentPreview.PreviewThumbnail.Should().BeNull();
    }
}
