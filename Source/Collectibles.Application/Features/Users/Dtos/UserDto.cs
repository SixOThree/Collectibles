namespace Collectibles.Application.Features.Users.Dtos;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool IsLockedOut { get; set; }
    public int AccessFailedCount { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool SyncToolEnabled { get; set; }
}
