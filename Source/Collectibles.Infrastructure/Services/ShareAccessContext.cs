using Collectibles.Application.Interfaces;

namespace Collectibles.Infrastructure.Services;

/// <summary>
/// Per-request record of the showcases a validated share token grants access to.
///
/// Registered scoped, so a grant lives exactly as long as the request that proved it and can never
/// leak into another caller's request. The type deliberately offers no way to revoke or enumerate
/// grants: an endpoint either proved a token or it did not.
/// </summary>
public class ShareAccessContext : IShareAccessContext
{
    private readonly HashSet<long> _grantedShowcaseIds = [];

    /// <inheritdoc />
    public void GrantShowcaseAccess(long showcaseId) => _grantedShowcaseIds.Add(showcaseId);

    /// <inheritdoc />
    public bool HasAccessTo(long showcaseId) => _grantedShowcaseIds.Contains(showcaseId);

    /// <inheritdoc />
    public bool HasAccessToAny(IEnumerable<long> showcaseIds)
    {
        ArgumentNullException.ThrowIfNull(showcaseIds);

        if (_grantedShowcaseIds.Count == 0)
        {
            return false;
        }

        return showcaseIds.Any(_grantedShowcaseIds.Contains);
    }
}
