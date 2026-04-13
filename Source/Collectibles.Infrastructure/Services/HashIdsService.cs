using Collectibles.Application.Services;
using HashidsNet;
using Microsoft.Extensions.Configuration;

namespace Collectibles.Infrastructure.Services;

public class HashIdsService : IHashIdsService
{
    private readonly IHashids _hashids;

    public HashIdsService(IConfiguration configuration)
    {
        var salt = configuration["HashIds:Salt"];

        if (string.IsNullOrWhiteSpace(salt))
        {
            throw new InvalidOperationException(
                "HashIds:Salt configuration is required. " +
                "Set a unique salt value in appsettings.json or user secrets.");
        }

        string[] placeholderValues =
        [
            "YOUR_UNIQUE_SALT_HERE",
            "collectibles-default-salt-change-in-production",
        ];

        if (placeholderValues.Contains(salt, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "HashIds:Salt is still set to a placeholder value. " +
                "Replace it with a unique, secret string in appsettings.json or user secrets.");
        }

        var minHashLength = configuration.GetValue<int?>("HashIds:MinHashLength") ?? 8;
        var alphabet = configuration["HashIds:Alphabet"] ?? "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";

        _hashids = new Hashids(salt, minHashLength, alphabet);
    }

    public string Encode(long id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("ID must be greater than 0", nameof(id));
        }

        return _hashids.EncodeLong(id);
    }

    public long Decode(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            throw new ArgumentException("Hash cannot be null or empty", nameof(hash));
        }

        var result = _hashids.DecodeLong(hash);

        if (result.Length == 0)
        {
            throw new ArgumentException("Invalid hash", nameof(hash));
        }

        return result[0];
    }

    public string Encode(params long[] ids)
    {
        if (ids == null || ids.Length == 0)
        {
            throw new ArgumentException("IDs cannot be null or empty", nameof(ids));
        }

        if (ids.Any(id => id <= 0))
        {
            throw new ArgumentException("All IDs must be greater than 0", nameof(ids));
        }

        return _hashids.EncodeLong(ids);
    }

    public long[] DecodeMultiple(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            throw new ArgumentException("Hash cannot be null or empty", nameof(hash));
        }

        var result = _hashids.DecodeLong(hash);

        if (result.Length == 0)
        {
            throw new ArgumentException("Invalid hash", nameof(hash));
        }

        return result;
    }
}
