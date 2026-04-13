using System.Collections.Concurrent;
using Collectibles.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Collectibles.Infrastructure.Services;

public class ItemHierarchyService : IItemHierarchyService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _pathLocks = new();

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

        var lockKey = $"{showcaseId}:{string.Join("/", folderSegments)}";
        var semaphore = _pathLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

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
                    .FirstOrDefaultAsync(i =>
                        i.Name == folderName
                        && i.ParentId == parentId
                        && i.Deleted == null
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
                    CreatedBy = userId
                };
                newItem.Showcases.Add(showcase);

                context.CollectibleItems.Add(newItem);
                await context.SaveChangesAsync(ct);

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
            AttachmentId = attachmentId
        });

        await context.SaveChangesAsync(ct);
    }

    public async Task<long?> FindDuplicateAttachmentAsync(
        long itemId, string contentHash, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var attachmentId = await context.CollectibleItemAttachments
            .Where(cia => cia.CollectibleItemId == itemId)
            .Join(context.Attachments,
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
                .FirstOrDefaultAsync(i => i.Id == currentId && i.Deleted == null, ct);

            if (item == null)
            {
                break;
            }

            if (item.CollectibleItemAttachments.Any())
            {
                break;
            }

            var hasChildren = await context.CollectibleItems
                .AnyAsync(i => i.ParentId == currentId && i.Deleted == null, ct);

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
