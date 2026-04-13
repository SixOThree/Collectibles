using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Interfaces;
using MediatR;

namespace Collectibles.Application.Features.QRCodes.Commands;

public class GenerateQRCodesCommand : IRequest<GenerateQRCodesResult>
{
    public int Quantity { get; set; }
    public string? UserId { get; set; } // Optional UserId to handle Blazor context issues
}

public class GenerateQRCodesResult
{
    public bool Success { get; set; }
    public List<QRCodeDto> GeneratedCodes { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class GenerateQRCodesCommandHandler : IRequestHandler<GenerateQRCodesCommand, GenerateQRCodesResult>
{
    private readonly IQRCodeRepository _qrCodeRepository;
    private readonly ICurrentUserService _currentUserService;

    public GenerateQRCodesCommandHandler(
        IQRCodeRepository qrCodeRepository,
        ICurrentUserService currentUserService)
    {
        _qrCodeRepository = qrCodeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<GenerateQRCodesResult> Handle(GenerateQRCodesCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0 || request.Quantity > 100)
        {
            return new GenerateQRCodesResult
            {
                Success = false,
                ErrorMessage = "Quantity must be between 1 and 100",
            };
        }

        var qrCodes = new List<QRCode>();
        var generatedCodes = new HashSet<string>();

        for (int i = 0; i < request.Quantity; i++)
        {
            string code;
            int attempts = 0;

            do
            {
                code = GenerateUniqueCode();
                attempts++;

                if (attempts > 10)
                {
                    return new GenerateQRCodesResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to generate unique codes. Please try again.",
                    };
                }
            }
            while (generatedCodes.Contains(code) || await _qrCodeRepository.CodeExistsAsync(code, cancellationToken));

            generatedCodes.Add(code);

            // Use the provided UserId if available, otherwise fall back to CurrentUserService
            var userId = request.UserId ?? _currentUserService.UserId;

            if (string.IsNullOrEmpty(userId))
            {
                return new GenerateQRCodesResult
                {
                    Success = false,
                    ErrorMessage = "User context not available. Please ensure you are logged in.",
                };
            }

            var qrCode = new QRCode
            {
                Code = code,
                Status = Domain.Common.Enums.QRCodeStatus.Unassigned,

                // Explicitly set audit fields when UserId is provided
                CreatedBy = userId,
                Created = DateTime.UtcNow,
            };

            qrCodes.Add(qrCode);
        }

        var savedCodes = await _qrCodeRepository.AddRangeAsync(qrCodes, cancellationToken);

        var result = new GenerateQRCodesResult
        {
            Success = true,
            GeneratedCodes = savedCodes.Select(q => new QRCodeDto
            {
                Id = q.Id,
                Code = q.Code,
                Status = q.Status,
                Created = q.Created ?? DateTime.UtcNow,
                CreatedBy = q.CreatedBy,
            }).ToList(),
        };

        return result;
    }

    private static string GenerateUniqueCode()
    {
        var guid = Guid.NewGuid().ToString("N");
        return $"QR-{guid.Substring(0, 8).ToUpper(System.Globalization.CultureInfo.CurrentCulture)}-{guid.Substring(8, 4).ToUpper(System.Globalization.CultureInfo.CurrentCulture)}";
    }
}
