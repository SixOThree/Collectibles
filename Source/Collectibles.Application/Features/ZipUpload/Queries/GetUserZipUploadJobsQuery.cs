using Collectibles.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.ZipUpload.Queries;

public class GetUserZipUploadJobsQuery : IRequest<List<ZipUploadJobDto>>
{
    public bool IncludeCompleted { get; set; }
}

public class GetUserZipUploadJobsQueryHandler : IRequestHandler<GetUserZipUploadJobsQuery, List<ZipUploadJobDto>>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ICurrentUserService _currentUserService;

    public GetUserZipUploadJobsQueryHandler(
        IApplicationDbContextFactory contextFactory,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    public async Task<List<ZipUploadJobDto>> Handle(GetUserZipUploadJobsQuery request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return new List<ZipUploadJobDto>();
        }

        var query = context.ZipUploadJobs
            .Where(j => j.UserId == userId);

        if (!request.IncludeCompleted)
        {
            query = query.Where(j => j.Status != Domain.Common.Enums.JobStatus.Done &&
                                   j.Status != Domain.Common.Enums.JobStatus.DoneWithErrors &&
                                   j.Status != Domain.Common.Enums.JobStatus.Failed &&
                                   j.Status != Domain.Common.Enums.JobStatus.Cancelled);
        }

        var jobs = await query
            .OrderByDescending(j => j.Created)
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
            .ToListAsync(cancellationToken);

        return jobs;
    }
}
