using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Features.CollectibleItems.Commands;

public class UpdateMissingPreviewImagesCommand : IRequest<UpdateMissingPreviewImagesResult>
{
    public bool ProcessAllItems { get; set; } = true;
    public long? ShowcaseId { get; set; }
}

public class UpdateMissingPreviewImagesResult
{
    public int ItemsProcessed { get; set; }
    public int ItemsUpdated { get; set; }
    public List<string> Messages { get; set; } = new();
}

public class UpdateMissingPreviewImagesCommandHandler : IRequestHandler<UpdateMissingPreviewImagesCommand, UpdateMissingPreviewImagesResult>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly IEventLogService _eventLogService;
    private readonly ILogger<UpdateMissingPreviewImagesCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public UpdateMissingPreviewImagesCommandHandler(
        IApplicationDbContextFactory contextFactory,
        IEventLogService eventLogService,
        ILogger<UpdateMissingPreviewImagesCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _eventLogService = eventLogService;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<UpdateMissingPreviewImagesResult> Handle(UpdateMissingPreviewImagesCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAdministrator)
        {
            throw new UnauthorizedAccessException("Only administrators can update preview images.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var result = new UpdateMissingPreviewImagesResult();

        // Query for items with no preview image but with attachments
        var query = context.CollectibleItems
            .Include(ci => ci.CollectibleItemAttachments)
                .ThenInclude(cia => cia.Attachment)
            .Where(ci => ci.Deleted == null)
            .Where(ci => ci.PreviewImageId == null || ci.PreviewImageId == 0)
            .Where(ci => ci.CollectibleItemAttachments.Any());

        // Filter by showcase if specified
        if (request.ShowcaseId.HasValue)
        {
            query = query.Where(ci => ci.Showcases.Any(s => s.Id == request.ShowcaseId.Value));
        }

        var itemsToUpdate = await query.ToListAsync(cancellationToken);
        result.ItemsProcessed = itemsToUpdate.Count;

        foreach (var item in itemsToUpdate)
        {
            // Find the first image attachment
            var firstImage = item.CollectibleItemAttachments
                .Select(cia => cia.Attachment)
                .FirstOrDefault(a => a != null && a.FileType != null && a.FileType.StartsWith("image/"));

            if (firstImage != null)
            {
                var oldPreviewId = item.PreviewImageId;
                item.PreviewImageId = firstImage.Id;
                result.ItemsUpdated++;

                // Log the update
                await _eventLogService.LogEventAsync(
                    EventAction.Update,
                    nameof(CollectibleItem),
                    item.Id,
                    item.Name,
                    new { PreviewImageId = oldPreviewId },
                    new { PreviewImageId = firstImage.Id, Reason = "Automatic preview image assignment" },
                    cancellationToken: cancellationToken);

                result.Messages.Add($"Updated item '{item.Name}' (ID: {item.Id}) with preview image '{firstImage.OriginalFilename}'");
                _logger.LogInformation("Set preview image for item {ItemId} to attachment {AttachmentId}", item.Id, firstImage.Id);
            }
        }

        if (result.ItemsUpdated > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        result.Messages.Insert(0, $"Processed {result.ItemsProcessed} items, updated {result.ItemsUpdated} with preview images");
        _logger.LogInformation(
            "Preview image update completed: {ItemsProcessed} items processed, {ItemsUpdated} updated",
            result.ItemsProcessed, result.ItemsUpdated);

        return result;
    }
}
