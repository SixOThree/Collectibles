using Collectibles.Application.Interfaces;
using Collectibles.Domain.Configuration.Storage;
using Collectibles.Domain.Entities;
using Collectibles.Domain.Enums;
using Collectibles.Domain.Interfaces;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Collectibles.Application.Features.Attachments.Commands;

/// <summary>
/// Response DTO for direct upload initiation.
/// </summary>
public record DirectUploadInitiation
{
    /// <summary>
    /// Gets unique identifier for this upload session.
    /// </summary>
    public required string UploadId { get; init; }

    /// <summary>
    /// Gets the SAS URL to upload the file directly to Azure Blob Storage.
    /// </summary>
    public required string SasUrl { get; init; }

    /// <summary>
    /// Gets the blob name/path where the file will be stored.
    /// </summary>
    public required string BlobName { get; init; }

    /// <summary>
    /// Gets when the SAS URL expires (UTC).
    /// </summary>
    public required DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Command to initiate a direct upload to Azure Blob Storage.
/// Returns a SAS URL that the client can use to upload directly.
/// </summary>
public record InitiateDirectUploadCommand : IRequest<DirectUploadInitiation>
{
    /// <summary>
    /// Gets the original file name.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public required long FileSize { get; init; }

    /// <summary>
    /// Gets the MIME type of the file.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Gets optional showcase ID for folder organization.
    /// </summary>
    public long? ShowcaseId { get; init; }
}

public class InitiateDirectUploadCommandValidator : AbstractValidator<InitiateDirectUploadCommand>
{
    public InitiateDirectUploadCommandValidator()
    {
        RuleFor(v => v.FileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(v => v.FileSize)
            .GreaterThan(0)
            .WithMessage("File size must be greater than 0.");

        RuleFor(v => v.ContentType)
            .NotEmpty()
            .MaximumLength(100);
    }
}

public class InitiateDirectUploadCommandHandler : IRequestHandler<InitiateDirectUploadCommand, DirectUploadInitiation>
{
    private readonly IApplicationDbContextFactory _contextFactory;
    private readonly IFileStorage _fileStorage;
    private readonly StorageSettings _storageSettings;
    private readonly IEventLogService _eventLogService;
    private readonly ICurrentUserService _currentUserService;

    public InitiateDirectUploadCommandHandler(
        IApplicationDbContextFactory contextFactory,
        IFileStorage fileStorage,
        IOptions<StorageSettings> storageOptions,
        IEventLogService eventLogService,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _fileStorage = fileStorage;
        _storageSettings = storageOptions.Value;
        _eventLogService = eventLogService;
        _currentUserService = currentUserService;
    }

    public async Task<DirectUploadInitiation> Handle(InitiateDirectUploadCommand request, CancellationToken cancellationToken)
    {
        if (request.ShowcaseId.HasValue)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var showcaseOwnerId = await context.Showcases
                .Where(s => s.Id == request.ShowcaseId.Value)
                .Select(s => s.UserId)
                .FirstOrDefaultAsync(cancellationToken);

            if (showcaseOwnerId == null)
            {
                throw new UnauthorizedAccessException("You are not authorized to upload to this showcase.");
            }

            if (string.IsNullOrEmpty(_currentUserService.UserId) || showcaseOwnerId != _currentUserService.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to upload to this showcase.");
            }
        }

        // Verify direct upload is supported and enabled
        if (!_fileStorage.SupportsDirectUpload)
        {
            throw new NotSupportedException(
                "Direct upload is not supported with the current storage provider. " +
                "Direct upload requires Azure Blob Storage.");
        }

        var directUploadSettings = _storageSettings.DirectUpload ?? new DirectUploadSettings();
        if (!directUploadSettings.Enabled)
        {
            throw new InvalidOperationException("Direct upload is disabled in configuration.");
        }

        // Verify Azure Blob Storage is configured
        if (_storageSettings.Provider != StorageProvider.AzureBlobStorage)
        {
            throw new NotSupportedException(
                "Direct upload requires Azure Blob Storage to be configured as the storage provider.");
        }

        // Generate a unique upload ID
        var uploadId = Guid.NewGuid().ToString("N");

        // Generate the blob name
        var blobName = _fileStorage.GenerateBlobName(request.FileName, request.ShowcaseId);

        // Calculate expiry
        var expiryMinutes = directUploadSettings.SasExpiryMinutes > 0
            ? directUploadSettings.SasExpiryMinutes
            : 30;
        var expiry = TimeSpan.FromMinutes(expiryMinutes);
        var expiresAt = DateTime.UtcNow.Add(expiry);

        // Generate SAS URL with write permissions
        var sasUrl = _fileStorage.GenerateUploadSasUrl(blobName, expiry, request.ContentType);

        var result = new DirectUploadInitiation
        {
            UploadId = uploadId,
            SasUrl = sasUrl,
            BlobName = blobName,
            ExpiresAt = expiresAt,
        };

        // Log the upload initiation event
        await _eventLogService.LogEventAsync(
            EventAction.Upload,
            nameof(Attachment),
            null,
            request.FileName,
            null,
            new
            {
                FileName = request.FileName,
                FileSize = request.FileSize,
                ContentType = request.ContentType,
                ShowcaseId = request.ShowcaseId,
                UploadId = uploadId,
                DirectUpload = true,
                Phase = "Initiated",
            },
            cancellationToken: cancellationToken);

        return result;
    }
}
