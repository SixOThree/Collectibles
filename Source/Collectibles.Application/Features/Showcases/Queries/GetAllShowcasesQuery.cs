using Collectibles.Application.Interfaces;
using Collectibles.Application.Mappings.Explicit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Showcases.Queries;

public record GetAllShowcasesQuery : IRequest<List<ShowcaseCardDto>>;

public class GetAllShowcasesQueryHandler : IRequestHandler<GetAllShowcasesQuery, List<ShowcaseCardDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IShowcaseMappingService _showcaseMappingService;
    private readonly ICurrentUserService _currentUserService;

    public GetAllShowcasesQueryHandler(
        IApplicationDbContext context,
        IShowcaseMappingService showcaseMappingService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _showcaseMappingService = showcaseMappingService;
        _currentUserService = currentUserService;
    }

    public async Task<List<ShowcaseCardDto>> Handle(GetAllShowcasesQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAdministrator)
        {
            throw new UnauthorizedAccessException("Only administrators can view all showcases.");
        }

        var showcases = await _context.Showcases
            .Include(s => s.ShowcaseTags)
                .ThenInclude(st => st.Tag)
            .Include(s => s.CollectibleItems.Where(ci => ci.Deleted == null))
            .Include(s => s.PreviewImage)
                .ThenInclude(p => p!.AttachmentContent)
            .Include(s => s.PreviewImage)
                .ThenInclude(p => p!.AttachmentPreview)
            .Where(s => s.Deleted == null)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return await _showcaseMappingService.MapManyToCardDtoAsync(showcases, cancellationToken);
    }
}
