using System.Security.Cryptography;
using Collectibles.Domain.Interfaces;

namespace Collectibles.Infrastructure.Services;

/// <summary>
/// Service for computing SHA-256 content hashes for attachment duplicate detection.
/// </summary>
public class AttachmentHashService : IAttachmentHashService
{
    /// <inheritdoc />
    public string ComputeHash(byte[] content)
    {
        if (content == null || content.Length == 0)
        {
            throw new ArgumentException("Content cannot be null or empty", nameof(content));
        }

        var hashBytes = SHA256.HashData(content);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <inheritdoc />
    public async Task<string> ComputeHashAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (!stream.CanRead)
        {
            throw new ArgumentException("Stream must be readable", nameof(stream));
        }

        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
