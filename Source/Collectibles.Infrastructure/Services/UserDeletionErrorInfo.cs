using FluentValidation;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Infrastructure.Services;

public sealed record UserDeletionErrorInfo(string Message, bool UserLikelyMissing)
{
    private const string GenericDeleteMessage = "An error occurred while deleting the user. Please try again.";
    private const string ConcurrencyMessage = "This user was already changed or removed. Refresh and try again.";
    private const string MissingUserMessage = "This user no longer exists. The list has been refreshed.";

    public static UserDeletionErrorInfo FromException(Exception exception)
    {
        var unwrapped = Unwrap(exception);

        return unwrapped switch
        {
            DbUpdateConcurrencyException => new UserDeletionErrorInfo(ConcurrencyMessage, true),
            KeyNotFoundException => new UserDeletionErrorInfo(MissingUserMessage, true),
            ValidationException validationException when !string.IsNullOrWhiteSpace(validationException.Message)
                => new UserDeletionErrorInfo(validationException.Message, false),
            _ when !string.IsNullOrWhiteSpace(unwrapped.Message)
                => new UserDeletionErrorInfo(unwrapped.Message, false),
            _ => new UserDeletionErrorInfo(GenericDeleteMessage, false),
        };
    }

    private static Exception Unwrap(Exception exception)
    {
        var current = exception;

        while (current.InnerException is not null)
        {
            current = current.InnerException;
        }

        return current;
    }
}
