using Collectibles.Application.Common.Models;
using Collectibles.Application.Features.Users.Dtos;
using Collectibles.Application.Interfaces;
using Collectibles.Infrastructure.Mappings.Explicit;
using Collectibles.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Infrastructure.Services;

public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _dbContext;
    private static readonly string[] Errors = new[] { "User ID cannot be null." };

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    public async Task<PaginatedList<UserListDto>> GetUsersAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        string? roleFilter,
        bool? activeFilter,
        string? sortBy,
        bool sortDescending,
        CancellationToken cancellationToken = default)
    {
        var query = _userManager.Users.AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchTermLower = searchTerm.ToLower(System.Globalization.CultureInfo.CurrentCulture);
            query = query.Where(u =>
                u.UserName!.Contains(searchTermLower, StringComparison.CurrentCultureIgnoreCase) ||
                u.Email!.Contains(searchTermLower, StringComparison.CurrentCultureIgnoreCase) ||
                (u.FirstName != null && u.FirstName.Contains(searchTermLower, StringComparison.CurrentCultureIgnoreCase)) ||
                (u.LastName != null && u.LastName.Contains(searchTermLower, StringComparison.CurrentCultureIgnoreCase)));
        }

        // Apply active filter
        if (activeFilter.HasValue)
        {
            query = query.Where(u => u.IsActive == activeFilter.Value);
        }

        // Apply sorting
        query = sortBy?.ToLower(System.Globalization.CultureInfo.CurrentCulture) switch
        {
            "username" => sortDescending
                ? query.OrderByDescending(u => u.UserName)
                : query.OrderBy(u => u.UserName),
            "email" => sortDescending
                ? query.OrderByDescending(u => u.Email)
                : query.OrderBy(u => u.Email),
            "fullname" => sortDescending
                ? query.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName)
                : query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName),
            "lastlogin" => sortDescending
                ? query.OrderByDescending(u => u.LastLoginDate)
                : query.OrderBy(u => u.LastLoginDate),
            "created" => sortDescending
                ? query.OrderByDescending(u => u.CreatedDate)
                : query.OrderBy(u => u.CreatedDate),
            _ => sortDescending
                ? query.OrderByDescending(u => u.UserName)
                : query.OrderBy(u => u.UserName),
        };

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and fetch users
        var users = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Map to DTOs
        var userDtos = users.Select(u => u.ToUserListDto()).ToList();

        // Create paginated list
        var paginatedList = new PaginatedList<UserListDto>(userDtos, totalCount, pageNumber, pageSize);

        // Load roles for each user (has to be done separately due to Identity design)
        foreach (var userDto in paginatedList.Items)
        {
            var user = await _userManager.FindByIdAsync(userDto.Id);
            if (user != null)
            {
                userDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
            }
        }

        // Apply role filter after loading roles
        if (!string.IsNullOrWhiteSpace(roleFilter))
        {
            var filteredItems = paginatedList.Items
                .Where(u => u.Roles.Contains(roleFilter))
                .ToList();

            // Create a new PaginatedList with filtered items
            return new PaginatedList<UserListDto>(filteredItems, filteredItems.Count, pageNumber, pageSize);
        }

        return paginatedList;
    }

    public async Task<UserDto> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }

        var userDto = user.ToUserDto();

        // Load roles
        userDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();

        return userDto;
    }

    public async Task<Result> UpdateUserProfileAsync(string? userId, string? firstName, string? lastName, string? profilePictureUrl)
    {
        if (userId is null)
        {
            return Result.Failure(Errors);
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return Result.Failure(new[] { $"User with ID {userId} not found." });
        }

        user.FirstName = firstName;
        user.LastName = lastName;

        if (profilePictureUrl is not null)
        {
            user.ProfilePictureUrl = profilePictureUrl;
        }

        user.ModifiedDate = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        return result.Succeeded
            ? Result.Success()
            : Result.Failure(result.Errors.Select(e => e.Description));
    }

    public async Task<string> CreateUserAsync(
        string email,
        string password,
        string? displayName,
        bool isActive,
        List<string> roles,
        CancellationToken cancellationToken = default)
    {
        // Check if user already exists by email (email will be used as username)
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            throw new ValidationException("Email already exists.");
        }

        // Also check if email is already used as a username
        existingUser = await _userManager.FindByNameAsync(email);
        if (existingUser != null)
        {
            throw new ValidationException("Email is already in use.");
        }

        // Create new user - use email as username
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            IsActive = isActive,
            CreatedDate = DateTime.UtcNow,
            EmailConfirmed = true, // Set to true for admin-created users
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ValidationException($"Failed to create user: {errors}");
        }

        // Assign roles if provided
        if (roles.Count != 0)
        {
            foreach (var roleName in roles)
            {
                if (await _roleManager.RoleExistsAsync(roleName))
                {
                    await _userManager.AddToRoleAsync(user, roleName);
                }
            }
        }

        return user.Id;
    }

    public async Task UpdateUserAsync(
        string userId,
        string email,
        string? displayName,
        string? profilePictureUrl,
        bool isActive,
        List<string> roles,
        bool syncToolEnabled,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }

        // Check if email is being changed and if it's already taken
        if (user.Email != email)
        {
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null && existingUser.Id != userId)
            {
                throw new ValidationException("Email already exists.");
            }

            // Also check if the email is already used as a username
            var existingUserByName = await _userManager.FindByNameAsync(email);
            if (existingUserByName != null && existingUserByName.Id != userId)
            {
                throw new ValidationException("Email is already in use as a username.");
            }
        }

        // Update user properties - username automatically syncs with email
        user.UserName = email;
        user.Email = email;
        user.DisplayName = displayName;
        user.ProfilePictureUrl = profilePictureUrl;
        user.IsActive = isActive;
        user.SyncToolEnabled = syncToolEnabled;
        user.ModifiedDate = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ValidationException($"Failed to update user: {errors}");
        }

        // Update roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        var rolesToRemove = currentRoles.Except(roles).ToList();
        var rolesToAdd = roles.Except(currentRoles).ToList();

        if (rolesToRemove.Count != 0)
        {
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
        }

        if (rolesToAdd.Count != 0)
        {
            foreach (var roleName in rolesToAdd)
            {
                if (await _roleManager.RoleExistsAsync(roleName))
                {
                    await _userManager.AddToRoleAsync(user, roleName);
                }
            }
        }
    }

    public async Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        // In Blazor Server, the scoped Identity store can keep a stale tracked user
        // alive across UI interactions. Detaching forces a fresh read with the latest
        // concurrency stamp before DeleteAsync attaches and removes the entity.
        DetachTrackedUser(userId);

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ValidationException($"Failed to delete user: {errors}");
        }
    }

    private void DetachTrackedUser(string userId)
    {
        var trackedUserEntry = _dbContext.ChangeTracker.Entries<ApplicationUser>()
            .FirstOrDefault(entry => entry.Entity.Id == userId);

        if (trackedUserEntry is not null)
        {
            trackedUserEntry.State = EntityState.Detached;
        }
    }

    public async Task ChangeUserPasswordAsync(
        string userId,
        string newPassword,
        bool requirePasswordChange,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }

        // Remove current password and set new one
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ValidationException($"Failed to change password: {errors}");
        }

        // Update security stamp to invalidate existing tokens
        await _userManager.UpdateSecurityStampAsync(user);

        // If require password change is set, we could set a custom claim or flag
        // This would need to be checked on login to force password change
        if (requirePasswordChange)
        {
            // This could be implemented with a custom claim or user property
            // For now, we'll just update the modified date
            user.ModifiedDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
        }
    }

    public async Task LockUnlockUserAsync(
        string userId,
        bool isLocked,
        int? lockoutDays,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }

        if (isLocked)
        {
            // Lock the user account
            var lockoutEnd = lockoutDays.HasValue
                ? DateTimeOffset.UtcNow.AddDays(lockoutDays.Value)
                : DateTimeOffset.MaxValue; // Permanent lockout

            await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);
            await _userManager.SetLockoutEnabledAsync(user, true);

            // Also deactivate the user
            user.IsActive = false;
            user.ModifiedDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
        }
        else
        {
            // Unlock the user account
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            // Reactivate the user
            user.IsActive = true;
            user.ModifiedDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
        }
    }

    public async Task<List<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleManager.Roles
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        var roleDtos = new List<RoleDto>();

        foreach (var role in roles)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);

            roleDtos.Add(new RoleDto
            {
                Id = role.Id,
                Name = role.Name!,
                UserCount = usersInRole.Count,
            });
        }

        return roleDtos;
    }

    public async Task<string> CreateRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var roleExists = await _roleManager.RoleExistsAsync(roleName);
        if (roleExists)
        {
            throw new ValidationException($"Role '{roleName}' already exists.");
        }

        var role = new IdentityRole(roleName);
        var result = await _roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ValidationException($"Failed to create role: {errors}");
        }

        return role.Id;
    }

    public async Task DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId);

        if (role == null)
        {
            throw new KeyNotFoundException($"Role with ID {roleId} not found.");
        }

        // Check if any users are assigned to this role
        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
        if (usersInRole.Any())
        {
            throw new ValidationException($"Cannot delete role '{role.Name}' because {usersInRole.Count} user(s) are assigned to it.");
        }

        var result = await _roleManager.DeleteAsync(role);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ValidationException($"Failed to delete role: {errors}");
        }
    }
}
