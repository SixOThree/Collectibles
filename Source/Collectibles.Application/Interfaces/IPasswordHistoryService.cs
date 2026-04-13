namespace Collectibles.Application.Interfaces;

public interface IPasswordHistoryService
{
    Task AddToHistoryAsync(string userId, string passwordHash, CancellationToken cancellationToken = default);
    Task<bool> IsInHistoryAsync(string userId, string password, CancellationToken cancellationToken = default);
    Task ClearHistoryAsync(string userId, CancellationToken cancellationToken = default);
}
