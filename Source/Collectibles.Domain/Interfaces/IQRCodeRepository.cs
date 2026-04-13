using Collectibles.Domain.Entities;

namespace Collectibles.Domain.Interfaces;

public interface IQRCodeRepository
{
    Task<QRCode?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<QRCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IEnumerable<QRCode>> GetByStatusAsync(QRCodeStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<QRCode>> GetByUserAsync(string userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountByUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<QRCode> AddAsync(QRCode qrCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<QRCode>> AddRangeAsync(IEnumerable<QRCode> qrCodes, CancellationToken cancellationToken = default);
    Task UpdateAsync(QRCode qrCode, CancellationToken cancellationToken = default);
    Task DeleteAsync(QRCode qrCode, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);
    Task IncrementScanCountAsync(long id, CancellationToken cancellationToken = default);
}
