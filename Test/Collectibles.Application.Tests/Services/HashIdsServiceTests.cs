using Collectibles.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace Collectibles.Application.Tests.Services;

public class HashIdsServiceTests
{
    [Fact]
    public void Constructor_WithValidSalt_CreatesService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HashIds:Salt"] = "my-unique-salt-value",
            })
            .Build();

        var service = new HashIdsService(config);

        var encoded = service.Encode(42);
        Assert.NotNull(encoded);
        Assert.NotEmpty(encoded);
    }

    [Fact]
    public void Constructor_WithMissingSalt_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() => new HashIdsService(config));
        Assert.Contains("HashIds:Salt", ex.Message);
    }

    [Fact]
    public void Constructor_WithPlaceholderSalt_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HashIds:Salt"] = "YOUR_UNIQUE_SALT_HERE",
            })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() => new HashIdsService(config));
        Assert.Contains("placeholder", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WithOldFallbackSalt_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HashIds:Salt"] = "collectibles-default-salt-change-in-production",
            })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() => new HashIdsService(config));
        Assert.Contains("placeholder", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
