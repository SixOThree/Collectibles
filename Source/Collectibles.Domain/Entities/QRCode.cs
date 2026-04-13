namespace Collectibles.Domain.Entities;

public class QRCode : BaseAuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public QRCodeStatus Status { get; set; } = QRCodeStatus.Unassigned;
    public long? CollectibleItemId { get; set; }
    public CollectibleItem? CollectibleItem { get; set; }
    public DateTime? AssignedDate { get; set; }
    public DateTime? RevokedDate { get; set; }
    public string? RevokedReason { get; set; }
    public int ScanCount { get; set; }
    public DateTime? LastScannedDate { get; set; }
}
