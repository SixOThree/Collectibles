using System.Text.Json;

using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using Collectibles.Domain.ValueObjects.Templates;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.ContentDefinitions.Commands;

public class UpdateContentDefinitionCommand : IRequest
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsGlobal { get; set; }
    public long? ShowcaseId { get; set; }
    public bool HideAttachments { get; set; }
    public bool AllowMultipleEntries { get; set; }
    public string? BorderColor { get; set; }
    public string? Icon { get; set; }
    public List<FieldDefinitionDto> Fields { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

public class UpdateContentDefinitionCommandValidator : AbstractValidator<UpdateContentDefinitionCommand>
{
    public UpdateContentDefinitionCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Valid template ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Template name is required.")
            .MaximumLength(200).WithMessage("Template name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

        RuleFor(x => x.ShowcaseId)
            .NotNull().When(x => !x.IsGlobal)
            .WithMessage("A showcase must be selected for non-global templates.");

        RuleFor(x => x.ShowcaseId)
            .Null().When(x => x.IsGlobal)
            .WithMessage("Global templates cannot be associated with a specific showcase.");

        RuleFor(x => x.Fields)
            .NotEmpty().WithMessage("At least one field must be defined.")
            .When(x => !x.HideAttachments);

        RuleForEach(x => x.Fields).ChildRules(field =>
        {
            field.RuleFor(f => f.Name)
                .NotEmpty().WithMessage("Field name is required.")
                .Matches("^[a-zA-Z][a-zA-Z0-9_]*$").WithMessage("Field name must start with a letter and contain only alphanumeric characters and underscores.");

            field.RuleFor(f => f.Label)
                .NotEmpty().WithMessage("Field label is required.")
                .MaximumLength(100).WithMessage("Field label must not exceed 100 characters.");

            field.RuleFor(f => f.DisplayOrder)
                .GreaterThanOrEqualTo(0).WithMessage("Display order must be non-negative.");

            field.RuleFor(f => f.Options)
                .Must((fieldDto, options) => ValidateDropdownOptions(fieldDto))
                .WithMessage("Dropdown fields must have at least one option defined.");
        }).When(x => x.Fields.Any());

        RuleFor(x => x.Fields)
            .Must(HaveUniqueFieldNames).WithMessage("Field names must be unique within the template.")
            .When(x => x.Fields.Any());
    }

    private bool HaveUniqueFieldNames(List<FieldDefinitionDto> fields)
    {
        var names = fields.Select(f => f.Name.ToLower(System.Globalization.CultureInfo.CurrentCulture)).ToList();
        return names.Count == names.Distinct().Count();
    }

    private static bool ValidateDropdownOptions(FieldDefinitionDto field)
    {
        if (field.FieldType != FieldType.Dropdown)
        {
            return true; // Not a dropdown, no validation needed
        }

        if (!field.Options.TryGetValue("dropdownOptions", out var optionsObj))
        {
            return false; // Dropdown must have options
        }

        // Handle different types that might come from serialization
        List<string>? options = null;

        if (optionsObj is List<string> stringList)
        {
            options = stringList;
        }
        else if (optionsObj is System.Text.Json.JsonElement jsonElement)
        {
            try
            {
                options = jsonElement.Deserialize<List<string>>();
            }
            catch
            {
                return false;
            }
        }
        else if (optionsObj is string[] stringArray)
        {
            options = stringArray.ToList();
        }
        else if (optionsObj is IEnumerable<object> objectList)
        {
            // Handle cases where the list contains objects that can be converted to strings
            options = objectList.Select(o => o?.ToString() ?? string.Empty).ToList();
        }

        if (options != null)
        {
            // Must have at least one non-empty option
            return options.Any(o => !string.IsNullOrWhiteSpace(o));
        }

        return false;
    }
}

public class UpdateContentDefinitionCommandHandler : IRequestHandler<UpdateContentDefinitionCommand>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly IEventLogService _eventLogService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateContentDefinitionCommandHandler(
        IApplicationDbContextFactory contextFactory,
        IEventLogService eventLogService,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _eventLogService = eventLogService;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateContentDefinitionCommand request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var contentDefinition = await context.ContentDefinitions
            .FirstOrDefaultAsync(cd => cd.Id == request.Id, cancellationToken);

        if (contentDefinition == null)
        {
            throw new InvalidOperationException($"Template with ID {request.Id} not found.");
        }

        // Verify the current user can edit this template
        if (!_currentUserService.IsAdministrator)
        {
            if (contentDefinition.CreatedBy != _currentUserService.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to update this template.");
            }
        }

        // Check if another template with the same name exists in the same scope
        var existingTemplate = await context.ContentDefinitions
            .Where(cd => cd.Id != request.Id &&
                        cd.Name == request.Name &&
                        cd.IsGlobal == request.IsGlobal &&
                        cd.ShowcaseId == request.ShowcaseId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingTemplate != null)
        {
            var scope = request.IsGlobal ? "globally" : $"in showcase {request.ShowcaseId}";
            throw new InvalidOperationException($"Another template with the name '{request.Name}' already exists {scope}.");
        }

        // Capture old values for event logging
        var oldTemplate = contentDefinition.GetTemplateDefinition();
        var oldValues = new
        {
            Name = oldTemplate?.Name,
            Description = oldTemplate?.Description,
            IsActive = contentDefinition.IsActive,
            FieldCount = oldTemplate?.Fields?.Count ?? 0,
            Fields = oldTemplate?.Fields?.Select(f => new { f.Name, f.Label, f.FieldType, f.IsRequired, f.DisplayOrder }),
        };

        // Create the updated template definition
        var templateDefinition = new TemplateDefinition
        {
            Name = request.Name,
            Description = request.Description,
            AllowMultipleEntries = request.AllowMultipleEntries,
            Fields = request.Fields.Select(f => new FieldDefinition
            {
                Name = f.Name,
                Label = f.Label,
                FieldType = f.FieldType,
                IsRequired = f.IsRequired,
                DisplayOrder = f.DisplayOrder,
                Placeholder = f.Placeholder,
                DefaultValue = f.DefaultValue,
                HelpText = f.HelpText,
                ValidationRules = new FieldValidationRules
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
                Options = ConvertOptions(f.Options, f.FieldType),
            }).ToList(),
        };

        // Update the ContentDefinition entity
        contentDefinition.SetTemplateDefinition(templateDefinition);
        contentDefinition.IsActive = request.IsActive;
        contentDefinition.HideAttachments = request.HideAttachments;
        contentDefinition.IsGlobal = request.IsGlobal;
        contentDefinition.ShowcaseId = request.ShowcaseId;
        contentDefinition.BorderColor = request.BorderColor;
        contentDefinition.Icon = request.Icon;

        await context.SaveChangesAsync(cancellationToken);

        // Log the update event
        var newValues = new
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive,
            FieldCount = request.Fields.Count,
            Fields = request.Fields.Select(f => new { f.Name, f.Label, f.FieldType, f.IsRequired, f.DisplayOrder }),
        };

        await _eventLogService.LogEventAsync(
            EventAction.Update,
            nameof(ContentDefinition),
            contentDefinition.Id,
            contentDefinition.Name,
            oldValues,
            newValues,
            cancellationToken: cancellationToken);
    }

    private static Dictionary<string, object> ConvertOptions(Dictionary<string, object> options, FieldType fieldType)
    {
        var convertedOptions = new Dictionary<string, object>();

        foreach (var kvp in options)
        {
            if (kvp.Key == "dropdownOptions" && fieldType == FieldType.Dropdown)
            {
                List<string>? dropdownOptions = null;

                // Handle different types that might come from serialization
                if (kvp.Value is List<string> stringList)
                {
                    dropdownOptions = stringList;
                }
                else if (kvp.Value is System.Text.Json.JsonElement jsonElement)
                {
                    try
                    {
                        dropdownOptions = jsonElement.Deserialize<List<string>>();
                    }
                    catch
                    {
                        dropdownOptions = new List<string>();
                    }
                }
                else if (kvp.Value is string[] stringArray)
                {
                    dropdownOptions = stringArray.ToList();
                }
                else if (kvp.Value is IEnumerable<object> objectList)
                {
                    dropdownOptions = objectList.Select(o => o?.ToString() ?? string.Empty).ToList();
                }

                if (dropdownOptions != null)
                {
                    // Ensure dropdown options are stored as a clean list
                    convertedOptions[kvp.Key] = dropdownOptions.Where(o => !string.IsNullOrWhiteSpace(o)).ToList();
                }
            }
            else
            {
                convertedOptions[kvp.Key] = kvp.Value;
            }
        }

        return convertedOptions;
    }
}
