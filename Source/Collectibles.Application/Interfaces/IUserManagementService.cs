using Collectibles.Application.Common.Models;
using Collectibles.Application.Features.Users.Dtos;

namespace Collectibles.Application.Interfaces;

public interface IUserManagementService
{
    // User queries
    Task<PaginatedList<UserListDto>> GetUsersAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        string? roleFilter,
        bool? activeFilter,
        string? sortBy,
        bool sortDescending,
        CancellationToken cancellationToken = default);

    Task<UserDto> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<Result> UpdateUserProfileAsync(string? userId, string? firstName, string? lastName, string? profilePictureUrl);

    // User commands
    Task<string> CreateUserAsync(
        string email,
        string password,
        string? displayName,
        bool isActive,
        List<string> roles,
        CancellationToken cancellationToken = default);

    Task UpdateUserAsync(
        string userId,
        string email,
        string? displayName,
        string? profilePictureUrl,
        bool isActive,
        List<string> roles,
        bool syncToolEnabled,
        CancellationToken cancellationToken = default);

    Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default);

    Task ChangeUserPasswordAsync(
        string userId,
        string newPassword,
        bool requirePasswordChange,
        CancellationToken cancellationToken = default);

    Task LockUnlockUserAsync(
        string userId,
        bool isLocked,
        int? lockoutDays,
        CancellationToken cancellationToken = default);

    // Role queries
    Task<List<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);

    // Role commands
    Task<string> CreateRoleAsync(string roleName, CancellationToken cancellationToken = default);
    Task DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default);
}
