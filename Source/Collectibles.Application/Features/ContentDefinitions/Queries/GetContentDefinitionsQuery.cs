using Collectibles.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.ContentDefinitions.Queries;

public class GetContentDefinitionsQuery : IRequest<List<ContentDefinitionListDto>>
{
    public bool? IsActive { get; set; }

    /// <summary>
    /// Gets or sets if provided, filters templates to only global templates and templates for this specific showcase.
    /// </summary>
    public long? ShowcaseId { get; set; }

    /// <summary>
    /// Gets or sets if true, only returns global templates. If false, only returns showcase-specific templates.
    /// </summary>
    public bool? IsGlobal { get; set; }
}

public class GetContentDefinitionsQueryHandler : IRequestHandler<GetContentDefinitionsQuery, List<ContentDefinitionListDto>>
{
    private readonly IApplicationDbContextFactory _contextFactory;

    public GetContentDefinitionsQueryHandler(IApplicationDbContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<ContentDefinitionListDto>> Handle(GetContentDefinitionsQuery request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.ContentDefinitions
            .Include(cd => cd.CollectibleItems)
                .ThenInclude(ci => ci.Showcases)
            .Include(cd => cd.Showcase)
            .AsQueryable();

        if (request.IsActive.HasValue)
        {
            query = query.Where(cd => cd.IsActive == request.IsActive.Value);
        }

        // Filter by showcase - show global templates and templates for specific showcase
        if (request.ShowcaseId.HasValue)
        {
            query = query.Where(cd => cd.IsGlobal || cd.ShowcaseId == request.ShowcaseId.Value);
        }

        // Filter by IsGlobal flag if specified
        if (request.IsGlobal.HasValue)
        {
            query = query.Where(cd => cd.IsGlobal == request.IsGlobal.Value);
        }

        var contentDefinitions = await query
            .OrderBy(cd => cd.Name)
            .ToListAsync(cancellationToken);

        // Process the field count after materialization since GetTemplateDefinition() can't be translated to SQL
        var result = contentDefinitions.Select(cd =>
        {
            var templateDef = cd.GetTemplateDefinition();
            return new ContentDefinitionListDto
            {
                Id = cd.Id,
                Name = cd.Name ?? string.Empty,
                Description = cd.Description,
                IsActive = cd.IsActive,
                IsDefault = cd.IsDefault,
                HideAttachments = cd.HideAttachments,
                ItemDetailPreviewHeight = cd.ItemDetailPreviewHeight,
                IsGlobal = cd.IsGlobal,
                ShowcaseId = cd.ShowcaseId,
                ShowcaseName = cd.Showcase?.Name,
                BorderColor = cd.BorderColor,
                Icon = cd.Icon,
                AllowMultipleEntries = templateDef?.AllowMultipleEntries ?? false,
                FieldCount = templateDef?.Fields.Count ?? 0,
                ItemCount = request.ShowcaseId.HasValue
                    ? cd.CollectibleItems.Count(ci => ci.Showcases.Any(s => s.Id == request.ShowcaseId.Value))
                    : cd.CollectibleItems.Count,
                CreatedBy = cd.CreatedBy,
                Created = cd.Created,
                LastModified = cd.LastModified,
            };
        }).ToList();

        return result;
    }
}
