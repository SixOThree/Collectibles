using Collectibles.Application.Interfaces;
using Collectibles.Domain.Common.Enums;
using Collectibles.Domain.Interfaces;

using MediatR;

namespace Collectibles.Application.Features.QRCodes.Commands;

public class RevokeQRCodeCommand : IRequest<RevokeQRCodeResult>
{
    public long QRCodeId { get; set; }
    public string? Reason { get; set; }
}

public class RevokeQRCodeResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class RevokeQRCodeCommandHandler : IRequestHandler<RevokeQRCodeCommand, RevokeQRCodeResult>
{
    private readonly IQRCodeRepository _qrCodeRepository;
    private readonly ICurrentUserService _currentUserService;

    public RevokeQRCodeCommandHandler(
        IQRCodeRepository qrCodeRepository,
        ICurrentUserService currentUserService)
    {
        _qrCodeRepository = qrCodeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<RevokeQRCodeResult> Handle(RevokeQRCodeCommand request, CancellationToken cancellationToken)
    {
        var qrCode = await _qrCodeRepository.GetByIdAsync(request.QRCodeId, cancellationToken);

        if (qrCode == null)
        {
            return new RevokeQRCodeResult
            {
                Success = false,
                ErrorMessage = "QR code not found",
            };
        }

        // Check ownership
        if (qrCode.CreatedBy != _currentUserService.UserId)
        {
            return new RevokeQRCodeResult
            {
                Success = false,
                ErrorMessage = "You can only revoke your own QR codes",
            };
        }

        if (qrCode.Status == QRCodeStatus.Revoked)
        {
            return new RevokeQRCodeResult
            {
                Success = false,
                ErrorMessage = "QR code is already revoked",
            };
        }

        qrCode.Status = QRCodeStatus.Revoked;
        qrCode.RevokedDate = DateTime.UtcNow;
        qrCode.RevokedReason = request.Reason ?? "Manually revoked by user";
        qrCode.LastModified = DateTime.UtcNow;
        qrCode.LastModifiedBy = _currentUserService.UserId;

        await _qrCodeRepository.UpdateAsync(qrCode, cancellationToken);

        return new RevokeQRCodeResult
        {
            Success = true,
        };
    }
}
