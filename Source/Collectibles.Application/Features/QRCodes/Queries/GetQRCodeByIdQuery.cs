using Collectibles.Application.Interfaces;
using Collectibles.Application.Services;
using Collectibles.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.QRCodes.Queries;

public class GetQRCodeByIdQuery : IRequest<QRCodeDto?>
{
    public long Id { get; set; }
}

public class GetQRCodeByIdQueryHandler : IRequestHandler<GetQRCodeByIdQuery, QRCodeDto?>
{
    private readonly IQRCodeRepository _qrCodeRepository;
    private readonly IApplicationDbContext _context;
    private readonly IHashIdsService _hashIdsService;
    private readonly ICurrentUserService _currentUserService;

    public GetQRCodeByIdQueryHandler(
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

    public async Task<QRCodeDto?> Handle(GetQRCodeByIdQuery request, CancellationToken cancellationToken)
    {
        var qrCode = await _qrCodeRepository.GetByIdAsync(request.Id, cancellationToken);

        if (qrCode == null)
        {
            return null;
        }

        // Verify the current user created this QR code
        if (qrCode.CreatedBy != _currentUserService.UserId)
        {
            return null;
        }

        var dto = new QRCodeDto
        {
            Id = qrCode.Id,
            Code = qrCode.Code,
            Status = qrCode.Status,
            CollectibleItemId = qrCode.CollectibleItemId,
            AssignedDate = qrCode.AssignedDate,
            RevokedDate = qrCode.RevokedDate,
            RevokedReason = qrCode.RevokedReason,
            ScanCount = qrCode.ScanCount,
            LastScannedDate = qrCode.LastScannedDate,
            Created = qrCode.Created ?? DateTime.UtcNow,
            CreatedBy = qrCode.CreatedBy,
        };

        if (qrCode.CollectibleItemId.HasValue)
        {
            var item = await _context.CollectibleItems
                .Where(i => i.Id == qrCode.CollectibleItemId.Value)
                .Select(i => new { i.Name })
                .FirstOrDefaultAsync(cancellationToken);

            if (item != null)
            {
                dto.CollectibleItemName = item.Name;
                dto.CollectibleItemHashId = _hashIdsService.Encode(qrCode.CollectibleItemId.Value);
            }
        }

        return dto;
    }
}
