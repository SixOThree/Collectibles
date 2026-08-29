using Collectibles.Application.Common.Authorization.Requirements;
using Collectibles.Application.Interfaces;
using Collectibles.Application.Mappings.Explicit;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Collectibles.Application.Features.Attachments.Queries;

public record GetAttachmentByIdQuery(long Id) : IRequest<AttachmentDto>;

public class GetAttachmentByIdQueryHandler(
    IApplicationDbContext context,
    IAttachmentMappingService attachmentMappingService,
    IAuthorizationService authorizationService,
    ILogger<GetAttachmentByIdQueryHandler> logger) : IRequestHandler<GetAttachmentByIdQuery, AttachmentDto>
{
    private readonly IApplicationDbContext _context = context;
    private readonly IAttachmentMappingService _attachmentMappingService = attachmentMappingService;
    private readonly IAuthorizationService _authorizationService = authorizationService;
    private readonly ILogger<GetAttachmentByIdQueryHandler> _logger = logger;

    public async Task<AttachmentDto> Handle(GetAttachmentByIdQuery request, CancellationToken cancellationToken)
    {
        var attachment = await _context.Attachments
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

        // Use the new mapping service to handle all the complexity of loading content from storage
        var attachmentDto = await _attachmentMappingService.MapWithContentAsync(attachment, cancellationToken);

        return attachmentDto;
    }
}
