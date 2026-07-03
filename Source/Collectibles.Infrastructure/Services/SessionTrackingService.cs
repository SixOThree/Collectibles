using Collectibles.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Collectibles.Infrastructure.Services;

public class SessionTrackingService : ISessionTrackingService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionTrackingService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? TrackingId { get; private set; }

    public string? SessionId => TrackingId != null ? $"session_{TrackingId}" : null;

    public void SetTrackingId(string trackingId)
    {
        if (string.IsNullOrEmpty(TrackingId))
        {
            TrackingId = trackingId;
        }
    }

    public void RefreshExpiration()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null || string.IsNullOrEmpty(TrackingId))
        {
            return;
        }

        const string trackingCookieName = "CollectiblesTrackingId";
        const int cookieExpirationMinutes = 1440;

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(cookieExpirationMinutes),
        };

        if (!httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            cookieOptions.Secure = true;
        }

        httpContext.Response.Cookies.Append(trackingCookieName, TrackingId, cookieOptions);
    }
}
