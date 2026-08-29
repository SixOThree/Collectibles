namespace Collectibles.Domain.Configuration;

public class ExternalLinksOptions
{
    public const string SectionName = "ExternalLinks";

    public bool Enabled { get; set; } = true;
    public bool CachingEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether link capture may fetch URLs that resolve to
    /// loopback, link-local, or private addresses. Off by default: enabling it removes the
    /// SSRF guard and should only be done on an isolated, trusted network.
    /// </summary>
    public bool AllowPrivateNetworkTargets { get; set; }

    /// <summary>
    /// Gets or sets the maximum time a single link capture (navigation plus screenshot)
    /// may take before it is abandoned.
    /// </summary>
    public int CaptureTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the age in minutes after which a link cache row stuck in
    /// <c>Processing</c> is swept back to <c>Pending</c> for another attempt.
    /// </summary>
    public int StuckProcessingResetMinutes { get; set; } = 15;
}
