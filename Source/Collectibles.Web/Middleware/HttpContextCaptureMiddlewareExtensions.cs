namespace Collectibles.Web.Middleware;

public static class HttpContextCaptureMiddlewareExtensions
{
    public static IApplicationBuilder UseHttpContextCapture(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<HttpContextCaptureMiddleware>();
    }
}
