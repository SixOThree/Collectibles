namespace Collectibles.Application.Interfaces;

public interface ISiteConfigurationService
{
    Task<string> GetConfigurationValueAsync(string key, string? defaultValue = null);
    Task SetConfigurationValueAsync(string key, string value, string? description = null);
    Task<bool> ConfigurationExistsAsync(string key);
}
