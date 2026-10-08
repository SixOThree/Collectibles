using Collectibles.Application.Common.Authorization.Requirements;
using Collectibles.Application.Interfaces;
using Collectibles.Application.Mappings.Explicit;
using Collectibles.Domain.Entities;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Attachments.Queries;

public record GetAttachmentForDownloadQuery(long Id) : IRequest<AttachmentDownloadDto>;

public class AttachmentDownloadDto
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string? OriginalFilename { get; set; }
    public string? FileType { get; set; }
    public long FileSize { get; set; }
    public byte[]? Content { get; set; }
    public bool IsUsingExternalStorage { get; set; }
    public StorageMode StorageMode { get; set; }
}

public class GetAttachmentForDownloadQueryHandler : IRequestHandler<GetAttachmentForDownloadQuery, AttachmentDownloadDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IAttachmentMappingService _attachmentMappingService;
    private readonly IEventLogService _eventLogService;
    private readonly IAuthorizationService _authorizationService;

    public GetAttachmentForDownloadQueryHandler(
        IApplicationDbContext context,
        IAttachmentMappingService attachmentMappingService,
        IEventLogService eventLogService,
        IAuthorizationService authorizationService)
    {
        _context = context;
        _attachmentMappingService = attachmentMappingService;
        _eventLogService = eventLogService;
        _authorizationService = authorizationService;
    }

    public async Task<AttachmentDownloadDto> Handle(GetAttachmentForDownloadQuery request, CancellationToken cancellationToken)
    {
        var attachment = await _context.Attachments
            .AsNoTracking() // Read-only query
            .Include(a => a.AttachmentContent)
            .Where(a => a.Id == request.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (attachment == null)
        {
            throw new ArgumentException($"Attachment with ID {request.Id} not found.", nameof(request));
        }

        // Visibility is decided here, by the same requirement GetAttachmentById enforces,
        // so every path that serves an attachment's bytes runs one authoritative check —
        // rather than depending on which of four code paths the caller happened to use.
        var authorizationResult = await _authorizationService.AuthorizeAsync(
            new System.Security.Claims.ClaimsPrincipal(),
            attachment,
            new ViewAttachmentRequirement());

        if (!authorizationResult.Succeeded)
        {
            throw new UnauthorizedAccessException("You do not have permission to view this attachment.");
        }

        // Use the mapping service to load content
        var fullDto = await _attachmentMappingService.MapWithContentAsync(attachment, cancellationToken);

        // Convert to download DTO format
        var dto = new AttachmentDownloadDto
        {
            Id = attachment.Id,
            Name = attachment.Name,
            OriginalFilename = attachment.OriginalFilename,
            FileType = attachment.FileType,
            FileSize = attachment.FileSize,
        };

        // Determine storage mode and get content
        if (!string.IsNullOrEmpty(attachment.FilePath))
        {
            dto.IsUsingExternalStorage = true;
            dto.StorageMode = StorageMode.External;
        }
        else if (attachment.AttachmentContent?.Content != null)
        {
            dto.IsUsingExternalStorage = false;
            dto.StorageMode = StorageMode.Database;
        }
        else
        {
            dto.StorageMode = StorageMode.Unknown;
            throw new InvalidOperationException($"Attachment {request.Id} has no content in either external storage or database.");
        }

        // Convert base64 content back to byte array
        if (!string.IsNullOrEmpty(fullDto.Base64Content))
        {
            dto.Content = Convert.FromBase64String(fullDto.Base64Content);
        }
        else
        {
            throw new InvalidOperationException($"Attachment {request.Id} has no content available.");
        }

        // Validate that we have content
        if (dto.Content == null || dto.Content.Length == 0)
        {
            throw new InvalidOperationException($"Attachment {request.Id} content is empty.");
        }

        // Log the download event
        await _eventLogService.LogEventAsync(
            EventAction.Download,
            nameof(Attachment),
            attachment.Id,
            attachment.Name,
            null,
            null,
            System.Text.Json.JsonSerializer.Serialize(new
            {
                FileName = attachment.OriginalFilename,
                FileType = attachment.FileType,
                FileSize = attachment.FileSize,
                StorageMode = dto.StorageMode.ToString(),
            }),
            cancellationToken);

        return dto;
    }
}
