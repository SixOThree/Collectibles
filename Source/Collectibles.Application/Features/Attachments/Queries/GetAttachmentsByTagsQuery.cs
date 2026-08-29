using Collectibles.Application.Interfaces;
using Collectibles.Application.Mappings.Explicit;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Attachments.Queries;

public class GetAttachmentsByTagsQuery : IRequest<List<AttachmentBriefDto>>
{
    public List<long> TagIds { get; set; } = new();
}

public class GetAttachmentsByTagsQueryHandler : IRequestHandler<GetAttachmentsByTagsQuery, List<AttachmentBriefDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAttachmentsByTagsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<AttachmentBriefDto>> Handle(GetAttachmentsByTagsQuery request, CancellationToken cancellationToken)
    {
        if (request.TagIds == null || request.TagIds.Count == 0)
        {
            return new List<AttachmentBriefDto>();
        }

        var collectibleItems = await _context.CollectibleItems
            .Include(ci => ci.CollectibleItemAttachments)
                .ThenInclude(cia => cia.Attachment)
                    .ThenInclude(a => a!.AttachmentPreview)
            .Include(ci => ci.CollectibleItemTags)
            .Include(ci => ci.Showcases)
            .Where(ci => ci.CollectibleItemTags.Any(cit => request.TagIds.Contains(cit.TagId)))
            .Where(ci => ci.Showcases.Any(s => s.UserId == _currentUserService.UserId || !s.IsPrivate))
            .ToListAsync(cancellationToken);

        var attachments = collectibleItems
            .SelectMany(ci => ci.CollectibleItemAttachments)
            .Select(cia => cia.Attachment)
            .Where(a => a != null)
            .ToList();

        // Use explicit mapping extension method for brief DTOs
        return attachments.Select(a => a!.ToBriefDto()).ToList();
    }
}
