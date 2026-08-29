namespace Collectibles.Application.Interfaces;

/// <summary>
/// Validates that a user-supplied URL is safe for the server to fetch.
/// </summary>
public interface IUrlEgressGuard
{
    /// <summary>
    /// Checks a URL against the egress policy, resolving its host to confirm every
    /// address it maps to is publicly routable.
    /// </summary>
    /// <param name="url">The raw user-supplied URL.</param>
    /// <param name="cancellationToken">Token cancelling the name resolution.</param>
    /// <returns>The validated absolute URI, or the reason it was rejected.</returns>
    Task<UrlEgressResult> ValidateAsync(string? url, CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome of an egress check.
/// </summary>
/// <param name="IsAllowed">Whether the server may fetch the URL.</param>
/// <param name="Uri">The parsed URI when allowed.</param>
/// <param name="Reason">Why the URL was rejected, when not allowed.</param>
public readonly record struct UrlEgressResult(bool IsAllowed, Uri? Uri, string Reason)
{
    public static UrlEgressResult Allowed(Uri uri) => new(true, uri, string.Empty);

    public static UrlEgressResult Denied(string reason) => new(false, null, reason);
}
