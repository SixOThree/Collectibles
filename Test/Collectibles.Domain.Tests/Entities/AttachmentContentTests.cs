namespace Collectibles.Domain.Tests.Entities;

public class AttachmentContentTests
{
    [Fact]
    public void AttachmentContentShouldCreateWithProperties()
    {
        // Arrange
        var content = new byte[] { 1, 2, 3, 4, 5 };
        var attachment = new Attachment { Name = "test.pdf" };

        // Act
        var attachmentContent = new AttachmentContent
        {
            Id = 123,
            Content = content,
            Attachment = attachment,
        };

        // Assert
        attachmentContent.Should().NotBeNull();
        attachmentContent.Id.Should().Be(123);
        attachmentContent.Content.Should().BeEquivalentTo(content);
        attachmentContent.Attachment.Should().Be(attachment);
    }

    [Fact]
    public void AttachmentContentShouldInheritFromBaseEntity()
    {
        // Arrange & Act
        var attachmentContent = new AttachmentContent();

        // Assert
        attachmentContent.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void AttachmentContentShouldAllowNullContent()
    {
        // Arrange & Act
        var attachmentContent = new AttachmentContent
        {
            Content = null,
        };

        // Assert
        attachmentContent.Content.Should().BeNull();
    }
}
