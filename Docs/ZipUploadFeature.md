# Zip Upload Feature

## Overview
The Zip Upload feature allows users to upload zip files that are automatically processed into collectible items based on the folder structure within the zip.

## Features
- **Folder-based organization**: Each folder in the zip creates a new collectible item
- **Parent-child relationships**: Nested folders create hierarchical collectible items  
- **Automatic attachment**: Files within folders are attached to their respective collectible items
- **Background processing**: Upload processing continues even when navigating away
- **Progress tracking**: Real-time progress updates with 2-second refresh intervals
- **Error resilience**: Automatic retry with exponential backoff for failed operations

## How to Use
1. Navigate to "Zip Upload (BG)" in the menu
2. Select a target showcase from the dropdown
3. Choose a zip file to upload
4. Click "Upload & Start Processing"
5. Monitor progress on the same page or navigate away - processing continues

## Background Processing Details
- Uses **Hangfire** for job processing — jobs are enqueued immediately when the upload completes, not polled on a timer
- Hangfire's `QueuePollInterval` is set to zero for near-instant pickup
- Jobs are stored in the database and survive application restarts
- An atomic SQL claim pattern (`Pending → Doing`) prevents duplicate processing across multiple servers
- Automatic retry on failure (3 attempts with 10s, 30s, 60s delays)
- Progress updates via database polling from the UI (2-second refresh intervals, no SignalR dependency)
- Supports multiple concurrent uploads

## Technical Implementation
- **Entity**: `ZipUploadJob` tracks job status and progress
- **Job Service**: `ZipUploadJobService` (implements `IZipUploadJobService`) processes jobs via Hangfire
- **Upload Paths**: Three entry points — standard upload (`CreateZipUploadJobCommand`), direct-to-Azure (`CompleteZipDirectUploadCommand`), and chunked upload (`UploadZipChunkCommand`)
- **UI**: `ZipUploadBackgroundSimple.razor` provides polling-based UI
- **Storage**: Temporary zip files stored via `IFileStorage` interface, deleted after processing
- **Cleanup**: A recurring Hangfire job (`cleanup-orphaned-zip-upload-jobs`) runs hourly to mark jobs stuck in `NotStart` for over an hour as `Failed` and deletes partial files

## Testing Notes
- Test with nested folder structures to verify parent-child relationships
- Navigate away during processing to confirm background operation
- Check that progress persists across page refreshes

## Troubleshooting

### Storage Path Issues
- Both LocalFileStorage and AzureBlobFileStorage preserve directory structure
- Files are saved with GUID names but maintain folder hierarchy
- Example: `zip-uploads/3/abc123def456.zip` instead of just `abc123def456.zip`

### Race Condition Prevention
- Jobs are created with `NotStart` status to prevent premature processing
- Status changes to `Pending` only after file upload completes, at which point a Hangfire job is enqueued
- `ZipUploadJobService.ProcessJobAsync` uses an atomic SQL UPDATE to claim jobs, preventing race conditions
- This ensures the file is fully uploaded before processing begins

### Authorization in Background Processing
- Hangfire job service uses `AddAttachmentsToCollectibleItemSystemCommand` which bypasses user authorization
- This allows the background job to attach files to items without a user context
- Regular user operations still use the authorized `AddAttachmentsToCollectibleItemCommand`