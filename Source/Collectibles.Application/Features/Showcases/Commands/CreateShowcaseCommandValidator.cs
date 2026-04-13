using Collectibles.Domain.Constants;
using FluentValidation;

namespace Collectibles.Application.Features.Showcases.Commands;

public class CreateShowcaseCommandValidator : AbstractValidator<CreateShowcaseCommand>
{
    public CreateShowcaseCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(ApplicationConstants.ValidationLengths.NameMaxLength)
            .WithMessage($"Name must not exceed {ApplicationConstants.ValidationLengths.NameMaxLength} characters.");

        RuleFor(x => x.Description)
            .MaximumLength(ApplicationConstants.ValidationLengths.ExtendedDescriptionMaxLength)
            .WithMessage($"Description must not exceed {ApplicationConstants.ValidationLengths.ExtendedDescriptionMaxLength} characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.PreviewImageId)
            .GreaterThan(0).WithMessage("PreviewImageId must be a valid ID.")
            .When(x => x.PreviewImageId.HasValue);

        RuleFor(x => x.TagIds)
            .Must(tagIds => tagIds.All(id => id > 0))
            .WithMessage("All tag IDs must be valid.")
            .When(x => x.TagIds.Count != 0);
    }
}
