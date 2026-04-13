using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.CollectibleItems.Commands;

public class AddAttachmentsToCollectibleItemSystemCommand : IRequest<Unit>
{
    public long CollectibleItemId { get; set; }
    public List<long> AttachmentIds { get; set; } = new();
}

public class AddAttachmentsToCollectibleItemSystemCommandHandler : IRequestHandler<AddAttachmentsToCollectibleItemSystemCommand, Unit>
{
    private readonly IApplicationDbContextFactory _contextFactory;

    public AddAttachmentsToCollectibleItemSystemCommandHandler(IApplicationDbContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Unit> Handle(AddAttachmentsToCollectibleItemSystemCommand request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var collectibleItem = await context.CollectibleItems
            .Include(ci => ci.CollectibleItemAttachments)
            .FirstOrDefaultAsync(ci => ci.Id == request.CollectibleItemId && ci.Deleted == null, cancellationToken);

        if (collectibleItem == null)
        {
            throw new InvalidOperationException($"Collectible item with ID {request.CollectibleItemId} not found.");
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
