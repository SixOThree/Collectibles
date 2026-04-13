using Collectibles.Domain.Entities;

namespace Collectibles.Domain.Interfaces;

public interface IShowcaseShareTokenRepository
{
    Task<ShowcaseShareToken?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ShowcaseShareToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShowcaseShareToken>> GetByShowcaseIdAsync(long showcaseId, CancellationToken cancellationToken = default);
    Task<ShowcaseShareToken?> GetActiveTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<ShowcaseShareToken> AddAsync(ShowcaseShareToken shareToken, CancellationToken cancellationToken = default);
    Task UpdateAsync(ShowcaseShareToken shareToken, CancellationToken cancellationToken = default);
    Task DeleteAsync(ShowcaseShareToken shareToken, CancellationToken cancellationToken = default);
    Task IncrementViewCountAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> TokenExistsAsync(string token, CancellationToken cancellationToken = default);
}
