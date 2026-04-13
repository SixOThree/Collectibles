using System.IO.Compression;
using Collectibles.Application.Features.Attachments.Commands;
using Collectibles.Application.Features.ZipUpload;
using Collectibles.Application.Interfaces;
using Collectibles.Application.Services;
using Collectibles.Domain.Common.Enums;
using Collectibles.Domain.Interfaces;
using Hangfire;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Collectibles.Infrastructure.Services;

public class ZipUploadJobService : IZipUploadJobService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ZipUploadJobService> _logger;

    public ZipUploadJobService(IServiceProvider serviceProvider, ILogger<ZipUploadJobService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 10, 30, 60 })]
    public async Task ProcessJobAsync(long jobId)
    {
        using var scope = _serviceProvider.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IApplicationDbContextFactory>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var sysLogService = scope.ServiceProvider.GetRequiredService<ISysLogService>();
        var hierarchyService = scope.ServiceProvider.GetRequiredService<IItemHierarchyService>();

        await using var context = await contextFactory.CreateDbContextAsync(CancellationToken.None);

        var job = await context.ZipUploadJobs.FindAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Zip upload job {JobId} not found", jobId);
            return;
        }

        // Atomic claim: only process if we can transition from Pending to Doing
        var dbContext = (DbContext)context;
        var rowsAffected = await dbContext.Database.ExecuteSqlRawAsync(
            "UPDATE ZipUploadJobs SET Status = {0} WHERE Id = {1} AND Status = {2}",
            (int)JobStatus.Doing, jobId, (int)JobStatus.Pending);

        if (rowsAffected == 0)
        {
            _logger.LogInformation("Job {JobId} already claimed by another processor", jobId);
            return;
        }

        // Reload the job entity after the raw SQL update
        await dbContext.Entry(job).ReloadAsync();

        try
        {
            await ProcessJob(job, context, mediator, fileStorage, hierarchyService, scope.ServiceProvider, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing zip upload job {JobId}", job.Id);

            await sysLogService.LogErrorAsync(
                $"Failed to process zip upload job {job.Id}",
                ex,
                "BackgroundJob.ZipUpload",
                new Dictionary<string, object>
                {
                    ["JobId"] = job.Id,
                    ["UserId"] = job.UserId,
                });

            job.Status = JobStatus.Failed;
            job.ErrorDetails = ex.Message;

            // Clean up the file if it exists
            if (!string.IsNullOrEmpty(job.StoragePath))
            {
                try
                {
                    await fileStorage.DeleteFileAsync(job.StoragePath, CancellationToken.None);
                    _logger.LogInformation("Cleaned up failed job file at {StoragePath} for job {JobId}", job.StoragePath, job.Id);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "Failed to cleanup file for failed job {JobId}", job.Id);
                }
            }

            await context.SaveChangesAsync(CancellationToken.None);
            throw; // Re-throw to let Hangfire handle retry logic
        }
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task CleanupOrphanedJobsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IApplicationDbContextFactory>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var sysLogService = scope.ServiceProvider.GetRequiredService<ISysLogService>();

        await using var context = await contextFactory.CreateDbContextAsync(CancellationToken.None);

        // Find jobs stuck in NotStart status for more than 1 hour
        var orphanedTime = DateTime.UtcNow.AddHours(-1);
        var orphanedJobs = await context.ZipUploadJobs
            .Where(j => j.Status == JobStatus.NotStart && j.Created < orphanedTime)
            .ToListAsync(CancellationToken.None);

        if (orphanedJobs.Count != 0)
        {
            _logger.LogWarning("Found {Count} orphaned zip upload jobs to clean up", orphanedJobs.Count);

            foreach (var job in orphanedJobs)
            {
                try
                {
                    // Mark as failed
                    job.Status = JobStatus.Failed;
                    job.ErrorDetails = "Upload abandoned - file transfer was interrupted or incomplete";
                    job.CompletedAt = DateTime.UtcNow;

                    // Try to clean up any partial file
                    if (!string.IsNullOrEmpty(job.StoragePath))
                    {
                        try
                        {
                            await fileStorage.DeleteFileAsync(job.StoragePath, CancellationToken.None);
                            _logger.LogInformation("Cleaned up orphaned file at {StoragePath} for job {JobId}", job.StoragePath, job.Id);
                        }
                        catch
                        {
                            // File might not exist, that's ok
                        }
                    }

                    await sysLogService.LogWarningAsync(
                        $"Cleaned up orphaned zip upload job {job.Id}",
                        "BackgroundJob.ZipUpload",
                        new Dictionary<string, object>
                        {
                            ["JobId"] = job.Id,
                            ["UserId"] = job.UserId,
                            ["CreatedAt"] = job.Created,
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to cleanup orphaned job {JobId}", job.Id);
                }
            }

            await context.SaveChangesAsync(CancellationToken.None);
        }
    }

    private async Task ProcessJob(
        ZipUploadJob job,
        IApplicationDbContext context,
        IMediator mediator,
        IFileStorage fileStorage,
        IItemHierarchyService hierarchyService,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting processing of zip upload job {JobId}", job.Id);

        var sysLogService = serviceProvider.GetRequiredService<ISysLogService>();
        await sysLogService.LogInformationAsync($"Starting zip upload job {job.Id}", "BackgroundJob.ZipUpload", new Dictionary<string, object>
        {
            ["JobId"] = job.Id,
            ["UserId"] = job.UserId,
            ["FileName"] = job.FileName,
        }, cancellationToken);

        // Status already set to Doing by atomic claim in ProcessJobAsync
        job.StartedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            // Retrieve the zip file from storage
            var storagePath = job.StoragePath ?? string.Empty;

            _logger.LogInformation("Attempting to retrieve zip file for job {JobId} from path: {Path}", job.Id, storagePath);

            // First check if file exists
            if (fileStorage is Collectibles.Domain.Interfaces.IFileStorage storageWithExists)
            {
                var exists = await storageWithExists.FileExistsAsync(storagePath, cancellationToken);
                _logger.LogInformation("File existence check for job {JobId}: {Exists}", job.Id, exists);
            }

            var fileStream = await fileStorage.GetFileStreamAsync(storagePath, cancellationToken);

            if (fileStream == null)
            {
                _logger.LogError("Zip file not found for job {JobId} at path: {Path}", job.Id, storagePath);
                throw new InvalidOperationException($"Zip file not found in storage at path: {storagePath}");
            }

            _logger.LogInformation("Successfully retrieved zip file stream for job {JobId}", job.Id);

            using (fileStream)
            {
                // For large ZIP files, ensure we have a seekable stream to prevent memory issues
                Stream zipStream = fileStream;
                if (!fileStream.CanSeek)
                {
                    // Azure Blob streams might not be seekable, which can cause ZipArchive to buffer everything
                    // Create a temporary file stream for large files
                    var tempFile = Path.GetTempFileName();
                    try
                    {
                        using (var tempFileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                        {
                            await fileStream.CopyToAsync(tempFileStream, cancellationToken);
                        }

                        zipStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);

                        // Clean up temp file after we're done
                        zipStream = new TempFileStream(zipStream, tempFile);
                    }
                    catch
                    {
                        // Clean up temp file if something goes wrong
                        if (File.Exists(tempFile))
                        {
                            File.Delete(tempFile);
                        }

                        throw;
                    }
                }

                using (zipStream)
                {
                    using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false);

                    // Analyze structure
                    var folderStructure = AnalyzeZipStructure(archive);
                    job.TotalItems = folderStructure.TotalFolders + folderStructure.TotalFiles;
                    await context.SaveChangesAsync(cancellationToken);

                    // Look up template IDs for auto-assignment
                    var defaultTemplateId = await context.ContentDefinitions
                        .Where(cd => cd.IsDefault && cd.IsActive)
                        .Select(cd => (long?)cd.Id)
                        .FirstOrDefaultAsync(cancellationToken);
                    var groupingTemplateId = await context.ContentDefinitions
                        .Where(cd => cd.Name == "Grouping" && cd.IsActive)
                        .Select(cd => (long?)cd.Id)
                        .FirstOrDefaultAsync(cancellationToken);

                    // Process folder structure
                    var errors = new List<string>();
                    await ProcessFolderStructure(
                        folderStructure,
                        archive,
                        job,
                        context,
                        mediator,
                        hierarchyService,
                        errors,
                        serviceProvider,
                        defaultTemplateId,
                        groupingTemplateId,
                        cancellationToken);

                    // Update job completion
                    job.Status = errors.Count != 0 ? JobStatus.Done : JobStatus.Done;
                    job.CompletedAt = DateTime.UtcNow;
                    job.ErrorDetails = errors.Count != 0 ? string.Join("\n", errors) : null;
                    await context.SaveChangesAsync(cancellationToken);
                }
            }

            // Send completion notification
            var progressService = serviceProvider.GetService<IZipUploadProgressService>();
            if (progressService != null)
            {
                var finalJobDto = new ZipUploadJobDto
                {
                    Id = job.Id,
                    ShowcaseId = job.ShowcaseId,
                    FileName = job.FileName,
                    FileSize = job.FileSize,
                    Status = job.Status,
                    StartedAt = job.StartedAt,
                    CompletedAt = job.CompletedAt,
                    TotalItems = job.TotalItems,
                    ProcessedItems = job.ProcessedItems,
                    FoldersCreated = job.FoldersCreated,
                    FilesAttached = job.FilesAttached,
                    ErrorCount = job.ErrorCount,
                    CurrentItemName = job.CurrentItemName,
                    ErrorDetails = job.ErrorDetails,
                    ProgressPercentage = 100,
                };
                await progressService.SendJobCompleted(job.Id, finalJobDto);
            }

            // Generate collage previews for items that need them
            try
            {
                var previewService = serviceProvider.GetService<ICollectibleItemPreviewService>();
                if (previewService != null)
                {
                    var generatedCount = await previewService.GenerateCollagePreviewsForShowcaseAsync(job.ShowcaseId, cancellationToken);
                    if (generatedCount > 0)
                    {
                        _logger.LogInformation(
                            "Generated {Count} collage previews for showcase {ShowcaseId} after zip upload",
                            generatedCount, job.ShowcaseId);
                    }
                }
            }
            catch (Exception previewEx)
            {
                _logger.LogWarning(previewEx, "Failed to generate collage previews after zip upload for job {JobId}", job.Id);

                // Don't fail the job for preview generation errors
            }

            // Clean up the stored zip file
            if (!string.IsNullOrEmpty(job.StoragePath))
            {
                await fileStorage.DeleteFileAsync(job.StoragePath, cancellationToken);
            }

            _logger.LogInformation("Completed processing of zip upload job {JobId}", job.Id);

            await sysLogService.LogInformationAsync($"Completed zip upload job {job.Id}", "BackgroundJob.ZipUpload", new Dictionary<string, object>
            {
                ["JobId"] = job.Id,
                ["UserId"] = job.UserId,
                ["FileName"] = job.FileName,
                ["TotalItems"] = job.TotalItems,
                ["ProcessedItems"] = job.ProcessedItems,
                ["FoldersCreated"] = job.FoldersCreated,
                ["FilesAttached"] = job.FilesAttached,
                ["ErrorCount"] = job.ErrorCount,
                ["Duration"] = (job.CompletedAt - job.StartedAt)?.TotalSeconds ?? 0,
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            job.Status = JobStatus.Cancelled;
            job.CompletedAt = DateTime.UtcNow;

            // Clean up the file on cancellation
            if (!string.IsNullOrEmpty(job.StoragePath))
            {
                try
                {
                    await fileStorage.DeleteFileAsync(job.StoragePath, cancellationToken);
                    _logger.LogInformation("Cleaned up cancelled job file at {StoragePath} for job {JobId}", job.StoragePath, job.Id);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "Failed to cleanup file for cancelled job {JobId}", job.Id);
                }
            }

            await context.SaveChangesAsync(cancellationToken);

            await sysLogService.LogWarningAsync($"Zip upload job {job.Id} was cancelled", "BackgroundJob.ZipUpload", new Dictionary<string, object>
            {
                ["JobId"] = job.Id,
                ["UserId"] = job.UserId,
            }, cancellationToken);

            throw;
        }
    }

    private static FolderNode AnalyzeZipStructure(ZipArchive archive)
    {
        var root = new FolderNode { Name = "root", FullPath = string.Empty };
        var folderMap = new Dictionary<string, FolderNode> { [string.Empty] = root };

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.FullName))
            {
                continue;
            }

            var parts = entry.FullName.Split('/', '\\');
            var currentPath = string.Empty;
            var currentNode = root;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                var folderName = parts[i];
                if (string.IsNullOrWhiteSpace(folderName))
                {
                    continue;
                }

                currentPath = string.IsNullOrEmpty(currentPath) ? folderName : $"{currentPath}/{folderName}";

                if (!folderMap.TryGetValue(currentPath, out FolderNode? value))
                {
                    var newNode = new FolderNode
                    {
                        Name = folderName,
                        FullPath = currentPath,
                        Parent = currentNode,
                    };
                    currentNode.Children.Add(newNode);
                    value = newNode;
                    folderMap[currentPath] = value;
                    root.TotalFolders++;
                }

                currentNode = value;
            }

            if (!entry.FullName.EndsWith("/") && !entry.FullName.EndsWith("\\"))
            {
                currentNode.Files.Add(entry);
                root.TotalFiles++;
            }
        }

        return root;
    }

    private async Task ProcessFolderStructure(
        FolderNode node,
        ZipArchive archive,
        ZipUploadJob job,
        IApplicationDbContext context,
        IMediator mediator,
        IItemHierarchyService hierarchyService,
        List<string> errors,
        IServiceProvider serviceProvider,
        long? defaultTemplateId,
        long? groupingTemplateId,
        CancellationToken cancellationToken)
    {
        // Use hierarchy service to resolve or create the item for this folder
        long currentItemId;
        if (!string.IsNullOrEmpty(node.FullPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            job.CurrentItemName = node.Name;
            await context.SaveChangesAsync(cancellationToken);

            try
            {
                var pathSegments = node.FullPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var isGrouping = node.Files.Count == 0 && node.Children.Count > 0;
                var templateId = isGrouping ? groupingTemplateId : defaultTemplateId;
                currentItemId = await hierarchyService.ResolveOrCreateHierarchyAsync(
                    job.ShowcaseId, pathSegments, job.UserId, cancellationToken, templateId);
                job.FoldersCreated++;
                job.ProcessedItems++;
                await context.SaveChangesAsync(cancellationToken);
                await SendProgressUpdate(job, serviceProvider);

                // Process files in this folder
                foreach (var file in node.Files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var attachmentId = await ProcessFileWithRetry(file, mediator, job.ShowcaseId, cancellationToken);
                    if (attachmentId.HasValue)
                    {
                        await hierarchyService.LinkAttachmentAsync(currentItemId, attachmentId.Value, cancellationToken);
                        job.FilesAttached++;
                    }
                    else
                    {
                        job.ErrorCount++;
                        errors.Add($"Failed to process file: {file.Name}");
                    }

                    job.ProcessedItems++;
                    await context.SaveChangesAsync(cancellationToken);
                    await SendProgressUpdate(job, serviceProvider);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating collectible item for folder {FolderPath}", node.FullPath);
                errors.Add($"Failed to create item for folder '{node.Name}': {ex.Message}");
                job.ErrorCount++;
            }
        }
        else
        {
            // Root node — skip item creation
            currentItemId = 0;
        }

        // Process child folders
        foreach (var child in node.Children)
        {
            await ProcessFolderStructure(child, archive, job, context, mediator, hierarchyService, errors, serviceProvider, defaultTemplateId, groupingTemplateId, cancellationToken);
        }
    }

    private async Task<long?> ProcessFileWithRetry(
        ZipArchiveEntry file,
        IMediator mediator,
        long showcaseId,
        CancellationToken cancellationToken,
        int maxRetries = 3)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var attachmentType = DetermineAttachmentType(file.Name);
                var contentType = GetContentType(file.Name);
                var fileName = Path.GetFileName(file.Name);

                // Use streaming for files larger than 10MB
                const long streamingThreshold = 10 * 1024 * 1024; // 10MB

                if (file.Length > streamingThreshold)
                {
                    // Use streaming approach for large files
                    using var entryStream = file.Open();
                    var createStreamCommand = new CreateAttachmentStreamCommand
                    {
                        Name = fileName,
                        OriginalFilename = fileName,
                        FileType = contentType,
                        AttachmentType = attachmentType,
                        FileStream = entryStream,
                        FileSize = file.Length,
                        ShowcaseId = showcaseId,
                    };

                    return await mediator.Send(createStreamCommand, cancellationToken);
                }
                else
                {
                    // Use the existing approach for smaller files
                    using var entryStream = file.Open();
                    using var memoryStream = new MemoryStream();
                    await entryStream.CopyToAsync(memoryStream, cancellationToken);
                    var fileBytes = memoryStream.ToArray();

                    var createAttachmentCommand = new CreateAttachmentCommand
                    {
                        Name = fileName,
                        OriginalFilename = fileName,
                        FileType = contentType,
                        AttachmentType = attachmentType,
                        Base64Content = Convert.ToBase64String(fileBytes),
                        ShowcaseId = showcaseId,
                    };

                    return await mediator.Send(createAttachmentCommand, cancellationToken);
                }
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Attempt {Attempt} failed for file {FileName}, retrying...", attempt, file.Name);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process file {FileName} after {MaxRetries} attempts", file.Name, maxRetries);
                return null;
            }
        }

        return null;
    }

    private static AttachmentType DetermineAttachmentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".svg" => AttachmentType.Image,
            ".mp4" or ".avi" or ".mov" or ".wmv" or ".flv" or ".webm" => AttachmentType.Video,
            ".mp3" or ".wav" or ".ogg" or ".m4a" or ".flac" => AttachmentType.Audio,
            ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" or ".txt" => AttachmentType.Document,
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => AttachmentType.Archive,
            _ => AttachmentType.Other,
        };
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".mp4" => "video/mp4",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream",
        };
    }

    private static async Task SendProgressUpdate(ZipUploadJob job, IServiceProvider serviceProvider)
    {
        var progressService = serviceProvider.GetService<IZipUploadProgressService>();
        if (progressService != null)
        {
            var progressPercentage = job.TotalItems > 0
                ? (int)(job.ProcessedItems * 100.0 / job.TotalItems)
                : 0;

            var jobDto = new ZipUploadJobDto
            {
                Id = job.Id,
                ShowcaseId = job.ShowcaseId,
                FileName = job.FileName,
                FileSize = job.FileSize,
                Status = job.Status,
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt,
                TotalItems = job.TotalItems,
                ProcessedItems = job.ProcessedItems,
                FoldersCreated = job.FoldersCreated,
                FilesAttached = job.FilesAttached,
                ErrorCount = job.ErrorCount,
                CurrentItemName = job.CurrentItemName,
                ErrorDetails = job.ErrorDetails,
                ProgressPercentage = progressPercentage,
            };

            await progressService.SendProgressUpdate(job.Id, jobDto);
        }
    }

    private class FolderNode
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public FolderNode? Parent { get; set; }
        public List<FolderNode> Children { get; set; } = new List<FolderNode>();
        public List<ZipArchiveEntry> Files { get; set; } = new List<ZipArchiveEntry>();
        public int TotalFolders { get; set; }
        public int TotalFiles { get; set; }
    }

    private class TempFileStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly string _tempFilePath;

        public TempFileStream(Stream innerStream, string tempFilePath)
        {
            _innerStream = innerStream;
            _tempFilePath = tempFilePath;
        }

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;
        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _innerStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerStream?.Dispose();

                try
                {
                    if (File.Exists(_tempFilePath))
                    {
                        File.Delete(_tempFilePath);
                    }
                }
                catch
                {
                    // Best effort cleanup
                }
            }

            base.Dispose(disposing);
        }
    }
}
