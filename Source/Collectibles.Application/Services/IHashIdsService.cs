namespace Collectibles.Application.Services;

/// <summary>
/// Translates database identifiers to and from the opaque hashes used at every external
/// boundary.
/// </summary>
public interface IHashIdsService
{
    /// <summary>
    /// Encodes an identifier.
    /// </summary>
    /// <exception cref="ArgumentException">The id is not positive.</exception>
    /// <returns></returns>
    string Encode(long id);

    /// <summary>
    /// Decodes a hash.
    /// </summary>
    /// <remarks>
    /// Throws on malformed input. Callers that treat a bad hash as "not found" - which is
    /// every HTTP endpoint - should use <see cref="TryDecode"/>; guarding on a
    /// <c>0</c> return, as several call sites used to, never fired.
    /// </remarks>
    /// <exception cref="ArgumentException">The hash is empty or cannot be decoded.</exception>
    /// <returns></returns>
    long Decode(string hash);

    /// <summary>
    /// Attempts to decode a hash.
    /// </summary>
    /// <param name="hash">The hash to decode; may be null, empty, or malformed.</param>
    /// <param name="id">The decoded identifier when the hash is valid.</param>
    /// <returns><c>true</c> when the hash decoded to an identifier.</returns>
    bool TryDecode(string? hash, out long id);

    /// <summary>
    /// Encodes several identifiers into one hash.
    /// </summary>
    /// <exception cref="ArgumentException">The ids are empty or contain a non-positive value.</exception>
    /// <returns></returns>
    string Encode(params long[] ids);

    /// <summary>
    /// Decodes a multi-identifier hash.
    /// </summary>
    /// <exception cref="ArgumentException">The hash is empty or cannot be decoded.</exception>
    /// <returns></returns>
    long[] DecodeMultiple(string hash);
}
