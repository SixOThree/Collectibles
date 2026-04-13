using Collectibles.Application.Interfaces;
using Collectibles.Application.Mappings.Explicit;
using Collectibles.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Attachments.Queries;

public class GetAttachmentDetailQuery : IRequest<AttachmentDto>
{
    public long Id { get; set; }
}

public class GetAttachmentDetailQueryHandler : IRequestHandler<GetAttachmentDetailQuery, AttachmentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IAttachmentMappingService _attachmentMappingService;
    private readonly IEventLogService _eventLogService;

    public GetAttachmentDetailQueryHandler(
        IApplicationDbContext context,
        IAttachmentMappingService attachmentMappingService,
        IEventLogService eventLogService)
    {
        _context = context;
        _attachmentMappingService = attachmentMappingService;
        _eventLogService = eventLogService;
    }

    public async Task<AttachmentDto?> Handle(GetAttachmentDetailQuery request, CancellationToken cancellationToken)
    {
        var attachment = await _context.Attachments
            .AsNoTracking() // Read-only query
            .Include(a => a.Tags)
            .Include(a => a.AttachmentPreview)
            .Include(a => a.AttachmentContent)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (attachment == null)
        {
            return null;
        }

        // Log the view event
        await _eventLogService.LogEventAsync(
            EventAction.View,
            nameof(Attachment),
            attachment.Id,
            attachment.Name,
            cancellationToken: cancellationToken);

        // Use the mapping service which handles both content and preview loading
        var attachmentDto = await _attachmentMappingService.MapWithContentAsync(attachment, cancellationToken);

        return attachmentDto;
    }
}
