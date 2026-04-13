namespace Collectibles.Application.Features.Attachments.Dtos;

public class MigrationError
{
    public long AttachmentId { get; set; }
    public string AttachmentName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}
