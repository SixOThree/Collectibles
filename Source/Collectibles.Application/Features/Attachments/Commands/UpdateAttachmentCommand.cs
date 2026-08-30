using Collectibles.Application.Common;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Common;
using Collectibles.Domain.Common.Enums;
using Collectibles.Domain.Configuration.Storage;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Enums;
using Collectibles.Domain.Interfaces;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Collectibles.Application.Features.Attachments.Commands;

public record UpdateAttachmentCommand : IRequest
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string? OriginalFilename { get; set; }
    public string? FileType { get; set; }
    public AttachmentType? AttachmentType { get; set; }
    public string? Base64Content { get; set; }
    public string? Base64PreviewThumbnail { get; set; }
    public long? ShowcaseId { get; set; }
}

public class UpdateAttachmentCommandValidator : AbstractValidator<UpdateAttachmentCommand>
{
    public UpdateAttachmentCommandValidator()
    {
        RuleFor(v => v.Name)
            .MaximumLength(255)
            .NotEmpty();

        RuleFor(v => v.OriginalFilename)
            .MaximumLength(255);

        RuleFor(v => v.FileType)
            .MaximumLength(100)
            .Must(AttachmentContentRules.BeAnAcceptableContentType)
            .WithMessage(AttachmentContentRules.UnsupportedContentTypeMessage);

        RuleFor(v => v.Base64Content)
            .Must(BeValidBase64)
            .When(v => !string.IsNullOrEmpty(v.Base64Content))
            .WithMessage("Content must be valid base64 encoded string.");

        RuleFor(v => v.Base64PreviewThumbnail)
            .Must(BeValidBase64)
            .When(v => !string.IsNullOrEmpty(v.Base64PreviewThumbnail))
            .WithMessage("Preview thumbnail must be valid base64 encoded string.");

        // A preview is served inline, so its bytes must be an image and not merely claim to be.
        RuleFor(v => v.Base64PreviewThumbnail)
            .Must(AttachmentContentRules.BeARecognisedImage)
            .When(v => !string.IsNullOrEmpty(v.Base64PreviewThumbnail))
            .WithMessage(AttachmentContentRules.PreviewNotAnImageMessage);
    }

    private bool BeValidBase64(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        try
        {
            Convert.FromBase64String(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class UpdateAttachmentCommandHandler : IRequestHandler<UpdateAttachmentCommand>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly IFileStorage _fileStorage;
    private readonly IFileProcessingService _fileProcessingService;
    private readonly IEventLogService _eventLogService;
    private readonly StorageSettings _storageSettings;
    private readonly ICurrentUserService _currentUserService;

    public UpdateAttachmentCommandHandler(
        IApplicationDbContextFactory contextFactory,
        IFileStorage fileStorage,
        IFileProcessingService fileProcessingService,
        IEventLogService eventLogService,
        IOptions<StorageSettings> storageOptions,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _fileStorage = fileStorage;
        _fileProcessingService = fileProcessingService;
        _eventLogService = eventLogService;
        _storageSettings = storageOptions.Value;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateAttachmentCommand request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context.Attachments
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            throw new ArgumentException($"Attachment with ID {request.Id} not found.", nameof(request));
        }

        // Verify current user owns this attachment
        var ownerShowcaseIds = await context.CollectibleItemAttachments
            .Where(cia => cia.AttachmentId == entity.Id)
            .Select(cia => cia.CollectibleItem)
            .SelectMany(ci => ci.Showcases)
            .Select(s => s.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var isOwner = ownerShowcaseIds.Contains(_currentUserService.UserId);
        if (!isOwner && entity.CreatedBy != _currentUserService.UserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to update this attachment.");
        }

        // Determine showcase ID if not provided
        long? showcaseId = request.ShowcaseId;
        if (!showcaseId.HasValue)
        {
            // Try to get showcase ID from the attachment's relationship with collectible items
            var showcaseIds = await context.CollectibleItemAttachments
                .Where(cia => cia.AttachmentId == entity.Id)
                .Select(cia => cia.CollectibleItem)
                .SelectMany(ci => ci.Showcases)
                .Select(s => s.Id)
                .Distinct()
                .ToListAsync(cancellationToken);

            // Use the first showcase ID if available
            showcaseId = showcaseIds.FirstOrDefault();
        }

        // Capture old values for event logging
        var oldValues = new
        {
            Name = entity.Name,
            OriginalFilename = entity.OriginalFilename,
            FileType = entity.FileType,
            AttachmentType = entity.AttachmentType,
            FileSize = entity.FileSize,
            HasContent = entity.AttachmentContent != null || !string.IsNullOrEmpty(entity.FilePath),
            HasPreview = entity.AttachmentPreview != null || !string.IsNullOrEmpty(entity.PreviewPath),
        };

        entity.Name = request.Name;
        entity.OriginalFilename = request.OriginalFilename;

        // The declared type is a caller-supplied hint and is what later responses would announce
        // to a browser. Screen it here; where new content is supplied the signature refines it
        // further below.
        var storedFileType = FileContentType.IsAcceptableDeclaredType(request.FileType)
            ? request.FileType
            : FileContentType.Fallback;

        entity.AttachmentType = request.AttachmentType;

        // Track the GUID for file naming consistency
        string? fileGuid = null;

        // Blobs the update supersedes. They are deleted only after the database write is
        // committed: deleting first would destroy the content if the save then failed.
        var supersededPaths = new List<string>();

        if (!string.IsNullOrEmpty(request.Base64Content))
        {
            var content = Convert.FromBase64String(request.Base64Content);

            // New content is in memory, so its own signature decides the stored type.
            storedFileType = FileContentType.ResolveStoredType(content, request.FileType);

            // Update file size
            entity.FileSize = content.Length;

            // Remember the old file; it is removed after the save succeeds
            if (!string.IsNullOrEmpty(entity.FilePath))
            {
                supersededPaths.Add(entity.FilePath);
            }

            // Generate a new GUID for the updated file
            fileGuid = Guid.NewGuid().ToString("N");
            var originalFileName = request.OriginalFilename ?? request.Name;
            var extension = Path.GetExtension(originalFileName);
            var guidFileName = $"{fileGuid}{extension}";

            // Save new file
            entity.FilePath = await _fileStorage.SaveFileAsync(
                content,
                guidFileName,
                storedFileType ?? FileContentType.Fallback,
                showcaseId,
                cancellationToken);

            // Only store content in the database when using the Database storage provider
            if (_storageSettings.Provider == StorageProvider.Database)
            {
                if (entity.AttachmentContent == null)
                {
                    entity.AttachmentContent = new AttachmentContent();
                }

                entity.AttachmentContent.Content = content;
            }
        }

        if (!string.IsNullOrEmpty(request.Base64PreviewThumbnail))
        {
            var previewThumbnail = Convert.FromBase64String(request.Base64PreviewThumbnail);

            // Remember the old preview; it is removed after the save succeeds
            if (!string.IsNullOrEmpty(entity.PreviewPath))
            {
                supersededPaths.Add(entity.PreviewPath);
            }

            // Use the same GUID as the main file if it was updated, otherwise generate a new one
            if (string.IsNullOrEmpty(fileGuid))
            {
                // Extract GUID from existing file path if available, otherwise generate new
                if (!string.IsNullOrEmpty(entity.FilePath))
                {
                    var fileName = Path.GetFileNameWithoutExtension(entity.FilePath);

                    // Check if it looks like a GUID (32 hex characters)
                    if (fileName != null && fileName.Length == 32 && System.Text.RegularExpressions.Regex.IsMatch(fileName, "^[a-f0-9]{32}$"))
                    {
                        fileGuid = fileName;
                    }
                    else
                    {
                        fileGuid = Guid.NewGuid().ToString("N");
                    }
                }
                else
                {
                    fileGuid = Guid.NewGuid().ToString("N");
                }
            }

            // Save new preview with matching GUID
            var previewFileName = $"{fileGuid}_preview.jpg";
            entity.PreviewPath = await _fileStorage.SaveFileAsync(
                previewThumbnail,
                previewFileName,
                "image/jpeg",
                showcaseId,
                cancellationToken);

            // Only store preview in the database when using the Database storage provider
            if (_storageSettings.Provider == StorageProvider.Database)
            {
                if (entity.AttachmentPreview == null)
                {
                    entity.AttachmentPreview = new AttachmentPreview();
                }

                entity.AttachmentPreview.PreviewThumbnail = previewThumbnail;
            }
        }
        else if (!string.IsNullOrEmpty(request.Base64Content) && string.IsNullOrEmpty(entity.PreviewPath))
        {
            // Generate preview if content changed but no preview provided
            var content = Convert.FromBase64String(request.Base64Content);
            if (request.FileType != null)
            {
                var previewThumbnail = await _fileProcessingService.GeneratePreviewAsync(
                    content,
                    request.FileType,
                    cancellationToken);

                if (previewThumbnail != null)
                {
                    // Use the same GUID as the main file
                    if (string.IsNullOrEmpty(fileGuid))
                    {
                        fileGuid = Guid.NewGuid().ToString("N");
                    }

                    var previewFileName = $"{fileGuid}_preview.jpg";
                    entity.PreviewPath = await _fileStorage.SaveFileAsync(
                        previewThumbnail,
                        previewFileName,
                        "image/jpeg",
                        showcaseId,
                        cancellationToken);

                    // Only store preview in the database when using the Database storage provider
                    if (_storageSettings.Provider == StorageProvider.Database)
                    {
                        if (entity.AttachmentPreview == null)
                        {
                            entity.AttachmentPreview = new AttachmentPreview();
                        }

                        entity.AttachmentPreview.PreviewThumbnail = previewThumbnail;
                    }
                }
            }
        }

        // Assigned last so it reflects any refinement the new content's signature produced above.
        entity.FileType = storedFileType;

        await context.SaveChangesAsync(cancellationToken);

        // The new content is now committed, so removing the superseded blobs is safe.
        foreach (var supersededPath in supersededPaths)
        {
            if (supersededPath != entity.FilePath && supersededPath != entity.PreviewPath)
            {
                await _fileStorage.DeleteFileAsync(supersededPath, cancellationToken);
            }
        }

        // Log the update event
        var newValues = new
        {
            Name = request.Name,
            OriginalFilename = request.OriginalFilename,
            FileType = request.FileType,
            AttachmentType = request.AttachmentType,
            FileSize = entity.FileSize,
            ContentUpdated = !string.IsNullOrEmpty(request.Base64Content),
            PreviewUpdated = !string.IsNullOrEmpty(request.Base64PreviewThumbnail),
        };

        var logTask = _eventLogService.LogEventAsync(
            EventAction.Update,
            nameof(Attachment),
            entity.Id,
            entity.Name,
            oldValues,
            newValues,
            cancellationToken: cancellationToken);
        if (logTask != null)
        {
            await logTask;
        }
    }
}
