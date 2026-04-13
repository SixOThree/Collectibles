# Large File Upload Feature

The application implements a dual-mode upload system designed to handle files of any size.

## Upload Modes

| File Size     | Upload Method                              |
|---------------|--------------------------------------------|
| Under 50 MB   | Standard upload                            |
| 50 MB – 2 GB  | Direct Azure Blob upload via SAS tokens    |
| Over 2 GB     | Chunked upload (10 MB chunks)              |

## Key Components

### 1. Chunked Upload Pipeline

- `InitializeChunkedUploadCommand` — Creates a `ZipUploadJob` record and returns a job ID
- `UploadZipChunkCommand` — Receives 10 MB chunks, writes to temp files, uploads to Azure on final chunk
- Automatic retry with exponential backoff (3 attempts max)

### 2. Direct Upload (SAS Token Flow)

- `InitiateDirectUploadCommand` — Generates a 30-minute SAS URL for direct Azure upload
- `CompleteDirectUploadCommand` — Verifies blob, creates attachment record
- `directUpload.js` handles the actual PUT to Azure

### 3. Blazor Upload Pages

- `/zip-upload-bg-simple` — Standard upload with auto-detection for large files
- `/zip-upload-bg-chunked` — Dedicated chunked upload interface with speed metrics

### 4. Background Processing

- `ZipUploadBackgroundService` processes completed uploads every 5 seconds
- Extracts ZIP contents, maps folders to collectible items
- Tracks progress in database with real-time UI polling (2-second refresh)

## Configuration

In `appsettings.json`:

```json
"DirectUpload": {
  "Enabled": true,
  "ThresholdBytes": 52428800,
  "SasExpiryMinutes": 30
}
```

`ThresholdBytes` defaults to 50 MB (52,428,800 bytes).

## Key Features

- 20 GB max file size supported
- Real-time progress tracking (percentage, speed, bytes transferred)
- Automatic chunking for files over 2 GB
- Retry logic with exponential backoff
- ZIP structure preservation — folder hierarchy becomes item hierarchy
- Error resilience — continues processing despite individual item failures
- Automatic cleanup of temp files and processed ZIPs
