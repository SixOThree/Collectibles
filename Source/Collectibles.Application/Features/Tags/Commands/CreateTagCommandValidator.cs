using Collectibles.Application.Interfaces;
using Collectibles.Domain.Constants;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Tags.Commands;

public class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateTagCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Tag name is required.")
            .MaximumLength(ApplicationConstants.ValidationLengths.ShortNameMaxLength)
            .WithMessage($"Tag name must not exceed {ApplicationConstants.ValidationLengths.ShortNameMaxLength} characters.")
            .MustAsync(BeUniqueName).WithMessage("A tag with this name already exists.");
    }

    private async Task<bool> BeUniqueName(string name, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim().ToLowerInvariant();

        return !await _context.Tags
            .AnyAsync(t => t.Name.ToLower() == normalizedName, cancellationToken);
    }
}
