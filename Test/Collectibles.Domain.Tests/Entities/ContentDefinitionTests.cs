namespace Collectibles.Domain.Tests.Entities;

public class ContentDefinitionTests
{
    [Fact]
    public void ContentDefinitionShouldCreateWithDefaultValues()
    {
        // Arrange & Act
        var contentDef = new ContentDefinition();

        // Assert
        contentDef.Should().NotBeNull();
        contentDef.Name.Should().BeNull();
        contentDef.DefinitionJson.Should().BeNull();
    }

    [Fact]
    public void ContentDefinitionShouldSetProperties()
    {
        // Arrange & Act
        var contentDef = new ContentDefinition
        {
            Name = "Trading Card Template",
            DefinitionJson = @"{
                ""fields"": [
                    { ""name"": ""cardName"", ""type"": ""string"", ""required"": true },
                    { ""name"": ""setNumber"", ""type"": ""string"" },
                    { ""name"": ""rarity"", ""type"": ""string"" }
                ]
            }",
        };

        // Assert
        contentDef.Name.Should().Be("Trading Card Template");
        contentDef.DefinitionJson.Should().Contain("cardName");
        contentDef.DefinitionJson.Should().Contain("setNumber");
        contentDef.DefinitionJson.Should().Contain("rarity");
    }

    [Fact]
    public void ContentDefinitionShouldInheritFromBaseAuditableEntity()
    {
        // Arrange & Act
        var contentDef = new ContentDefinition();

        // Assert
        contentDef.Should().BeAssignableTo<BaseAuditableEntity>();
        contentDef.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void ContentDefinitionShouldNotInheritFromSoftDeleteEntity()
    {
        // Arrange & Act
        var contentDef = new ContentDefinition();

        // Assert
        contentDef.Should().NotBeAssignableTo<BaseAuditableSoftDeleteEntity>();
        contentDef.Should().NotBeAssignableTo<ISoftDelete>();
    }

    [Fact]
    public void ContentDefinitionShouldHaveInheritedAuditProperties()
    {
        // Arrange
        var created = DateTime.UtcNow;

        // Act
        var contentDef = new ContentDefinition
        {
            Name = "Audited Template",
            Created = created,
            CreatedBy = "designer@example.com",
            LastModified = created.AddHours(2),
            LastModifiedBy = "admin@example.com",
        };

        // Assert
        contentDef.Created.Should().Be(created);
        contentDef.CreatedBy.Should().Be("designer@example.com");
        contentDef.LastModified.Should().Be(created.AddHours(2));
        contentDef.LastModifiedBy.Should().Be("admin@example.com");
    }

    [Fact]
    public void ContentDefinitionShouldHaveInheritedIdProperty()
    {
        // Arrange & Act
        var contentDef = new ContentDefinition
        {
            Id = 789,
            Name = "Identified Template",
        };

        // Assert
        contentDef.Id.Should().Be(789);
    }

    [Fact]
    public void ContentDefinitionShouldAllowComplexJsonDefinition()
    {
        // Arrange
        var complexJson = @"{
            ""version"": ""1.0"",
            ""fields"": [
                {
                    ""name"": ""title"",
                    ""type"": ""string"",
                    ""required"": true,
                    ""maxLength"": 100
                },
                {
                    ""name"": ""year"",
                    ""type"": ""number"",
                    ""min"": 1900,
                    ""max"": 2100
                },
                {
                    ""name"": ""condition"",
                    ""type"": ""enum"",
                    ""values"": [""Mint"", ""Near Mint"", ""Good"", ""Fair"", ""Poor""]
                }
            ],
            ""validation"": {
                ""allowAdditionalFields"": false
            }
        }";

        // Act
        var contentDef = new ContentDefinition
        {
            Name = "Complex Item Template",
            DefinitionJson = complexJson,
        };

        // Assert
        contentDef.DefinitionJson.Should().Contain("version");
        contentDef.DefinitionJson.Should().Contain("validation");
        contentDef.DefinitionJson.Should().Contain("maxLength");
    }
}
