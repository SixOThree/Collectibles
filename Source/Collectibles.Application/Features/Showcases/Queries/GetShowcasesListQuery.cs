using Collectibles.Application.Interfaces;
using Collectibles.Application.Mappings.Explicit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Showcases.Queries;

public record GetShowcasesListQuery : IRequest<List<ShowcaseCardDto>>
{
    public string? UserId { get; init; }
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
        // Use provided UserId if available, otherwise fall back to CurrentUserService
        var userId = request.UserId ?? _currentUserService.UserId;

        var showcases = await _context.Showcases
            .Include(s => s.ShowcaseTags)
                .ThenInclude(st => st.Tag)
            .Include(s => s.CollectibleItems.Where(ci => ci.Deleted == null))
            .Include(s => s.PreviewImage)
                .ThenInclude(p => p!.AttachmentContent)
            .Include(s => s.PreviewImage)
                .ThenInclude(p => p!.AttachmentPreview)
            .Where(s => s.Deleted == null)
            .Where(s => request.UserId != null
                ? s.UserId == request.UserId // My Showcases: only the user's own showcases
                : !s.IsPrivate || s.UserId == userId) // Browse: public showcases and user's own showcases
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        // Use the mapping service to map showcases to DTOs
        return await _showcaseMappingService.MapManyToCardDtoAsync(showcases, cancellationToken);
    }
}
