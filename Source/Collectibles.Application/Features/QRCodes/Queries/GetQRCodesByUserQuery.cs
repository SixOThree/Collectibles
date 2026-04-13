using Collectibles.Application.Interfaces;
using Collectibles.Application.Services;
using Collectibles.Domain.Interfaces;
using MediatR;

namespace Collectibles.Application.Features.QRCodes.Queries;

public class GetQRCodesByUserQuery : IRequest<List<QRCodeDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 100;
}

public class GetQRCodesByUserQueryHandler : IRequestHandler<GetQRCodesByUserQuery, List<QRCodeDto>>
{
    private readonly IQRCodeRepository _qrCodeRepository;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHashIdsService _hashIdsService;

    public GetQRCodesByUserQueryHandler(
        IQRCodeRepository qrCodeRepository,
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IHashIdsService hashIdsService)
    {
        _qrCodeRepository = qrCodeRepository;
        _context = context;
        _currentUserService = currentUserService;
        _hashIdsService = hashIdsService;
    }

    public async Task<List<QRCodeDto>> Handle(GetQRCodesByUserQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return new List<QRCodeDto>();
        }

        var qrCodes = await _qrCodeRepository.GetByUserAsync(userId, request.PageNumber, request.PageSize, cancellationToken);

        var dtos = new List<QRCodeDto>();

        foreach (var qrCode in qrCodes)
        {
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

            if (qrCode.CollectibleItemId.HasValue && qrCode.CollectibleItem != null)
            {
                dto.CollectibleItemName = qrCode.CollectibleItem.Name;
                dto.CollectibleItemHashId = _hashIdsService.Encode(qrCode.CollectibleItemId.Value);
            }

            dtos.Add(dto);
        }

        return dtos;
    }
}
