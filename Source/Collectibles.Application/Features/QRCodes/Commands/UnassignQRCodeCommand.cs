using Collectibles.Application.Interfaces;
using Collectibles.Application.Services;
using Collectibles.Domain.Common.Enums;
using Collectibles.Domain.Interfaces;
using MediatR;

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

        if (!collectibleItem.QRCodeId.HasValue)
        {
            return new UnassignQRCodeResult
            {
                Success = false,
                ErrorMessage = "This item does not have a QR code assigned",
            };
        }

        var qrCode = await _qrCodeRepository.GetByIdAsync(collectibleItem.QRCodeId.Value, cancellationToken);

        if (qrCode == null)
        {
            return new UnassignQRCodeResult
            {
                Success = false,
                ErrorMessage = "QR code not found",
            };
        }

        qrCode.CollectibleItemId = null;
        qrCode.Status = QRCodeStatus.Unassigned;
        qrCode.AssignedDate = null;
        qrCode.LastModified = DateTime.UtcNow;
        qrCode.LastModifiedBy = _currentUserService.UserId;

        await _qrCodeRepository.UpdateAsync(qrCode, cancellationToken);

        collectibleItem.QRCodeId = null;
        await _context.SaveChangesAsync(cancellationToken);

        return new UnassignQRCodeResult
        {
            Success = true,
        };
    }
}
