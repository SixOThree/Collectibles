using Collectibles.Application.Interfaces;
using Collectibles.Application.Mappings.Explicit;
using Collectibles.Domain.Common.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Attachments.Queries;

public record GetAttachmentsListQuery : IRequest<AttachmentsListVm>
{
    public string? SearchTerm { get; set; }
    public AttachmentType? AttachmentType { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class AttachmentsListVm
{
    public IList<AttachmentBriefDto> Items { get; set; } = new List<AttachmentBriefDto>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class GetAttachmentsListQueryHandler : IRequestHandler<GetAttachmentsListQuery, AttachmentsListVm>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly IAttachmentMappingService _attachmentMappingService;
    private readonly ICurrentUserService _currentUserService;

    public GetAttachmentsListQueryHandler(
        IApplicationDbContextFactory contextFactory,
        IAttachmentMappingService attachmentMappingService,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _attachmentMappingService = attachmentMappingService;
        _currentUserService = currentUserService;
    }

    public async Task<AttachmentsListVm> Handle(GetAttachmentsListQuery request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var userId = _currentUserService.UserId;

        // Include attachments created by the user OR owned through the showcase hierarchy
        // (handles legacy data where CreatedBy may be null)
        var ownedAttachmentIds = context.CollectibleItemAttachments
            .Where(cia => cia.CollectibleItem.Showcases.Any(s => s.UserId == userId))
            .Select(cia => cia.AttachmentId);

        var query = context.Attachments
            .Where(a => a.CreatedBy == userId || ownedAttachmentIds.Contains(a.Id));

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(a => a.Name.Contains(request.SearchTerm) ||
                                   (a.OriginalFilename != null && a.OriginalFilename.Contains(request.SearchTerm)));
        }

        if (request.AttachmentType.HasValue)
        {
            query = query.Where(a => a.AttachmentType == request.AttachmentType);
        }

        if (request.CreatedFrom.HasValue)
        {
            query = query.Where(a => a.Created >= request.CreatedFrom.Value);
        }

        if (request.CreatedTo.HasValue)
        {
            query = query.Where(a => a.Created <= request.CreatedTo.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .Include(a => a.AttachmentPreview)
            .OrderBy(a => a.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Map entities to DTOs using the mapping service for preview loading
        var items = new List<AttachmentBriefDto>();
        foreach (var entity in entities)
        {
            var dto = await _attachmentMappingService.MapToBriefWithPreviewAsync(entity, false, cancellationToken);
            items.Add(dto);
        }

        return new AttachmentsListVm
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }
}
