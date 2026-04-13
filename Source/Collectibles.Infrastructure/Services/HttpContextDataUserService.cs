using Collectibles.Application.Interfaces;

namespace Collectibles.Infrastructure.Services;

/// <summary>
/// Implementation of ICurrentUserService that uses HttpContextDataService
/// which is populated by middleware during the initial HTTP request.
/// This is safe to use in Blazor Server Interactive components.
/// </summary>
public class HttpContextDataUserService : ICurrentUserService
{
    private readonly IHttpContextDataService _httpContextDataService;

    public HttpContextDataUserService(IHttpContextDataService httpContextDataService)
    {
        _httpContextDataService = httpContextDataService;
    }

    public string? UserId => _httpContextDataService.UserId;

    public string? UserName => _httpContextDataService.UserName;

    public bool IsAdministrator => _httpContextDataService.IsAdministrator;
}
