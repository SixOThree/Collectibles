using Microsoft.AspNetCore.Identity;

namespace Collectibles.Infrastructure.Persistence;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
    public bool SyncToolEnabled { get; set; }
    public string? ApiKeyHash { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
