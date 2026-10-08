using Collectibles.Application.Features.Maintenance;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Maintenance.Queries;

public record OrphanedAttachmentDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? OriginalFilename { get; init; }
    public string? FileType { get; init; }
    public long FileSize { get; init; }
    public DateTime? Created { get; init; }
}

public record OrphanedBlobDto
{
    public string Name { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
}

public record OrphanedItemDto
{
    public long Id { get; init; }
    public string? Name { get; init; }
    public DateTime? Created { get; init; }
}

public record OrphanDetailsDto
{
    public List<OrphanedAttachmentDto> Attachments { get; init; } = new();
    public List<OrphanedBlobDto> Blobs { get; init; } = new();
    public List<OrphanedItemDto> Items { get; init; } = new();
}

public record GetOrphanDetailsQuery : IRequest<OrphanDetailsDto>;

public class GetOrphanDetailsQueryHandler : IRequestHandler<GetOrphanDetailsQuery, OrphanDetailsDto>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly IFileStorage _fileStorage;
    private readonly ICurrentUserService _currentUserService;

    public GetOrphanDetailsQueryHandler(
        IApplicationDbContextFactory contextFactory,
        IFileStorage fileStorage,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _fileStorage = fileStorage;
        _currentUserService = currentUserService;
    }

    public async Task<OrphanDetailsDto> Handle(GetOrphanDetailsQuery request, CancellationToken cancellationToken)
    {
        // Reports across every user's content and exposes raw storage paths; CleanupOrphans
        // already requires administrator, and its read siblings must match.
        if (!_currentUserService.IsAdministrator)
        {
            throw new UnauthorizedAccessException("Only administrators can view orphan details.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var orphanedAttachments = await OrphanClassification.OrphanedAttachments(context)
            .OrderBy(a => a.Name)
            .Select(a => new OrphanedAttachmentDto
            {
                Id = a.Id,
                Name = a.Name,
                OriginalFilename = a.OriginalFilename,
                FileType = a.FileType,
                FileSize = a.FileSize,
                Created = a.Created,
            })
            .ToListAsync(cancellationToken);

        var emptyItems = await OrphanClassification.EmptyItems(context)
            .OrderBy(ci => ci.Name)
            .Select(ci => new OrphanedItemDto
            {
                Id = ci.Id,
                Name = ci.Name,
                Created = ci.Created,
            })
            .ToListAsync(cancellationToken);

        var blobs = await GetOrphanedBlobs(context, cancellationToken);

        return new OrphanDetailsDto
        {
            Attachments = orphanedAttachments,
            Blobs = blobs,
            Items = emptyItems,
        };
    }

    private async Task<List<OrphanedBlobDto>> GetOrphanedBlobs(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        var orphanedBlobs = await OrphanClassification.GetOrphanedBlobsAsync(context, _fileStorage, cancellationToken);

        return orphanedBlobs
            .OrderBy(b => b.Name, StringComparer.Ordinal)
            .Select(b => new OrphanedBlobDto { Name = b.Name, SizeBytes = b.SizeBytes })
            .ToList();
    }
}
