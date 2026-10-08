using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.ZipUpload;

/// <summary>
/// Ownership checks shared by every zip-upload command.
/// </summary>
/// <remarks>
/// The whole zip pipeline previously accepted a <c>ShowcaseId</c> or job id with no
/// ownership check at all, end to end, so a caller could bulk-import into a showcase
/// belonging to someone else. Every entry point now runs the same check.
/// </remarks>
public static class ZipUploadAuthorization
{
    /// <summary>
    /// Confirms the showcase exists and is owned by the caller.
    /// </summary>
    /// <exception cref="InvalidOperationException">The showcase does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller does not own the showcase.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task EnsureShowcaseOwnedAsync(
        IApplicationDbContext context,
        long showcaseId,
        string userId,
        CancellationToken cancellationToken)
    {
        var ownerId = await context.Showcases
            .Where(s => s.Id == showcaseId)
            .Select(s => s.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (ownerId == null)
        {
            throw new InvalidOperationException($"Showcase with ID {showcaseId} not found.");
        }

        if (ownerId != userId)
        {
            throw new UnauthorizedAccessException("You don't have permission to upload to this showcase.");
        }
    }

    /// <summary>
    /// Loads a zip upload job, confirming it belongs to the caller.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">The caller does not own the job.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task<ZipUploadJob?> GetOwnedJobAsync(
        IApplicationDbContext context,
        long jobId,
        string userId,
        CancellationToken cancellationToken)
    {
        var job = await context.ZipUploadJobs
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        if (job == null)
        {
            return null;
        }

        if (job.UserId != userId)
        {
            throw new UnauthorizedAccessException("You don't have permission to modify this upload job.");
        }

        return job;
    }
}
