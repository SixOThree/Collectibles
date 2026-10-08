using Collectibles.Application.Interfaces;
using Collectibles.Application.Services;
using Collectibles.Domain.Common.Enums;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.QRCodes.Commands;

public class AssignQRCodeCommand : IRequest<AssignQRCodeResult>
{
    public string QRCode { get; set; } = string.Empty;
    public string CollectibleItemHashId { get; set; } = string.Empty;
}

public class AssignQRCodeResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public QRCodeDto? QRCode { get; set; }
}

public class AssignQRCodeCommandHandler : IRequestHandler<AssignQRCodeCommand, AssignQRCodeResult>
{
    private readonly IQRCodeRepository _qrCodeRepository;
    private readonly IApplicationDbContext _context;
    private readonly IHashIdsService _hashIdsService;
    private readonly ICurrentUserService _currentUserService;

    public AssignQRCodeCommandHandler(
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

    public async Task<AssignQRCodeResult> Handle(AssignQRCodeCommand request, CancellationToken cancellationToken)
    {
        // Extract the QR code from URL if it's a full URL
        var qrCodeValue = request.QRCode;
        if (qrCodeValue.Contains("/qr/"))
        {
            var lastSlashIndex = qrCodeValue.LastIndexOf("/qr/");
            qrCodeValue = qrCodeValue.Substring(lastSlashIndex + 4); // Extract everything after "/qr/"
        }

        var qrCode = await _qrCodeRepository.GetByCodeAsync(qrCodeValue, cancellationToken);

        if (qrCode == null)
        {
            // Create a new QR code on-the-fly if it doesn't exist
            qrCode = new QRCode
            {
                Code = qrCodeValue, // Use the extracted code value, not the full URL
                Status = QRCodeStatus.Unassigned,
                CreatedBy = _currentUserService.UserId,
                Created = DateTime.UtcNow,
            };

            try
            {
                await _qrCodeRepository.AddAsync(qrCode, cancellationToken);
            }
            catch (Exception ex)
            {
                return new AssignQRCodeResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to create QR code: {ex.Message}",
                };
            }
        }

        if (qrCode.Status == QRCodeStatus.Assigned)
        {
            return new AssignQRCodeResult
            {
                Success = false,
                ErrorMessage = "QR code is already assigned to another item",
            };
        }

        if (qrCode.Status == QRCodeStatus.Revoked)
        {
            return new AssignQRCodeResult
            {
                Success = false,
                ErrorMessage = "QR code has been revoked and cannot be assigned",
            };
        }

        long collectibleItemId;
        try
        {
            collectibleItemId = _hashIdsService.Decode(request.CollectibleItemHashId);
        }
        catch
        {
            return new AssignQRCodeResult
            {
                Success = false,
                ErrorMessage = "Invalid collectible item ID",
            };
        }

        var collectibleItem = await _context.CollectibleItems
            .FindAsync(new object[] { collectibleItemId }, cancellationToken);

        if (collectibleItem == null)
        {
            return new AssignQRCodeResult
            {
                Success = false,
                ErrorMessage = "Collectible item not found",
            };
        }

        // Verify current user owns the item through its showcases
        var ownsItem = await _context.CollectibleItems
            .Where(ci => ci.Id == collectibleItemId)
            .SelectMany(ci => ci.Showcases)
            .AnyAsync(s => s.UserId == _currentUserService.UserId, cancellationToken);

        if (!ownsItem)
        {
            return new AssignQRCodeResult
            {
                Success = false,
                ErrorMessage = "You are not authorized to assign a QR code to this item.",
            };
        }

        var alreadyAssigned = await _context.QRCodes
            .AnyAsync(q => q.CollectibleItemId == collectibleItemId, cancellationToken);

        if (alreadyAssigned)
        {
            return new AssignQRCodeResult
            {
                Success = false,
                ErrorMessage = "This item already has a QR code assigned",
            };
        }

        // One relationship, one write. Previously both sides were written through two
        // separate saves, so a failure between them left them disagreeing.
        qrCode.CollectibleItemId = collectibleItemId;
        qrCode.Status = QRCodeStatus.Assigned;
        qrCode.AssignedDate = DateTime.UtcNow;
        qrCode.LastModified = DateTime.UtcNow;
        qrCode.LastModifiedBy = _currentUserService.UserId;

        await _qrCodeRepository.UpdateAsync(qrCode, cancellationToken);

        return new AssignQRCodeResult
        {
            Success = true,
            QRCode = new QRCodeDto
            {
                Id = qrCode.Id,
                Code = qrCode.Code,
                Status = qrCode.Status,
                CollectibleItemId = qrCode.CollectibleItemId,
                CollectibleItemName = collectibleItem.Name,
                CollectibleItemHashId = request.CollectibleItemHashId,
                AssignedDate = qrCode.AssignedDate,
                Created = qrCode.Created ?? DateTime.UtcNow,
                CreatedBy = qrCode.CreatedBy,
            },
        };
    }
}
