namespace Collectibles.Application.Interfaces;

public interface IThemeService
{
    Task<string> GetCurrentThemeAsync();
    Task SetThemeAsync(string themeName);
    IReadOnlyList<string> GetAvailableThemes();
    bool IsValidTheme(string themeName);
    bool IsCustomTheme(string themeName);
    Task<string?> GetCurrentBackgroundImageAsync();
    Task SetBackgroundImageAsync(string? imageName);
    IReadOnlyList<string> GetAvailableBackgroundImages();
    Task InitializeThemeAsync(CancellationToken cancellationToken = default);
}
