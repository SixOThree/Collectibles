using Collectibles.Application.Interfaces;

using Microsoft.Extensions.Logging;

namespace Collectibles.Infrastructure.Services;

public class ItemHierarchyService : IItemHierarchyService
{
    // A fixed set of striped locks keyed by showcase.
    //
    // The previous lock was keyed by the *full* folder path, which does not serialize the
    // operations that actually collide: importing "A/B" and "A/C" concurrently took two
    // different locks and both created parent "A". It also grew a dictionary entry per
    // distinct path forever. Locking per showcase serializes the whole hierarchy walk for
    // that showcase, and a fixed array cannot leak.
    //
    // This is still a single-process guard: a multi-instance deployment needs the check to
    // move into the database. Duplicates that slip through are reconciled below by
    // re-querying after a failed insert.
    private const int LockStripeCount = 64;

    private static readonly SemaphoreSlim[] ShowcaseLocks =
        [.. Enumerable.Range(0, LockStripeCount).Select(_ => new SemaphoreSlim(1, 1))];

    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly ILogger<ItemHierarchyService> _logger;

    public ItemHierarchyService(
        IApplicationDbContextFactory contextFactory,
        ILogger<ItemHierarchyService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<long> ResolveOrCreateHierarchyAsync(
        long showcaseId, string[] folderSegments, string? userId, CancellationToken ct,
        long? contentDefinitionId = null)
    {
        if (folderSegments.Length == 0)
        {
            throw new ArgumentException("At least one folder segment is required.", nameof(folderSegments));
        }

        var semaphore = ShowcaseLocks[(int)((ulong)showcaseId % LockStripeCount)];

        await semaphore.WaitAsync(ct);
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var showcase = await context.Showcases
                .FirstOrDefaultAsync(s => s.Id == showcaseId, ct)
                ?? throw new InvalidOperationException($"Showcase {showcaseId} not found.");

            long? parentId = null;

            for (int i = 0; i < folderSegments.Length; i++)
            {
                var folderName = folderSegments[i];
                var isLeaf = i == folderSegments.Length - 1;

                var existingItem = await context.CollectibleItems
                    .Include(i => i.Showcases)
                    .FirstOrDefaultAsync(
                        i =>
                        i.Name == folderName
                        && i.ParentId == parentId
                        && i.Showcases.Any(s => s.Id == showcaseId), ct);

                if (existingItem != null)
                {
                    parentId = existingItem.Id;
                    continue;
                }

                var newItem = new CollectibleItem
                {
                    Name = folderName,
                    ParentId = parentId,
                    ContentDefinitionId = isLeaf ? contentDefinitionId : null,
                    Created = DateTime.UtcNow,
                    CreatedBy = userId,
                };
                newItem.Showcases.Add(showcase);

                context.CollectibleItems.Add(newItem);

                try
                {
                    await context.SaveChangesAsync(ct);
                }
                catch (DbUpdateException ex)
                {
                    // Another instance may have created the same folder between the lookup
                    // and the insert. Fall back to whatever is now in the database rather
                    // than failing the import or creating a duplicate.
                    _logger.LogWarning(
                        ex,
                        "Insert of folder item '{Name}' in showcase {ShowcaseId} failed; re-reading",
                        folderName,
                        showcaseId);

                    var capturedParentId = parentId;
                    var raced = await context.CollectibleItems
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            i => i.Name == folderName
                                && i.ParentId == capturedParentId
                                && i.Showcases.Any(s => s.Id == showcaseId),
                            ct);

                    if (raced == null)
                    {
                        throw;
                    }

                    parentId = raced.Id;
                    continue;
                }

                _logger.LogInformation(
                    "Created item '{Name}' (Id={Id}) under parent {ParentId} in showcase {ShowcaseId}",
                    folderName, newItem.Id, parentId, showcaseId);

                parentId = newItem.Id;
            }

            return parentId!.Value;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task LinkAttachmentAsync(long itemId, long attachmentId, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var item = await context.CollectibleItems
            .Include(i => i.CollectibleItemAttachments)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct)
            ?? throw new InvalidOperationException($"CollectibleItem {itemId} not found.");

        if (item.CollectibleItemAttachments.Any(a => a.AttachmentId == attachmentId))
        {
            return;
        }

        item.CollectibleItemAttachments.Add(new CollectibleItemAttachment
        {
            CollectibleItemId = itemId,
            AttachmentId = attachmentId,
        });

        await context.SaveChangesAsync(ct);
    }

    public async Task<long?> FindDuplicateAttachmentAsync(
        long itemId, string contentHash, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var attachmentId = await context.CollectibleItemAttachments
            .Where(cia => cia.CollectibleItemId == itemId)
            .Join(
                context.Attachments,
                cia => cia.AttachmentId,
                a => a.Id,
                (cia, a) => a)
            .Where(a => a.ContentHash == contentHash)
            .Select(a => (long?)a.Id)
            .FirstOrDefaultAsync(ct);

        return attachmentId;
    }

    public async Task CleanupEmptyParentsAsync(long itemId, long showcaseId, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var currentId = itemId;

        while (true)
        {
            var item = await context.CollectibleItems
                .Include(i => i.CollectibleItemAttachments)
                .FirstOrDefaultAsync(i => i.Id == currentId, ct);

            if (item == null)
            {
                break;
            }

            if (item.CollectibleItemAttachments.Any())
            {
                break;
            }

            var hasChildren = await context.CollectibleItems
                .AnyAsync(i => i.ParentId == currentId, ct);

            if (hasChildren)
            {
                break;
            }

            item.Deleted = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);

            _logger.LogInformation("Soft-deleted empty item '{Name}' (Id={Id})", item.Name, item.Id);

            if (!item.ParentId.HasValue)
            {
                break;
            }

            currentId = item.ParentId.Value;
        }
    }
}
