namespace Collectibles.Application.Interfaces;

/// <summary>
/// Carries the access a validated share token grants for the current request.
///
/// Share links are the one way an anonymous caller legitimately reaches a private showcase, but
/// that grant used to exist only at the endpoint that validated the token. Resource authorization
/// ran afterwards knowing nothing about it, so a valid link to a private showcase was denied by the
/// resource handler and surfaced as a server fault. Recording the grant here lets both gates be
/// reasoned about together: the endpoint establishes what the token proves, and the authorization
/// handler decides on that basis alongside ownership and visibility.
/// </summary>
public interface IShareAccessContext
{
    /// <summary>
    /// Records that a share token valid for the given showcase was presented on this request.
    /// Called only after the token has been validated.
    /// </summary>
    /// <param name="showcaseId">The showcase the presented token grants access to.</param>
    void GrantShowcaseAccess(long showcaseId);

    /// <summary>
    /// Determines whether this request presented a valid share token for the given showcase.
    /// </summary>
    /// <param name="showcaseId">The showcase being accessed.</param>
    /// <returns><c>true</c> when a validated token for that showcase was recorded.</returns>
    bool HasAccessTo(long showcaseId);

    /// <summary>
    /// Determines whether this request presented a valid share token for any of the given showcases.
    /// </summary>
    /// <param name="showcaseIds">The showcases that could authorize the resource.</param>
    /// <returns><c>true</c> when at least one is covered by a validated token.</returns>
    bool HasAccessToAny(IEnumerable<long> showcaseIds);
}
