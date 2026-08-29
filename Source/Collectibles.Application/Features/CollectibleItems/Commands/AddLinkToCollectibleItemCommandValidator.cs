using Collectibles.Application.Interfaces;
using Collectibles.Domain.Constants;

using FluentValidation;

namespace Collectibles.Application.Features.CollectibleItems.Commands;

/// <summary>
/// Rejects link URLs the server must never fetch before they are ever persisted, so a
/// blocked target is reported to the user immediately instead of failing in the background.
/// </summary>
public class AddLinkToCollectibleItemCommandValidator : AbstractValidator<AddLinkToCollectibleItemCommand>
{
    private readonly IUrlEgressGuard _egressGuard;

    public AddLinkToCollectibleItemCommandValidator(IUrlEgressGuard egressGuard)
    {
        _egressGuard = egressGuard;

        RuleFor(v => v.CollectibleItemId)
            .GreaterThan(0).WithMessage("A collectible item is required.");

        RuleFor(v => v.Url)
            .NotEmpty().WithMessage("A URL is required.")
            .MaximumLength(ApplicationConstants.ValidationLengths.UrlMaxLength)
            .WithMessage($"URL must not exceed {ApplicationConstants.ValidationLengths.UrlMaxLength} characters.")
            .CustomAsync(ValidateEgressAsync);
    }

    private async Task ValidateEgressAsync(string url, ValidationContext<AddLinkToCollectibleItemCommand> context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        var result = await _egressGuard.ValidateAsync(url, cancellationToken);
        if (!result.IsAllowed)
        {
            context.AddFailure(nameof(AddLinkToCollectibleItemCommand.Url), result.Reason);
        }
    }
}
