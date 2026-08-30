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

using Microsoft.Extensions.Options;

namespace Collectibles.Application.Features.Attachments.Commands;

public record CreateAttachmentCommand : IRequest<long>
{
    public string Name { get; set; } = default!;
    public string? OriginalFilename { get; set; }
    public string? FileType { get; set; }
    public AttachmentType? AttachmentType { get; set; }
    public string? Base64Content { get; set; }
    public string? Base64PreviewThumbnail { get; set; }
    public long? ShowcaseId { get; set; }
}

public class CreateAttachmentCommandValidator : AbstractValidator<CreateAttachmentCommand>
{
    public CreateAttachmentCommandValidator()
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

public class CreateAttachmentCommandHandler : IRequestHandler<CreateAttachmentCommand, long>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly IFileProcessingService _fileProcessingService;
    private readonly IFileStorage _fileStorage;
    private readonly IEventLogService _eventLogService;
    private readonly IAttachmentHashService _hashService;
    private readonly StorageSettings _storageSettings;
    private readonly ICurrentUserService _currentUserService;

    public CreateAttachmentCommandHandler(
        IApplicationDbContextFactory contextFactory,
        IFileProcessingService fileProcessingService,
        IFileStorage fileStorage,
        IEventLogService eventLogService,
        IAttachmentHashService hashService,
        IOptions<StorageSettings> storageOptions,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _fileProcessingService = fileProcessingService;
        _fileStorage = fileStorage;
        _eventLogService = eventLogService;
        _hashService = hashService;
        _storageSettings = storageOptions.Value;
        _currentUserService = currentUserService;
    }

    public async Task<long> Handle(CreateAttachmentCommand request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        await AttachmentAuthorization.EnsureShowcaseOwnedAsync(context, request.ShowcaseId, _currentUserService, cancellationToken);

        byte[]? content = null;
        byte[]? previewThumbnail = null;
        string? filePath = null;
        string? previewPath = null;

        if (!string.IsNullOrEmpty(request.Base64Content))
        {
            content = Convert.FromBase64String(request.Base64Content);

            // Generate preview thumbnail if not provided
            if (string.IsNullOrEmpty(request.Base64PreviewThumbnail) && request.FileType != null)
            {
                previewThumbnail = await _fileProcessingService.GeneratePreviewAsync(
                    content,
                    request.FileType,
                    cancellationToken);
            }
        }

        // Use provided thumbnail if available
        if (!string.IsNullOrEmpty(request.Base64PreviewThumbnail))
        {
            previewThumbnail = Convert.FromBase64String(request.Base64PreviewThumbnail);
        }

        // The declared type is a caller-supplied hint, and it is what later responses would
        // announce to a browser. Derive it from the content's own signature where the content is
        // recognisable, so the uploader does not get to choose how their bytes are interpreted.
        var storedFileType = FileContentType.ResolveStoredType(content, request.FileType);

        // Save files to external storage
        // Generate a common GUID for both main file and preview to keep them related
        var fileGuid = Guid.NewGuid().ToString("N");

        if (content != null)
        {
            var originalFileName = request.OriginalFilename ?? request.Name;
            var extension = Path.GetExtension(originalFileName);
            var guidFileName = $"{fileGuid}{extension}";

            filePath = await _fileStorage.SaveFileAsync(
                content,
                guidFileName,
                storedFileType ?? FileContentType.Fallback,
                request.ShowcaseId,
                cancellationToken);
        }

        if (previewThumbnail != null)
        {
            // Use the same GUID with _preview suffix for the preview file
            var previewFileName = $"{fileGuid}_preview.jpg";
            previewPath = await _fileStorage.SaveFileAsync(
                previewThumbnail,
                previewFileName,
                "image/jpeg",
                request.ShowcaseId,
                cancellationToken);
        }

        // Compute content hash for duplicate detection
        string? contentHash = null;
        if (content != null)
        {
            contentHash = _hashService.ComputeHash(content);
        }

        var entity = new Attachment
        {
            Name = request.Name,
            OriginalFilename = request.OriginalFilename,
            FileType = storedFileType,
            AttachmentType = request.AttachmentType,
            FilePath = filePath,
            PreviewPath = previewPath,
            FileSize = content?.Length ?? 0,
            ContentHash = contentHash,
            HashComputedAt = contentHash != null ? DateTime.UtcNow : null,
        };

        // Only store content/preview in the database when using the Database storage provider.
        // External providers (Azure, LocalFileSystem) store files via IFileStorage above.
        if (_storageSettings.Provider == StorageProvider.Database)
        {
            if (content != null)
            {
                entity.AttachmentContent = new AttachmentContent
                {
                    Content = content,
                };
            }

            if (previewThumbnail != null)
            {
                entity.AttachmentPreview = new AttachmentPreview
                {
                    PreviewThumbnail = previewThumbnail,
                };
            }
        }

        context.Attachments.Add(entity);

        await context.SaveChangesAsync(cancellationToken);

        // Log the creation event
        // Best-effort logging; avoid failing when mocked service returns null Task
        var logTask = _eventLogService.LogEventAsync(
            EventAction.Upload,
            nameof(Attachment),
            entity.Id,
            entity.Name,
            null,
            new
            {
                Name = request.Name,
                OriginalFilename = request.OriginalFilename,
                FileType = request.FileType,
                AttachmentType = request.AttachmentType,
                FileSize = entity.FileSize,
                HasPreview = previewThumbnail != null || !string.IsNullOrEmpty(previewPath),
            },
            cancellationToken: cancellationToken);
        if (logTask != null)
        {
            await logTask;
        }

        return entity.Id;
    }
}
