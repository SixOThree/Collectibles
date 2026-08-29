using System.Security.Cryptography;
using System.Text;

using Collectibles.Application.Interfaces;

namespace Collectibles.Infrastructure.Services;

public class ApiKeyService : IApiKeyService
{
    public ApiKeyGenerationResult GenerateKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var rawKey = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var keyHash = HashKey(rawKey);
        return new ApiKeyGenerationResult(rawKey, keyHash);
    }

    public string HashKey(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes);
    }
}
