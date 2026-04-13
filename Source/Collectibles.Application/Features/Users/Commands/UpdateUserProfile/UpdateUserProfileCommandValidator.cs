using FluentValidation;

namespace Collectibles.Application.Features.Users.Commands.UpdateUserProfile;

public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(v => v.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(v => v.FirstName)
            .MaximumLength(50).WithMessage("First name must not exceed 50 characters.");

        RuleFor(v => v.LastName)
            .MaximumLength(50).WithMessage("Last name must not exceed 50 characters.");
    }
}
