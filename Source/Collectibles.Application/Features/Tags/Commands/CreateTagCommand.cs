using Collectibles.Application.Interfaces;
using Collectibles.Application.Mappings.Explicit;
using Collectibles.Domain.Entities;
using MediatR;

namespace Collectibles.Application.Features.Tags.Commands;

public record CreateTagCommand : IRequest<TagDto>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public class CreateTagCommandHandler : IRequestHandler<CreateTagCommand, TagDto>
{
    private readonly IApplicationDbContextFactory _contextFactory;

    public CreateTagCommandHandler(IApplicationDbContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<TagDto> Handle(CreateTagCommand request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var tag = new Tag
        {
            Name = request.Name.Trim(),
        };

        context.Tags.Add(tag);
        await context.SaveChangesAsync(cancellationToken);

        return request.Description != null
            ? tag.ToDtoWithDescription(request.Description)
            : tag.ToDto();
    }
}
