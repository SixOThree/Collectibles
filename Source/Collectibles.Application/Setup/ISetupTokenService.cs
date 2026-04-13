namespace Collectibles.Application.Setup;

public interface ISetupTokenService
{
    Task<bool> IsSetupRequiredAsync();
    Task<string> GenerateSetupTokenAsync();
    Task<bool> ValidateTokenAsync(string token);
    Task DeleteTokenAsync();
    string GetTokenFilePath();
}
