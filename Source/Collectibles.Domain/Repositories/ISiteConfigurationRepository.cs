using Collectibles.Domain.Entities;

namespace Collectibles.Domain.Repositories;

public interface ISiteConfigurationRepository
{
    Task<SiteConfiguration?> GetByKeyAsync(string key);
    Task<SiteConfiguration> CreateAsync(SiteConfiguration configuration);
    Task UpdateAsync(SiteConfiguration configuration);
    Task<bool> ExistsAsync(string key);
}
