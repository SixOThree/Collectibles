namespace Collectibles.Domain.Tests.Entities;

public class TagTests
{
    [Fact]
    public void TagShouldCreateWithDefaultValues()
    {
        // Arrange & Act
        var tag = new Tag();

        // Assert
        tag.Should().NotBeNull();
        tag.Name.Should().NotBeNull(); // String properties have default empty string
    }

    [Fact]
    public void TagShouldSetNameProperty()
    {
        // Arrange & Act
        var tag = new Tag
        {
            Name = "Vintage",
        };

        // Assert
        tag.Name.Should().Be("Vintage");
    }

    [Fact]
    public void TagShouldInheritFromBaseAuditableSoftDeleteEntity()
    {
        // Arrange & Act
        var tag = new Tag();

        // Assert
        tag.Should().BeAssignableTo<BaseAuditableSoftDeleteEntity>();
        tag.Should().BeAssignableTo<BaseAuditableEntity>();
        tag.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void TagShouldHaveInheritedAuditProperties()
    {
        // Arrange
        var created = DateTime.UtcNow;

        // Act
        var tag = new Tag
        {
            Name = "Rare",
            Created = created,
            CreatedBy = "user@example.com",
            LastModified = created.AddHours(1),
            LastModifiedBy = "admin@example.com",
        };

        // Assert
        tag.Created.Should().Be(created);
        tag.CreatedBy.Should().Be("user@example.com");
        tag.LastModified.Should().Be(created.AddHours(1));
        tag.LastModifiedBy.Should().Be("admin@example.com");
    }

    [Fact]
    public void TagShouldHaveInheritedSoftDeleteProperties()
    {
        // Arrange
        var deleted = DateTime.UtcNow;

        // Act
        var tag = new Tag
        {
            Name = "Obsolete",
            Deleted = deleted,
            DeletedBy = "admin@example.com",
        };

        // Assert
        tag.Deleted.Should().Be(deleted);
        tag.DeletedBy.Should().Be("admin@example.com");
    }

    [Fact]
    public void TagShouldHaveInheritedIdProperty()
    {
        // Arrange & Act
        var tag = new Tag
        {
            Id = 99,
            Name = "Special",
        };

        // Assert
        tag.Id.Should().Be(99);
    }
}
