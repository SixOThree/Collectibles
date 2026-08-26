using System.Security.Claims;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Constants;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace Collectibles.Infrastructure.Services;

/// <summary>
/// Implementation of ICurrentUserService specifically for Blazor Server components.
/// Uses AuthenticationStateProvider instead of IHttpContextAccessor.
/// Falls back to HttpContext.User for API endpoints (non-Blazor HTTP requests).
/// </summary>
public class BlazorCurrentUserService : ICurrentUserService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private string? _cachedUserId;
    private string? _cachedUserName;
    private bool _cachedIsAdministrator;
    private List<string> _cachedRoles = new();
    private DateTime _cacheExpiry;
    private readonly TimeSpan _cacheTimeout = TimeSpan.FromSeconds(ApplicationConstants.Caching.UserCacheSeconds);

    public BlazorCurrentUserService(
        AuthenticationStateProvider authenticationStateProvider,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _httpContextAccessor = httpContextAccessor;
        _cacheExpiry = DateTime.MinValue;
    }

    public string? UserId
    {
        get
        {
            RefreshCacheIfNeeded();
            return _cachedUserId;
        }
    }

    public string? UserName
    {
        get
        {
            RefreshCacheIfNeeded();
            return _cachedUserName;
        }
    }

    public bool IsAdministrator
    {
        get
        {
            RefreshCacheIfNeeded();
            return _cachedIsAdministrator;
        }
    }

    public bool IsInRole(string role)
    {
        RefreshCacheIfNeeded();
        return _cachedRoles.Contains(role);
    }

    private void RefreshCacheIfNeeded()
    {
        if (DateTime.UtcNow < _cacheExpiry)
        {
            return;
        }

        try
        {
            // This is synchronous but internally AuthenticationStateProvider may have cached the state
            var authStateTask = _authenticationStateProvider.GetAuthenticationStateAsync();

            // We need to block here because the interface properties are not async
            // This should be fast since AuthenticationStateProvider typically caches the state
            if (authStateTask.IsCompleted)
            {
                // Task is already complete, we can get the result immediately
                var authState = authStateTask.Result;
                UpdateCache(authState);
            }
            else
            {
                // Task is not complete, we need to wait but with a timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                try
                {
                    var authState = authStateTask.GetAwaiter().GetResult();
                    UpdateCache(authState);
                }
                catch (OperationCanceledException)
                {
                    // Timeout - leave cached values as-is
                }
            }
        }
        catch (InvalidOperationException)
        {
            // This happens when called outside of a Blazor component context
            // (e.g., from API endpoints or background services)
            // Fall back to HttpContext.User for API key / cookie auth on HTTP requests
            TryGetFromHttpContext();
        }
        catch (Exception)
        {
            // Any other exception, fall back to HttpContext.User
            TryGetFromHttpContext();
        }
    }

    private void TryGetFromHttpContext()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            _cachedUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _cachedUserName = httpContext.User.Identity?.Name;
            _cachedIsAdministrator = httpContext.User.IsInRole(ApplicationConstants.Roles.Administrator);
            _cachedRoles = httpContext.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
        }
        else
        {
            _cachedUserId = null;
            _cachedUserName = null;
            _cachedIsAdministrator = false;
            _cachedRoles = new List<string>();
        }

        _cacheExpiry = DateTime.UtcNow.AddSeconds(5); // Shorter cache for non-Blazor context
    }

    private void UpdateCache(AuthenticationState? authState)
    {
        if (authState?.User != null)
        {
            _cachedUserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _cachedUserName = authState.User.Identity?.Name;
            _cachedIsAdministrator = authState.User.IsInRole(ApplicationConstants.Roles.Administrator);
            _cachedRoles = authState.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
        }
        else
        {
            _cachedUserId = null;
            _cachedUserName = null;
            _cachedIsAdministrator = false;
            _cachedRoles = new List<string>();
        }

        _cacheExpiry = DateTime.UtcNow.Add(_cacheTimeout);
    }
}
