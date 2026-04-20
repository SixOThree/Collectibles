using System.Text.Json;

using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using Collectibles.Domain.ValueObjects.Templates;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.ContentDefinitions.Commands;

public class CreateContentDefinitionCommand : IRequest<long>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsGlobal { get; set; }
    public long? ShowcaseId { get; set; }
    public bool HideAttachments { get; set; }
    public int? ItemDetailPreviewHeight { get; set; }
    public bool AllowMultipleEntries { get; set; }
    public string? BorderColor { get; set; }
    public string? Icon { get; set; }
    public List<FieldDefinitionDto> Fields { get; set; } = new();
}

public class FieldDefinitionDto
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public FieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public string? Placeholder { get; set; }
    public string? DefaultValue { get; set; }
    public string? HelpText { get; set; }
    public FieldValidationRulesDto ValidationRules { get; set; } = new();
    public Dictionary<string, object> Options { get; set; } = new();
}

public class FieldValidationRulesDto
{
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? Pattern { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
    public bool AllowDecimals { get; set; } = true;
    public int? DecimalPlaces { get; set; }
}

public class CreateContentDefinitionCommandValidator : AbstractValidator<CreateContentDefinitionCommand>
{
    public CreateContentDefinitionCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Template name is required.")
            .MaximumLength(200).WithMessage("Template name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

        RuleFor(x => x.ItemDetailPreviewHeight)
            .InclusiveBetween(100, 500)
            .When(x => x.ItemDetailPreviewHeight.HasValue)
            .WithMessage("Item page preview height override must be between 100 and 500 pixels when provided.");

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

public class CreateContentDefinitionCommandHandler : IRequestHandler<CreateContentDefinitionCommand, long>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly IEventLogService _eventLogService;
    private readonly ICurrentUserService _currentUserService;

    public CreateContentDefinitionCommandHandler(
        IApplicationDbContextFactory contextFactory,
        IEventLogService eventLogService,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _eventLogService = eventLogService;
        _currentUserService = currentUserService;
    }

    public async Task<long> Handle(CreateContentDefinitionCommand request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // For non-global templates, verify the current user owns the showcase
        if (!request.IsGlobal && request.ShowcaseId.HasValue)
        {
            var showcase = await context.Showcases
                .FirstOrDefaultAsync(s => s.Id == request.ShowcaseId.Value, cancellationToken);

            if (showcase == null || showcase.UserId != _currentUserService.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to create templates for this showcase.");
            }
        }

        // Check if a template with the same name already exists in the same scope
        var existingTemplate = await context.ContentDefinitions
            .FirstOrDefaultAsync(
                cd => cd.Name == request.Name &&
                                      cd.IsGlobal == request.IsGlobal &&
                                      cd.ShowcaseId == request.ShowcaseId, cancellationToken);

        if (existingTemplate != null)
        {
            var scope = request.IsGlobal ? "globally" : $"in showcase {request.ShowcaseId}";
            throw new InvalidOperationException($"A template with the name '{request.Name}' already exists {scope}.");
        }

        // Create the template definition
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

        // Create the ContentDefinition entity
        var contentDefinition = new ContentDefinition();
        contentDefinition.SetTemplateDefinition(templateDefinition);
        contentDefinition.IsActive = true;
        contentDefinition.HideAttachments = request.HideAttachments;
        contentDefinition.ItemDetailPreviewHeight = request.ItemDetailPreviewHeight;
        contentDefinition.IsGlobal = request.IsGlobal;
        contentDefinition.ShowcaseId = request.ShowcaseId;
        contentDefinition.BorderColor = request.BorderColor;
        contentDefinition.Icon = request.Icon;

        context.ContentDefinitions.Add(contentDefinition);
        await context.SaveChangesAsync(cancellationToken);

        // Log the creation event
        await _eventLogService.LogEventAsync(
            EventAction.Create,
            nameof(ContentDefinition),
            contentDefinition.Id,
            contentDefinition.Name,
            null,
            new
            {
                Name = request.Name,
                Description = request.Description,
                FieldCount = request.Fields.Count,
                Fields = request.Fields.Select(f => new { f.Name, f.Label, f.FieldType, f.IsRequired, f.DisplayOrder }),
                IsActive = true,
                ItemDetailPreviewHeight = request.ItemDetailPreviewHeight,
            },
            cancellationToken: cancellationToken);

        return contentDefinition.Id;
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
