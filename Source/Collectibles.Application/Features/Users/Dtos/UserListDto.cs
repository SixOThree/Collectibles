namespace Collectibles.Application.Features.Users.Dtos;

public class UserListDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsLockedOut { get; set; }
    public List<string> Roles { get; set; } = new();
}
