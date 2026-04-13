using Collectibles.Application.Features.Users.Dtos;

namespace Collectibles.Application.Mappings.Explicit;

/// <summary>
/// Extension methods for mapping User DTOs.
/// </summary>
public static class UserMappingExtensions
{
    /// <summary>
    /// Maps a UserDto to UserProfileDto.
    /// </summary>
    /// <returns></returns>
    public static UserProfileDto ToProfileDto(this UserDto user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserProfileDto
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            ProfilePictureUrl = user.ProfilePictureUrl,
        };
    }
}
