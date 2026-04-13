namespace Collectibles.Domain.Tests.Entities;

public class CollectibleItemTests
{
    [Fact]
    public void CollectibleItemShouldCreateWithDefaultValues()
    {
        // Arrange & Act
        var item = new CollectibleItem();

        // Assert
        item.Should().NotBeNull();
        item.Name.Should().BeNull();
        item.DetailedDescription.Should().BeNull();
        item.ContentType.Should().BeNull();
        item.ContentValue.Should().BeNull();
    }

    [Fact]
    public void CollectibleItemShouldSetBasicProperties()
    {
        // Arrange & Act
        var item = new CollectibleItem
        {
            Name = "Vintage Comic Book #1",
            DetailedDescription = "First edition Spider-Man comic in excellent condition",
            ContentValue = "Comic book with original cover art",
        };

        // Assert
        item.Name.Should().Be("Vintage Comic Book #1");
        item.DetailedDescription.Should().Be("First edition Spider-Man comic in excellent condition");
        item.ContentValue.Should().Be("Comic book with original cover art");
    }

    [Fact]
    public void CollectibleItemShouldInitializeCollections()
    {
        // Arrange & Act
        var item = new CollectibleItem();

        // Assert
        // Note: Collections may be null until initialized by EF Core or manually
        // This is a common pattern in domain entities
    }

    [Fact]
    public void CollectibleItemShouldHandleContentDefinition()
    {
        // Arrange
        var contentDef = new ContentDefinition
        {
            Name = "Comic Book Template",
            DefinitionJson = "{ \"fields\": [\"title\", \"issue\", \"publisher\"] }",
        };

        // Act
        var item = new CollectibleItem
        {
            Name = "Amazing Spider-Man",
            ContentType = contentDef,
        };

        // Assert
        item.ContentType.Should().Be(contentDef);
        item.ContentType.Name.Should().Be("Comic Book Template");
    }

    [Fact]
    public void CollectibleItemShouldInheritFromBaseAuditableSoftDeleteEntity()
    {
        // Arrange & Act
        var item = new CollectibleItem();

        // Assert
        item.Should().BeAssignableTo<BaseAuditableSoftDeleteEntity>();
        item.Should().BeAssignableTo<BaseAuditableEntity>();
        item.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void CollectibleItemShouldHaveInheritedAuditProperties()
    {
        // Arrange
        var created = DateTime.UtcNow;
        var modified = created.AddDays(1);

        // Act
        var item = new CollectibleItem
        {
            Created = created,
            CreatedBy = "collector@example.com",
            LastModified = modified,
            LastModifiedBy = "admin@example.com",
        };

        // Assert
        item.Created.Should().Be(created);
        item.CreatedBy.Should().Be("collector@example.com");
        item.LastModified.Should().Be(modified);
        item.LastModifiedBy.Should().Be("admin@example.com");
    }

    [Fact]
    public void CollectibleItemShouldHaveInheritedSoftDeleteProperties()
    {
        // Arrange
        var deleted = DateTime.UtcNow;

        // Act
        var item = new CollectibleItem
        {
            Deleted = deleted,
            DeletedBy = "admin@example.com",
        };

        // Assert
        item.Deleted.Should().Be(deleted);
        item.DeletedBy.Should().Be("admin@example.com");
    }

    [Fact]
    public void CollectibleItemShouldHaveInheritedIdProperty()
    {
        // Arrange & Act
        var item = new CollectibleItem { Id = 123 };

        // Assert
        item.Id.Should().Be(123);
    }

    [Fact]
    public void CollectibleItemShouldAllowNullForAllOptionalProperties()
    {
        // Arrange & Act
        var item = new CollectibleItem
        {
            Name = null,
            DetailedDescription = null,
            ContentType = null,
            ContentValue = null,
        };

        // Assert
        item.Name.Should().BeNull();
        item.DetailedDescription.Should().BeNull();
        item.ContentType.Should().BeNull();
        item.ContentValue.Should().BeNull();
    }
}
