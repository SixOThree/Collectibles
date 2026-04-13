namespace Collectibles.Domain.Interfaces;

/// <summary>
/// Service for computing content hashes for attachment duplicate detection.
/// </summary>
public interface IAttachmentHashService
{
    /// <summary>
    /// Computes a SHA-256 hash from a byte array.
    /// </summary>
    /// <param name="content">The file content to hash.</param>
    /// <returns>The lowercase hexadecimal hash string (64 characters).</returns>
    string ComputeHash(byte[] content);

    /// <summary>
    /// Computes a SHA-256 hash from a stream.
    /// </summary>
    /// <param name="stream">The stream containing file content to hash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The lowercase hexadecimal hash string (64 characters).</returns>
    Task<string> ComputeHashAsync(Stream stream, CancellationToken cancellationToken = default);
}
