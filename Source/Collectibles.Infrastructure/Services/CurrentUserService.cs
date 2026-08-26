using System.Security.Claims;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Constants;
using Microsoft.AspNetCore.Http;

namespace Collectibles.Infrastructure.Services;

/// <summary>
/// Implementation of ICurrentUserService that uses IHttpContextAccessor
/// for non-Blazor contexts like middleware, background services, and MVC actions.
/// For Blazor Server components, use AuthenticationUserService instead.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId
    {
        get
        {
            // Gracefully handle null HttpContext (common in Blazor Server after SignalR connection)
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
            {
                return null;
            }

            return httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }

    public string? UserName
    {
        get
        {
            // Gracefully handle null HttpContext (common in Blazor Server after SignalR connection)
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
            {
                return null;
            }

            return httpContext.User?.Identity?.Name;
        }
    }

    public bool IsAdministrator
    {
        get
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            return httpContext?.User?.IsInRole(ApplicationConstants.Roles.Administrator) ?? false;
        }
    }

    public bool IsInRole(string role)
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        return httpContext?.User?.IsInRole(role) ?? false;
    }
}
