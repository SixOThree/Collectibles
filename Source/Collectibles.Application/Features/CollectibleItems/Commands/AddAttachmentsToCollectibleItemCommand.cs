using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.CollectibleItems.Commands;

public class AddAttachmentsToCollectibleItemCommand : IRequest<Unit>
{
    public long CollectibleItemId { get; set; }
    public List<long> AttachmentIds { get; set; } = new();
}

public class AddAttachmentsToCollectibleItemCommandHandler : IRequestHandler<AddAttachmentsToCollectibleItemCommand, Unit>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ICurrentUserService _currentUserService;

    public AddAttachmentsToCollectibleItemCommandHandler(IApplicationDbContextFactory contextFactory, ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(AddAttachmentsToCollectibleItemCommand request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var collectibleItem = await context.CollectibleItems
            .Include(ci => ci.CollectibleItemAttachments)
            .FirstOrDefaultAsync(ci => ci.Id == request.CollectibleItemId, cancellationToken);

        if (collectibleItem == null)
        {
            throw new InvalidOperationException($"Collectible item with ID {request.CollectibleItemId} not found.");
        }

        // Check if the current user owns any of the showcases this item belongs to
        var showcases = await context.CollectibleItems
            .Where(ci => ci.Id == collectibleItem.Id)
            .SelectMany(ci => ci.Showcases)
            .ToListAsync(cancellationToken);

        if (!showcases.Any(s => s.UserId == _currentUserService.UserId))
        {
            throw new UnauthorizedAccessException("You don't have permission to add attachments to this item.");
        }

        if (request.AttachmentIds.Count != 0)
        {
            var attachments = await context.Attachments
                .Where(a => request.AttachmentIds.Contains(a.Id))
                .ToListAsync(cancellationToken);

            foreach (var attachment in attachments)
            {
                if (!collectibleItem.CollectibleItemAttachments.Any(cia => cia.AttachmentId == attachment.Id))
                {
                    collectibleItem.CollectibleItemAttachments.Add(new CollectibleItemAttachment
                    {
                        CollectibleItemId = collectibleItem.Id,
                        AttachmentId = attachment.Id,
                        IsFeatured = false,
                        DisplayOrder = 0,
                    });
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
