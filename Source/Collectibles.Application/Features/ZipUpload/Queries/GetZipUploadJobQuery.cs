using Collectibles.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.ZipUpload.Queries;

public class GetZipUploadJobQuery : IRequest<ZipUploadJobDto?>
{
    public long JobId { get; set; }
}

public class GetZipUploadJobQueryHandler : IRequestHandler<GetZipUploadJobQuery, ZipUploadJobDto?>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ICurrentUserService _currentUserService;

    public GetZipUploadJobQueryHandler(
        IApplicationDbContextFactory contextFactory,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    public async Task<ZipUploadJobDto?> Handle(GetZipUploadJobQuery request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        var job = await context.ZipUploadJobs
            .Where(j => j.Id == request.JobId && j.UserId == userId)
            .Select(j => new ZipUploadJobDto
            {
                Id = j.Id,
                ShowcaseId = j.ShowcaseId,
                FileName = j.FileName,
                FileSize = j.FileSize,
                Status = j.Status,
                StartedAt = j.StartedAt,
                CompletedAt = j.CompletedAt,
                TotalItems = j.TotalItems,
                ProcessedItems = j.ProcessedItems,
                FoldersCreated = j.FoldersCreated,
                FilesAttached = j.FilesAttached,
                ErrorCount = j.ErrorCount,
                CurrentItemName = j.CurrentItemName,
                ErrorDetails = j.ErrorDetails,
                ProgressPercentage = j.TotalItems > 0 ? j.ProcessedItems * 100 / j.TotalItems : 0,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return job;
    }
}
