using Collectibles.Application.Interfaces;
using Collectibles.Application.Mappings.Explicit;
using Collectibles.Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Tags.Commands;

public record CreateTagCommand : IRequest<TagDto>
{
    public string Name { get; init; } = string.Empty;
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

        var name = request.Name.Trim();

        // Tag names carry a unique index. The validator reports the duplicate for a normal
        // request; this handles the concurrent-create race by returning the winner rather
        // than failing or creating a second row.
        var existing = await context.Tags
            .FirstOrDefaultAsync(t => t.Name == name, cancellationToken);

        if (existing != null)
        {
            return existing.ToDto();
        }

        var tag = new Tag
        {
            Name = name,
        };

        context.Tags.Add(tag);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            context.Tags.Remove(tag);

            var raced = await context.Tags
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Name == name, cancellationToken);

            if (raced == null)
            {
                throw;
            }

            return raced.ToDto();
        }

        return tag.ToDto();
    }
}
