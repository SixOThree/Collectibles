using FluentValidation;

namespace Collectibles.Application.Features.CollectibleItems.Commands;

public class UpdateCollectibleItemCommandValidator : AbstractValidator<UpdateCollectibleItemCommand>
{
    public UpdateCollectibleItemCommandValidator()
    {
        RuleFor(v => v.Id)
            .GreaterThan(0).WithMessage("Id must be greater than 0.");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(v => v.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");
    }
}
