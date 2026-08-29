using Collectibles.Application.Interfaces;
using Collectibles.Domain.Common.Enums;
using Collectibles.Domain.Interfaces;

using MediatR;

namespace Collectibles.Application.Features.QRCodes.Queries;

public class GetUnassignedQRCodesQuery : IRequest<List<QRCodeBriefDto>>
{
}

public class GetUnassignedQRCodesQueryHandler : IRequestHandler<GetUnassignedQRCodesQuery, List<QRCodeBriefDto>>
{
    private readonly IQRCodeRepository _qrCodeRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetUnassignedQRCodesQueryHandler(
        IQRCodeRepository qrCodeRepository,
        ICurrentUserService currentUserService)
    {
        _qrCodeRepository = qrCodeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<QRCodeBriefDto>> Handle(GetUnassignedQRCodesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return new List<QRCodeBriefDto>();
        }

        var qrCodes = await _qrCodeRepository.GetByUserAsync(userId, 1, 100, cancellationToken);

        var unassignedCodes = qrCodes
            .Where(q => q.Status == QRCodeStatus.Unassigned)
            .Select(q => new QRCodeBriefDto
            {
                Id = q.Id,
                Code = q.Code,
                Status = q.Status,
                Created = q.Created ?? DateTime.UtcNow,
            })
            .OrderByDescending(q => q.Created)
            .ToList();

        return unassignedCodes;
    }
}
