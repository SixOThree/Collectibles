using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace Collectibles.Domain.Common;

/// <summary>
/// Syntactic egress policy for user-supplied external links. The server fetches these
/// URLs from a privileged network position, so anything that is not a public http(s)
/// endpoint is rejected before a request is ever issued.
/// </summary>
/// <remarks>
/// This covers the parts of the check that need no network access (scheme, credentials,
/// literal addresses, obviously-internal host names). Name resolution is layered on top
/// of it by the infrastructure guard, which re-runs <see cref="IsAllowedAddress"/> against
/// every resolved address and every redirect target.
/// </remarks>
public static class ExternalUrlPolicy
{
    private static readonly string[] BlockedHostSuffixes =
    [
        ".local",
        ".localhost",
        ".internal",
        ".home.arpa",
    ];

    /// <summary>
    /// Validates the shape of a user-supplied URL.
    /// </summary>
    /// <param name="url">The raw URL as supplied by the user.</param>
    /// <param name="uri">The parsed absolute URI when the check passes.</param>
    /// <param name="error">A human-readable reason when the check fails.</param>
    /// <returns><c>true</c> when the URL is an absolute, public-looking http(s) URL.</returns>
    public static bool TryValidate(string? url, [NotNullWhen(true)] out Uri? uri, out string error)
    {
        uri = null;

        if (string.IsNullOrWhiteSpace(url))
        {
            error = "A URL is required.";
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed))
        {
            error = "The URL is not a valid absolute URL.";
            return false;
        }

        if (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps)
        {
            error = "Only http and https URLs are supported.";
            return false;
        }

        if (!string.IsNullOrEmpty(parsed.UserInfo))
        {
            error = "URLs containing credentials are not supported.";
            return false;
        }

        if (!IsAllowedHost(parsed.DnsSafeHost, out error))
        {
            return false;
        }

        uri = parsed;
        error = string.Empty;
        return true;
    }

    /// <summary>
    /// Rejects host names that are unambiguously internal, and literal addresses that
    /// point at a non-public range.
    /// </summary>
    /// <returns></returns>
    public static bool IsAllowedHost(string host, out string error)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            error = "The URL has no host.";
            return false;
        }

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            BlockedHostSuffixes.Any(suffix => host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
        {
            error = "The URL points at an internal host.";
            return false;
        }

        if (IPAddress.TryParse(host, out var literal) && !IsAllowedAddress(literal))
        {
            error = "The URL points at a non-public IP address.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <summary>
    /// Returns whether an address is outside every range that could reach internal
    /// infrastructure — loopback, link-local (including cloud metadata at 169.254.169.254),
    /// private, carrier-grade NAT, multicast, and unspecified/reserved space.
    /// </summary>
    /// <returns></returns>
    public static bool IsAllowedAddress(IPAddress address)
    {
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv4MappedToIPv6)
            {
                return IsAllowedAddress(address.MapToIPv4());
            }

            return !IPAddress.IPv6Any.Equals(address)
                && !IPAddress.IPv6Loopback.Equals(address)
                && !address.IsIPv6LinkLocal
                && !address.IsIPv6SiteLocal
                && !address.IsIPv6Multicast
                && !IsIPv6UniqueLocal(address);
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var octets = address.GetAddressBytes();

        return octets[0] switch
        {
            0 => false,                                     // 0.0.0.0/8 "this network"
            10 => false,                                    // 10.0.0.0/8 private
            127 => false,                                   // 127.0.0.0/8 loopback
            100 when octets[1] >= 64 && octets[1] <= 127 => false, // 100.64.0.0/10 CGNAT
            169 when octets[1] == 254 => false,             // 169.254.0.0/16 link-local / metadata
            172 when octets[1] >= 16 && octets[1] <= 31 => false, // 172.16.0.0/12 private
            192 when octets[1] == 168 => false,             // 192.168.0.0/16 private
            192 when octets[1] == 0 && octets[2] == 0 => false, // 192.0.0.0/24 IETF protocol
            198 when octets[1] == 18 || octets[1] == 19 => false, // 198.18.0.0/15 benchmarking
            >= 224 => false,                                // multicast + reserved
            _ => true,
        };
    }

    private static bool IsIPv6UniqueLocal(IPAddress address)
    {
        // fc00::/7
        return (address.GetAddressBytes()[0] & 0xFE) == 0xFC;
    }
}
