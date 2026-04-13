using FluentValidation;

namespace Collectibles.Application.Features.Showcases.Commands;

public class UpdateShowcaseCommandValidator : AbstractValidator<UpdateShowcaseCommand>
{
    public UpdateShowcaseCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id must be a valid ID.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
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
