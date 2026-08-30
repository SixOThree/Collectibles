using Collectibles.Domain.Common;
using Collectibles.Domain.Entities;

namespace Collectibles.Domain.Tests.Common;

/// <summary>
/// Covers the stored form of a share token, and the expiry rule that stops a leaked link from
/// remaining usable indefinitely.
/// </summary>
public class ShareTokenHashTests
{
    [Fact]
    public void ComputeShouldProduceLowercaseHexOfTheExpectedLength()
    {
        var hash = ShareTokenHash.Compute("some-share-token");

        hash.Should().HaveLength(ShareTokenHash.Length);
        hash.Should().MatchRegex("^[0-9a-f]+$");
    }

    [Fact]
    public void ComputeShouldBeDeterministic()
    {
        ShareTokenHash.Compute("token-value")
            .Should().Be(ShareTokenHash.Compute("token-value"));
    }

    [Fact]
    public void ComputeShouldDifferForDifferentTokens()
    {
        ShareTokenHash.Compute("token-a")
            .Should().NotBe(ShareTokenHash.Compute("token-b"));
    }

    [Fact]
    public void ComputeShouldNotContainThePlaintext()
    {
        const string token = "abcdefghijklmnop";

        ShareTokenHash.Compute(token).Should().NotContain(token);
    }

    [Fact]
    public void ComputeShouldMatchTheKnownSha256OfItsInput()
    {
        // Guards the migration, which derives the same value in T-SQL. If this changes, every
        // previously stored hash stops matching.
        ShareTokenHash.Compute("abc")
            .Should().Be("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ComputeShouldRejectAnAbsentToken(string? token)
    {
        var act = () => ShareTokenHash.Compute(token!);

        act.Should().Throw<ArgumentException>();
    }
}

/// <summary>
/// Covers <see cref="ShowcaseShareToken.IsExpired"/>, which decides whether a presented link works.
/// </summary>
public class ShowcaseShareTokenExpiryTests
{
    [Fact]
    public void IsExpiredShouldBeFalseForAnActiveTokenWithinItsWindow()
    {
        var token = new ShowcaseShareToken
        {
            TokenHash = ShareTokenHash.Compute("t"),
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsActive = true,
        };

        token.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpiredShouldBeTrueOncePastTheExpiry()
    {
        var token = new ShowcaseShareToken
        {
            TokenHash = ShareTokenHash.Compute("t"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            IsActive = true,
        };

        token.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsExpiredShouldBeTrueWhenRevoked()
    {
        var token = new ShowcaseShareToken
        {
            TokenHash = ShareTokenHash.Compute("t"),
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsActive = false,
        };

        token.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsExpiredShouldBeTrueWhenSoftDeleted()
    {
        var token = new ShowcaseShareToken
        {
            TokenHash = ShareTokenHash.Compute("t"),
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsActive = true,
            Deleted = DateTime.UtcNow,
        };

        token.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void ADefaultConstructedTokenShouldNotBeUsable()
    {
        // Expiry is non-nullable, so an unset value is DateTime.MinValue - already in the past.
        // "Never expires" is deliberately not a representable state.
        new ShowcaseShareToken().IsExpired().Should().BeTrue();
    }
}
