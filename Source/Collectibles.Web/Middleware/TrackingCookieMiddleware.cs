using Collectibles.Application.Interfaces;

namespace Collectibles.Web.Middleware;

public class TrackingCookieMiddleware
{
    private const string TrackingCookieName = "CollectiblesTrackingId";
    private const int CookieExpirationMinutes = 1440;

    private readonly RequestDelegate _next;

    public TrackingCookieMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sessionTrackingService = context.RequestServices.GetRequiredService<ISessionTrackingService>();
        var trackingId = context.Request.Cookies[TrackingCookieName];

        if (string.IsNullOrEmpty(trackingId))
        {
            trackingId = Guid.NewGuid().ToString("N");

            context.Response.Cookies.Append(TrackingCookieName, trackingId, CreateCookieOptions(context));
            sessionTrackingService.SetTrackingId(trackingId);
        }
        else
        {
            sessionTrackingService.SetTrackingId(trackingId);
        }

        await _next(context);
    }

    private static CookieOptions CreateCookieOptions(HttpContext context)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(CookieExpirationMinutes),
        };

        if (!context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            cookieOptions.Secure = true;
        }

        return cookieOptions;
    }
}
