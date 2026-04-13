using Collectibles.Application.Interfaces;
using Collectibles.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Collectibles.Infrastructure.Services;

public class CustomSignInManager : SignInManager<ApplicationUser>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<CustomSignInManager> _logger;
    private readonly IServiceProvider _serviceProvider;

    public CustomSignInManager(
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<CustomSignInManager> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<ApplicationUser> confirmation,
        IServiceProvider serviceProvider)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
        _userManager = userManager;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public override async Task<SignInResult> PasswordSignInAsync(string userName, string password, bool isPersistent, bool lockoutOnFailure)
    {
        var result = await base.PasswordSignInAsync(userName, password, isPersistent, lockoutOnFailure);

        if (result.Succeeded)
        {
            await UpdateLastLoginDateAsync(userName);
            await EnsureAdminRoleForSpecialUsers(userName);
            await LogAuthenticationEvent(userName, EventAction.Login, "Password sign-in successful");
        }
        else if (result.IsLockedOut)
        {
            await LogAuthenticationEvent(userName, EventAction.Login, "Account locked out");
        }
        else if (result.RequiresTwoFactor)
        {
            await LogAuthenticationEvent(userName, EventAction.Login, "Two-factor authentication required");
        }
        else if (result.IsNotAllowed)
        {
            await LogAuthenticationEvent(userName, EventAction.Login, "Sign-in not allowed");
        }
        else
        {
            await LogAuthenticationEvent(userName, EventAction.Login, "Password sign-in failed");
        }

        return result;
    }

    public override async Task<SignInResult> PasswordSignInAsync(ApplicationUser user, string password, bool isPersistent, bool lockoutOnFailure)
    {
        var result = await base.PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);

        if (result.Succeeded)
        {
            await UpdateLastLoginDateAsync(user);
            await EnsureAdminRoleForSpecialUsers(user.Email);
            await LogAuthenticationEvent(user.Email ?? user.UserName, EventAction.Login, "Password sign-in successful");
        }
        else if (result.IsLockedOut)
        {
            await LogAuthenticationEvent(user.Email ?? user.UserName, EventAction.Login, "Account locked out");
        }
        else if (result.RequiresTwoFactor)
        {
            await LogAuthenticationEvent(user.Email ?? user.UserName, EventAction.Login, "Two-factor authentication required");
        }
        else if (result.IsNotAllowed)
        {
            await LogAuthenticationEvent(user.Email ?? user.UserName, EventAction.Login, "Sign-in not allowed");
        }
        else
        {
            await LogAuthenticationEvent(user.Email ?? user.UserName, EventAction.Login, "Password sign-in failed");
        }

        return result;
    }

    public override async Task SignOutAsync()
    {
        var user = await UserManager.GetUserAsync(Context.User);
        if (user != null)
        {
            await LogAuthenticationEvent(user.Email ?? user.UserName, EventAction.Logout, "User signed out");
        }

        await base.SignOutAsync();
    }

    public override async Task<SignInResult> TwoFactorAuthenticatorSignInAsync(string code, bool isPersistent, bool rememberClient)
    {
        var result = await base.TwoFactorAuthenticatorSignInAsync(code, isPersistent, rememberClient);
        var user = await GetTwoFactorAuthenticationUserAsync();

        if (user != null)
        {
            if (result.Succeeded)
            {
                await UpdateLastLoginDateAsync(user);
                await LogAuthenticationEvent(user.Email ?? user.UserName, EventAction.Login, "Two-factor authentication successful");
            }
            else
            {
                await LogAuthenticationEvent(user.Email ?? user.UserName, EventAction.Login, "Two-factor authentication failed");
            }
        }

        return result;
    }

    public override async Task<SignInResult> TwoFactorRecoveryCodeSignInAsync(string recoveryCode)
    {
        var result = await base.TwoFactorRecoveryCodeSignInAsync(recoveryCode);
        var user = await GetTwoFactorAuthenticationUserAsync();

        if (user != null)
        {
            if (result.Succeeded)
            {
                await UpdateLastLoginDateAsync(user);
                await LogAuthenticationEvent(user.Email ?? user.UserName, EventAction.Login, "Recovery code sign-in successful");
            }
            else
            {
                await LogAuthenticationEvent(user.Email ?? user.UserName, EventAction.Login, "Recovery code sign-in failed");
            }
        }

        return result;
    }

    public override async Task<SignInResult> ExternalLoginSignInAsync(string loginProvider, string providerKey, bool isPersistent, bool bypassTwoFactor)
    {
        var result = await base.ExternalLoginSignInAsync(loginProvider, providerKey, isPersistent, bypassTwoFactor);

        if (result.Succeeded)
        {
            var user = await UserManager.FindByLoginAsync(loginProvider, providerKey);
            if (user != null)
            {
                await UpdateLastLoginDateAsync(user);
                await LogAuthenticationEvent(user.Email ?? user.UserName, EventAction.Login, $"External login successful via {loginProvider}");
            }
        }

        return result;
    }

    private async Task UpdateLastLoginDateAsync(ApplicationUser user)
    {
        user.LastLoginDate = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
    }

    private async Task UpdateLastLoginDateAsync(string? userName)
    {
        if (string.IsNullOrEmpty(userName))
        {
            return;
        }

        var user = await _userManager.FindByNameAsync(userName);
        if (user != null)
        {
            await UpdateLastLoginDateAsync(user);
        }
    }

    private static async Task EnsureAdminRoleForSpecialUsers(string? userEmail)
    {
        // This method is kept for backward compatibility but no longer auto-assigns admin roles
        // Admin accounts should be created through the /Setup page on first run
        await Task.CompletedTask;
    }

    private async Task LogAuthenticationEvent(string? userIdentifier, EventAction action, string additionalData)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var eventLogService = scope.ServiceProvider.GetRequiredService<IEventLogService>();
            var sysLogService = scope.ServiceProvider.GetRequiredService<ISysLogService>();
            var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

            // Log to EventLog for user activity tracking
            await eventLogService.LogUserActivityAsync(action, additionalData);

            // Also log to SysLog for system-level monitoring
            var properties = new Dictionary<string, object>
            {
                ["UserIdentifier"] = userIdentifier ?? "Unknown",
                ["Action"] = action.ToString(),
                ["IPAddress"] = GetIPAddress(httpContextAccessor.HttpContext) ?? "Unknown",
                ["UserAgent"] = httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown",
            };

            var logLevel = action switch
            {
                EventAction.Login when additionalData.Contains("failed") => Collectibles.Domain.Entities.LogLevel.Warning,
                EventAction.Login when additionalData.Contains("locked") => Collectibles.Domain.Entities.LogLevel.Warning,
                _ => Collectibles.Domain.Entities.LogLevel.Information,
            };

            switch (logLevel)
            {
                case Collectibles.Domain.Entities.LogLevel.Warning:
                    await sysLogService.LogWarningAsync(
                        $"Authentication event: {action} for {userIdentifier} - {additionalData}",
                        "Security.Authentication",
                        properties);
                    break;
                default:
                    await sysLogService.LogInformationAsync(
                        $"Authentication event: {action} for {userIdentifier} - {additionalData}",
                        "Security.Authentication",
                        properties);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log authentication event for user {UserIdentifier}", userIdentifier);
        }
    }

    private static string? GetIPAddress(HttpContext? httpContext)
    {
        if (httpContext == null)
        {
            return null;
        }

        var ipAddress = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(ipAddress))
        {
            var addresses = ipAddress.Split(',');
            if (addresses.Length > 0)
            {
                return addresses[0].Trim();
            }
        }

        ipAddress = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(ipAddress))
        {
            return ipAddress;
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }
}
