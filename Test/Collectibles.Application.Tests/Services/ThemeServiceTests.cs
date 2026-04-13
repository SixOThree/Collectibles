using Collectibles.Application.Interfaces;
using Collectibles.Application.Tests.Helpers;
using Collectibles.Infrastructure.Persistence;
using Collectibles.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Collectibles.Application.Tests.Services;

public class ThemeServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _webRootPath;
    private readonly ApplicationDbContext _dbContext;

    public ThemeServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"collectibles-theme-tests-{Guid.NewGuid():N}");
        _webRootPath = Path.Combine(_tempRoot, "wwwroot");
        Directory.CreateDirectory(_webRootPath);
        _dbContext = DbContextFactory.Create();
    }

    [Fact]
    public async Task InitializeThemeAsync_RemovesStaleCompressedThemeConfigVariants()
    {
        CreateBootswatchTheme("quartz");

        _dbContext.SiteConfigurations.Add(new SiteConfiguration
        {
            Key = "Theme",
            Value = "quartz",
            Description = "Test theme",
            LastModified = DateTime.UtcNow,
        });
        await _dbContext.SaveChangesAsync();

        var themeConfigDir = Path.Combine(_webRootPath, "theme-config");
        Directory.CreateDirectory(themeConfigDir);

        var themeCssPath = Path.Combine(themeConfigDir, "theme-config.css");
        await File.WriteAllTextAsync(themeCssPath, "old css");
        await File.WriteAllTextAsync($"{themeCssPath}.gz", "stale gzip");
        await File.WriteAllTextAsync($"{themeCssPath}.br", "stale brotli");

        var service = CreateThemeService();

        await service.InitializeThemeAsync();

        File.Exists(themeCssPath).Should().BeTrue();
        File.Exists($"{themeCssPath}.gz").Should().BeFalse();
        File.Exists($"{themeCssPath}.br").Should().BeFalse();
        var css = await File.ReadAllTextAsync(themeCssPath);
        css.Should().Contain("/themes/bootswatch/quartz/bootstrap.min.css");
    }

    public void Dispose()
    {
        DbContextFactory.Destroy(_dbContext);

        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private IThemeService CreateThemeService()
    {
        var configuration = new ConfigurationBuilder().Build();
        var logger = Mock.Of<ILogger<ThemeService>>();
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.WebRootPath).Returns(_webRootPath);

        return new ThemeService(configuration, logger, environment.Object, _dbContext);
    }

    private void CreateBootswatchTheme(string themeName)
    {
        var themeDir = Path.Combine(_webRootPath, "themes", "bootswatch", themeName);
        Directory.CreateDirectory(themeDir);
        File.WriteAllText(Path.Combine(themeDir, "bootstrap.min.css"), "/* test theme */");
    }
}
