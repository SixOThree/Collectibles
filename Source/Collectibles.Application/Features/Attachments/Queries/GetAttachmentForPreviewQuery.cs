using Collectibles.Application.Common.Authorization.Requirements;
using Collectibles.Application.Interfaces;
using Collectibles.Application.Mappings.Explicit;
using Collectibles.Domain.Entities;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Features.Attachments.Queries;

public record GetAttachmentForPreviewQuery(long Id) : IRequest<AttachmentDto>;

public class GetAttachmentForPreviewQueryHandler : IRequestHandler<GetAttachmentForPreviewQuery, AttachmentDto>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly IAttachmentMappingService _attachmentMappingService;
    private readonly IEventLogService _eventLogService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<GetAttachmentForPreviewQueryHandler> _logger;

    public GetAttachmentForPreviewQueryHandler(
        IApplicationDbContextFactory contextFactory,
        IAttachmentMappingService attachmentMappingService,
        IEventLogService eventLogService,
        IAuthorizationService authorizationService,
        ILogger<GetAttachmentForPreviewQueryHandler> logger)
    {
        _contextFactory = contextFactory;
        _attachmentMappingService = attachmentMappingService;
        _eventLogService = eventLogService;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    public async Task<AttachmentDto> Handle(GetAttachmentForPreviewQuery request, CancellationToken cancellationToken)
    {
        // The result used to be cached in IMemoryCache keyed only by attachment id: the
        // full content was therefore served across users and stayed stale after an update,
        // rotate, or delete. Content is served per-request and authorized every time.
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

        var authorizationResult = await _authorizationService.AuthorizeAsync(
            new System.Security.Claims.ClaimsPrincipal(),
            attachment,
            new ViewAttachmentRequirement());

        if (!authorizationResult.Succeeded)
        {
            throw new UnauthorizedAccessException("You do not have permission to view this attachment.");
        }

        // Use the mapping service to load content and preview
        // For preview queries, we typically need both content and preview
        var attachmentDto = await _attachmentMappingService.MapWithContentAsync(attachment, cancellationToken);

        // Log the preview generation event
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
