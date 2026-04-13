using Collectibles.Domain.Common.Enums;
using Collectibles.Domain.Interfaces;
using Collectibles.Infrastructure.Persistence;

namespace Collectibles.Infrastructure.Repositories;

public class QRCodeRepository : IQRCodeRepository
{
    private readonly ApplicationDbContext _context;

    public QRCodeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<QRCode?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.QRCodes
            .Include(q => q.CollectibleItem)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
    }

    public async Task<QRCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.QRCodes
            .Include(q => q.CollectibleItem)
            .FirstOrDefaultAsync(q => q.Code == code, cancellationToken);
    }

    public async Task<IEnumerable<QRCode>> GetByStatusAsync(QRCodeStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.QRCodes
            .Where(q => q.Status == status)
            .Include(q => q.CollectibleItem)
            .OrderByDescending(q => q.Created)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<QRCode>> GetByUserAsync(string userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.QRCodes
            .Where(q => q.CreatedBy == userId)
            .Include(q => q.CollectibleItem)
            .OrderByDescending(q => q.Created)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.QRCodes
            .CountAsync(q => q.CreatedBy == userId, cancellationToken);
    }

    public async Task<QRCode> AddAsync(QRCode qrCode, CancellationToken cancellationToken = default)
    {
        _context.QRCodes.Add(qrCode);
        await _context.SaveChangesAsync(cancellationToken);
        return qrCode;
    }

    public async Task<IEnumerable<QRCode>> AddRangeAsync(IEnumerable<QRCode> qrCodes, CancellationToken cancellationToken = default)
    {
        var qrCodesList = qrCodes.ToList();
        _context.QRCodes.AddRange(qrCodesList);
        await _context.SaveChangesAsync(cancellationToken);
        return qrCodesList;
    }

    public async Task UpdateAsync(QRCode qrCode, CancellationToken cancellationToken = default)
    {
        _context.QRCodes.Update(qrCode);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(QRCode qrCode, CancellationToken cancellationToken = default)
    {
        _context.QRCodes.Remove(qrCode);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.QRCodes.AnyAsync(q => q.Code == code, cancellationToken);
    }

    public async Task IncrementScanCountAsync(long id, CancellationToken cancellationToken = default)
    {
        var qrCode = await _context.QRCodes.FindAsync(new object[] { id }, cancellationToken);
        if (qrCode != null)
        {
            qrCode.ScanCount++;
            qrCode.LastScannedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
