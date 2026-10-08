using Collectibles.Domain.Constants;

using FluentValidation;

namespace Collectibles.Application.Features.CollectibleItems.Commands;

public class CreateCollectibleItemCommandValidator : AbstractValidator<CreateCollectibleItemCommand>
{
    public CreateCollectibleItemCommandValidator()
    {
        RuleFor(v => v.ShowcaseId)
            .GreaterThan(0).WithMessage("Showcase ID must be greater than 0.");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(ApplicationConstants.ValidationLengths.NameMaxLength)
            .WithMessage($"Name must not exceed {ApplicationConstants.ValidationLengths.NameMaxLength} characters.");

        RuleFor(v => v.Description)
            .MaximumLength(ApplicationConstants.ValidationLengths.DescriptionMaxLength)
            .WithMessage($"Description must not exceed {ApplicationConstants.ValidationLengths.DescriptionMaxLength} characters.");
    }
}
