using Collectibles.Application.Common.Models;
using Collectibles.Application.Services;
using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Tests.Services;

public class HomePageContentServiceTests
{
    private readonly Mock<ISiteConfigurationService> _siteConfig = new();
    private readonly HomePageContentService _service;

    public HomePageContentServiceTests()
    {
        _service = new HomePageContentService(
            _siteConfig.Object,
            Mock.Of<ILogger<HomePageContentService>>());
    }

    private void SetupStoredValue(string value) =>
        _siteConfig
            .Setup(s => s.GetConfigurationValueAsync(
                HomePageContentService.ConfigurationKey, It.IsAny<string?>()))
            .ReturnsAsync(value);

    private static HomePageContent ValidContent() => new()
    {
        HeroTitle = "Hero",
        HeroLead = "Lead",
        Cards =
        [
            new HomePageCard { Icon = "bi-star", Title = "First", Text = "First text" },
            new HomePageCard { Icon = "bi-layers", Title = "Second", Text = "Second text" },
        ],
    };

    [Fact]
    public void Default_HasSixCardsWithValidIcons()
    {
        var d = HomePageContent.Default;
        d.HeroTitle.Should().NotBeNullOrWhiteSpace();
        d.HeroLead.Should().Contain("{SiteTitle}");
        d.Cards.Should().HaveCount(6);
        d.Cards.Should().OnlyContain(c => Collectibles.Application.Common.BootstrapIcons.IsValid(c.Icon));
    }

    [Fact]
    public void Default_ReturnsFreshInstancePerAccess()
    {
        var a = HomePageContent.Default;
        a.Cards.Clear();
        HomePageContent.Default.Cards.Should().HaveCount(6);
    }

    [Theory]
    [InlineData("{SiteTitle} is great", "Collectibles is great")]
    [InlineData("No token here", "No token here")]
    [InlineData("{SiteTitle} and {SiteTitle}", "Collectibles and Collectibles")]
    public void ReplaceSiteTitle_ReplacesEveryOccurrence(string input, string expected)
    {
        HomePageContent.ReplaceSiteTitle(input, "Collectibles").Should().Be(expected);
    }

    [Fact]
    public async Task GetAsync_MissingValue_ReturnsDefaults()
    {
        SetupStoredValue(string.Empty);
        var result = await _service.GetAsync();
        result.Should().BeEquivalentTo(HomePageContent.Default);
    }

    [Fact]
    public async Task GetAsync_MalformedJson_ReturnsDefaults()
    {
        SetupStoredValue("{ this is not json");
        var result = await _service.GetAsync();
        result.Should().BeEquivalentTo(HomePageContent.Default);
    }

    [Fact]
    public async Task GetAsync_JsonNullLiteral_ReturnsDefaults()
    {
        SetupStoredValue("null");
        var result = await _service.GetAsync();
        result.Should().BeEquivalentTo(HomePageContent.Default);
    }

    [Fact]
    public async Task GetAsync_RepositoryThrows_ReturnsDefaults()
    {
        _siteConfig
            .Setup(s => s.GetConfigurationValueAsync(It.IsAny<string>(), It.IsAny<string?>()))
            .ThrowsAsync(new InvalidOperationException("db down"));
        var result = await _service.GetAsync();
        result.Should().BeEquivalentTo(HomePageContent.Default);
    }

    [Fact]
    public async Task SaveThenGet_RoundTripsContentIncludingCardOrder()
    {
        string? savedJson = null;
        _siteConfig
            .Setup(s => s.SetConfigurationValueAsync(
                HomePageContentService.ConfigurationKey, It.IsAny<string>(), It.IsAny<string?>()))
            .Callback<string, string, string?>((_, value, _) => savedJson = value)
            .Returns(Task.CompletedTask);

        var content = ValidContent();
        await _service.SaveAsync(content);

        savedJson.Should().NotBeNullOrWhiteSpace();
        SetupStoredValue(savedJson!);

        var result = await _service.GetAsync();
        result.Should().BeEquivalentTo(content, o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task SaveAsync_EmptyHeroTitle_Throws()
    {
        var content = ValidContent();
        content.HeroTitle = "  ";
        var act = () => _service.SaveAsync(content);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SaveAsync_EmptyHeroLead_Throws()
    {
        var content = ValidContent();
        content.HeroLead = "";
        var act = () => _service.SaveAsync(content);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SaveAsync_CardMissingTitle_Throws()
    {
        var content = ValidContent();
        content.Cards[0].Title = "";
        var act = () => _service.SaveAsync(content);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SaveAsync_CardMissingText_Throws()
    {
        var content = ValidContent();
        content.Cards[1].Text = "   ";
        var act = () => _service.SaveAsync(content);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SaveAsync_UnknownIcon_Throws()
    {
        var content = ValidContent();
        content.Cards[0].Icon = "bi-not-a-real-icon";
        var act = () => _service.SaveAsync(content);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SaveAsync_EmptyCardList_IsAllowed()
    {
        _siteConfig
            .Setup(s => s.SetConfigurationValueAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        var content = ValidContent();
        content.Cards.Clear();
        await _service.SaveAsync(content);
        _siteConfig.Verify(
            s => s.SetConfigurationValueAsync(
                HomePageContentService.ConfigurationKey, It.IsAny<string>(), It.IsAny<string?>()),
            Times.Once);
    }
}
