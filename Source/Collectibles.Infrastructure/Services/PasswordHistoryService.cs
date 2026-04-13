using Collectibles.Application.Interfaces;
using Collectibles.Domain.Security;
using Collectibles.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Collectibles.Infrastructure.Services;

public class PasswordHistoryService : IPasswordHistoryService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PasswordHistoryService> _logger;

    public PasswordHistoryService(
        ApplicationDbContext context,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IConfiguration configuration,
        ILogger<PasswordHistoryService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task AddToHistoryAsync(string userId, string passwordHash, CancellationToken cancellationToken = default)
    {
        try
        {
            var maxHistoryCount = _configuration.GetValue("PasswordPolicy:PasswordHistoryCount", 5);

            var history = new PasswordHistory
            {
                UserId = userId,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow,
            };

            _context.Set<PasswordHistory>().Add(history);

            // Keep only last N passwords
            var oldEntries = await _context.Set<PasswordHistory>()
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.CreatedAt)
                .Skip(maxHistoryCount)
                .ToListAsync(cancellationToken);

            if (oldEntries.Count != 0)
            {
                _context.Set<PasswordHistory>().RemoveRange(oldEntries);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Added password to history for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding password to history for user {UserId}", userId);

            // Don't throw - password history is not critical for authentication
        }
    }

    public async Task<bool> IsInHistoryAsync(string userId, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var maxHistoryCount = _configuration.GetValue("PasswordPolicy:PasswordHistoryCount", 5);

            var histories = await _context.Set<PasswordHistory>()
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.CreatedAt)
                .Take(maxHistoryCount)
                .ToListAsync(cancellationToken);

            foreach (var history in histories)
            {
                var verificationResult = _passwordHasher.VerifyHashedPassword(
                    null!, history.PasswordHash, password);

                if (verificationResult == PasswordVerificationResult.Success ||
                    verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    _logger.LogWarning("User {UserId} attempted to reuse a password from history", userId);
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking password history for user {UserId}", userId);

            // On error, allow the password (fail open)
            return false;
        }
    }

    public async Task ClearHistoryAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var histories = await _context.Set<PasswordHistory>()
                .Where(h => h.UserId == userId)
                .ToListAsync(cancellationToken);

            if (histories.Count != 0)
            {
                _context.Set<PasswordHistory>().RemoveRange(histories);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Cleared password history for user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing password history for user {UserId}", userId);
        }
    }
}
