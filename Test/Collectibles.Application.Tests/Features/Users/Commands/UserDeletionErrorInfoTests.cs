using Collectibles.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Tests.Features.Users.Commands;

public class UserDeletionErrorInfoTests
{
    [Fact]
    public void FromExceptionShouldReturnConcurrencyMessageForConcurrencyException()
    {
        var info = UserDeletionErrorInfo.FromException(new DbUpdateConcurrencyException("stale row"));

        info.Message.Should().Be("This user was already changed or removed. Refresh and try again.");
        info.UserLikelyMissing.Should().BeTrue();
    }

    [Fact]
    public void FromExceptionShouldReturnMissingUserMessageForMissingUserException()
    {
        var info = UserDeletionErrorInfo.FromException(new KeyNotFoundException("User with ID 123 not found."));

        info.Message.Should().Be("This user no longer exists. The list has been refreshed.");
        info.UserLikelyMissing.Should().BeTrue();
    }

    [Fact]
    public void FromExceptionShouldReturnValidationMessageWhenAvailable()
    {
        var info = UserDeletionErrorInfo.FromException(new ValidationException("Cannot delete the last administrator."));

        info.Message.Should().Be("Cannot delete the last administrator.");
        info.UserLikelyMissing.Should().BeFalse();
    }
}
