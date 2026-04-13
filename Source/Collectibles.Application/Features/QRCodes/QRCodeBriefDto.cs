using Collectibles.Domain.Common.Enums;

namespace Collectibles.Application.Features.QRCodes;

public class QRCodeBriefDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public QRCodeStatus Status { get; set; }
    public string? CollectibleItemName { get; set; }
    public DateTime Created { get; set; }
}
