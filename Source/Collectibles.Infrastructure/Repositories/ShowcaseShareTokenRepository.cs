using Collectibles.Domain.Common;
using Collectibles.Domain.Interfaces;
using Collectibles.Infrastructure.Persistence;

namespace Collectibles.Infrastructure.Repositories;

public class ShowcaseShareTokenRepository : IShowcaseShareTokenRepository
{
    private readonly ApplicationDbContext _context;

    public ShowcaseShareTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ShowcaseShareToken?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.ShowcaseShareTokens
            .Include(s => s.Showcase)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<ShowcaseShareToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        // Storage holds only the hash, so the presented token is hashed and matched against it.
        var tokenHash = ShareTokenHash.Compute(token);

        return await _context.ShowcaseShareTokens
            .Include(s => s.Showcase)
                .ThenInclude(s => s.CollectibleItems)
                    .ThenInclude(ci => ci.CollectibleItemAttachments)
                        .ThenInclude(cia => cia.Attachment)
            .Include(s => s.Showcase)
                .ThenInclude(s => s.PreviewImage)
            .Include(s => s.Showcase)
                .ThenInclude(s => s.ShowcaseTags)
                    .ThenInclude(st => st.Tag)
            .FirstOrDefaultAsync(s => s.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IEnumerable<ShowcaseShareToken>> GetByShowcaseIdAsync(long showcaseId, CancellationToken cancellationToken = default)
    {
        return await _context.ShowcaseShareTokens
            .Where(s => s.ShowcaseId == showcaseId)
            .OrderByDescending(s => s.Created)
            .ToListAsync(cancellationToken);
    }

    public async Task<ShowcaseShareToken?> GetActiveTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var shareToken = await GetByTokenAsync(token, cancellationToken);

        if (shareToken == null || shareToken.IsExpired())
        {
            return null;
        }

        return shareToken;
    }

    public async Task<ShowcaseShareToken> AddAsync(ShowcaseShareToken shareToken, CancellationToken cancellationToken = default)
    {
        await _context.ShowcaseShareTokens.AddAsync(shareToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return shareToken;
    }

    public async Task UpdateAsync(ShowcaseShareToken shareToken, CancellationToken cancellationToken = default)
    {
        _context.ShowcaseShareTokens.Update(shareToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ShowcaseShareToken shareToken, CancellationToken cancellationToken = default)
    {
        shareToken.Deleted = DateTime.UtcNow;
        _context.ShowcaseShareTokens.Update(shareToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task IncrementViewCountAsync(string token, CancellationToken cancellationToken = default)
    {
        var tokenHash = ShareTokenHash.Compute(token);

        var shareToken = await _context.ShowcaseShareTokens
            .FirstOrDefaultAsync(s => s.TokenHash == tokenHash, cancellationToken);

        if (shareToken != null)
        {
            shareToken.ViewCount++;
            shareToken.LastViewedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> TokenExistsAsync(string token, CancellationToken cancellationToken = default)
    {
        var tokenHash = ShareTokenHash.Compute(token);

        return await _context.ShowcaseShareTokens
            .AnyAsync(s => s.TokenHash == tokenHash, cancellationToken);
    }
}
