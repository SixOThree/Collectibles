using Collectibles.Application.Interfaces;
using Collectibles.Application.Mappings.Explicit;
using Collectibles.Domain.Constants;
using Collectibles.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Features.Attachments.Queries;

public record GetAttachmentForPreviewQuery(long Id) : IRequest<AttachmentDto>;

public class GetAttachmentForPreviewQueryHandler : IRequestHandler<GetAttachmentForPreviewQuery, AttachmentDto>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly IAttachmentMappingService _attachmentMappingService;
    private readonly IMemoryCache? _memoryCache;
    private readonly IEventLogService _eventLogService;
    private readonly ILogger<GetAttachmentForPreviewQueryHandler> _logger;
    private const string CacheKeyPrefix = "AttachmentPreview_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(ApplicationConstants.Caching.AttachmentPreviewCacheMinutes);

    public GetAttachmentForPreviewQueryHandler(
        IApplicationDbContextFactory contextFactory,
        IAttachmentMappingService attachmentMappingService,
        IEventLogService eventLogService,
        ILogger<GetAttachmentForPreviewQueryHandler> logger,
        IMemoryCache? memoryCache = null)
    {
        _contextFactory = contextFactory;
        _attachmentMappingService = attachmentMappingService;
        _eventLogService = eventLogService;
        _logger = logger;
        _memoryCache = memoryCache;
    }

    public async Task<AttachmentDto> Handle(GetAttachmentForPreviewQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeyPrefix}{request.Id}";

        // Try to get from cache if available
        if (_memoryCache != null && _memoryCache.TryGetValue(cacheKey, out AttachmentDto? cachedAttachment))
        {
            return cachedAttachment!;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var attachment = await context.Attachments
            .AsNoTracking() // Read-only query
            .Include(a => a.AttachmentContent)
            .Include(a => a.AttachmentPreview)
            .Include(a => a.Tags)
            .Where(a => a.Id == request.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (attachment == null)
        {
            throw new ArgumentException($"Attachment with ID {request.Id} not found.", nameof(request));
        }

        // Use the mapping service to load content and preview
        // For preview queries, we typically need both content and preview
        var attachmentDto = await _attachmentMappingService.MapWithContentAsync(attachment, cancellationToken);

        // Add to cache if available
        if (_memoryCache != null)
        {
            _memoryCache.Set(cacheKey, attachmentDto, CacheDuration);
        }

        // Log the preview generation event (only for non-cached requests)
        var logTask = _eventLogService.LogEventAsync(
            EventAction.View,
            nameof(Attachment),
            attachment.Id,
            attachment.Name,
            null,
            null,
            System.Text.Json.JsonSerializer.Serialize(new
            {
                Action = "Preview",
                FileType = attachment.FileType,
                FileSize = attachment.FileSize,
                HasPreview = !string.IsNullOrEmpty(attachmentDto.Base64PreviewThumbnail),
            }),
            cancellationToken);
        if (logTask != null)
        {
            await logTask;
        }

        return attachmentDto;
    }
}
