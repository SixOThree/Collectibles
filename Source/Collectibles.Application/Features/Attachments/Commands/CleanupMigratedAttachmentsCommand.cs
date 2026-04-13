using Collectibles.Application.Features.Attachments.Dtos;
using FluentValidation;
using MediatR;

namespace Collectibles.Application.Features.Attachments.Commands;

public class CleanupMigratedAttachmentsCommand : IRequest<CleanupResult>
{
    public bool OnlyVerified { get; set; } = true;
    public int RetentionDays { get; set; } = 7;
    public int BatchSize { get; set; } = 100;
    public bool PreviewOnly { get; set; }
}

public class CleanupMigratedAttachmentsCommandValidator : AbstractValidator<CleanupMigratedAttachmentsCommand>
{
    public CleanupMigratedAttachmentsCommandValidator()
    {
        RuleFor(x => x.RetentionDays)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Retention days must be at least 1.");

        RuleFor(x => x.BatchSize)
            .InclusiveBetween(1, 1000)
            .WithMessage("Batch size must be between 1 and 1000.");
    }
}
