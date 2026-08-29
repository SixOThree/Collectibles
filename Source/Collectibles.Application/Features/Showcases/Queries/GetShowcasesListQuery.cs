using Collectibles.Application.Interfaces;
using Collectibles.Application.Mappings.Explicit;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Showcases.Queries;

public record GetShowcasesListQuery : IRequest<List<ShowcaseCardDto>>
{
    /// <summary>
    /// Gets a value indicating whether to restrict the result to the caller's own showcases
    /// ("My Showcases") rather than browsing public ones.
    /// </summary>
    /// <remarks>
    /// This replaced a caller-supplied <c>UserId</c>. Passing a foreign id returned that
    /// user's showcases without the <c>IsPrivate</c> filter, enumerating their private
    /// showcases. Identity is now always the authenticated principal.
    /// </remarks>
    public bool OwnedOnly { get; init; }
}

public class GetShowcasesListQueryHandler : IRequestHandler<GetShowcasesListQuery, List<ShowcaseCardDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IShowcaseMappingService _showcaseMappingService;

    public GetShowcasesListQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IShowcaseMappingService showcaseMappingService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _showcaseMappingService = showcaseMappingService;
    }

    public async Task<List<ShowcaseCardDto>> Handle(GetShowcasesListQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        var showcases = await _context.Showcases
            .Include(s => s.ShowcaseTags)
                .ThenInclude(st => st.Tag)
            .Include(s => s.CollectibleItems)

            // Deliberately no AttachmentContent include: card rendering needs metadata and
            // the thumbnail only, and loading originals here scales memory with total image
            // bytes rather than card count.
            .Include(s => s.PreviewImage)
                .ThenInclude(p => p!.AttachmentPreview)
            .Where(s => request.OwnedOnly
                ? s.UserId == userId // My Showcases: only the caller's own showcases
                : !s.IsPrivate || s.UserId == userId) // Browse: public showcases and the caller's own
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        // Use the mapping service to map showcases to DTOs
        return await _showcaseMappingService.MapManyToCardDtoAsync(showcases, cancellationToken);
    }
}
