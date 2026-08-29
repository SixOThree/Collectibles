using Collectibles.Application.Interfaces;
using Collectibles.Domain.Common.Enums;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Interfaces;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Collectibles.Application.Features.Attachments.Commands;

/// <summary>
/// Command to complete a direct upload after the client has uploaded to Azure.
/// Creates the Attachment entity and generates preview thumbnail.
/// </summary>
public record CompleteDirectUploadCommand : IRequest<long>
{
    /// <summary>
    /// Gets the upload ID from InitiateDirectUploadCommand.
    /// </summary>
    public required string UploadId { get; init; }

    /// <summary>
    /// Gets the blob name where the file was uploaded.
    /// </summary>
    public required string BlobName { get; init; }

    /// <summary>
    /// Gets the original file name.
    /// </summary>
    public required string OriginalFileName { get; init; }

    /// <summary>
    /// Gets the MIME type of the file.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public required long FileSize { get; init; }

    /// <summary>
    /// Gets the attachment type.
    /// </summary>
    public AttachmentType? AttachmentType { get; init; }

    /// <summary>
    /// Gets optional showcase ID for organization.
    /// </summary>
    public long? ShowcaseId { get; init; }
}

public class CompleteDirectUploadCommandValidator : AbstractValidator<CompleteDirectUploadCommand>
{
    public CompleteDirectUploadCommandValidator()
    {
        RuleFor(v => v.UploadId)
            .NotEmpty()
            .Length(32)
            .WithMessage("Invalid upload ID format.");

        RuleFor(v => v.BlobName)
            .NotEmpty()
            .MaximumLength(1024);

        RuleFor(v => v.OriginalFileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(v => v.ContentType)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(v => v.FileSize)
            .GreaterThan(0)
            .WithMessage("File size must be greater than 0.");
    }
}

public class CompleteDirectUploadCommandHandler : IRequestHandler<CompleteDirectUploadCommand, long>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly IFileProcessingService _fileProcessingService;
    private readonly IFileStorage _fileStorage;
    private readonly IEventLogService _eventLogService;
    private readonly IAttachmentHashService _hashService;
    private readonly ICurrentUserService _currentUserService;

    // Maximum file size to process for preview generation (50MB)
    private const long MaxPreviewProcessingSize = 50 * 1024 * 1024;

    public CompleteDirectUploadCommandHandler(
        IApplicationDbContextFactory contextFactory,
        IFileProcessingService fileProcessingService,
        IFileStorage fileStorage,
        IEventLogService eventLogService,
        IAttachmentHashService hashService,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _fileProcessingService = fileProcessingService;
        _fileStorage = fileStorage;
        _eventLogService = eventLogService;
        _hashService = hashService;
        _currentUserService = currentUserService;
    }

    public async Task<long> Handle(CompleteDirectUploadCommand request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Verify showcase ownership if ShowcaseId is provided
        if (request.ShowcaseId.HasValue)
        {
            var showcase = await context.Showcases
                .FirstOrDefaultAsync(s => s.Id == request.ShowcaseId.Value, cancellationToken);

            if (showcase == null || showcase.UserId != _currentUserService.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to upload to this showcase.");
            }
        }

        // Verify the blob exists in storage
        var fileExists = await _fileStorage.FileExistsAsync(request.BlobName, cancellationToken);
        if (!fileExists)
        {
            throw new InvalidOperationException(
                $"The file was not found in storage. The upload may have failed or the SAS URL expired. " +
                $"Blob: {request.BlobName}");
        }

        // Verify the file size matches (optional security check)
        var actualSize = await _fileStorage.GetFileSizeAsync(request.BlobName, cancellationToken);
        if (actualSize.HasValue && Math.Abs(actualSize.Value - request.FileSize) > 1024)
        {
            // Allow small difference for potential encoding differences, but flag large mismatches
            throw new InvalidOperationException(
                $"File size mismatch. Expected: {request.FileSize}, Actual: {actualSize.Value}. " +
                $"The upload may have been corrupted or tampered with.");
        }

        string? previewPath = null;
        byte[]? previewThumbnail = null;
        string? contentHash = null;

        // Generate preview thumbnail and compute hash for files under the size limit
        if (request.FileSize <= MaxPreviewProcessingSize)
        {
            try
            {
                // Fetch the file content from storage for preview generation and hash computation
                var fileContent = await _fileStorage.GetFileAsync(request.BlobName, cancellationToken);
                if (fileContent != null)
                {
                    // Compute content hash for duplicate detection
                    contentHash = _hashService.ComputeHash(fileContent);

                    previewThumbnail = await _fileProcessingService.GeneratePreviewAsync(
                        fileContent,
                        request.ContentType,
                        cancellationToken);

                    if (previewThumbnail != null)
                    {
                        // Extract the GUID from the blob name (remove subfolder prefixes if any)
                        var lastSegment = request.BlobName.Split('/').Last();
                        var guidPart = Path.GetFileNameWithoutExtension(lastSegment);
                        var previewFileName = $"{guidPart}_preview.jpg";

                        // Let SaveFileAsync handle path construction (subfolder + showcase folder)
                        // to avoid double-nesting when SubfolderPath is configured
                        previewPath = await _fileStorage.SaveFileAsync(
                            previewThumbnail,
                            previewFileName,
                            "image/jpeg",
                            request.ShowcaseId,
                            cancellationToken);
                    }
                }
            }
            catch (Exception)
            {
                // Preview generation failed - continue without preview
                // This is non-critical, the attachment can still be created
                // Hash may also be null if file fetch failed
                previewThumbnail = null;
                previewPath = null;
            }
        }

        // Create the attachment entity
        var entity = new Attachment
        {
            Name = Path.GetFileNameWithoutExtension(request.OriginalFileName),
            OriginalFilename = request.OriginalFileName,
            FileType = request.ContentType,
            AttachmentType = request.AttachmentType,
            FilePath = request.BlobName,
            PreviewPath = previewPath,
            FileSize = request.FileSize,
            ContentHash = contentHash,
            HashComputedAt = contentHash != null ? DateTime.UtcNow : null,
        };

        // For direct uploads, we don't store content in the database
        // The file is already in Azure Blob Storage
        // This differs from regular uploads which store a backup in the database
        context.Attachments.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        // Log the creation event
        var logTask = _eventLogService.LogEventAsync(
            EventAction.Upload,
            nameof(Attachment),
            entity.Id,
            entity.Name,
            null,
            new
            {
                Name = entity.Name,
                OriginalFilename = request.OriginalFileName,
                FileType = request.ContentType,
                AttachmentType = request.AttachmentType,
                FileSize = request.FileSize,
                HasPreview = previewPath != null,
                DirectUpload = true,
                UploadId = request.UploadId,
            },
            cancellationToken: cancellationToken);

        if (logTask != null)
        {
            await logTask;
        }

        return entity.Id;
    }
}
