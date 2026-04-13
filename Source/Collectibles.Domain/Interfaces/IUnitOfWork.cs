namespace Collectibles.Domain.Interfaces;

/// <summary>
/// Interface for managing transactions and unit of work.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
}
