using Collectibles.Domain.Common.Enums;

namespace Collectibles.Application.Features.QRCodes;

public class QRCodeDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public QRCodeStatus Status { get; set; }
    public long? CollectibleItemId { get; set; }
    public string? CollectibleItemName { get; set; }
    public string? CollectibleItemHashId { get; set; }
    public DateTime? AssignedDate { get; set; }
    public DateTime? RevokedDate { get; set; }
    public string? RevokedReason { get; set; }
    public int ScanCount { get; set; }
    public DateTime? LastScannedDate { get; set; }
    public DateTime Created { get; set; }
    public string? CreatedBy { get; set; }
}
