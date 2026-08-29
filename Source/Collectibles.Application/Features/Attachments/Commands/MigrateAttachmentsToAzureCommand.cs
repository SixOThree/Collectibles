using Collectibles.Application.Features.Attachments.Dtos;

using FluentValidation;

using MediatR;

namespace Collectibles.Application.Features.Attachments.Commands;

public class MigrateAttachmentsToAzureCommand : IRequest<MigrationResult>
{
    public int BatchSize { get; set; } = 100;
    public bool SkipVerification { get; set; }
}

public class MigrateAttachmentsToAzureCommandValidator : AbstractValidator<MigrateAttachmentsToAzureCommand>
{
    public MigrateAttachmentsToAzureCommandValidator()
    {
        RuleFor(x => x.BatchSize)
            .InclusiveBetween(1, 1000)
            .WithMessage("Batch size must be between 1 and 1000.");
    }
}
