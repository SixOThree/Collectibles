namespace Collectibles.Web.Middleware;

/// <summary>
/// Removes credentials from request metadata before it is written to durable log storage.
///
/// A share token travels in the URL path, and request logging persists paths and query strings to
/// the database. That put a live bearer credential - one that grants anonymous access to a showcase
/// - into a store with a different access model from the application's own authorization, and into
/// every backup and export taken from it. Redacting rather than dropping the log entry keeps the
/// access recorded while removing what makes it replayable.
/// </summary>
public static class SensitiveRequestDataRedactor
{
    /// <summary>
    /// Stands in for a redacted credential.
    /// </summary>
    public const string Marker = "[redacted]";

    private const string SharePrefix = "/share/";
    private const string PublicApiPrefix = "/api/public/";

    /// <summary>
    /// Query parameter names whose values are treated as credentials.
    /// </summary>
    private static readonly HashSet<string> SensitiveQueryParameters = new(StringComparer.OrdinalIgnoreCase)
    {
        "token",
        "access_token",
        "apikey",
        "api_key",
        "key",
        "secret",
        "password",
        "code",
    };

    /// <summary>
    /// Replaces the token segment of paths known to carry one.
    /// </summary>
    /// <remarks>
    /// <c>/share/{token}</c> carries it in the second segment;
    /// <c>/api/public/attachments/{hash}/preview/{token}</c> carries it last. The route stays
    /// identifiable in the logs; the credential does not survive.
    /// </remarks>
    /// <param name="path">The request path.</param>
    /// <returns>The path with any credential segment replaced.</returns>
    public static string RedactPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        if (path.StartsWith(SharePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return SharePrefix + Marker;
        }

        if (path.StartsWith(PublicApiPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var lastSeparator = path.LastIndexOf('/');

            // Only the trailing token segment goes; the hash and action stay readable.
            if (lastSeparator > 0 && lastSeparator < path.Length - 1)
            {
                return string.Concat(path.AsSpan(0, lastSeparator + 1), Marker);
            }
        }

        return path;
    }

    /// <summary>
    /// Replaces the value of any query parameter whose name suggests it carries a credential.
    /// </summary>
    /// <param name="queryString">The raw query string, with or without its leading question mark.</param>
    /// <returns>The query string with sensitive values replaced.</returns>
    public static string RedactQueryString(string queryString)
    {
        if (string.IsNullOrEmpty(queryString) || queryString == "?")
        {
            return queryString;
        }

        var leading = queryString.StartsWith('?') ? "?" : string.Empty;
        var pairs = queryString.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        var rebuilt = new List<string>(pairs.Length);

        foreach (var pair in pairs)
        {
            var equals = pair.IndexOf('=');
            if (equals <= 0)
            {
                rebuilt.Add(pair);
                continue;
            }

            var name = pair[..equals];
            rebuilt.Add(SensitiveQueryParameters.Contains(name)
                ? $"{name}={Marker}"
                : pair);
        }

        return leading + string.Join('&', rebuilt);
    }
}
