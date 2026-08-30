using System.Diagnostics;

using Collectibles.Application.Interfaces;
using Collectibles.Web.Services;

namespace Collectibles.Web.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly RequestLogQueue _requestLogQueue;

    // Paths to exclude from logging
    private readonly HashSet<string> _excludedPaths = new()
    {
        "/health",
        "/metrics",
        "/_blazor",
        "/_framework",
        "/css",
        "/js",
        "/images",
        "/favicon.ico",
    };

    private readonly HashSet<string> _excludedExtensions = new()
    {
        ".css",
        ".js",
        ".map",
        ".ico",
        ".png",
        ".jpg",
        ".jpeg",
        ".gif",
        ".svg",
        ".woff",
        ".woff2",
        ".ttf",
        ".eot",
    };

    private readonly IClientIpResolver _clientIpResolver;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        RequestLogQueue requestLogQueue,
        IClientIpResolver clientIpResolver)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _requestLogQueue = requestLogQueue;
        _clientIpResolver = clientIpResolver;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkipLogging(context.Request))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var requestId = Activity.Current?.Id ?? context.TraceIdentifier;
        var correlationId = context.TraceIdentifier;

        // Capture initial request details. Share routes carry a bearer token in the path, so the
        // logged value is redacted rather than the request being dropped: the access is still
        // recorded, without persisting a credential that would remain usable to any log reader.
        var method = context.Request.Method;
        var path = SensitiveRequestDataRedactor.RedactPath(context.Request.Path.ToString());
        var queryString = SensitiveRequestDataRedactor.RedactQueryString(context.Request.QueryString.ToString());
        var scheme = context.Request.Scheme;
        var host = context.Request.Host.ToString();
        var ipAddress = GetIPAddress(context);
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        var contentType = context.Request.ContentType;
        var contentLength = context.Request.ContentLength;

        // Get user info
        string? userId = null;
        string? userName = null;
        using (var scope = _serviceProvider.CreateScope())
        {
            var currentUserService = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();
            userId = currentUserService.UserId;
            userName = currentUserService.UserName;
        }

        Exception? capturedException = null;
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            capturedException = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            // Queue request log entry for background processing (non-blocking)
            // This removes database write from the request pipeline, dramatically improving performance
            var logEntry = new RequestLogEntry
            {
                Method = method,
                Path = path,
                QueryString = queryString,
                StatusCode = context.Response.StatusCode,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                RequestId = requestId,
                CorrelationId = correlationId,
                UserId = userId,
                UserName = userName,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Scheme = scheme,
                Host = host,
                ContentType = contentType,
                ContentLength = contentLength,
                ResponseContentType = context.Response.ContentType,
                ResponseContentLength = context.Response.ContentLength,
                Exception = capturedException,
            };

            // Fire and forget - don't wait for queue operation
            _ = _requestLogQueue.EnqueueAsync(logEntry);
        }
    }

    private bool ShouldSkipLogging(HttpRequest request)
    {
        var path = request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Check if path starts with any excluded path
        if (_excludedPaths.Any(excludedPath => path.StartsWith(excludedPath)))
        {
            return true;
        }

        // Check file extension
        var extension = Path.GetExtension(path)?.ToLowerInvariant();
        if (!string.IsNullOrEmpty(extension) && _excludedExtensions.Contains(extension))
        {
            return true;
        }

        return false;
    }

    private string? GetIPAddress(HttpContext context)
    {
        // Resolved centrally so the logged address cannot be chosen by the client.
        return _clientIpResolver.Resolve(context);
    }
}
