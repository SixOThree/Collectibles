using System.Security.Claims;

using Collectibles.Application.Interfaces;
using Collectibles.Domain.Constants;

namespace Collectibles.Web.Services;

public class HttpContextDataService : IHttpContextDataService
{
    private bool _isInitialized;

    public string? UserAgent { get; private set; }
    public string? ClientIpAddress { get; private set; }
    public string? RequestPath { get; private set; }
    public string? QueryString { get; private set; }
    public string? Host { get; private set; }
    public string? Scheme { get; private set; }
    public Dictionary<string, string> Headers { get; private set; } = new();
    public ClaimsPrincipal? User { get; private set; }
    public bool IsAuthenticated { get; private set; }
    public bool IsAdministrator { get; private set; }
    public string? UserId { get; private set; }
    public string? UserName { get; private set; }
    public List<string> UserRoles { get; private set; } = new();

    public bool IsInitialized => _isInitialized;

    public void CaptureHttpContext(HttpContext httpContext)
    {
        if (_isInitialized || httpContext == null)
        {
            return;
        }

        // Capture request data
        UserAgent = httpContext.Request.Headers["User-Agent"].ToString();
        ClientIpAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        RequestPath = httpContext.Request.Path;
        QueryString = httpContext.Request.QueryString.ToString();
        Host = httpContext.Request.Host.ToString();
        Scheme = httpContext.Request.Scheme;

        // Capture important headers
        foreach (var header in httpContext.Request.Headers)
        {
            if (header.Key.StartsWith("X-") ||
                header.Key.Equals("Accept-Language", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Referer", StringComparison.OrdinalIgnoreCase))
            {
                Headers[header.Key] = header.Value.ToString();
            }
        }

        // Capture user information
        User = httpContext.User;
        IsAuthenticated = httpContext.User?.Identity?.IsAuthenticated ?? false;

        if (IsAuthenticated && httpContext.User != null)
        {
            UserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            UserName = httpContext.User.Identity?.Name;
            UserRoles = httpContext.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
            IsAdministrator = httpContext.User.IsInRole(ApplicationConstants.Roles.Administrator);
        }

        _isInitialized = true;
    }

    public void Reset()
    {
        _isInitialized = false;
        UserAgent = null;
        ClientIpAddress = null;
        RequestPath = null;
        QueryString = null;
        Host = null;
        Scheme = null;
        Headers.Clear();
        User = null;
        IsAuthenticated = false;
        IsAdministrator = false;
        UserId = null;
        UserName = null;
        UserRoles.Clear();
    }
}
