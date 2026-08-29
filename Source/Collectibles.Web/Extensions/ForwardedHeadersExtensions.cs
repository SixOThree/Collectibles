using System.Net;

using Microsoft.AspNetCore.HttpOverrides;

namespace Collectibles.Web.Extensions;

/// <summary>
/// Configures trusted-proxy handling for <c>X-Forwarded-*</c> headers.
/// </summary>
/// <remarks>
/// The middleware previously parsed <c>CF-Connecting-IP</c>, <c>X-Forwarded-For</c> and
/// <c>X-Real-IP</c> by hand in three separate places with no check that the immediate peer
/// was a proxy at all. Any client could therefore present a forged header: the scan
/// blocker keyed its per-IP throttle on the forged value so the block counter never
/// accumulated, and audit logs recorded whatever the client claimed.
///
/// With this registered, <c>HttpContext.Connection.RemoteIpAddress</c> is the authoritative
/// client IP, and it is only rewritten from a forwarded header when the request actually
/// arrived from a configured proxy.
/// </remarks>
public static class ForwardedHeadersExtensions
{
    /// <summary>
    /// Registers forwarded-header processing restricted to the proxies named in
    /// <c>ForwardedHeaders:KnownProxies</c> and <c>ForwardedHeaders:KnownNetworks</c>.
    /// </summary>
    /// <returns></returns>
    public static IServiceCollection AddTrustedProxyForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // The defaults trust only loopback. Clearing them means nothing is trusted
            // unless it is configured, so an unconfigured deployment fails closed.
            options.KnownProxies.Clear();
            options.KnownNetworks.Clear();

            var knownProxies = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
            foreach (var proxy in knownProxies)
            {
                if (IPAddress.TryParse(proxy, out var address))
                {
                    options.KnownProxies.Add(address);
                }
            }

            var knownNetworks = configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];
            foreach (var network in knownNetworks)
            {
                var parts = network.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 2
                    && IPAddress.TryParse(parts[0], out var prefix)
                    && int.TryParse(parts[1], out var prefixLength))
                {
                    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, prefixLength));
                }
            }

            // A forwarded chain longer than the configured proxy depth cannot be trusted.
            options.ForwardLimit = configuration.GetValue("ForwardedHeaders:ForwardLimit", 1);
        });

        return services;
    }
}
