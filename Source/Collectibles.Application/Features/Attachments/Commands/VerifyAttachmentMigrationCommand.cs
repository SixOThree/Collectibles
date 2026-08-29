using Collectibles.Application.Features.Attachments.Dtos;

using FluentValidation;

using MediatR;

namespace Collectibles.Application.Features.Attachments.Commands;

public class VerifyAttachmentMigrationCommand : IRequest<VerificationResult>
{
    public int BatchSize { get; set; } = 100;
    public bool VerifyFileSize { get; set; } = true;
    public bool VerifyFileExists { get; set; } = true;
}

public class VerifyAttachmentMigrationCommandValidator : AbstractValidator<VerifyAttachmentMigrationCommand>
{
    public VerifyAttachmentMigrationCommandValidator()
    {
        RuleFor(x => x.BatchSize)
            .InclusiveBetween(1, 1000)
            .WithMessage("Batch size must be between 1 and 1000.");
    }
}
