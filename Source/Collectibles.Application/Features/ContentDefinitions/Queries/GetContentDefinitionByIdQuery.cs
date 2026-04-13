using Collectibles.Application.Features.ContentDefinitions.Commands;
using Collectibles.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.ContentDefinitions.Queries;

public class GetContentDefinitionByIdQuery : IRequest<ContentDefinitionDto?>
{
    public long Id { get; set; }

    public GetContentDefinitionByIdQuery(long id)
    {
        Id = id;
    }
}

public class GetContentDefinitionByIdQueryHandler : IRequestHandler<GetContentDefinitionByIdQuery, ContentDefinitionDto?>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ICurrentUserService _currentUserService;

    public GetContentDefinitionByIdQueryHandler(
        IApplicationDbContextFactory contextFactory,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    public async Task<ContentDefinitionDto?> Handle(GetContentDefinitionByIdQuery request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var contentDefinition = await context.ContentDefinitions
            .Include(cd => cd.CollectibleItems)
            .Include(cd => cd.Showcase)
            .FirstOrDefaultAsync(cd => cd.Id == request.Id, cancellationToken);

        if (contentDefinition == null)
        {
            return null;
        }

        // Non-global templates are only visible if the user owns the showcase
        if (!contentDefinition.IsGlobal && contentDefinition.ShowcaseId.HasValue)
        {
            var showcase = contentDefinition.Showcase;
            if (showcase != null && showcase.UserId != _currentUserService.UserId)
            {
                return null;
            }
        }

        var templateDefinition = contentDefinition.GetTemplateDefinition();

        var dto = new ContentDefinitionDto
        {
            Id = contentDefinition.Id,
            Name = contentDefinition.Name ?? string.Empty,
            Description = contentDefinition.Description,
            IsActive = contentDefinition.IsActive,
            HideAttachments = contentDefinition.HideAttachments,
            IsGlobal = contentDefinition.IsGlobal,
            ShowcaseId = contentDefinition.ShowcaseId,
            ShowcaseName = contentDefinition.Showcase?.Name,
            BorderColor = contentDefinition.BorderColor,
            Icon = contentDefinition.Icon,
            AllowMultipleEntries = templateDefinition?.AllowMultipleEntries ?? false,
            ItemCount = contentDefinition.CollectibleItems.Count,
            CreatedBy = contentDefinition.CreatedBy,
            Created = contentDefinition.Created,
            LastModified = contentDefinition.LastModified,
            Fields = templateDefinition?.Fields.Select(f => new FieldDefinitionDto
            {
                Name = f.Name,
                Label = f.Label,
                FieldType = f.FieldType,
                IsRequired = f.IsRequired,
                DisplayOrder = f.DisplayOrder,
                Placeholder = f.Placeholder,
                DefaultValue = f.DefaultValue,
                HelpText = f.HelpText,
                ValidationRules = new FieldValidationRulesDto
                {
                    MinLength = f.ValidationRules.MinLength,
                    MaxLength = f.ValidationRules.MaxLength,
                    Pattern = f.ValidationRules.Pattern,
                    ErrorMessage = f.ValidationRules.ErrorMessage,
                    MinValue = f.ValidationRules.MinValue,
                    MaxValue = f.ValidationRules.MaxValue,
                    MinDate = f.ValidationRules.MinDate,
                    MaxDate = f.ValidationRules.MaxDate,
                    AllowDecimals = f.ValidationRules.AllowDecimals,
                    DecimalPlaces = f.ValidationRules.DecimalPlaces,
                },
                Options = f.Options,
            }).OrderBy(f => f.DisplayOrder).ToList() ?? new List<FieldDefinitionDto>(),
        };

        return dto;
    }
}
