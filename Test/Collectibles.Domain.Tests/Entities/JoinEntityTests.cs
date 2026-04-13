namespace Collectibles.Domain.Tests.Entities;

public class JoinEntityTests
{
    [Fact]
    public void CollectibleItemTagShouldSetProperties()
    {
        // Arrange & Act
        var itemTag = new CollectibleItemTag
        {
            CollectibleItemId = 10,
            TagId = 20,
        };

        // Assert
        itemTag.CollectibleItemId.Should().Be(10);
        itemTag.TagId.Should().Be(20);
    }

    [Fact]
    public void CollectibleItemTagShouldSetNavigationProperties()
    {
        // Arrange
        var item = new CollectibleItem { Id = 10, Name = "Test Item" };
        var tag = new Tag { Id = 20, Name = "Test Tag" };

        // Act
        var itemTag = new CollectibleItemTag
        {
            CollectibleItemId = item.Id,
            CollectibleItem = item,
            TagId = tag.Id,
            Tag = tag,
        };

        // Assert
        itemTag.CollectibleItem.Should().Be(item);
        itemTag.Tag.Should().Be(tag);
    }

    [Fact]
    public void CollectibleItemTagShouldInheritFromBaseAuditableSoftDeleteEntity()
    {
        // Arrange & Act
        var itemTag = new CollectibleItemTag();

        // Assert
        itemTag.Should().BeAssignableTo<BaseAuditableSoftDeleteEntity>();
        itemTag.Should().BeAssignableTo<BaseAuditableEntity>();
        itemTag.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void ShowcaseTagShouldSetProperties()
    {
        // Arrange & Act
        var showcaseTag = new ShowcaseTag
        {
            ShowcaseId = 30,
            TagId = 40,
        };

        // Assert
        showcaseTag.ShowcaseId.Should().Be(30);
        showcaseTag.TagId.Should().Be(40);
    }

    [Fact]
    public void ShowcaseTagShouldSetNavigationProperties()
    {
        // Arrange
        var showcase = new Showcase { Id = 30, Name = "Test Showcase" };
        var tag = new Tag { Id = 40, Name = "Featured" };

        // Act
        var showcaseTag = new ShowcaseTag
        {
            ShowcaseId = showcase.Id,
            Showcase = showcase,
            TagId = tag.Id,
            Tag = tag,
        };

        // Assert
        showcaseTag.Showcase.Should().Be(showcase);
        showcaseTag.Tag.Should().Be(tag);
    }

    [Fact]
    public void ShowcaseTagShouldInheritFromBaseAuditableSoftDeleteEntity()
    {
        // Arrange & Act
        var showcaseTag = new ShowcaseTag();

        // Assert
        showcaseTag.Should().BeAssignableTo<BaseAuditableSoftDeleteEntity>();
        showcaseTag.Should().BeAssignableTo<BaseAuditableEntity>();
        showcaseTag.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void JoinEntitiesShouldHaveInheritedAuditProperties()
    {
        // Arrange
        var created = DateTime.UtcNow;

        // Act
        var itemTag = new CollectibleItemTag
        {
            CollectibleItemId = 1,
            TagId = 2,
            Created = created,
            CreatedBy = "system@example.com",
            LastModified = created.AddMinutes(5),
            LastModifiedBy = "admin@example.com",
        };

        var showcaseTag = new ShowcaseTag
        {
            ShowcaseId = 3,
            TagId = 4,
            Created = created,
            CreatedBy = "system@example.com",
            LastModified = created.AddMinutes(10),
            LastModifiedBy = "admin@example.com",
        };

        // Assert
        itemTag.Created.Should().Be(created);
        itemTag.CreatedBy.Should().Be("system@example.com");
        itemTag.LastModified.Should().Be(created.AddMinutes(5));
        itemTag.LastModifiedBy.Should().Be("admin@example.com");

        showcaseTag.Created.Should().Be(created);
        showcaseTag.CreatedBy.Should().Be("system@example.com");
        showcaseTag.LastModified.Should().Be(created.AddMinutes(10));
        showcaseTag.LastModifiedBy.Should().Be("admin@example.com");
    }

    [Fact]
    public void JoinEntitiesShouldHaveInheritedSoftDeleteProperties()
    {
        // Arrange
        var deleted = DateTime.UtcNow;

        // Act
        var itemTag = new CollectibleItemTag
        {
            CollectibleItemId = 1,
            TagId = 2,
            Deleted = deleted,
            DeletedBy = "cleanup@example.com",
        };

        var showcaseTag = new ShowcaseTag
        {
            ShowcaseId = 3,
            TagId = 4,
            Deleted = deleted.AddHours(1),
            DeletedBy = "admin@example.com",
        };

        // Assert
        itemTag.Deleted.Should().Be(deleted);
        itemTag.DeletedBy.Should().Be("cleanup@example.com");

        showcaseTag.Deleted.Should().Be(deleted.AddHours(1));
        showcaseTag.DeletedBy.Should().Be("admin@example.com");
    }

    [Fact]
    public void JoinEntitiesShouldHaveInheritedIdProperty()
    {
        // Arrange & Act
        var itemTag = new CollectibleItemTag
        {
            Id = 100,
            CollectibleItemId = 1,
            TagId = 2,
        };

        var showcaseTag = new ShowcaseTag
        {
            Id = 200,
            ShowcaseId = 3,
            TagId = 4,
        };

        // Assert
        itemTag.Id.Should().Be(100);
        showcaseTag.Id.Should().Be(200);
    }
}
