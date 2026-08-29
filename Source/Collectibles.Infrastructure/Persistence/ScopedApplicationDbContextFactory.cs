using System.Security.Claims;

using Collectibles.Application.Interfaces;
using Collectibles.Domain.Constants;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Collectibles.Infrastructure.Persistence;

public class ScopedApplicationDbContextFactory : IApplicationDbContextFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private string? _userId;
    private string? _userName;
    private List<string> _userRoles = new();
    private bool _userInfoCaptured;

    public ScopedApplicationDbContextFactory(
        IServiceProvider serviceProvider,
        DbContextOptions<ApplicationDbContext> options,
        IHttpContextAccessor? httpContextAccessor)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _httpContextAccessor = httpContextAccessor;

        // Try to capture current user info at factory creation time
        TryCaptureUserInfo();
    }

    private void TryCaptureUserInfo()
    {
        if (_userInfoCaptured)
        {
            return;
        }

        // First try HttpContext (works for initial HTTP requests and middleware)
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            _userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _userName = httpContext.User.Identity.Name;
            _userRoles = httpContext.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
            _userInfoCaptured = true;
            return;
        }

        // Try to get user info from IHttpContextDataService for Blazor Server components
        try
        {
            var httpContextDataService = _serviceProvider.GetService<IHttpContextDataService>();
            if (httpContextDataService?.IsInitialized == true && httpContextDataService.IsAuthenticated)
            {
                _userId = httpContextDataService.UserId;
                _userName = httpContextDataService.UserName;
                _userRoles = httpContextDataService.UserRoles?.ToList() ?? new List<string>();
                _userInfoCaptured = true;
            }
        }
        catch
        {
            // Silently fail - we'll use null values for user info
        }
    }

    public async Task<IApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(CreateDbContext());
    }

    public IApplicationDbContext CreateDbContext()
    {
        // Ensure we have the latest user info
        TryCaptureUserInfo();

        // Create a custom current user service with captured values
        var currentUserService = new CapturedCurrentUserService(_userId, _userName, _userRoles);

        // Create the context with the captured current user service
        var context = new ApplicationDbContext(_options, currentUserService);
        return context;
    }

    // Implementation of ICurrentUserService that uses captured values
    private class CapturedCurrentUserService : ICurrentUserService
    {
        private readonly List<string> _roles;

        public CapturedCurrentUserService(string? userId, string? userName, List<string> roles)
        {
            UserId = userId;
            UserName = userName;
            _roles = roles;
        }

        public string? UserId { get; }
        public string? UserName { get; }
        public bool IsAdministrator => _roles.Contains(ApplicationConstants.Roles.Administrator);
        public bool IsInRole(string role) => _roles.Contains(role);
    }
}
