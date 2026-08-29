using Collectibles.Application.Interfaces;
using Collectibles.Domain.Constants;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Collectibles.Infrastructure.Services;

public class ThemeService : IThemeService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ThemeService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IApplicationDbContext _dbContext;

    public ThemeService(
        IConfiguration configuration,
        ILogger<ThemeService> logger,
        IWebHostEnvironment environment,
        IApplicationDbContext dbContext)
    {
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
        _dbContext = dbContext;
    }

    public async Task<string> GetCurrentThemeAsync()
    {
        try
        {
            var themeConfig = await _dbContext.SiteConfigurations
                .FirstOrDefaultAsync(sc => sc.Key == "Theme");

            if (themeConfig != null && !string.IsNullOrEmpty(themeConfig.Value))
            {
                // Validate that the theme exists on disk
                if (IsValidTheme(themeConfig.Value))
                {
                    return themeConfig.Value;
                }

                _logger.LogWarning("Invalid theme '{Theme}' found in database, using default theme", themeConfig.Value);
            }

            // Default theme
            return ApplicationConstants.Theme.DefaultTheme;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current theme");
            return ApplicationConstants.Theme.DefaultTheme;
        }
    }

    public async Task SetThemeAsync(string themeName)
    {
        if (!IsValidTheme(themeName))
        {
            throw new ArgumentException($"Invalid theme name: {themeName}");
        }

        try
        {
            var themeConfig = await _dbContext.SiteConfigurations
                .FirstOrDefaultAsync(sc => sc.Key == "Theme");

            if (themeConfig != null)
            {
                // Update existing
                themeConfig.Value = themeName;
                themeConfig.LastModified = DateTime.UtcNow;
            }
            else
            {
                // Insert new
                themeConfig = new SiteConfiguration
                {
                    Key = "Theme",
                    Value = themeName,
                    Description = "The Bootswatch theme used for the site",
                    LastModified = DateTime.UtcNow,
                };
                _dbContext.SiteConfigurations.Add(themeConfig);
            }

            await _dbContext.SaveChangesAsync(CancellationToken.None);

            // Write theme to static CSS file with current background image
            var currentBackgroundImage = await GetCurrentBackgroundImageAsync();
            await WriteThemeToStaticFile(themeName, currentBackgroundImage);

            _logger.LogInformation("Theme changed to {ThemeName}", themeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting theme to {ThemeName}", themeName);
            throw;
        }
    }

    private async Task WriteThemeToStaticFile(string themeName, string? backgroundImage = null)
    {
        try
        {
            var themeConfigDir = Path.Combine(_environment.WebRootPath, "theme-config");
            _logger.LogInformation(
                "WriteThemeToStaticFile: WebRootPath={WebRootPath}, themeConfigDir={ThemeConfigDir}",
                _environment.WebRootPath, themeConfigDir);

            if (!Directory.Exists(themeConfigDir))
            {
                Directory.CreateDirectory(themeConfigDir);
                _logger.LogInformation("WriteThemeToStaticFile: Created directory {ThemeConfigDir}", themeConfigDir);
            }

            // Determine theme path by checking disk for the actual CSS file
            var themeImportPath = ResolveThemeImportPath(themeName)
                ?? $"/themes/bootswatch/{themeName}/bootstrap.min.css";
            _logger.LogInformation(
                "WriteThemeToStaticFile: Resolved import path for {ThemeName} = {ImportPath}",
                themeName, themeImportPath);

            // Write main theme configuration
            var themeCssPath = Path.Combine(themeConfigDir, "theme-config.css");
            var themeCssContent = $@"/* Auto-generated theme configuration - Do not edit manually */
/* Current theme: {themeName} */
@import url('{themeImportPath}');
";

            // Add import for background CSS if a background image is specified
            if (!string.IsNullOrEmpty(backgroundImage))
            {
                themeCssContent += $@"
/* Background styling */
body {{
    background-image: url('/themes/backdrops/{backgroundImage}');
}}
@import url('/theme-config/background.css');
";
            }

            _logger.LogInformation(
                "WriteThemeToStaticFile: Writing to {CssPath}, content length={Length}",
                themeCssPath, themeCssContent.Length);
            await File.WriteAllTextAsync(themeCssPath, themeCssContent);
            DeleteStaleCompressedThemeConfigVariants(themeCssPath);

            // Verify the write
            var written = await File.ReadAllTextAsync(themeCssPath);
            _logger.LogInformation("WriteThemeToStaticFile: Verified file contents: {Contents}", written.Trim());
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access error writing theme to static file");
            throw new InvalidOperationException(
                $"Unable to write theme file to {Path.Combine("wwwroot", "theme-config")}. " +
                "Please ensure the IIS_IUSRS account (or the account under which IIS is running) " +
                "has write permissions to this folder.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing theme to static file");
            throw new InvalidOperationException(
                "Failed to write theme configuration file. Please check file system permissions.", ex);
        }
    }

    public IReadOnlyList<string> GetAvailableThemes()
    {
        var themes = new List<string>();

        var bootswatchPath = Path.Combine(_environment.WebRootPath, "themes", "bootswatch");
        if (Directory.Exists(bootswatchPath))
        {
            themes.AddRange(Directory.GetDirectories(bootswatchPath)
                .Where(dir => GetThemeCssFile(dir) != null)
                .Select(dir => Path.GetFileName(dir)!)
                .OrderBy(name => name));
        }

        var customPath = Path.Combine(_environment.WebRootPath, "themes", "custom");
        if (Directory.Exists(customPath))
        {
            themes.AddRange(Directory.GetDirectories(customPath)
                .Where(dir => GetThemeCssFile(dir) != null)
                .Select(dir => Path.GetFileName(dir)!)
                .OrderBy(name => name));
        }

        return themes.AsReadOnly();
    }

    public bool IsValidTheme(string themeName)
    {
        return !string.IsNullOrEmpty(themeName) &&
            ResolveThemeImportPath(themeName) != null;
    }

    public bool IsCustomTheme(string themeName)
    {
        var customDir = Path.Combine(_environment.WebRootPath, "themes", "custom", themeName);
        return Directory.Exists(customDir) && GetThemeCssFile(customDir) != null;
    }

    private string? ResolveThemeImportPath(string themeName)
    {
        // Check custom themes first
        var customDir = Path.Combine(_environment.WebRootPath, "themes", "custom", themeName);
        if (Directory.Exists(customDir))
        {
            var cssFile = GetThemeCssFile(customDir);
            if (cssFile != null)
            {
                return $"/themes/custom/{themeName}/{cssFile}";
            }
        }

        // Check bootswatch themes
        var bootswatchDir = Path.Combine(_environment.WebRootPath, "themes", "bootswatch", themeName);
        if (Directory.Exists(bootswatchDir))
        {
            var cssFile = GetThemeCssFile(bootswatchDir);
            if (cssFile != null)
            {
                return $"/themes/bootswatch/{themeName}/{cssFile}";
            }
        }

        return null;
    }

    private static string? GetThemeCssFile(string themeDir)
    {
        if (File.Exists(Path.Combine(themeDir, "bootstrap.min.css")))
        {
            return "bootstrap.min.css";
        }

        if (File.Exists(Path.Combine(themeDir, "theme.css")))
        {
            return "theme.css";
        }

        return null;
    }

    public async Task<string?> GetCurrentBackgroundImageAsync()
    {
        try
        {
            var backgroundConfig = await _dbContext.SiteConfigurations
                .FirstOrDefaultAsync(sc => sc.Key == "BackgroundImage");

            return backgroundConfig?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current background image");
            return null;
        }
    }

    public async Task SetBackgroundImageAsync(string? imageName)
    {
        try
        {
            var backgroundConfig = await _dbContext.SiteConfigurations
                .FirstOrDefaultAsync(sc => sc.Key == "BackgroundImage");

            if (backgroundConfig != null)
            {
                // Update existing
                backgroundConfig.Value = imageName ?? string.Empty;
                backgroundConfig.LastModified = DateTime.UtcNow;
            }
            else
            {
                // Insert new
                backgroundConfig = new SiteConfiguration
                {
                    Key = "BackgroundImage",
                    Value = imageName ?? string.Empty,
                    Description = "The background image used for the site",
                    LastModified = DateTime.UtcNow,
                };
                _dbContext.SiteConfigurations.Add(backgroundConfig);
            }

            await _dbContext.SaveChangesAsync(CancellationToken.None);

            // Update the theme CSS file to include background image
            var currentTheme = await GetCurrentThemeAsync();
            await WriteThemeToStaticFile(currentTheme, imageName);

            _logger.LogInformation("Background image changed to {ImageName}", imageName ?? "none");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting background image to {ImageName}", imageName);
            throw;
        }
    }

    public IReadOnlyList<string> GetAvailableBackgroundImages()
    {
        try
        {
            var backdropPath = Path.Combine(_environment.WebRootPath, "themes", "backdrops");
            if (Directory.Exists(backdropPath))
            {
                var imageFiles = Directory.GetFiles(backdropPath)
                    .Select(Path.GetFileName)
                    .Where(f => f != null && (f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                              f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                              f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                                              f.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)))
                    .Select(f => f!)
                    .OrderBy(f => f)
                    .ToList();
                return imageFiles.AsReadOnly();
            }

            return new List<string>().AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available background images");
            return new List<string>().AsReadOnly();
        }
    }

    public async Task InitializeThemeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing theme configuration...");

        try
        {
            // Get the current theme from the database
            var currentTheme = await GetCurrentThemeAsync();

            // Get the current background image from the database
            var currentBackgroundImage = await GetCurrentBackgroundImageAsync();

            // Ensure the theme-config directory exists
            var themeConfigDir = Path.Combine(_environment.WebRootPath, "theme-config");
            if (!Directory.Exists(themeConfigDir))
            {
                Directory.CreateDirectory(themeConfigDir);
            }

            // Write the theme configuration using the existing method
            await WriteThemeToStaticFile(currentTheme, currentBackgroundImage);

            _logger.LogInformation(
                "Theme configuration initialized with theme: {Theme} and background: {BackgroundImage}",
                currentTheme, currentBackgroundImage ?? "none");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing theme configuration");

            // Create a default theme file as fallback
            try
            {
                var themeConfigDir = Path.Combine(_environment.WebRootPath, "theme-config");
                if (!Directory.Exists(themeConfigDir))
                {
                    Directory.CreateDirectory(themeConfigDir);
                }

                var themeConfigPath = Path.Combine(themeConfigDir, "theme-config.css");
                var defaultContent = $@"/* Auto-generated theme configuration - Do not edit manually */
/* Current theme: {ApplicationConstants.Theme.DefaultTheme} (default) */
@import url('/themes/bootswatch/{ApplicationConstants.Theme.DefaultTheme}/bootstrap.min.css');
";
                await File.WriteAllTextAsync(themeConfigPath, defaultContent, cancellationToken);
                DeleteStaleCompressedThemeConfigVariants(themeConfigPath);
                _logger.LogInformation("Created default theme configuration file");
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Failed to create default theme configuration file");
            }
        }
    }

    private static void DeleteStaleCompressedThemeConfigVariants(string themeCssPath)
    {
        DeleteIfExists($"{themeCssPath}.gz");
        DeleteIfExists($"{themeCssPath}.br");
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
