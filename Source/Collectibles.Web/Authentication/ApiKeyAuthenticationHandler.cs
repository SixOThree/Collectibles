using System.Security.Claims;
using System.Text.Encodings.Web;

using Collectibles.Application.Interfaces;
using Collectibles.Domain.Configuration;
using Collectibles.Infrastructure.Persistence;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Serilog;

namespace Collectibles.Web.Authentication;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiKey";
    private const string ApiKeyHeaderName = "X-Api-Key";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApiKeyService _apiKeyService;
    private readonly IOptionsMonitor<SyncToolSettings> _syncToolSettings;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        UserManager<ApplicationUser> userManager,
        IApiKeyService apiKeyService,
        IOptionsMonitor<SyncToolSettings> syncToolSettings)
        : base(options, logger, encoder)
    {
        _userManager = userManager;
        _apiKeyService = apiKeyService;
        _syncToolSettings = syncToolSettings;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeader))
        {
            return AuthenticateResult.NoResult();
        }

        var providedKey = apiKeyHeader.ToString();
        if (string.IsNullOrWhiteSpace(providedKey))
        {
            return AuthenticateResult.NoResult();
        }

        if (!_syncToolSettings.CurrentValue.Enabled)
        {
            Log.Warning("API key authentication attempted but sync tool is disabled globally");
            return AuthenticateResult.Fail("Sync tool is not enabled.");
        }

        var keyHash = _apiKeyService.HashKey(providedKey);
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.ApiKeyHash == keyHash);

        if (user == null)
        {
            Log.Warning("Invalid API key provided from {RemoteIp}", Context.Connection.RemoteIpAddress);
            return AuthenticateResult.Fail("Invalid API key.");
        }

        if (!user.IsActive)
        {
            Log.Warning("API key authentication attempted for inactive user {UserId}", user.Id);
            return AuthenticateResult.Fail("User account is inactive.");
        }

        if (!user.SyncToolEnabled)
        {
            Log.Warning("API key authentication attempted for user {UserId} without sync tool access", user.Id);
            return AuthenticateResult.Fail("Sync tool access is not enabled for this user.");
        }

        var displayName = user.DisplayName ?? user.Email ?? user.UserName ?? "API User";
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, displayName),
            new Claim(ClaimTypes.AuthenticationMethod, SchemeName),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        Log.Debug("API key authentication successful for user {UserId}", user.Id);

        return AuthenticateResult.Success(ticket);
    }
}
