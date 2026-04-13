namespace Collectibles.Domain.Tests.Common;

public class BaseEntityTests
{
    // Test classes that inherit from base entities
    private class TestEntity : BaseEntity
    {
        public string TestProperty { get; set; } = string.Empty;
    }

    private class TestAuditableEntity : BaseAuditableEntity
    {
        public string TestProperty { get; set; } = string.Empty;
    }

    private class TestSoftDeleteEntity : BaseAuditableSoftDeleteEntity
    {
        public string TestProperty { get; set; } = string.Empty;
    }

    [Fact]
    public void BaseEntityShouldHaveIdProperty()
    {
        // Arrange & Act
        var entity = new TestEntity { Id = 42 };

        // Assert
        entity.Id.Should().Be(42);
    }

    [Fact]
    public void BaseEntityShouldDefaultIdToZero()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        entity.Id.Should().Be(0);
    }

    [Fact]
    public void BaseAuditableEntityShouldHaveAuditProperties()
    {
        // Arrange
        var created = DateTime.UtcNow;
        var modified = created.AddDays(1);

        // Act
        var entity = new TestAuditableEntity
        {
            Created = created,
            CreatedBy = "user@example.com",
            LastModified = modified,
            LastModifiedBy = "admin@example.com",
        };

        // Assert
        entity.Created.Should().Be(created);
        entity.CreatedBy.Should().Be("user@example.com");
        entity.LastModified.Should().Be(modified);
        entity.LastModifiedBy.Should().Be("admin@example.com");
    }

    [Fact]
    public void BaseAuditableEntityShouldAllowNullAuditProperties()
    {
        // Arrange & Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.Created.Should().BeNull();
        entity.CreatedBy.Should().BeNull();
        entity.LastModified.Should().BeNull();
        entity.LastModifiedBy.Should().BeNull();
    }

    [Fact]
    public void BaseAuditableEntityShouldInheritFromBaseEntity()
    {
        // Arrange & Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.Should().BeAssignableTo<BaseEntity>();
        entity.Should().BeAssignableTo<IEntity<long>>();
    }

    [Fact]
    public void BaseAuditableEntityShouldImplementIAuditableEntity()
    {
        // Arrange & Act
        var entity = new TestAuditableEntity();

        // Assert
        entity.Should().BeAssignableTo<IAuditableEntity>();
    }

    [Fact]
    public void BaseAuditableSoftDeleteEntityShouldHaveSoftDeleteProperties()
    {
        // Arrange
        var deleted = DateTime.UtcNow;

        // Act
        var entity = new TestSoftDeleteEntity
        {
            Deleted = deleted,
            DeletedBy = "admin@example.com",
        };

        // Assert
        entity.Deleted.Should().Be(deleted);
        entity.DeletedBy.Should().Be("admin@example.com");
    }

    [Fact]
    public void BaseAuditableSoftDeleteEntityShouldAllowNullSoftDeleteProperties()
    {
        // Arrange & Act
        var entity = new TestSoftDeleteEntity();

        // Assert
        entity.Deleted.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void BaseAuditableSoftDeleteEntityShouldInheritFromBaseAuditableEntity()
    {
        // Arrange & Act
        var entity = new TestSoftDeleteEntity();

        // Assert
        entity.Should().BeAssignableTo<BaseAuditableEntity>();
        entity.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void BaseAuditableSoftDeleteEntityShouldImplementISoftDelete()
    {
        // Arrange & Act
        var entity = new TestSoftDeleteEntity();

        // Assert
        entity.Should().BeAssignableTo<ISoftDelete>();
    }

    [Fact]
    public void BaseAuditableSoftDeleteEntityShouldHaveAllInheritedProperties()
    {
        // Arrange
        var created = DateTime.UtcNow;
        var modified = created.AddHours(1);
        var deleted = created.AddDays(1);

        // Act
        var entity = new TestSoftDeleteEntity
        {
            Id = 999,
            TestProperty = "Test Value",
            Created = created,
            CreatedBy = "creator@example.com",
            LastModified = modified,
            LastModifiedBy = "editor@example.com",
            Deleted = deleted,
            DeletedBy = "admin@example.com",
        };

        // Assert
        // Base Entity properties
        entity.Id.Should().Be(999);

        // Test Entity properties
        entity.TestProperty.Should().Be("Test Value");

        // Auditable properties
        entity.Created.Should().Be(created);
        entity.CreatedBy.Should().Be("creator@example.com");
        entity.LastModified.Should().Be(modified);
        entity.LastModifiedBy.Should().Be("editor@example.com");

        // Soft Delete properties
        entity.Deleted.Should().Be(deleted);
        entity.DeletedBy.Should().Be("admin@example.com");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void BaseEntityShouldAcceptVariousIdValues(long id)
    {
        // Arrange & Act
        var entity = new TestEntity { Id = id };

        // Assert
        entity.Id.Should().Be(id);
    }
}
