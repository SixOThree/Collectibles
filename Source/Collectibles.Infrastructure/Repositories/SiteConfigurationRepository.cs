using Collectibles.Domain.Repositories;
using Collectibles.Infrastructure.Persistence;

namespace Collectibles.Infrastructure.Repositories;

public class SiteConfigurationRepository : ISiteConfigurationRepository
{
    private readonly ApplicationDbContext _context;

    public SiteConfigurationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SiteConfiguration?> GetByKeyAsync(string key)
    {
        return await _context.SiteConfigurations
            .FirstOrDefaultAsync(sc => sc.Key == key);
    }

    public async Task<SiteConfiguration> CreateAsync(SiteConfiguration configuration)
    {
        configuration.LastModified = DateTime.UtcNow;
        _context.SiteConfigurations.Add(configuration);
        await _context.SaveChangesAsync();
        return configuration;
    }

    public async Task UpdateAsync(SiteConfiguration configuration)
    {
        configuration.LastModified = DateTime.UtcNow;
        _context.SiteConfigurations.Update(configuration);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _context.SiteConfigurations
            .AnyAsync(sc => sc.Key == key);
    }
}
