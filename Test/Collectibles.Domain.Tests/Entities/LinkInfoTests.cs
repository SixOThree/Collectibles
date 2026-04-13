namespace Collectibles.Domain.Tests.Entities;

public class LinkInfoTests
{
    [Fact]
    public void LinkInfoShouldCreateWithDefaultValues()
    {
        // Arrange & Act
        var link = new LinkInfo();

        // Assert
        link.Should().NotBeNull();
        link.Title.Should().BeNull();
        link.Url.Should().Be(string.Empty);
    }

    [Fact]
    public void LinkInfoShouldSetProperties()
    {
        // Arrange & Act
        var link = new LinkInfo
        {
            Title = "eBay Listing",
            Url = "https://www.ebay.com/itm/123456789",
        };

        // Assert
        link.Title.Should().Be("eBay Listing");
        link.Url.Should().Be("https://www.ebay.com/itm/123456789");
    }

    [Fact]
    public void LinkInfoShouldInheritFromBaseAuditableEntity()
    {
        // Arrange & Act
        var link = new LinkInfo();

        // Assert
        link.Should().BeAssignableTo<BaseAuditableEntity>();
        link.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void LinkInfoShouldNotInheritFromSoftDeleteEntity()
    {
        // Arrange & Act
        var link = new LinkInfo();

        // Assert
        link.Should().NotBeAssignableTo<BaseAuditableSoftDeleteEntity>();
        link.Should().NotBeAssignableTo<ISoftDelete>();
    }

    [Fact]
    public void LinkInfoShouldHaveInheritedAuditProperties()
    {
        // Arrange
        var created = DateTime.UtcNow;

        // Act
        var link = new LinkInfo
        {
            Title = "Reference Link",
            Created = created,
            CreatedBy = "researcher@example.com",
            LastModified = created.AddMinutes(15),
            LastModifiedBy = "editor@example.com",
        };

        // Assert
        link.Created.Should().Be(created);
        link.CreatedBy.Should().Be("researcher@example.com");
        link.LastModified.Should().Be(created.AddMinutes(15));
        link.LastModifiedBy.Should().Be("editor@example.com");
    }

    [Fact]
    public void LinkInfoShouldHaveInheritedIdProperty()
    {
        // Arrange & Act
        var link = new LinkInfo
        {
            Id = 555,
            Title = "Identified Link",
        };

        // Assert
        link.Id.Should().Be(555);
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com")]
    [InlineData("ftp://files.example.com")]
    [InlineData("www.example.com")]
    [InlineData("example.com/path/to/resource")]
    [InlineData("/relative/path")]
    public void LinkInfoShouldAcceptVariousUrlFormats(string? url)
    {
        // Arrange & Act
        var link = new LinkInfo
        {
            Url = url!,
        };

        // Assert
        link.Url.Should().Be(url);
    }
}
