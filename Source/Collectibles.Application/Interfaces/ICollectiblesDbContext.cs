using Collectibles.Domain.Entities;

namespace Collectibles.Application.Interfaces;

/// <summary>
/// Application-specific database context interface
/// This interface is intentionally persistence-agnostic and doesn't expose
/// specific ORM features like DbSet.
/// </summary>
public interface ICollectiblesDbContext
{
    // Read operations
    Task<T?> GetByIdAsync<T>(long id)
        where T : class;
    Task<IReadOnlyList<T>> ListAllAsync<T>()
        where T : class;
    Task<IReadOnlyList<T>> ListAsync<T>(System.Linq.Expressions.Expression<Func<T, bool>> filter)
        where T : class;

    // Write operations
    Task<T> AddAsync<T>(T entity)
        where T : class;
    Task UpdateAsync<T>(T entity)
        where T : class;
    Task DeleteAsync<T>(T entity)
        where T : class;

    // Transaction management
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Type-specific methods for convenience (can be expanded as needed)
    Task<IReadOnlyList<Attachment>> GetAttachmentsAsync(long collectibleItemId);
    Task<IReadOnlyList<CollectibleItem>> GetCollectibleItemsAsync(string? searchTerm = null, int? skip = null, int? take = null);
    Task<IReadOnlyList<Showcase>> GetShowcasesAsync(string? userId = null, bool includePrivate = false);
}
