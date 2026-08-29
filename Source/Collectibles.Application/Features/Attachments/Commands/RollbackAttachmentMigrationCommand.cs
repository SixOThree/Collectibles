using Collectibles.Application.Features.Attachments.Dtos;

using FluentValidation;

using MediatR;

namespace Collectibles.Application.Features.Attachments.Commands;

public class RollbackAttachmentMigrationCommand : IRequest<RollbackResult>
{
    public List<long> AttachmentIds { get; set; } = new();
    public bool DeleteFromStorage { get; set; } = true;
    public int BatchSize { get; set; } = 100;
}

public class RollbackAttachmentMigrationCommandValidator : AbstractValidator<RollbackAttachmentMigrationCommand>
{
    public RollbackAttachmentMigrationCommandValidator()
    {
        RuleFor(x => x.AttachmentIds)
            .NotNull()
            .WithMessage("AttachmentIds cannot be null");

        RuleForEach(x => x.AttachmentIds)
            .GreaterThan(0)
            .WithMessage("Attachment ID must be greater than 0");

        RuleFor(x => x.BatchSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(1000)
            .WithMessage("Batch size must be between 1 and 1000");
    }
}
