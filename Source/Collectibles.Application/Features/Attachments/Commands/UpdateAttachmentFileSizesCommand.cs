using Collectibles.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Attachments.Commands;

public record UpdateAttachmentFileSizesCommand : IRequest<int>;

public class UpdateAttachmentFileSizesCommandHandler : IRequestHandler<UpdateAttachmentFileSizesCommand, int>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ICurrentUserService _currentUserService;

    public UpdateAttachmentFileSizesCommandHandler(
        IApplicationDbContextFactory contextFactory,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(UpdateAttachmentFileSizesCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAdministrator)
        {
            throw new UnauthorizedAccessException("You are not authorized to update attachment file sizes.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Get all attachments with their content where FileSize is 0
        var attachmentsToUpdate = await context.Attachments
            .Include(a => a.AttachmentContent)
            .Where(a => a.FileSize == 0 && a.AttachmentContent != null && a.AttachmentContent.Content != null)
            .ToListAsync(cancellationToken);

        var updatedCount = 0;

        foreach (var attachment in attachmentsToUpdate)
        {
            if (attachment.AttachmentContent?.Content != null)
            {
                attachment.FileSize = attachment.AttachmentContent.Content.Length;
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return updatedCount;
    }
}
