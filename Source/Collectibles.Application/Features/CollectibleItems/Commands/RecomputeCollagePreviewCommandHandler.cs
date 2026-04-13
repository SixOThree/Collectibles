using System.Text.Json;

using Collectibles.Application.Interfaces;
using Collectibles.Application.Services;
using Collectibles.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Features.CollectibleItems.Commands;

public class RecomputeCollagePreviewCommandHandler : IRequestHandler<RecomputeCollagePreviewCommand, RecomputeCollagePreviewResult>
{
    private readonly ICollectibleItemPreviewService _previewService;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<RecomputeCollagePreviewCommandHandler> _logger;
    private readonly IEventLogService _eventLogService;

    public RecomputeCollagePreviewCommandHandler(
        ICollectibleItemPreviewService previewService,
        IApplicationDbContext context,
        ILogger<RecomputeCollagePreviewCommandHandler> logger,
        IEventLogService eventLogService)
    {
        _previewService = previewService;
        _context = context;
        _logger = logger;
        _eventLogService = eventLogService;
    }

    public async Task<RecomputeCollagePreviewResult> Handle(RecomputeCollagePreviewCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _context.CollectibleItems
                .FirstOrDefaultAsync(i => i.Id == request.CollectibleItemId && i.Deleted == null, cancellationToken);
            if (item == null)
            {
                return new RecomputeCollagePreviewResult
                {
                    Success = false,
                    ErrorMessage = "Collectible item not found.",
                };
            }

            var success = await _previewService.GenerateCollagePreviewAsync(request.CollectibleItemId, cancellationToken, useRandomSelection: true);

            if (success)
            {
                await _eventLogService.LogEventAsync(
                    EventAction.Update,
                    entityType: "CollectibleItem",
                    entityId: request.CollectibleItemId,
                    entityName: item.Name,
                    additionalData: JsonSerializer.Serialize(new { Action = "CollagePreviewRecomputed" }),
                    cancellationToken: cancellationToken);

                var updatedItem = await _context.CollectibleItems
                    .Include(i => i.PreviewImage)
                        .ThenInclude(p => p!.AttachmentPreview)
                    .FirstOrDefaultAsync(i => i.Id == request.CollectibleItemId, cancellationToken);
                string? base64Thumbnail = null;
                if (updatedItem?.PreviewImage?.AttachmentPreview?.PreviewThumbnail != null)
                {
                    base64Thumbnail = Convert.ToBase64String(updatedItem.PreviewImage.AttachmentPreview.PreviewThumbnail);
                }

                return new RecomputeCollagePreviewResult
                {
                    Success = true,
                    Base64Thumbnail = base64Thumbnail,
                };
            }
            else
            {
                return new RecomputeCollagePreviewResult
                {
                    Success = false,
                    ErrorMessage = "Failed to generate collage preview. Please ensure the item has child items with images.",
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recomputing collage preview for item {ItemId}", request.CollectibleItemId);
            return new RecomputeCollagePreviewResult
            {
                Success = false,
                ErrorMessage = "An error occurred while generating the collage preview.",
            };
        }
    }
}
