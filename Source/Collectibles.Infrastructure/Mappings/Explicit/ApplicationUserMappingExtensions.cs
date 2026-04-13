using Collectibles.Application.Features.Users.Dtos;
using Collectibles.Infrastructure.Persistence;

namespace Collectibles.Infrastructure.Mappings.Explicit;

/// <summary>
/// Extension methods for mapping ApplicationUser to DTOs.
/// </summary>
public static class ApplicationUserMappingExtensions
{
    /// <summary>
    /// Maps an ApplicationUser entity to UserDto.
    /// </summary>
    /// <returns></returns>
    public static UserDto ToUserDto(this ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            ProfilePictureUrl = user.ProfilePictureUrl,
            IsActive = true, // Can be determined based on business logic
            LastLoginDate = user.LastLoginDate,
            CreatedDate = DateTime.MinValue, // Set separately if needed
            ModifiedDate = null, // Set separately if needed
            CreatedBy = null, // Set separately if needed
            ModifiedBy = null, // Set separately if needed
            EmailConfirmed = user.EmailConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            IsLockedOut = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow,
            AccessFailedCount = user.AccessFailedCount,
            Roles = new List<string>(), // Set separately if needed
            SyncToolEnabled = user.SyncToolEnabled,
        };
    }

    /// <summary>
    /// Maps an ApplicationUser entity to UserListDto.
    /// </summary>
    /// <returns></returns>
    public static UserListDto ToUserListDto(this ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserListDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            DisplayName = user.DisplayName,
            ProfilePictureUrl = user.ProfilePictureUrl,
            IsActive = true, // Can be determined based on business logic
            LastLoginDate = user.LastLoginDate,
            EmailConfirmed = user.EmailConfirmed,
            IsLockedOut = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow,
            Roles = new List<string>(), // Set separately if needed
        };
    }
}
