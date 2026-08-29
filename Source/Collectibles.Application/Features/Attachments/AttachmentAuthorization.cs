using Collectibles.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Attachments;

/// <summary>
/// Ownership checks shared by the attachment create commands.
/// </summary>
/// <remarks>
/// The direct-upload commands verify showcase ownership; the create/create-stream twins
/// accepted a <c>ShowcaseId</c> with no check at all. Both now run the same guard.
/// </remarks>
public static class AttachmentAuthorization
{
    /// <summary>
    /// Confirms the target showcase, if one was supplied, is owned by the caller.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">
    /// No user is authenticated, the showcase does not exist, or it belongs to someone else.
    /// </exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task EnsureShowcaseOwnedAsync(
        IApplicationDbContext context,
        long? showcaseId,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User context not available. Please ensure you are logged in.");
        }

        if (!showcaseId.HasValue || showcaseId.Value <= 0)
        {
            return;
        }

        var ownerId = await context.Showcases
            .Where(s => s.Id == showcaseId.Value)
            .Select(s => s.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (ownerId != userId)
        {
            throw new UnauthorizedAccessException("You don't have permission to add attachments to this showcase.");
        }
    }
}
