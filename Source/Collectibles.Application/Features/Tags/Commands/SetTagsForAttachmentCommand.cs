using Collectibles.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Tags.Commands;

public class SetTagsForAttachmentCommand : IRequest<Unit>
{
    public long AttachmentId { get; set; }
    public List<long> TagIds { get; set; } = new();
}

public class SetTagsForAttachmentCommandHandler : IRequestHandler<SetTagsForAttachmentCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SetTagsForAttachmentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(SetTagsForAttachmentCommand request, CancellationToken cancellationToken)
    {
        var attachment = await _context.Attachments
            .Include(a => a.Tags)
            .FirstOrDefaultAsync(a => a.Id == request.AttachmentId, cancellationToken);

        if (attachment != null)
        {
            // Verify ownership
            var ownerUserIds = await _context.CollectibleItemAttachments
                .Where(cia => cia.AttachmentId == attachment.Id)
                .SelectMany(cia => cia.CollectibleItem.Showcases)
                .Select(s => s.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (!ownerUserIds.Contains(_currentUserService.UserId) && attachment.CreatedBy != _currentUserService.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to modify tags on this attachment.");
            }

            var tags = await _context.Tags.Where(t => request.TagIds.Contains(t.Id)).ToListAsync(cancellationToken);
            attachment.Tags.Clear();
            foreach (var tag in tags)
            {
                attachment.Tags.Add(tag);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
