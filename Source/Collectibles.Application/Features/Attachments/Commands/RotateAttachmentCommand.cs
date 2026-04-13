using Collectibles.Application.Interfaces;
using Collectibles.Domain.Configuration.Storage;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Enums;
using Collectibles.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Collectibles.Application.Features.Attachments.Commands;

public class RotateAttachmentCommand : IRequest<bool>
{
    public long AttachmentId { get; set; }
    public int RotationDegrees { get; set; } // 90, -90, 180, 270
}

public class RotateAttachmentCommandHandler : IRequestHandler<RotateAttachmentCommand, bool>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly IFileProcessingService _fileProcessingService;
    private readonly IFileStorage _fileStorage;
    private readonly StorageSettings _storageSettings;
    private readonly ICurrentUserService _currentUserService;

    public RotateAttachmentCommandHandler(
        IApplicationDbContextFactory contextFactory,
        IFileProcessingService fileProcessingService,
        IFileStorage fileStorage,
        IOptions<StorageSettings> storageOptions,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _fileProcessingService = fileProcessingService;
        _fileStorage = fileStorage;
        _storageSettings = storageOptions.Value;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(RotateAttachmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var attachment = await context.Attachments
                .Include(a => a.AttachmentPreview)
                .Include(a => a.CollectibleItemAttachments)
                    .ThenInclude(cia => cia.CollectibleItem)
                .FirstOrDefaultAsync(a => a.Id == request.AttachmentId, cancellationToken);

            if (attachment == null)
            {
                throw new InvalidOperationException("Attachment not found");
            }

            // Verify ownership through showcase chain
            var showcaseUserIds = await context.CollectibleItemAttachments
                .Where(cia => cia.AttachmentId == attachment.Id)
                .SelectMany(cia => cia.CollectibleItem.Showcases)
                .Select(s => s.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (!showcaseUserIds.Contains(_currentUserService.UserId) && attachment.CreatedBy != _currentUserService.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to rotate this attachment.");
            }

            // Only allow rotation for images
            if (attachment.AttachmentType != Domain.Common.Enums.AttachmentType.Image)
            {
                throw new InvalidOperationException("Can only rotate image attachments");
            }

            // Determine the showcase ID for this attachment
            long? showcaseId = null;
            if (attachment.CollectibleItemAttachments.Any())
            {
                var collectibleItem = attachment.CollectibleItemAttachments.First().CollectibleItem;
                var showcase = await context.Showcases
                    .Where(s => s.Deleted == null && s.CollectibleItems.Any(ci => ci.Id == collectibleItem.Id))
                    .OrderBy(s => s.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                showcaseId = showcase?.Id;
            }
            else
            {
                // Check if this attachment is used as a preview image
                var itemWithPreview = await context.CollectibleItems
                    .Where(ci => ci.Deleted == null && ci.PreviewImageId == attachment.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (itemWithPreview != null)
                {
                    var showcase = await context.Showcases
                        .Where(s => s.Deleted == null && s.CollectibleItems.Any(ci => ci.Id == itemWithPreview.Id))
                        .OrderBy(s => s.Id)
                        .FirstOrDefaultAsync(cancellationToken);
                    showcaseId = showcase?.Id;
                }
            }

            // Get the current file content
            var fileContent = await _fileStorage.GetFileAsync(attachment.FilePath, cancellationToken);
            if (fileContent == null)
            {
                throw new InvalidOperationException("Could not retrieve file content");
            }

            // Rotate the image
            var rotatedContent = await _fileProcessingService.RotateImageAsync(
                fileContent,
                request.RotationDegrees,
                cancellationToken);

            if (rotatedContent == null)
            {
                throw new InvalidOperationException("Failed to rotate image");
            }

            // Delete the old file and save the rotated image with showcase ID
            var oldPath = attachment.FilePath;
            var newPath = await _fileStorage.SaveFileAsync(
                rotatedContent,
                attachment.OriginalFilename ?? $"attachment_{attachment.Id}",
                attachment.FileType ?? "image/jpeg",
                showcaseId,  // Now passing the showcase ID
                cancellationToken);

            // Update the file path
            attachment.FilePath = newPath;

            // Delete the old file after successful save
            if (!string.IsNullOrEmpty(oldPath) && oldPath != newPath)
            {
                try
                {
                    await _fileStorage.DeleteFileAsync(oldPath, cancellationToken);
                }
                catch
                {
                    // Ignore errors when deleting old file
                }
            }

            // Regenerate the preview/thumbnail
            var preview = await _fileProcessingService.GeneratePreviewAsync(
                rotatedContent,
                attachment.FileType,
                cancellationToken);

            // Update the preview
            if (preview != null)
            {
                // Save the new preview to file storage if we're using file storage
                if (!string.IsNullOrEmpty(attachment.PreviewPath) || !string.IsNullOrEmpty(attachment.FilePath))
                {
                    var oldPreviewPath = attachment.PreviewPath;
                    var previewPath = await _fileStorage.SaveFileAsync(
                        preview,
                        $"preview_{attachment.Id}",
                        "image/jpeg",
                        showcaseId,  // Now passing the showcase ID
                        cancellationToken);

                    attachment.PreviewPath = previewPath;

                    // Delete the old preview file after successful save
                    if (!string.IsNullOrEmpty(oldPreviewPath) && oldPreviewPath != previewPath)
                    {
                        try
                        {
                            await _fileStorage.DeleteFileAsync(oldPreviewPath, cancellationToken);
                        }
                        catch
                        {
                            // Ignore errors when deleting old preview
                        }
                    }
                }

                // Only store preview in the database when using the Database storage provider
                if (_storageSettings.Provider == StorageProvider.Database)
                {
                    if (attachment.AttachmentPreview != null)
                    {
                        attachment.AttachmentPreview.PreviewThumbnail = preview;
                    }
                    else
                    {
                        attachment.AttachmentPreview = new AttachmentPreview
                        {
                            Id = attachment.Id,
                            PreviewThumbnail = preview,
                            Attachment = attachment,
                        };
                    }
                }
            }

            // Update modification timestamp to bust cache
            attachment.LastModified = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            // Log the error for debugging
            System.Diagnostics.Debug.WriteLine($"RotateAttachmentCommand failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            throw; // Re-throw to see the actual error
        }
    }
}
