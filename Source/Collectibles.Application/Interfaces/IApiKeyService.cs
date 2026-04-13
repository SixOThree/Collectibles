namespace Collectibles.Application.Interfaces;

public record ApiKeyGenerationResult(string RawKey, string KeyHash);

public interface IApiKeyService
{
    ApiKeyGenerationResult GenerateKey();
    string HashKey(string rawKey);
}
