using System.Security.Cryptography;
using Collectibles.Application.Setup;
using Collectibles.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Collectibles.Infrastructure.Services;

public class SetupTokenService : ISetupTokenService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SetupTokenService> _logger;
    private readonly string _tokenFilePath;
    private const int TokenLength = 32;

    public SetupTokenService(
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<SetupTokenService> logger)
    {
        _userManager = userManager;
        _environment = environment;
        _configuration = configuration;
        _logger = logger;

        var dataPath = Path.Combine(_environment.ContentRootPath, "App_Data");
        if (!Directory.Exists(dataPath))
        {
            Directory.CreateDirectory(dataPath);
        }

        _tokenFilePath = Path.Combine(dataPath, "setup-token.txt");
    }

    public async Task<bool> IsSetupRequiredAsync()
    {
        try
        {
            var adminRole = "Administrator";
            var admins = await _userManager.GetUsersInRoleAsync(adminRole);
            var setupRequired = admins.Count == 0;

            if (setupRequired)
            {
                _logger.LogInformation("Setup is required - no administrators found in the system");
            }

            return setupRequired;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if setup is required");
            return false;
        }
    }

    public async Task<string> GenerateSetupTokenAsync()
    {
        try
        {
            if (!await IsSetupRequiredAsync())
            {
                _logger.LogWarning("Attempted to generate setup token when setup is not required");
                return string.Empty;
            }

            if (File.Exists(_tokenFilePath))
            {
                var existingToken = await File.ReadAllTextAsync(_tokenFilePath);
                if (!string.IsNullOrWhiteSpace(existingToken))
                {
                    _logger.LogInformation("Using existing setup token from file: {FilePath}", _tokenFilePath);
                    return existingToken.Trim();
                }
            }

            var token = GenerateSecureToken();
            await File.WriteAllTextAsync(_tokenFilePath, token);

            File.SetAttributes(_tokenFilePath, FileAttributes.Normal);

            _logger.LogWarning("========================================");
            _logger.LogWarning("INITIAL SETUP REQUIRED");
            _logger.LogWarning("========================================");
            _logger.LogWarning("No administrators found in the system.");
            _logger.LogWarning("A setup token has been generated and saved to:");
            _logger.LogWarning("  {TokenFilePath}", _tokenFilePath);
            _logger.LogWarning(string.Empty);
            _logger.LogWarning("To create the first administrator account:");
            _logger.LogWarning("1. Navigate to: /Setup");
            _logger.LogWarning("2. Enter the token from the file above");
            _logger.LogWarning("3. Create your administrator account");
            _logger.LogWarning("========================================");

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating setup token");
            throw;
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            if (!await IsSetupRequiredAsync())
            {
                _logger.LogWarning("Setup token validation attempted when setup is not required");
                return false;
            }

            if (!File.Exists(_tokenFilePath))
            {
                _logger.LogWarning("Setup token file not found at: {FilePath}", _tokenFilePath);
                return false;
            }

            var storedToken = await File.ReadAllTextAsync(_tokenFilePath);
            var isValid = string.Equals(token.Trim(), storedToken.Trim(), StringComparison.Ordinal);

            if (isValid)
            {
                _logger.LogInformation("Setup token validated successfully");
            }
            else
            {
                _logger.LogWarning("Invalid setup token provided");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating setup token");
            return false;
        }
    }

    public async Task DeleteTokenAsync()
    {
        try
        {
            if (File.Exists(_tokenFilePath))
            {
                File.Delete(_tokenFilePath);
                _logger.LogInformation("Setup token file deleted: {FilePath}", _tokenFilePath);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting setup token file");
        }
    }

    public string GetTokenFilePath()
    {
        return _tokenFilePath;
    }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[TokenLength];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", string.Empty)
            .Replace("/", string.Empty)
            .Replace("=", string.Empty)
            .Substring(0, TokenLength);
    }
}
