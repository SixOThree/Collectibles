using Collectibles.Application.Common;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Common;
using Collectibles.Domain.Common.Enums;
using Collectibles.Domain.Constants;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Interfaces;

using FluentValidation;

using MediatR;

namespace Collectibles.Application.Features.Attachments.Commands;

public class CreateAttachmentStreamCommand : IRequest<long>
{
    public string Name { get; set; } = string.Empty;
    public string? OriginalFilename { get; set; }
    public string? FileType { get; set; }
    public AttachmentType AttachmentType { get; set; }
    public Stream? FileStream { get; set; }
    public long FileSize { get; set; }
    public byte[]? PreviewThumbnail { get; set; }
    public long? ShowcaseId { get; set; }
}

/// <summary>
/// Mirrors <see cref="CreateAttachmentCommandValidator"/>: the streaming twin previously
/// had no validator at all, so its inputs were unchecked.
/// </summary>
public class CreateAttachmentStreamCommandValidator : AbstractValidator<CreateAttachmentStreamCommand>
{
    public CreateAttachmentStreamCommandValidator()
    {
        RuleFor(v => v.Name)
            .MaximumLength(ApplicationConstants.ValidationLengths.FileNameMaxLength)
            .NotEmpty();

        RuleFor(v => v.OriginalFilename)
            .MaximumLength(ApplicationConstants.ValidationLengths.FileNameMaxLength);

        RuleFor(v => v.FileType)
            .MaximumLength(ApplicationConstants.ValidationLengths.TypeMaxLength)
            .Must(AttachmentContentRules.BeAnAcceptableContentType)
            .WithMessage(AttachmentContentRules.UnsupportedContentTypeMessage);

        RuleFor(v => v.FileSize)
            .GreaterThanOrEqualTo(0);

        // A preview is served inline, so its bytes must be an image and not merely claim to be.
        RuleFor(v => v.PreviewThumbnail)
            .Must(bytes => bytes == null || bytes.Length == 0 || FileContentType.TryResolveImageType(bytes, out _))
            .WithMessage(AttachmentContentRules.PreviewNotAnImageMessage);
    }
}

public class CreateAttachmentStreamCommandHandler : IRequestHandler<CreateAttachmentStreamCommand, long>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly IFileStorage _fileStorage;
    private readonly IFileProcessingService _fileProcessingService;
    private readonly IEventLogService _eventLogService;
    private readonly IAttachmentHashService _hashService;
    private readonly ICurrentUserService _currentUserService;

    public CreateAttachmentStreamCommandHandler(
        IApplicationDbContextFactory contextFactory,
        IFileStorage fileStorage,
        IFileProcessingService fileProcessingService,
        IEventLogService eventLogService,
        IAttachmentHashService hashService,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _fileStorage = fileStorage;
        _fileProcessingService = fileProcessingService;
        _eventLogService = eventLogService;
        _hashService = hashService;
        _currentUserService = currentUserService;
    }

    public async Task<long> Handle(CreateAttachmentStreamCommand request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        await AttachmentAuthorization.EnsureShowcaseOwnedAsync(context, request.ShowcaseId, _currentUserService, cancellationToken);

        string? filePath = null;
        string? previewPath = null;
        string? contentHash = null;
        byte[]? previewThumbnail = request.PreviewThumbnail;

        // Content arrives as a stream, so the signature is not available here without buffering.
        // The declared type is screened instead, and the preview routes derive what they announce
        // from the stored bytes themselves, so an unscreened type cannot reach a browser.
        var storedFileType = FileContentType.IsAcceptableDeclaredType(request.FileType)
            ? request.FileType
            : FileContentType.Fallback;

        // Generate a common GUID for both main file and preview to keep them related
        var fileGuid = Guid.NewGuid().ToString("N");

        // Save the main file using stream
        if (request.FileStream != null)
        {
            var originalFileName = request.OriginalFilename ?? request.Name;
            var extension = Path.GetExtension(originalFileName);
            var guidFileName = $"{fileGuid}{extension}";

            // Generate preview thumbnail if it's an image and we don't have one
            if (previewThumbnail == null && storedFileType != null && storedFileType.StartsWith("image/", StringComparison.Ordinal))
            {
                // For images, we need to read a small portion to generate preview
                // But only for reasonable sized images
                if (request.FileSize < 50 * 1024 * 1024) // Less than 50MB
                {
                    // Copy the stream to memory to generate preview
                    using var memoryStream = new MemoryStream();
                    await request.FileStream.CopyToAsync(memoryStream, cancellationToken);
                    var imageBytes = memoryStream.ToArray();

                    // Compute content hash for duplicate detection
                    contentHash = _hashService.ComputeHash(imageBytes);

                    previewThumbnail = await _fileProcessingService.GeneratePreviewAsync(
                        imageBytes,
                        storedFileType,
                        cancellationToken);

                    // Reset memory stream position for saving the file
                    memoryStream.Position = 0;

                    // Save the file using the memory stream
                    filePath = await _fileStorage.SaveFileAsync(
                        memoryStream,
                        guidFileName,
                        storedFileType,
                        request.ShowcaseId,
                        cancellationToken);
                }
                else
                {
                    // For large files, skip preview generation and save directly
                    // Hash will be computed later by background service
                    filePath = await _fileStorage.SaveFileAsync(
                        request.FileStream,
                        guidFileName,
                        storedFileType,
                        request.ShowcaseId,
                        cancellationToken);
                }
            }
            else
            {
                // Not an image or already has preview
                // For smaller non-image files, read into memory to compute hash
                if (request.FileSize < 50 * 1024 * 1024) // Less than 50MB
                {
                    using var memoryStream = new MemoryStream();
                    await request.FileStream.CopyToAsync(memoryStream, cancellationToken);
                    var fileBytes = memoryStream.ToArray();

                    // Compute content hash for duplicate detection
                    contentHash = _hashService.ComputeHash(fileBytes);

                    // Reset memory stream position for saving the file
                    memoryStream.Position = 0;

                    filePath = await _fileStorage.SaveFileAsync(
                        memoryStream,
                        guidFileName,
                        storedFileType,
                        request.ShowcaseId,
                        cancellationToken);
                }
                else
                {
                    // Large non-image file, save directly
                    // Hash will be computed later by background service
                    filePath = await _fileStorage.SaveFileAsync(
                        request.FileStream,
                        guidFileName,
                        storedFileType,
                        request.ShowcaseId,
                        cancellationToken);
                }
            }
        }

        // Save preview thumbnail if we have one
        if (previewThumbnail != null)
        {
            var previewFileName = $"{fileGuid}_preview.jpg";
            previewPath = await _fileStorage.SaveFileAsync(
                previewThumbnail,
                previewFileName,
                "image/jpeg",
                request.ShowcaseId,
                cancellationToken);
        }

        var entity = new Attachment
        {
            Name = request.Name,
            OriginalFilename = request.OriginalFilename,
            FileType = storedFileType,
            AttachmentType = request.AttachmentType,
            FilePath = filePath,
            PreviewPath = previewPath,
            FileSize = request.FileSize,
            ContentHash = contentHash,
            HashComputedAt = contentHash != null ? DateTime.UtcNow : null,
        };

        context.Attachments.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        // Log the creation event
        await _eventLogService.LogEventAsync(
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

        return entity.Id;
    }
}
