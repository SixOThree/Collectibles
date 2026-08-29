using System.Globalization;

using Collectibles.Application.Common.Models;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.ValueObjects.Templates;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.CollectibleItems.Queries;

public class GetItemsByTemplateQuery : IRequest<GetItemsByTemplateResult?>
{
    public long ContentDefinitionId { get; set; }
    public long ShowcaseId { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class GetItemsByTemplateResult
{
    public long ContentDefinitionId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public List<FieldDefinition> Fields { get; set; } = new();
    public PaginatedList<TemplatedItemRowDto> Items { get; set; } = null!;
}

public class TemplatedItemRowDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public DateTime? LastModified { get; set; }
    public int ChildCount { get; set; }
    public int AttachmentCount { get; set; }
    public int EntryCount { get; set; }
    public Dictionary<string, object?> FieldValues { get; set; } = new();
    public List<Dictionary<string, object?>> AllEntries { get; set; } = new();
}

public class GetItemsByTemplateQueryHandler : IRequestHandler<GetItemsByTemplateQuery, GetItemsByTemplateResult?>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ICurrentUserService _currentUserService;

    public GetItemsByTemplateQueryHandler(
        IApplicationDbContextFactory contextFactory,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    public async Task<GetItemsByTemplateResult?> Handle(GetItemsByTemplateQuery request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // This returns item names and every template field value in the showcase, so it
        // follows the showcase's visibility rather than returning whatever id was asked for.
        var userId = _currentUserService.UserId;
        var showcaseVisible = await context.Showcases
            .AnyAsync(
                s => s.Id == request.ShowcaseId && (!s.IsPrivate || s.UserId == userId),
                cancellationToken);

        if (!showcaseVisible && !_currentUserService.IsAdministrator)
        {
            throw new UnauthorizedAccessException("You are not authorized to view this showcase's items.");
        }

        var contentDefinition = await context.ContentDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(cd => cd.Id == request.ContentDefinitionId, cancellationToken);

        if (contentDefinition == null)
        {
            return null;
        }

        var templateDef = contentDefinition.GetTemplateDefinition();

        var items = await context.CollectibleItems
            .AsNoTracking()
            .Include(ci => ci.Children)
            .Include(ci => ci.CollectibleItemAttachments)
            .Where(ci => ci.ContentDefinitionId == request.ContentDefinitionId
                         && ci.Showcases.Any(s => s.Id == request.ShowcaseId))
            .ToListAsync(cancellationToken);

        var dtos = items.Select(ci =>
        {
            var (fieldValues, entryCount, allEntries) = GetFieldValuesAndEntryCount(ci);
            return new TemplatedItemRowDto
            {
                Id = ci.Id,
                Name = ci.Name ?? string.Empty,
                Created = ci.Created ?? DateTime.MinValue,
                LastModified = ci.LastModified,
                ChildCount = ci.Children?.Count ?? 0,
                AttachmentCount = ci.CollectibleItemAttachments?.Count ?? 0,
                EntryCount = entryCount,
                FieldValues = fieldValues,
                AllEntries = allEntries,
            };
        }).ToList();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            dtos = dtos.Where(d =>
                d.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                d.FieldValues.Values.Any(v => v?.ToString()?.Contains(term, StringComparison.OrdinalIgnoreCase) == true))
            .ToList();
        }

        // Apply sorting
        var fields = templateDef?.Fields ?? new List<FieldDefinition>();
        dtos = ApplySort(dtos, fields, request.SortBy, request.SortDescending);

        // Paginate
        var totalCount = dtos.Count;
        var pagedItems = dtos
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new GetItemsByTemplateResult
        {
            ContentDefinitionId = contentDefinition.Id,
            TemplateName = contentDefinition.Name ?? string.Empty,
            Fields = fields.OrderBy(f => f.DisplayOrder).ToList(),
            Items = new PaginatedList<TemplatedItemRowDto>(pagedItems, totalCount, request.PageNumber, request.PageSize),
        };
    }

    private static (Dictionary<string, object?> FieldValues, int EntryCount, List<Dictionary<string, object?>> AllEntries) GetFieldValuesAndEntryCount(Domain.Entities.CollectibleItem entity)
    {
        var contentValue = entity.ContentValue;
        if (!string.IsNullOrWhiteSpace(contentValue) && contentValue.TrimStart().StartsWith('['))
        {
            // Multi-entry: return first entry's values for display, count, and all entries for export
            var entries = entity.GetFieldValueEntries();
            var firstValues = entries.Count > 0
                ? new Dictionary<string, object?>(entries.Entries[0].Values)
                : new Dictionary<string, object?>();
            var allEntries = entries.Entries
                .OrderBy(e => e.SortOrder)
                .Select(e => new Dictionary<string, object?>(e.Values))
                .ToList();
            return (firstValues, entries.Count, allEntries);
        }

        var fieldValues = entity.GetFieldValues();
        var result = new Dictionary<string, object?>();
        foreach (var kvp in fieldValues.Values)
        {
            result[kvp.Key] = kvp.Value.Value;
        }

        return (result, 0, new List<Dictionary<string, object?>> { result });
    }

    private static List<TemplatedItemRowDto> ApplySort(
        List<TemplatedItemRowDto> items,
        List<FieldDefinition> fields,
        string? sortBy,
        bool descending)
    {
        if (string.IsNullOrEmpty(sortBy))
        {
            return descending
                ? items.OrderByDescending(i => i.Name).ToList()
                : items.OrderBy(i => i.Name).ToList();
        }

        return sortBy switch
        {
            "name" => descending
                ? items.OrderByDescending(i => i.Name).ToList()
                : items.OrderBy(i => i.Name).ToList(),
            "created" => descending
                ? items.OrderByDescending(i => i.Created).ToList()
                : items.OrderBy(i => i.Created).ToList(),
            "lastModified" => descending
                ? items.OrderByDescending(i => i.LastModified).ToList()
                : items.OrderBy(i => i.LastModified).ToList(),
            "childCount" => descending
                ? items.OrderByDescending(i => i.ChildCount).ToList()
                : items.OrderBy(i => i.ChildCount).ToList(),
            "attachmentCount" => descending
                ? items.OrderByDescending(i => i.AttachmentCount).ToList()
                : items.OrderBy(i => i.AttachmentCount).ToList(),
            "entryCount" => descending
                ? items.OrderByDescending(i => i.EntryCount).ToList()
                : items.OrderBy(i => i.EntryCount).ToList(),
            _ => ApplySortByField(items, fields, sortBy, descending),
        };
    }

    private static List<TemplatedItemRowDto> ApplySortByField(
        List<TemplatedItemRowDto> items,
        List<FieldDefinition> fields,
        string fieldName,
        bool descending)
    {
        var fieldDef = fields.FirstOrDefault(f => f.Name == fieldName);
        var fieldType = fieldDef?.FieldType ?? FieldType.Text;

        // Use type-aware comparison for sorting
        Func<TemplatedItemRowDto, object?> keySelector = fieldType switch
        {
            FieldType.Number => i => GetDecimalValue(i.FieldValues, fieldName),
            FieldType.Date or FieldType.DateTime => i => GetDateTimeValue(i.FieldValues, fieldName),
            FieldType.Boolean => i => GetBoolValue(i.FieldValues, fieldName),
            _ => i => i.FieldValues.TryGetValue(fieldName, out var v) ? v?.ToString() ?? string.Empty : string.Empty,
        };

        return descending
            ? items.OrderByDescending(keySelector).ToList()
            : items.OrderBy(keySelector).ToList();
    }

    private static decimal? GetDecimalValue(Dictionary<string, object?> fieldValues, string fieldName)
    {
        if (!fieldValues.TryGetValue(fieldName, out var value) || value == null)
        {
            return null;
        }

        if (value is decimal d)
        {
            return d;
        }

        // Same invariant-parsing rule as FieldValue: these values come from the
        // invariant-format JSON stored in ContentValue.
        if (decimal.TryParse(value.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }

    private static DateTime? GetDateTimeValue(Dictionary<string, object?> fieldValues, string fieldName)
    {
        if (!fieldValues.TryGetValue(fieldName, out var value) || value == null)
        {
            return null;
        }

        if (value is DateTime dt)
        {
            return dt;
        }

        if (DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
        {
            return result;
        }

        return null;
    }

    private static bool? GetBoolValue(Dictionary<string, object?> fieldValues, string fieldName)
    {
        if (!fieldValues.TryGetValue(fieldName, out var value) || value == null)
        {
            return null;
        }

        if (value is bool b)
        {
            return b;
        }

        if (bool.TryParse(value.ToString(), out var result))
        {
            return result;
        }

        return null;
    }
}
