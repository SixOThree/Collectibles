using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Repositories;

namespace Collectibles.Application.Services;

public class SiteConfigurationService : ISiteConfigurationService
{
    private readonly ISiteConfigurationRepository _repository;

    public SiteConfigurationService(ISiteConfigurationRepository repository)
    {
        _repository = repository;
    }

    public async Task<string> GetConfigurationValueAsync(string key, string? defaultValue = null)
    {
        var configuration = await _repository.GetByKeyAsync(key);
        return configuration?.Value ?? defaultValue ?? string.Empty;
    }

    public async Task SetConfigurationValueAsync(string key, string value, string? description = null)
    {
        var configuration = await _repository.GetByKeyAsync(key);

        if (configuration == null)
        {
            configuration = new SiteConfiguration
            {
                Key = key,
                Value = value,
                Description = description ?? string.Empty,
                LastModified = DateTime.UtcNow,
            };
            await _repository.CreateAsync(configuration);
        }
        else
        {
            configuration.Value = value;
            if (!string.IsNullOrEmpty(description))
            {
                configuration.Description = description;
            }

            await _repository.UpdateAsync(configuration);
        }
    }

    public async Task<bool> ConfigurationExistsAsync(string key)
    {
        return await _repository.ExistsAsync(key);
    }
}
