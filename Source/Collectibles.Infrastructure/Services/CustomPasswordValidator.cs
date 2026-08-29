using System.Text.RegularExpressions;

using Collectibles.Application.Interfaces;
using Collectibles.Infrastructure.Persistence;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Collectibles.Infrastructure.Services;

public class CustomPasswordValidator : IPasswordValidator<ApplicationUser>
{
    private readonly ILogger<CustomPasswordValidator> _logger;
    private readonly IPasswordHistoryService _passwordHistoryService;

    // Common passwords list (abbreviated for example)
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "123456", "12345678", "qwerty", "abc123", "monkey", "1234567",
        "letmein", "trustno1", "dragon", "baseball", "111111", "iloveyou", "master",
        "sunshine", "ashley", "bailey", "passw0rd", "shadow", "123123", "654321",
        "superman", "qazwsx", "michael", "football", "password1", "password123",
    };

    public CustomPasswordValidator(
        ILogger<CustomPasswordValidator> logger,
        IPasswordHistoryService passwordHistoryService)
    {
        _logger = logger;
        _passwordHistoryService = passwordHistoryService;
    }

    public async Task<IdentityResult> ValidateAsync(
        UserManager<ApplicationUser> manager,
        ApplicationUser user,
        string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordRequired",
                Description = "Password is required.",
            });
        }

        var errors = new List<IdentityError>();

        // Check against common passwords
        if (IsCommonPassword(password))
        {
            errors.Add(new IdentityError
            {
                Code = "CommonPassword",
                Description = "This password is too common. Please choose a more unique password.",
            });
        }

        // Check against user information
        if (ContainsUserInfo(password, user))
        {
            errors.Add(new IdentityError
            {
                Code = "ContainsUserInfo",
                Description = "Password cannot contain your username, email, or display name.",
            });
        }

        // Check password history (only for existing users)
        if (!string.IsNullOrEmpty(user.Id))
        {
            if (await _passwordHistoryService.IsInHistoryAsync(user.Id, password))
            {
                errors.Add(new IdentityError
                {
                    Code = "ReusedPassword",
                    Description = "You cannot reuse your last 5 passwords. Please choose a different password.",
                });
            }
        }

        // Check for sequential or repeated characters
        if (HasWeakPatterns(password))
        {
            errors.Add(new IdentityError
            {
                Code = "WeakPattern",
                Description = "Password cannot contain more than 3 sequential or repeated characters.",
            });
        }

        // Check entropy/complexity
        var entropy = CalculateEntropy(password);
        if (entropy < 50)
        {
            errors.Add(new IdentityError
            {
                Code = "LowEntropy",
                Description = "Password is not complex enough. Use a mix of uppercase, lowercase, numbers, and special characters.",
            });
        }

        if (errors.Count != 0)
        {
            _logger.LogWarning(
                "Password validation failed for user {UserId}: {Errors}",
                user.Id ?? "new", string.Join(", ", errors.Select(e => e.Code)));
        }

        return errors.Count == 0
            ? IdentityResult.Success
            : IdentityResult.Failed(errors.ToArray());
    }

    private static bool IsCommonPassword(string password)
    {
        // Check exact match
        if (CommonPasswords.Contains(password))
        {
            return true;
        }

        // Check with common substitutions (e.g., @ for a, 0 for o)
        var normalized = password
            .Replace("@", "a", StringComparison.OrdinalIgnoreCase)
            .Replace("0", "o", StringComparison.OrdinalIgnoreCase)
            .Replace("1", "i", StringComparison.OrdinalIgnoreCase)
            .Replace("3", "e", StringComparison.OrdinalIgnoreCase)
            .Replace("$", "s", StringComparison.OrdinalIgnoreCase)
            .Replace("5", "s", StringComparison.OrdinalIgnoreCase);

        return CommonPasswords.Contains(normalized);
    }

    private static bool ContainsUserInfo(string password, ApplicationUser user)
    {
        var lowerPassword = password.ToLowerInvariant();

        // Check username
        if (!string.IsNullOrEmpty(user.UserName) &&
            lowerPassword.Contains(user.UserName, StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        // Check email parts
        if (!string.IsNullOrEmpty(user.Email))
        {
            var emailParts = user.Email.Split('@')[0].Split('.');
            foreach (var part in emailParts)
            {
                if (part.Length > 3 && lowerPassword.Contains(part, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
        }

        // Check display name
        if (!string.IsNullOrEmpty(user.DisplayName))
        {
            var nameParts = user.DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in nameParts)
            {
                if (part.Length > 3 && lowerPassword.Contains(part, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasWeakPatterns(string password)
    {
        // Check for repeated characters (e.g., "aaa", "111")
        if (Regex.IsMatch(password, @"(.)\1{3,}"))
        {
            return true;
        }

        // Check for keyboard patterns
        var keyboardPatterns = new[]
        {
            "qwerty", "asdfgh", "zxcvbn", "qwertyuiop", "asdfghjkl", "zxcvbnm",
            "1234567890", "123456789", "12345678", "1234567", "123456",
            "abcdefgh", "abcdefg", "abcdef", "abcde", "abcd",
        };

        var lowerPassword = password.ToLowerInvariant();
        foreach (var pattern in keyboardPatterns)
        {
            if (lowerPassword.Contains(pattern) || lowerPassword.Contains(new string(pattern.Reverse().ToArray())))
            {
                return true;
            }
        }

        // Check for sequential characters
        for (int i = 0; i < password.Length - 3; i++)
        {
            if (IsSequential(password.Substring(i, 4)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSequential(string substr)
    {
        if (substr.Length < 4)
        {
            return false;
        }

        bool ascending = true;
        bool descending = true;

        for (int i = 1; i < substr.Length; i++)
        {
            if (substr[i] != substr[i - 1] + 1)
            {
                ascending = false;
            }

            if (substr[i] != substr[i - 1] - 1)
            {
                descending = false;
            }
        }

        return ascending || descending;
    }

    private static double CalculateEntropy(string password)
    {
        var charSetSize = 0;

        if (Regex.IsMatch(password, @"[a-z]"))
        {
            charSetSize += 26;
        }

        if (Regex.IsMatch(password, @"[A-Z]"))
        {
            charSetSize += 26;
        }

        if (Regex.IsMatch(password, @"\d"))
        {
            charSetSize += 10;
        }

        if (Regex.IsMatch(password, @"[^a-zA-Z\d\s]"))
        {
            charSetSize += 32; // Special characters
        }

        if (charSetSize == 0)
        {
            return 0;
        }

        // Calculate entropy: length * log2(charset size)
        return password.Length * Math.Log2(charSetSize);
    }
}
