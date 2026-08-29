using System.Net;

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace Collectibles.Web.Middleware;

/// <summary>
/// The single place the client IP is determined.
/// </summary>
/// <remarks>
/// Three middlewares each re-parsed <c>CF-Connecting-IP</c>, <c>X-Forwarded-For</c> and
/// <c>X-Real-IP</c> themselves, with no check that the request had actually come from a
/// proxy. A client could therefore choose its own apparent address — defeating the
/// abuse blocklist, whose per-IP counter never accumulated, and poisoning audit logs.
///
/// <see cref="Microsoft.AspNetCore.Builder.ForwardedHeadersExtensions.UseForwardedHeaders(Microsoft.AspNetCore.Builder.IApplicationBuilder)"/>
/// now rewrites <c>RemoteIpAddress</c> from <c>X-Forwarded-For</c> only for requests that
/// arrived from a configured known proxy, so <c>RemoteIpAddress</c> is authoritative here.
/// <c>CF-Connecting-IP</c> is only honoured when the immediate peer is one of those
/// trusted proxies.
/// </remarks>
public interface IClientIpResolver
{
    /// <summary>
    /// Returns the client's IP address, or an empty string when it cannot be determined.
    /// </summary>
    /// <returns></returns>
    string Resolve(HttpContext context);
}

/// <inheritdoc />
public class ClientIpResolver : IClientIpResolver
{
    private readonly ForwardedHeadersOptions _forwardedHeadersOptions;

    public ClientIpResolver(IOptions<ForwardedHeadersOptions> forwardedHeadersOptions)
    {
        _forwardedHeadersOptions = forwardedHeadersOptions.Value;
    }

    /// <inheritdoc />
    public string Resolve(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress;

        // Cloudflare sets CF-Connecting-IP itself and it cannot be spoofed through
        // Cloudflare — but it can be sent by anyone connecting directly, so it is only
        // trusted when the peer is a configured proxy.
        if (remoteIp is not null && IsTrustedProxy(remoteIp))
        {
            var cfConnectingIp = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(cfConnectingIp) && IPAddress.TryParse(cfConnectingIp, out var parsed))
            {
                return parsed.ToString();
            }
        }

        return remoteIp?.ToString() ?? string.Empty;
    }

    private bool IsTrustedProxy(IPAddress address)
    {
        if (_forwardedHeadersOptions.KnownProxies.Any(proxy => proxy.Equals(address)))
        {
            return true;
        }

        return _forwardedHeadersOptions.KnownNetworks.Any(network => network.Contains(address));
    }
}
