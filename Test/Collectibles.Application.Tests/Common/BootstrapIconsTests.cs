using Collectibles.Application.Common;

namespace Collectibles.Application.Tests.Common;

public class BootstrapIconsTests
{
    [Fact]
    public void All_ContainsKnownIcons()
    {
        BootstrapIcons.All.Should().NotBeEmpty();
        BootstrapIcons.All.Should().Contain("bi-star");
        BootstrapIcons.All.Should().Contain("bi-grid-3x3-gap");
        BootstrapIcons.All.Should().Contain("bi-speedometer2");
    }

    [Fact]
    public void All_EveryEntryHasBiPrefix()
    {
        BootstrapIcons.All.Should().OnlyContain(i => i.StartsWith("bi-"));
    }

    [Fact]
    public void IsValid_KnownIcon_ReturnsTrue()
    {
        BootstrapIcons.IsValid("bi-star").Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bi-not-a-real-icon")]
    [InlineData("bi-star\" onload=\"alert(1)")]
    public void IsValid_InvalidNames_ReturnsFalse(string? name)
    {
        BootstrapIcons.IsValid(name).Should().BeFalse();
    }
}
