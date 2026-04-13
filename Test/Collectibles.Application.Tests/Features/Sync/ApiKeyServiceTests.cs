using Collectibles.Infrastructure.Services;
using FluentAssertions;

namespace Collectibles.Application.Tests.Features.Sync;

public class ApiKeyServiceTests
{
    private readonly ApiKeyService _service = new();

    [Fact]
    public void GenerateKeyShouldReturnBase64UrlSafeKey()
    {
        var result = _service.GenerateKey();

        result.RawKey.Should().NotBeNullOrWhiteSpace();
        result.RawKey.Should().HaveLength(43);
        result.RawKey.Should().NotContain("+");
        result.RawKey.Should().NotContain("/");
        result.RawKey.Should().NotContain("=");
    }

    [Fact]
    public void GenerateKeyShouldReturnMatchingHash()
    {
        var result = _service.GenerateKey();

        var reHashed = _service.HashKey(result.RawKey);
        reHashed.Should().Be(result.KeyHash);
    }

    [Fact]
    public void GenerateKeyShouldProduceUniqueKeys()
    {
        var key1 = _service.GenerateKey();
        var key2 = _service.GenerateKey();

        key1.RawKey.Should().NotBe(key2.RawKey);
        key1.KeyHash.Should().NotBe(key2.KeyHash);
    }

    [Fact]
    public void HashKeyShouldBeConsistentForSameInput()
    {
        var rawKey = "test-key-value";

        var hash1 = _service.HashKey(rawKey);
        var hash2 = _service.HashKey(rawKey);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashKeyShouldReturnHexString()
    {
        var result = _service.GenerateKey();

        result.KeyHash.Should().MatchRegex("^[0-9A-F]{64}$");
    }
}
