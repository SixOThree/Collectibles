using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Attachments.Commands;

/// <summary>
/// Moves an attachment to a new location based on relative path.
/// Handles both simple renames and folder restructuring (creating parent items).
/// Used by the sync tool when a file's content hash matches but the path has changed.
/// </summary>
public record MoveAttachmentCommand : IRequest
{
    /// <summary>Attachment ID to move.</summary>
    public required long AttachmentId { get; init; }

    /// <summary>Relative path from the local sync folder (e.g., "Computers\photo.jpg" or "photo.jpg").</summary>
    public required string RelativePath { get; init; }

    /// <summary>Target showcase ID.</summary>
    public required long ShowcaseId { get; init; }
}

public class MoveAttachmentCommandHandler : IRequestHandler<MoveAttachmentCommand>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEventLogService _eventLogService;
    private readonly IItemHierarchyService _hierarchyService;

    public MoveAttachmentCommandHandler(
        IApplicationDbContextFactory contextFactory,
        IEventLogService eventLogService,
        IItemHierarchyService hierarchyService,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _eventLogService = eventLogService;
        _hierarchyService = hierarchyService;
        _currentUserService = currentUserService;
    }

    public async Task Handle(MoveAttachmentCommand request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Load the attachment
        var attachment = await context.Attachments
            .FirstOrDefaultAsync(a => a.Id == request.AttachmentId, cancellationToken);

        if (attachment == null)
        {
            throw new ArgumentException($"Attachment with ID {request.AttachmentId} not found.");
        }

        // Load the showcase
        var showcase = await context.Showcases
            .FirstOrDefaultAsync(s => s.Id == request.ShowcaseId && s.Deleted == null, cancellationToken);

        if (showcase == null)
        {
            throw new ArgumentException($"Showcase with ID {request.ShowcaseId} not found.");
        }

        if (string.IsNullOrEmpty(_currentUserService.UserId) || showcase.UserId != _currentUserService.UserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to move attachments in this showcase.");
        }

        // Parse relative path into folder segments and filename
        var normalizedPath = request.RelativePath.Replace('\\', '/');
        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var fileName = segments[^1];
        var folderSegments = segments[..^1];

        // Find the current item this attachment belongs to in this showcase
        var currentLink = await context.CollectibleItemAttachments
            .Include(cia => cia.CollectibleItem)
                .ThenInclude(ci => ci.Showcases)
            .FirstOrDefaultAsync(
                cia => cia.AttachmentId == attachment.Id
                    && cia.CollectibleItem.Showcases.Any(s => s.Id == request.ShowcaseId)
                    && cia.CollectibleItem.Deleted == null,
                cancellationToken);

        var oldItemId = currentLink?.CollectibleItemId;
        var oldItemName = currentLink?.CollectibleItem?.Name;

        // Rename the attachment
        var oldFilename = attachment.OriginalFilename;
        attachment.Name = Path.GetFileNameWithoutExtension(fileName);
        attachment.OriginalFilename = fileName;

        // Track whether we actually moved the attachment to a different item
        var movedToNewItem = false;

        if (folderSegments.Length > 0)
        {
            // File has folder structure: attach directly to the deepest folder item
            // (mirroring zip upload behavior — folders become items, files become attachments)
            var targetFolderId = await _hierarchyService.ResolveOrCreateHierarchyAsync(
                request.ShowcaseId, folderSegments, null, cancellationToken);

            if (currentLink != null && currentLink.CollectibleItemId == targetFolderId)
            {
                // Already on the correct folder item — rename already handled above
            }
            else
            {
                if (currentLink != null)
                {
                    context.CollectibleItemAttachments.Remove(currentLink);
                }

                context.CollectibleItemAttachments.Add(new CollectibleItemAttachment
                {
                    CollectibleItemId = targetFolderId,
                    AttachmentId = attachment.Id,
                    IsFeatured = false,
                    DisplayOrder = 0,
                });
                movedToNewItem = true;
            }
        }
        else
        {
            // Root-level file (no folders): needs its own collectible item
            if (currentLink != null)
            {
                var currentItem = currentLink.CollectibleItem;
                var attachmentCount = await context.CollectibleItemAttachments
                    .CountAsync(cia => cia.CollectibleItemId == currentItem.Id, cancellationToken);
                var hasChildren = await context.CollectibleItems
                    .AnyAsync(ci => ci.ParentId == currentItem.Id && ci.Deleted == null, cancellationToken);

                if (attachmentCount == 1 && !hasChildren && currentItem.ParentId == null)
                {
                    // Standalone root leaf with single attachment — just rename
                    currentItem.Name = attachment.Name;
                }
                else
                {
                    // Detach and create new root-level item
                    context.CollectibleItemAttachments.Remove(currentLink);

                    var newItem = new CollectibleItem
                    {
                        Name = attachment.Name,
                        ParentId = null,
                    };
                    newItem.Showcases.Add(showcase);
                    newItem.CollectibleItemAttachments.Add(new CollectibleItemAttachment
                    {
                        Attachment = attachment,
                        IsFeatured = false,
                        DisplayOrder = 0,
                    });
                    context.CollectibleItems.Add(newItem);
                    movedToNewItem = true;
                }
            }
            else
            {
                // Attachment not linked to any item — create new root-level item
                var newItem = new CollectibleItem
                {
                    Name = attachment.Name,
                    ParentId = null,
                };
                newItem.Showcases.Add(showcase);
                newItem.CollectibleItemAttachments.Add(new CollectibleItemAttachment
                {
                    Attachment = attachment,
                    IsFeatured = false,
                    DisplayOrder = 0,
                });
                context.CollectibleItems.Add(newItem);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        // Clean up old empty parent chain if the attachment was moved to a different item
        if (oldItemId.HasValue && movedToNewItem)
        {
            await _hierarchyService.CleanupEmptyParentsAsync(oldItemId.Value, request.ShowcaseId, cancellationToken);
        }

        // Log the move event
        await _eventLogService.LogEventAsync(
            EventAction.Update,
            nameof(Attachment),
            attachment.Id,
            attachment.Name,
            new { OriginalFilename = oldFilename, ItemName = oldItemName },
            new { OriginalFilename = attachment.OriginalFilename, RelativePath = request.RelativePath, Moved = true },
            cancellationToken: cancellationToken);
    }
}
