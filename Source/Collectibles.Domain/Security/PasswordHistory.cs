namespace Collectibles.Domain.Security;

public class PasswordHistory : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
