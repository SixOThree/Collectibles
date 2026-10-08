using System.Net;

using Collectibles.Application.Interfaces;
using Collectibles.Domain.Common;
using Collectibles.Domain.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Collectibles.Infrastructure.Services;

/// <summary>
/// Default <see cref="IUrlEgressGuard"/>: applies the syntactic policy, then resolves the
/// host and requires every resolved address to be publicly routable, so that a DNS name
/// pointing at loopback, a private range, or cloud metadata cannot be fetched.
/// </summary>
public class UrlEgressGuard : IUrlEgressGuard
{
    private readonly ExternalLinksOptions _options;
    private readonly ILogger<UrlEgressGuard> _logger;

    public UrlEgressGuard(IOptions<ExternalLinksOptions> options, ILogger<UrlEgressGuard> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UrlEgressResult> ValidateAsync(string? url, CancellationToken cancellationToken = default)
    {
        if (!ExternalUrlPolicy.TryValidate(url, out var uri, out var error))
        {
            return UrlEgressResult.Denied(error);
        }

        if (_options.AllowPrivateNetworkTargets)
        {
            // Opt-in escape hatch for self-hosted deployments that intentionally capture
            // links on their own network. Off by default.
            return UrlEgressResult.Allowed(uri);
        }

        if (IPAddress.TryParse(uri.DnsSafeHost, out _))
        {
            // Already validated as a literal address by the policy.
            return UrlEgressResult.Allowed(uri);
        }

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
        }
        catch (Exception ex) when (ex is System.Net.Sockets.SocketException or ArgumentException)
        {
            _logger.LogWarning(ex, "Could not resolve host {Host} for egress validation", uri.DnsSafeHost);
            return UrlEgressResult.Denied("The URL's host could not be resolved.");
        }

        if (addresses.Length == 0)
        {
            return UrlEgressResult.Denied("The URL's host could not be resolved.");
        }

        if (Array.Exists(addresses, address => !ExternalUrlPolicy.IsAllowedAddress(address)))
        {
            return UrlEgressResult.Denied("The URL resolves to a non-public IP address.");
        }

        return UrlEgressResult.Allowed(uri);
    }
}
