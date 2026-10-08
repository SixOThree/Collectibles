using Collectibles.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.ContentDefinitions.Queries;

public class ContentDefinitionSelectDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool AllowMultipleEntries { get; set; }
}

public class GetActiveContentDefinitionsQuery : IRequest<List<ContentDefinitionSelectDto>>
{
}

public class GetActiveContentDefinitionsQueryHandler : IRequestHandler<GetActiveContentDefinitionsQuery, List<ContentDefinitionSelectDto>>
{
    private readonly IApplicationDbContextFactory _contextFactory;

    public GetActiveContentDefinitionsQueryHandler(IApplicationDbContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<ContentDefinitionSelectDto>> Handle(GetActiveContentDefinitionsQuery request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var entities = await context.ContentDefinitions
            .Where(cd => cd.IsActive)
            .OrderBy(cd => cd.Name)
            .ToListAsync(cancellationToken);

        var contentDefinitions = entities.Select(cd =>
        {
            var templateDef = cd.GetTemplateDefinition();
            return new ContentDefinitionSelectDto
            {
                Id = cd.Id,
                Name = cd.Name ?? string.Empty,
                Description = cd.Description,
                IsDefault = cd.IsDefault,
                AllowMultipleEntries = templateDef?.AllowMultipleEntries ?? false,
            };
        }).ToList();

        return contentDefinitions;
    }
}
