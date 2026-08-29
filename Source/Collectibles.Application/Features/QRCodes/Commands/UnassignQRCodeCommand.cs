using Collectibles.Application.Interfaces;
using Collectibles.Application.Services;
using Collectibles.Domain.Common.Enums;
using Collectibles.Domain.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.QRCodes.Commands;

public class UnassignQRCodeCommand : IRequest<UnassignQRCodeResult>
{
    public string CollectibleItemHashId { get; set; } = string.Empty;
}

public class UnassignQRCodeResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class UnassignQRCodeCommandHandler : IRequestHandler<UnassignQRCodeCommand, UnassignQRCodeResult>
{
    private readonly IQRCodeRepository _qrCodeRepository;
    private readonly IApplicationDbContext _context;
    private readonly IHashIdsService _hashIdsService;
    private readonly ICurrentUserService _currentUserService;

    public UnassignQRCodeCommandHandler(
        IQRCodeRepository qrCodeRepository,
        IApplicationDbContext context,
        IHashIdsService hashIdsService,
        ICurrentUserService currentUserService)
    {
        _qrCodeRepository = qrCodeRepository;
        _context = context;
        _hashIdsService = hashIdsService;
        _currentUserService = currentUserService;
    }

    public async Task<UnassignQRCodeResult> Handle(UnassignQRCodeCommand request, CancellationToken cancellationToken)
    {
        long collectibleItemId;
        try
        {
            collectibleItemId = _hashIdsService.Decode(request.CollectibleItemHashId);
        }
        catch
        {
            return new UnassignQRCodeResult
            {
                Success = false,
                ErrorMessage = "Invalid collectible item ID",
            };
        }

        var collectibleItem = await _context.CollectibleItems
            .FindAsync(new object[] { collectibleItemId }, cancellationToken);

        if (collectibleItem == null)
        {
            return new UnassignQRCodeResult
            {
                Success = false,
                ErrorMessage = "Collectible item not found",
            };
        }

        // AssignQRCodeCommand verifies item ownership; unassigning must too.
        var ownsItem = await _context.CollectibleItems
            .Where(ci => ci.Id == collectibleItemId)
            .SelectMany(ci => ci.Showcases)
            .AnyAsync(s => s.UserId == _currentUserService.UserId, cancellationToken);

        if (!ownsItem && !_currentUserService.IsAdministrator)
        {
            throw new UnauthorizedAccessException("You don't have permission to unassign the QR code for this item.");
        }

        var qrCode = await _context.QRCodes
            .FirstOrDefaultAsync(q => q.CollectibleItemId == collectibleItemId, cancellationToken);

        if (qrCode == null)
        {
            return new UnassignQRCodeResult
            {
                Success = false,
                ErrorMessage = "This item does not have a QR code assigned",
            };
        }

        qrCode.CollectibleItemId = null;
        qrCode.Status = QRCodeStatus.Unassigned;
        qrCode.AssignedDate = null;
        qrCode.LastModified = DateTime.UtcNow;
        qrCode.LastModifiedBy = _currentUserService.UserId;

        await _qrCodeRepository.UpdateAsync(qrCode, cancellationToken);

        return new UnassignQRCodeResult
        {
            Success = true,
        };
    }
}
