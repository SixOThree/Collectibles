using Collectibles.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.CollectibleItems.Validators;

public interface ICollectibleItemPreviewValidator
{
    Task<bool> ValidatePreviewImageAsync(long collectibleItemId, long? previewImageId, CancellationToken cancellationToken = default);
    Task<ValidationResult> ValidateAndFixPreviewImageAsync(long collectibleItemId, CancellationToken cancellationToken = default);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public bool WasFixed { get; set; }
    public string? Message { get; set; }
    public long? NewPreviewImageId { get; set; }
}

public class CollectibleItemPreviewValidator : ICollectibleItemPreviewValidator
{
    private readonly IApplicationDbContextFactory _contextFactory;

    public CollectibleItemPreviewValidator(IApplicationDbContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<bool> ValidatePreviewImageAsync(long collectibleItemId, long? previewImageId, CancellationToken cancellationToken = default)
    {
        if (!previewImageId.HasValue || previewImageId.Value == 0)
        {
            return true; // Null preview is valid
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Check if the preview image exists and belongs to this item
        var isValid = await context.CollectibleItemAttachments
            .AnyAsync(
                cia =>
                cia.CollectibleItemId == collectibleItemId &&
                cia.AttachmentId == previewImageId.Value,
                cancellationToken);

        return isValid;
    }

    public async Task<ValidationResult> ValidateAndFixPreviewImageAsync(long collectibleItemId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var item = await context.CollectibleItems
            .Include(ci => ci.CollectibleItemAttachments)
                .ThenInclude(cia => cia.Attachment)
            .FirstOrDefaultAsync(ci => ci.Id == collectibleItemId, cancellationToken);

        if (item == null)
        {
            return new ValidationResult
            {
                IsValid = false,
                Message = "Collectible item not found",
            };
        }

        // If no preview is set, that's valid
        if (!item.PreviewImageId.HasValue || item.PreviewImageId.Value == 0)
        {
            return new ValidationResult { IsValid = true };
        }

        // Check if the current preview image belongs to this item
        var currentPreviewValid = item.CollectibleItemAttachments
            .Any(cia => cia.AttachmentId == item.PreviewImageId.Value);

        if (currentPreviewValid)
        {
            return new ValidationResult { IsValid = true };
        }

        // Preview is invalid, try to fix it
        var firstImage = item.CollectibleItemAttachments
            .Select(cia => cia.Attachment)
            .FirstOrDefault(a => a != null && a.FileType != null && a.FileType.StartsWith("image/"));

        if (firstImage != null)
        {
            item.PreviewImageId = firstImage.Id;
            await context.SaveChangesAsync(cancellationToken);

            return new ValidationResult
            {
                IsValid = false,
                WasFixed = true,
                Message = $"Preview image was invalid, automatically set to '{firstImage.OriginalFilename}'",
                NewPreviewImageId = firstImage.Id,
            };
        }

        // No valid image found, clear the invalid preview
        item.PreviewImageId = null;
        await context.SaveChangesAsync(cancellationToken);

        return new ValidationResult
        {
            IsValid = false,
            WasFixed = true,
            Message = "Preview image was invalid and no replacement found, cleared preview",
            NewPreviewImageId = null,
        };
    }
}
