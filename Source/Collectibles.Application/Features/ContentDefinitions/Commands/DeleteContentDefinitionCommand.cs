using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.ContentDefinitions.Commands;

public class DeleteContentDefinitionCommand : IRequest
{
    public long Id { get; set; }
}

public class DeleteContentDefinitionCommandValidator : AbstractValidator<DeleteContentDefinitionCommand>
{
    public DeleteContentDefinitionCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Valid template ID is required.");
    }
}

public class DeleteContentDefinitionCommandHandler : IRequestHandler<DeleteContentDefinitionCommand>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly IEventLogService _eventLogService;
    private readonly ICurrentUserService _currentUserService;

    public DeleteContentDefinitionCommandHandler(
        IApplicationDbContextFactory contextFactory,
        IEventLogService eventLogService,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _eventLogService = eventLogService;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteContentDefinitionCommand request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var contentDefinition = await context.ContentDefinitions
            .Include(cd => cd.CollectibleItems)
            .FirstOrDefaultAsync(cd => cd.Id == request.Id, cancellationToken);

        if (contentDefinition == null)
        {
            throw new InvalidOperationException($"Template with ID {request.Id} not found.");
        }

        // Verify the current user can delete this template
        if (!_currentUserService.IsAdministrator)
        {
            if (contentDefinition.CreatedBy != _currentUserService.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to delete this template.");
            }
        }

        // Check if any collectible items are using this template
        if (contentDefinition.CollectibleItems.Count != 0)
        {
            throw new InvalidOperationException($"Cannot delete template '{contentDefinition.Name}' because it is being used by {contentDefinition.CollectibleItems.Count} collectible item(s).");
        }

        // Capture template information for event logging
        var templateDef = contentDefinition.GetTemplateDefinition();
        var deletedTemplateInfo = new
        {
            Name = contentDefinition.Name,
            Description = templateDef?.Description,
            IsActive = contentDefinition.IsActive,
            IsDefault = contentDefinition.IsDefault,
            FieldCount = templateDef?.Fields?.Count ?? 0,
            Fields = templateDef?.Fields?.Select(f => new { f.Name, f.Label, f.FieldType }),
        };

        context.ContentDefinitions.Remove(contentDefinition);
        await context.SaveChangesAsync(cancellationToken);

        // Log the delete event
        await _eventLogService.LogEventAsync(
            EventAction.Delete,
            nameof(ContentDefinition),
            contentDefinition.Id,
            contentDefinition.Name,
            deletedTemplateInfo,
            null,
            cancellationToken: cancellationToken);
    }
}
