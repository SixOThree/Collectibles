# Sync-Collectibles Script

PowerShell script that compares local files against a Collectibles showcase and optionally uploads missing files.

Located at: `Scripts/Sync-Collectibles.ps1`

## Prerequisites

- PowerShell 5.1+ (Windows PowerShell) or PowerShell Core 6+
- A running Collectibles server
- An API key for authentication
- The HashId of the target showcase (found in the showcase URL)

## Parameters

| Parameter | Required | Description |
|---|---|---|
| `-ServerUrl` | Yes | Base URL of the Collectibles server (e.g., `https://localhost:7269`) |
| `-ApiKey` | Yes | API key for authentication |
| `-ShowcaseHashId` | Yes | The showcase's HashId from its URL (e.g., `aBcDeFgH`) |
| `-LocalFolder` | Yes | Path to the local folder containing files to sync |
| `-Upload` | No | When specified, uploads missing files to the server |
| `-SkipTlsValidation` | No | Skips TLS certificate validation (for development with self-signed certs) |

## Usage

### Dry Run (Compare Only)

To see which local files are missing from the server without uploading anything:

```powershell
.\Scripts\Sync-Collectibles.ps1 `
    -ServerUrl "https://localhost:7269" `
    -ApiKey "my-api-key" `
    -ShowcaseHashId "aBcDeFgH" `
    -LocalFolder "C:\Photos\MyCollection"
```

This will output three reports:
- **Moved/Copied** - Local files whose content exists on the server under a different filename (shows both local and server names)
- **Missing from Server** - Local files that don't exist on the server at all
- **Server-only** - Files on the server that aren't found anywhere in the local folder

### Upload Missing and Moved/Copied Files

Add the `-Upload` switch to upload moved/copied and missing files to the server:

```powershell
.\Scripts\Sync-Collectibles.ps1 `
    -ServerUrl "https://localhost:7269" `
    -ApiKey "my-api-key" `
    -ShowcaseHashId "aBcDeFgH" `
    -LocalFolder "C:\Photos\MyCollection" `
    -Upload
```

### Development (Self-Signed Certs)

When running against a local development server with self-signed certificates, add `-SkipTlsValidation`:

```powershell
.\Scripts\Sync-Collectibles.ps1 `
    -ServerUrl "https://localhost:7269" `
    -ApiKey "my-api-key" `
    -ShowcaseHashId "aBcDeFgH" `
    -LocalFolder "C:\Photos\MyCollection" `
    -Upload `
    -SkipTlsValidation
```

## How It Works

1. **Fetch manifest** - Retrieves the list of all attachments in the showcase from the server via `GET /api/sync/manifest/{showcaseHashId}`
2. **Scan local folder** - Recursively finds all files in the local folder
3. **Compare** - Computes SHA-256 hashes of local files and compares against the server manifest:
   - **Matched** - Hash and filename both match a server entry (or fallback: filename + size match when server hash is null)
   - **Moved/Copied** - Hash matches a server entry but the local filename is different
   - **Missing** - No match by hash or fallback
   - **Server-only** - Server entries not matched by any local file (includes the breadcrumb path to the item on the server)
4. **Report** - Displays counts for each category and detailed tables for moved/copied, missing, and server-only files
5. **Upload** (if `-Upload` is specified) - For each moved/copied and missing file:
   - Initiates an upload via `POST /api/attachments/initiate-upload`
   - Uploads the file to Azure Blob Storage using the returned SAS URL
   - Completes the upload via `POST /api/attachments/complete-upload`

Files over 200 MB are uploaded using Azure block upload (8 MB blocks) with progress indication.

## Supported File Types

The script automatically detects content types for common formats including images (jpg, png, gif, webp, bmp, tiff, svg), video (mp4, mov, avi, wmv, mkv), audio (mp3, wav, flac), and other formats (pdf, zip, rar, 7z). Unrecognized extensions default to `application/octet-stream`.

## Error Handling

- **401** - Authentication failed. Check your API key.
- **404** - Showcase not found. Check the showcase HashId.
- Individual file upload failures are reported but do not stop the remaining uploads.
