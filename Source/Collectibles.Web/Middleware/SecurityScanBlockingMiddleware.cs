using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;

namespace Collectibles.Web.Middleware;

public class SecurityScanBlockingMiddleware : IDisposable
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityScanBlockingMiddleware> _logger;
    private readonly SecurityScanBlockingOptions _options;
    private readonly ConcurrentDictionary<string, IpTrackingInfo> _ipTracking;
    private readonly ConcurrentDictionary<string, DateTime> _blockedIps;
    private readonly List<Regex> _suspiciousPatterns;
    private readonly Timer _cleanupTimer;

    private readonly IClientIpResolver _clientIpResolver;

    public SecurityScanBlockingMiddleware(
        RequestDelegate next,
        ILogger<SecurityScanBlockingMiddleware> logger,
        SecurityScanBlockingOptions options,
        IClientIpResolver clientIpResolver)
    {
        _next = next;
        _logger = logger;
        _options = options;
        _clientIpResolver = clientIpResolver;
        _ipTracking = new ConcurrentDictionary<string, IpTrackingInfo>();
        _blockedIps = new ConcurrentDictionary<string, DateTime>();
        _suspiciousPatterns = CompilePatterns(options.SuspiciousPatterns);

        _cleanupTimer = new Timer(
            CleanupExpiredEntries,
            null,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip API endpoints — they use their own authentication (API key / cookie)
        var requestPath = context.Request.Path.Value;
        if (requestPath != null && requestPath.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var ipAddress = _clientIpResolver.Resolve(context);

        if (string.IsNullOrEmpty(ipAddress))
        {
            await _next(context);
            return;
        }

        if (IsIpBlocked(ipAddress))
        {
            _logger.LogWarning("Blocked request from IP {IpAddress} - IP is currently blocked", ipAddress);
            await RespondWithForbidden(context);
            return;
        }

        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        if (IsSuspiciousRequest(path))
        {
            _logger.LogWarning("Suspicious request detected from IP {IpAddress}: {Path}", ipAddress, path);

            var trackingInfo = _ipTracking.AddOrUpdate(
                ipAddress,
                new IpTrackingInfo { FirstAttempt = DateTime.UtcNow, AttemptCount = 1 },
                (key, existing) =>
                {
                    existing.AttemptCount++;
                    existing.LastAttempt = DateTime.UtcNow;
                    return existing;
                });

            if (trackingInfo.AttemptCount >= _options.MaxAttemptsBeforeBlock)
            {
                BlockIpAddress(ipAddress);
                _logger.LogWarning(
                    "IP {IpAddress} has been blocked after {AttemptCount} suspicious requests",
                    ipAddress,
                    trackingInfo.AttemptCount);

                await RespondWithForbidden(context);
                return;
            }

            await RespondWithNotFound(context);
            return;
        }

        await _next(context);
    }

    private bool IsSuspiciousRequest(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        return _suspiciousPatterns.Any(pattern => pattern.IsMatch(path));
    }

    private bool IsIpBlocked(string ipAddress)
    {
        if (_blockedIps.TryGetValue(ipAddress, out var blockedUntil))
        {
            if (DateTime.UtcNow < blockedUntil)
            {
                return true;
            }

            _blockedIps.TryRemove(ipAddress, out _);
        }

        return false;
    }

    private void BlockIpAddress(string ipAddress)
    {
        var blockUntil = DateTime.UtcNow.Add(_options.BlockDuration);
        _blockedIps.AddOrUpdate(ipAddress, blockUntil, (key, existing) => blockUntil);
        _ipTracking.TryRemove(ipAddress, out _);
    }

    private List<Regex> CompilePatterns(List<string> patterns)
    {
        var compiledPatterns = new List<Regex>();

        foreach (var pattern in patterns)
        {
            try
            {
                compiledPatterns.Add(new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compile pattern: {Pattern}", pattern);
            }
        }

        return compiledPatterns;
    }

    private void CleanupExpiredEntries(object? state)
    {
        try
        {
            var now = DateTime.UtcNow;
            var expiredBlockedIps = _blockedIps
                .Where(kvp => kvp.Value < now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var ip in expiredBlockedIps)
            {
                _blockedIps.TryRemove(ip, out _);
            }

            var expiredTracking = _ipTracking
                .Where(kvp => (now - kvp.Value.LastAttempt).TotalMinutes > _options.TrackingWindowMinutes)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var ip in expiredTracking)
            {
                _ipTracking.TryRemove(ip, out _);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup of expired entries");
        }
    }

    private static async Task RespondWithForbidden(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync("Access denied");
    }

    private static async Task RespondWithNotFound(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync("Not found");
    }

    private class IpTrackingInfo
    {
        public DateTime FirstAttempt { get; set; }
        public DateTime LastAttempt { get; set; }
        public int AttemptCount { get; set; }
    }

    public void Dispose()
    {
        _cleanupTimer.Dispose();
    }
}
