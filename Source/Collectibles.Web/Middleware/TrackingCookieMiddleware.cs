using Collectibles.Application.Interfaces;

namespace Collectibles.Web.Middleware;

public class TrackingCookieMiddleware
{
    private readonly RequestDelegate _next;
    private const string TrackingCookieName = "CollectiblesTrackingId";
    private const int CookieExpirationMinutes = 1440;

    public TrackingCookieMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sessionTrackingService = context.RequestServices.GetRequiredService<ISessionTrackingService>();

        if (!context.Request.Cookies.ContainsKey(TrackingCookieName))
        {
            var trackingId = Guid.NewGuid().ToString("N");

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

            context.Response.Cookies.Append(TrackingCookieName, trackingId, cookieOptions);
            sessionTrackingService.SetTrackingId(trackingId);
        }
        else
        {
            var existingTrackingId = context.Request.Cookies[TrackingCookieName];
            if (!string.IsNullOrEmpty(existingTrackingId))
            {
                sessionTrackingService.SetTrackingId(existingTrackingId);

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

                context.Response.Cookies.Append(TrackingCookieName, existingTrackingId, cookieOptions);
            }
        }

        await _next(context);
    }
}
