namespace Collectibles.Domain.Tests.Entities;

public class ShowcaseTests
{
    [Fact]
    public void ShowcaseShouldCreateWithDefaultValues()
    {
        // Arrange & Act
        var showcase = new Showcase();

        // Assert
        showcase.Should().NotBeNull();
        showcase.Name.Should().NotBeNull(); // String has default empty value
        showcase.Description.Should().BeNull();
        showcase.UserId.Should().BeNull();
        showcase.PreviewImage.Should().BeNull();
        showcase.IsPrivate.Should().BeTrue(); // Default value is true
    }

    [Fact]
    public void ShowcaseShouldSetProperties()
    {
        // Arrange
        var previewImage = new Attachment { Name = "preview.jpg" };

        // Act
        var showcase = new Showcase
        {
            Name = "My Comic Collection",
            Description = "A collection of vintage Marvel comics",
            UserId = "user123",
            PreviewImage = previewImage,
            IsPrivate = false,
        };

        // Assert
        showcase.Name.Should().Be("My Comic Collection");
        showcase.Description.Should().Be("A collection of vintage Marvel comics");
        showcase.UserId.Should().Be("user123");
        showcase.PreviewImage.Should().Be(previewImage);
        showcase.IsPrivate.Should().BeFalse();
    }

    [Fact]
    public void ShowcaseShouldDefaultToPrivate()
    {
        // Arrange & Act
        var showcase = new Showcase
        {
            Name = "Private Collection",
        };

        // Assert
        showcase.IsPrivate.Should().BeTrue();
    }

    [Fact]
    public void ShowcaseShouldAllowPublicVisibility()
    {
        // Arrange & Act
        var showcase = new Showcase
        {
            Name = "Public Gallery",
            IsPrivate = false,
        };

        // Assert
        showcase.IsPrivate.Should().BeFalse();
    }

    [Fact]
    public void ShowcaseShouldInheritFromBaseAuditableSoftDeleteEntity()
    {
        // Arrange & Act
        var showcase = new Showcase();

        // Assert
        showcase.Should().BeAssignableTo<BaseAuditableSoftDeleteEntity>();
        showcase.Should().BeAssignableTo<BaseAuditableEntity>();
        showcase.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void ShowcaseShouldHaveInheritedAuditProperties()
    {
        // Arrange
        var created = DateTime.UtcNow;

        // Act
        var showcase = new Showcase
        {
            Name = "Audited Showcase",
            Created = created,
            CreatedBy = "creator@example.com",
            LastModified = created.AddDays(1),
            LastModifiedBy = "editor@example.com",
        };

        // Assert
        showcase.Created.Should().Be(created);
        showcase.CreatedBy.Should().Be("creator@example.com");
        showcase.LastModified.Should().Be(created.AddDays(1));
        showcase.LastModifiedBy.Should().Be("editor@example.com");
    }

    [Fact]
    public void ShowcaseShouldHaveInheritedSoftDeleteProperties()
    {
        // Arrange
        var deleted = DateTime.UtcNow;

        // Act
        var showcase = new Showcase
        {
            Name = "Deleted Showcase",
            Deleted = deleted,
            DeletedBy = "admin@example.com",
        };

        // Assert
        showcase.Deleted.Should().Be(deleted);
        showcase.DeletedBy.Should().Be("admin@example.com");
    }

    [Fact]
    public void ShowcaseShouldHaveInheritedIdProperty()
    {
        // Arrange & Act
        var showcase = new Showcase
        {
            Id = 456,
            Name = "Identified Showcase",
        };

        // Assert
        showcase.Id.Should().Be(456);
    }
}
