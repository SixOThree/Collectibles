using Collectibles.Application.Interfaces;
using Collectibles.Application.Services;
using Collectibles.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Attachments.Queries;

public record AttachmentContextDto
{
    public required string AttachmentHashId { get; init; }
    public required string CollectibleItemHashId { get; init; }
    public string? ItemName { get; init; }
    public string? ItemPath { get; init; }
    public int OtherAttachmentCount { get; init; }
    public int ChildItemCount { get; init; }
    public bool HasDescription { get; init; }
    public bool HasCustomFields { get; init; }
    public bool HasTags { get; init; }
    public bool HasExternalLinks { get; init; }
    public bool HasQrCode { get; init; }
    public bool HasAdditionalData => HasDescription || HasCustomFields || HasTags || HasExternalLinks || HasQrCode;
}

public record GetAttachmentContextQuery(long AttachmentId) : IRequest<AttachmentContextDto?>;

public class GetAttachmentContextQueryHandler : IRequestHandler<GetAttachmentContextQuery, AttachmentContextDto?>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly IHashIdsService _hashIdsService;
    private readonly IEventLogService _eventLogService;
    private readonly ICurrentUserService _currentUserService;

    public GetAttachmentContextQueryHandler(
        IApplicationDbContextFactory contextFactory,
        IHashIdsService hashIdsService,
        IEventLogService eventLogService,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _hashIdsService = hashIdsService;
        _eventLogService = eventLogService;
        _currentUserService = currentUserService;
    }

    public async Task<AttachmentContextDto?> Handle(GetAttachmentContextQuery request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var itemAttachment = await context.CollectibleItemAttachments
            .AsNoTracking()
            .Where(cia => cia.AttachmentId == request.AttachmentId)
            .Select(cia => new
            {
                ItemId = cia.CollectibleItemId,
                ItemName = cia.CollectibleItem.Name,
                ParentName = cia.CollectibleItem.Parent != null ? cia.CollectibleItem.Parent.Name : null,
                cia.CollectibleItem.DetailedDescription,
                cia.CollectibleItem.QRCodeId,
                ChildCount = cia.CollectibleItem.Children.Count(c => c.Deleted == null),
                OtherAttachments = cia.CollectibleItem.CollectibleItemAttachments
                    .Count(a => a.AttachmentId != request.AttachmentId && a.Attachment.Deleted == null),
                HasTags = cia.CollectibleItem.CollectibleItemTags.Any(),
                HasExternalLinks = cia.CollectibleItem.ExternalReferences.Any(),
                ContentValue = cia.CollectibleItem.ContentValue,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (itemAttachment == null)
        {
            return null;
        }

        // Verify the current user has access to this item through showcase ownership
        var hasAccess = await context.CollectibleItems
            .Where(ci => ci.Id == itemAttachment.ItemId)
            .SelectMany(ci => ci.Showcases)
            .AnyAsync(s => s.UserId == _currentUserService.UserId || !s.IsPrivate, cancellationToken);

        if (!hasAccess)
        {
            return null;
        }

        var hasCustomFields = !string.IsNullOrEmpty(itemAttachment.ContentValue)
            && itemAttachment.ContentValue != "[]"
            && itemAttachment.ContentValue != "{}";

        var result = new AttachmentContextDto
        {
            AttachmentHashId = _hashIdsService.Encode(request.AttachmentId),
            CollectibleItemHashId = _hashIdsService.Encode(itemAttachment.ItemId),
            ItemName = itemAttachment.ItemName,
            ItemPath = itemAttachment.ParentName != null
                ? $"{itemAttachment.ParentName} > {itemAttachment.ItemName}"
                : itemAttachment.ItemName,
            OtherAttachmentCount = itemAttachment.OtherAttachments,
            ChildItemCount = itemAttachment.ChildCount,
            HasDescription = !string.IsNullOrEmpty(itemAttachment.DetailedDescription),
            HasCustomFields = hasCustomFields,
            HasTags = itemAttachment.HasTags,
            HasExternalLinks = itemAttachment.HasExternalLinks,
            HasQrCode = itemAttachment.QRCodeId != null,
        };

        // Log the context view event
        await _eventLogService.LogEventAsync(
            EventAction.View,
            nameof(Attachment),
            request.AttachmentId,
            itemAttachment.ItemName,
            cancellationToken: cancellationToken);

        return result;
    }
}
