using System.Net;

namespace Collectibles.Web.Middleware;

public class CrawlerBlockingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CrawlerBlockingMiddleware> _logger;
    private readonly CrawlerBlockingOptions _options;
    private readonly List<string> _blockedLower;
    private readonly List<string> _allowedLower;

    public CrawlerBlockingMiddleware(
        RequestDelegate next,
        ILogger<CrawlerBlockingMiddleware> logger,
        CrawlerBlockingOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
        _blockedLower = options.BlockedUserAgents.Select(ua => ua.ToLowerInvariant()).ToList();
        _allowedLower = options.AllowedUserAgents.Select(ua => ua.ToLowerInvariant()).ToList();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip API endpoints — they use their own authentication (API key / cookie)
        var path = context.Request.Path.Value;
        if (path != null && path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var userAgent = context.Request.Headers.UserAgent.ToString();

        if (string.IsNullOrWhiteSpace(userAgent))
        {
            if (_options.BlockEmptyUserAgent)
            {
                _logger.LogWarning("Blocked request with empty User-Agent from {IpAddress}",
                    context.Connection.RemoteIpAddress);
                await RespondWithForbidden(context);
                return;
            }

            await _next(context);
            return;
        }

        var userAgentLower = userAgent.ToLowerInvariant();

        // Allow list takes precedence — real browsers and permitted bots pass through
        if (_allowedLower.Any(allowed => userAgentLower.Contains(allowed)))
        {
            await _next(context);
            return;
        }

        // Check against blocked patterns
        if (_blockedLower.Any(blocked => userAgentLower.Contains(blocked)))
        {
            _logger.LogWarning("Blocked crawler request from {IpAddress}: {UserAgent}",
                context.Connection.RemoteIpAddress, userAgent);
            await RespondWithForbidden(context);
            return;
        }

        await _next(context);
    }

    private static async Task RespondWithForbidden(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync("Access denied");
    }
}
