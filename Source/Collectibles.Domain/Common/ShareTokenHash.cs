using System.Security.Cryptography;
using System.Text;

namespace Collectibles.Domain.Common;

/// <summary>
/// Derives the stored form of a showcase share token.
///
/// A share token is a bearer credential: whoever holds it can read the showcase. Storing the
/// literal value means a database backup, a reporting connection, or a SQL-level compromise yields
/// working credentials for every shared showcase. Only this one-way derivation is persisted, so a
/// reader of the table learns nothing usable, and lookups hash the presented value instead.
///
/// A fast hash is appropriate here, unlike for passwords: the token is 256 bits of output from a
/// cryptographic RNG, so there is no guessable input space for an attacker to search.
/// </summary>
public static class ShareTokenHash
{
    /// <summary>
    /// Length of the produced hash in characters (SHA-256 rendered as lowercase hex).
    /// </summary>
    public const int Length = 64;

    /// <summary>
    /// Computes the stored form of a presented token.
    /// </summary>
    /// <param name="token">The token as it appears in a share URL.</param>
    /// <returns>Lowercase hexadecimal SHA-256 of the token.</returns>
    public static string Compute(string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));

        return Convert.ToHexStringLower(hash);
    }
}
