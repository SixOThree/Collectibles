using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Attachments.Commands;

public record DeleteAttachmentCommand(long Id) : IRequest;

public class DeleteAttachmentCommandHandler : IRequestHandler<DeleteAttachmentCommand>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _fileStorage;
    private readonly IEventLogService _eventLogService;

    public DeleteAttachmentCommandHandler(
        IApplicationDbContextFactory contextFactory,
        ICurrentUserService currentUserService,
        IFileStorage fileStorage,
        IEventLogService eventLogService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _fileStorage = fileStorage;
        _eventLogService = eventLogService;
    }

    public async Task Handle(DeleteAttachmentCommand request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context.Attachments
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            throw new ArgumentException($"Attachment with ID {request.Id} not found.", nameof(request));
        }

        // Check authorization
        var attachmentItems = await context.CollectibleItems
            .Where(ci => ci.CollectibleItemAttachments.Any(cia => cia.AttachmentId == entity.Id))
            .Include(ci => ci.Showcases)
            .ToListAsync(cancellationToken);

        if (attachmentItems.Count != 0)
        {
            // Get all showcases containing items with this attachment
            var showcases = attachmentItems.SelectMany(i => i.Showcases).Distinct().ToList();

            // Only showcase owner can delete attachments
            if (!showcases.Any(s => s.UserId == _currentUserService.UserId))
            {
                throw new UnauthorizedAccessException("You don't have permission to delete this attachment.");
            }
        }
        else
        {
            // If attachment is not associated with any item, only the creator can delete it
            if (entity.CreatedBy != _currentUserService.UserId)
            {
                throw new UnauthorizedAccessException("You don't have permission to delete this attachment.");
            }
        }

        // Capture attachment information for event logging
        var deletedAttachmentInfo = new
        {
            Name = entity.Name,
            OriginalFilename = entity.OriginalFilename,
            FileType = entity.FileType,
            FileSize = entity.FileSize,
            AttachmentType = entity.AttachmentType,
            AssociatedItemCount = attachmentItems.Count,
            AssociatedItems = attachmentItems.Select(i => new { i.Id, i.Name }).ToList(),
        };

        // Soft delete, matching DeleteCollectibleItemCommand. Attachments are a soft-delete
        // aggregate: hard-deleting here (and destroying the blob before the save committed)
        // made the same entity recoverable through one path and unrecoverable through another.
        // AttachmentPurgeBackgroundService reclaims the rows and their blobs after the
        // retention window, deleting files only after the database delete is committed.
        entity.Deleted = DateTime.UtcNow;
        entity.DeletedBy = _currentUserService.UserId;

        // Detach from any items so the attachment stops appearing in their listings.
        var links = await context.CollectibleItemAttachments
            .Where(cia => cia.AttachmentId == entity.Id)
            .ToListAsync(cancellationToken);

        if (links.Count != 0)
        {
            context.CollectibleItemAttachments.RemoveRange(links);
        }

        await context.SaveChangesAsync(cancellationToken);

        // Log the delete event
        await _eventLogService.LogEventAsync(
            EventAction.Delete,
            nameof(Attachment),
            entity.Id,
            entity.Name,
            deletedAttachmentInfo,
            null,
            cancellationToken: cancellationToken);
    }
}
