# Showcase Deletion - Attachment Behavior

## Summary

When a showcase is deleted, **attachments are NOT deleted from Azure Blob Storage**. Orphaned blobs can accumulate over time unless explicitly cleaned up.

## Deletion Flow

| Component | Behavior |
|-----------|----------|
| Showcase | Soft-deleted (marked with `Deleted` timestamp) |
| CollectibleItems | Soft-deleted by handler |
| CollectibleItemAttachment join entries | Cascade deleted by EF Core |
| Attachment records | Remain in the database |
| Azure Blob files | Remain in storage, untouched |

## Key Files

- **Showcase delete handler**: `Source/Collectibles.Application/Features/Showcases/Commands/DeleteShowcaseCommand.cs`
- **CollectibleItem delete handler**: `Source/Collectibles.Application/Features/CollectibleItems/Commands/DeleteCollectibleItemCommand.cs`
- **Attachment delete handler**: `Source/Collectibles.Application/Features/Attachments/Commands/DeleteAttachmentCommand.cs`
- **Orphan cleanup**: `CleanupOrphansCommand`
- **Blob storage**: `Source/Collectibles.Infrastructure/FileStorage/AzureBlobFileStorage.cs`

## When Blobs Are Deleted

Blob files are only removed from Azure Storage in two scenarios:

1. **Explicit attachment deletion** via `DeleteAttachmentCommand`, which calls `AzureBlobFileStorage.DeleteFileAsync()` to remove both the file and preview blobs.

2. **Orphan cleanup** via `CleanupOrphansCommand`, which finds attachments not linked to any collectible items and removes their blobs from storage.

## Cascade Configuration

The `CollectibleItemAttachment` join table uses `DeleteBehavior.Cascade` on both sides, so join entries are removed when either a CollectibleItem or Attachment is deleted. However, since showcases and items use soft deletes, the Attachment entities themselves are never automatically removed.

## Implications

- The orphan detector correctly identifies attachments linked only to soft-deleted items as orphaned, and removes their blobs from Azure Storage.
- Attachments shared across multiple collectible items are preserved as long as any non-deleted item still references them.
- `CleanupOrphansCommand` must be run periodically to reclaim storage from deleted showcases/items.
