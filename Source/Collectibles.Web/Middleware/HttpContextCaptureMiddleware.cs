using Collectibles.Web.Services;

namespace Collectibles.Web.Middleware;

public class HttpContextCaptureMiddleware
{
    private readonly RequestDelegate _next;

    public HttpContextCaptureMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, HttpContextDataService httpContextDataService)
    {
        // Capture the HTTP context data at the beginning of the request
        httpContextDataService.CaptureHttpContext(context);

        // Continue processing the request
        await _next(context);
    }
}
