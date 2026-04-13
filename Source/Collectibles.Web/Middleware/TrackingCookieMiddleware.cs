namespace Collectibles.Web.Middleware;

public class TrackingCookieMiddleware
{
    private readonly RequestDelegate _next;
    private const string TrackingCookieName = "CollectiblesTrackingId";
    private const int CookieExpirationMinutes = 30;

    public TrackingCookieMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if tracking cookie exists
        if (!context.Request.Cookies.ContainsKey(TrackingCookieName))
        {
            // Generate new tracking ID
            var trackingId = Guid.NewGuid().ToString("N");

            // Set cookie with sliding expiration
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true, // GDPR - this cookie is essential for the service
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(CookieExpirationMinutes),
            };

            // Set Secure flag in production
            if (!context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                cookieOptions.Secure = true;
            }

            context.Response.Cookies.Append(TrackingCookieName, trackingId, cookieOptions);
        }
        else
        {
            // Refresh cookie expiration on activity
            var existingTrackingId = context.Request.Cookies[TrackingCookieName];
            if (!string.IsNullOrEmpty(existingTrackingId))
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

                context.Response.Cookies.Append(TrackingCookieName, existingTrackingId, cookieOptions);
            }
        }

        await _next(context);
    }
}
