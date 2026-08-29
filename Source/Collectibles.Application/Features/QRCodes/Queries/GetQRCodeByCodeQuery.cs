using Collectibles.Application.Interfaces;
using Collectibles.Application.Services;
using Collectibles.Domain.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.QRCodes.Queries;

public class GetQRCodeByCodeQuery : IRequest<QRCodeDto?>
{
    public string Code { get; set; } = string.Empty;
}

public class GetQRCodeByCodeQueryHandler : IRequestHandler<GetQRCodeByCodeQuery, QRCodeDto?>
{
    private readonly IQRCodeRepository _qrCodeRepository;
    private readonly IApplicationDbContext _context;
    private readonly IHashIdsService _hashIdsService;

    public GetQRCodeByCodeQueryHandler(
        IQRCodeRepository qrCodeRepository,
        IApplicationDbContext context,
        IHashIdsService hashIdsService)
    {
        _qrCodeRepository = qrCodeRepository;
        _context = context;
        _hashIdsService = hashIdsService;
    }

    public async Task<QRCodeDto?> Handle(GetQRCodeByCodeQuery request, CancellationToken cancellationToken)
    {
        var qrCode = await _qrCodeRepository.GetByCodeAsync(request.Code, cancellationToken);

        if (qrCode == null)
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
