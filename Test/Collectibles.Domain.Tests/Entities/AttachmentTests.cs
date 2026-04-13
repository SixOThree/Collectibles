namespace Collectibles.Domain.Tests.Entities;

public class AttachmentTests
{
    [Fact]
    public void AttachmentShouldCreateWithRequiredProperties()
    {
        // Arrange & Act
        var attachment = new Attachment
        {
            Name = "test-document.pdf",
        };

        // Assert
        attachment.Should().NotBeNull();
        attachment.Name.Should().Be("test-document.pdf");
    }

    [Fact]
    public void AttachmentShouldSetOptionalProperties()
    {
        // Act
        var attachment = new Attachment
        {
            Name = "document.pdf",
            OriginalFilename = "My Important Document.pdf",
            FileType = "application/pdf",
            AttachmentType = AttachmentType.Document,
        };

        // Assert
        attachment.Name.Should().Be("document.pdf");
        attachment.OriginalFilename.Should().Be("My Important Document.pdf");
        attachment.FileType.Should().Be("application/pdf");
        attachment.AttachmentType.Should().Be(AttachmentType.Document);
    }

    [Fact]
    public void AttachmentShouldHaveNullableOptionalProperties()
    {
        // Arrange & Act
        var attachment = new Attachment
        {
            Name = "test.txt",
        };

        // Assert
        attachment.OriginalFilename.Should().BeNull();
        attachment.FileType.Should().BeNull();
        attachment.AttachmentType.Should().BeNull();
        attachment.AttachmentContent.Should().BeNull();
        attachment.AttachmentPreview.Should().BeNull();
    }

    [Theory]
    [InlineData(AttachmentType.Image)]
    [InlineData(AttachmentType.Document)]
    [InlineData(AttachmentType.Archive)]
    [InlineData(AttachmentType.File)]
    [InlineData(AttachmentType.Video)]
    [InlineData(AttachmentType.Audio)]
    [InlineData(AttachmentType.Other)]
    public void AttachmentShouldAcceptAllAttachmentTypes(AttachmentType type)
    {
        // Arrange & Act
        var attachment = new Attachment
        {
            Name = "file",
            AttachmentType = type,
        };

        // Assert
        attachment.AttachmentType.Should().Be(type);
    }

    [Fact]
    public void AttachmentShouldInheritFromBaseAuditableSoftDeleteEntity()
    {
        // Arrange & Act
        var attachment = new Attachment { Name = "test" };

        // Assert
        attachment.Should().BeAssignableTo<BaseAuditableSoftDeleteEntity>();
        attachment.Should().BeAssignableTo<BaseAuditableEntity>();
        attachment.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void AttachmentShouldHaveInheritedAuditProperties()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var attachment = new Attachment
        {
            Name = "test",
            Created = now,
            CreatedBy = "user@example.com",
            LastModified = now.AddMinutes(30),
            LastModifiedBy = "admin@example.com",
        };

        // Assert
        attachment.Created.Should().Be(now);
        attachment.CreatedBy.Should().Be("user@example.com");
        attachment.LastModified.Should().Be(now.AddMinutes(30));
        attachment.LastModifiedBy.Should().Be("admin@example.com");
    }

    [Fact]
    public void AttachmentShouldHaveInheritedSoftDeleteProperties()
    {
        // Arrange
        var deleteTime = DateTime.UtcNow;

        // Act
        var attachment = new Attachment
        {
            Name = "test",
            Deleted = deleteTime,
            DeletedBy = "admin@example.com",
        };

        // Assert
        attachment.Deleted.Should().Be(deleteTime);
        attachment.DeletedBy.Should().Be("admin@example.com");
    }

    [Fact]
    public void AttachmentShouldHaveInheritedIdProperty()
    {
        // Arrange & Act
        var attachment = new Attachment
        {
            Name = "test",
            Id = 42,
        };

        // Assert
        attachment.Id.Should().Be(42);
    }
}
