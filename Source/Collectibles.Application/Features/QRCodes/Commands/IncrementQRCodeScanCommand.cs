using Collectibles.Domain.Interfaces;
using MediatR;

namespace Collectibles.Application.Features.QRCodes.Commands;

public class IncrementQRCodeScanCommand : IRequest<bool>
{
    public long QRCodeId { get; set; }
}

public class IncrementQRCodeScanCommandHandler : IRequestHandler<IncrementQRCodeScanCommand, bool>
{
    private readonly IQRCodeRepository _qrCodeRepository;

    public IncrementQRCodeScanCommandHandler(IQRCodeRepository qrCodeRepository)
    {
        _qrCodeRepository = qrCodeRepository;
    }

    public async Task<bool> Handle(IncrementQRCodeScanCommand request, CancellationToken cancellationToken)
    {
        await _qrCodeRepository.IncrementScanCountAsync(request.QRCodeId, cancellationToken);
        return true;
    }
}
