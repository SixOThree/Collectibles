# Preview Generation Settings Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add per-category configuration toggles so each attachment preview type (Images, PDF, Video, Word, PowerPoint) can be independently enabled or disabled via `appsettings.json`.

**Architecture:** A new `PreviewGenerationSettings` class bound from the `PreviewGeneration` config section. `FileProcessingService` checks the relevant boolean before dispatching to each handler, returning `null` when disabled. `AttachmentPreviewBackgroundService` filters by the same settings so it does not fetch files for disabled types.

**Tech Stack:** .NET 10, `IOptions<T>`, existing `FileProcessingService`, `AttachmentPreviewBackgroundService`

---

### Task 1: Create PreviewGenerationSettings class

**Files:**
- Create: `Source/Collectibles.Domain/Configuration/PreviewGenerationSettings.cs`

- [ ] **Step 1: Create the settings class**

```csharp
namespace Collectibles.Domain.Configuration;

public class PreviewGenerationSettings
{
    public const string SectionName = "PreviewGeneration";

    public bool Images { get; set; } = true;
    public bool Pdf { get; set; } = true;
    public bool Video { get; set; } = true;
    public bool Word { get; set; } = true;
    public bool PowerPoint { get; set; } = true;
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build Source/Collectibles.Domain/Collectibles.Domain.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add Source/Collectibles.Domain/Configuration/PreviewGenerationSettings.cs
git commit -m "feat: add PreviewGenerationSettings configuration class"
```

---

### Task 2: Register settings in DI and add to appsettings.json

**Files:**
- Modify: `Source/Collectibles.Infrastructure/Common/ConfigureServices.cs:106` (near other `services.Configure<>` calls)
- Modify: `Source/Collectibles.Web/appsettings.json`

- [ ] **Step 1: Bind settings in ConfigureServices**

In `Source/Collectibles.Infrastructure/Common/ConfigureServices.cs`, add after the existing `services.Configure<StorageSettings>(...)` line (line 106):

```csharp
        // Configure preview generation settings
        services.Configure<PreviewGenerationSettings>(configuration.GetSection(PreviewGenerationSettings.SectionName));
```

Add the using at the top of the file (it may already be covered by the existing `using Collectibles.Domain.Configuration;` import — verify).

- [ ] **Step 2: Add PreviewGeneration section to appsettings.json**

In `Source/Collectibles.Web/appsettings.json`, add a new section after the `Storage` section (after line 27):

```json
    "PreviewGeneration": {
        "Images": true,
        "Pdf": true,
        "Video": true,
        "Word": true,
        "PowerPoint": true
    },
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build Source/Collectibles.Infrastructure/Collectibles.Infrastructure.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add Source/Collectibles.Infrastructure/Common/ConfigureServices.cs Source/Collectibles.Web/appsettings.json
git commit -m "feat: register PreviewGenerationSettings in DI and appsettings"
```

---

### Task 3: Gate preview generation by settings in FileProcessingService

**Files:**
- Modify: `Source/Collectibles.Infrastructure/FileProcessing/FileProcessingService.cs`

- [ ] **Step 1: Inject IOptions\<PreviewGenerationSettings\> into constructor**

Replace the existing constructor and field declarations (lines 23-28) with:

```csharp
    private readonly ILogger<FileProcessingService> _logger;
    private readonly PreviewGenerationSettings _previewSettings;

    public FileProcessingService(
        ILogger<FileProcessingService> logger,
        IOptions<PreviewGenerationSettings> previewSettings)
    {
        _logger = logger;
        _previewSettings = previewSettings.Value;
    }
```

Add these usings at the top of the file:

```csharp
using Collectibles.Domain.Configuration;
using Microsoft.Extensions.Options;
```

- [ ] **Step 2: Add settings guard in GeneratePreviewAsync**

Replace the body of `GeneratePreviewAsync` (lines 30-62) with:

```csharp
    public async Task<byte[]?> GeneratePreviewAsync(byte[] fileContent, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            if (contentType.StartsWith("image/"))
            {
                return _previewSettings.Images
                    ? await GenerateImageThumbnailAsync(fileContent, cancellationToken)
                    : null;
            }
            else if (contentType == "application/pdf")
            {
                return _previewSettings.Pdf
                    ? await GeneratePdfThumbnailAsync(fileContent, cancellationToken)
                    : null;
            }
            else if (contentType.StartsWith("video/"))
            {
                return _previewSettings.Video
                    ? await GenerateVideoThumbnailAsync(fileContent, contentType, cancellationToken)
                    : null;
            }
            else if (IsWordDocument(contentType))
            {
                return _previewSettings.Word
                    ? await GenerateWordThumbnailAsync(fileContent, cancellationToken)
                    : null;
            }
            else if (IsPowerPointDocument(contentType))
            {
                return _previewSettings.PowerPoint
                    ? await GeneratePowerPointThumbnailAsync(fileContent, cancellationToken)
                    : null;
            }

            return await GenerateGenericThumbnailAsync(contentType, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating preview");
            return await GenerateErrorThumbnailAsync(cancellationToken);
        }
    }
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build Source/Collectibles.Infrastructure/Collectibles.Infrastructure.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Run existing tests to verify no regressions**

Run: `dotnet test Test/Collectibles.Application.Tests/Collectibles.Application.Tests.csproj`
Expected: All tests pass. Existing tests mock `IFileProcessingService` so they are unaffected by the constructor change.

- [ ] **Step 5: Commit**

```bash
git add Source/Collectibles.Infrastructure/FileProcessing/FileProcessingService.cs
git commit -m "feat: gate preview generation by per-category settings"
```

---

### Task 4: Filter disabled types in AttachmentPreviewBackgroundService

**Files:**
- Modify: `Source/Collectibles.Infrastructure/Services/AttachmentPreviewBackgroundService.cs`

- [ ] **Step 1: Resolve settings from scope and pass to IsPreviewableType**

In the `ProcessMissingPreviewsAsync` method, add a line to resolve settings from the scope (after line 65 where other services are resolved):

```csharp
        var previewSettings = scope.ServiceProvider.GetRequiredService<IOptions<PreviewGenerationSettings>>().Value;
```

Add these usings at the top of the file:

```csharp
using Collectibles.Domain.Configuration;
using Microsoft.Extensions.Options;
```

- [ ] **Step 2: Update the filter call to pass settings**

Replace the filter on line 81:

```csharp
        // Filter to only previewable types that are enabled in settings
        attachmentsNeedingPreviews = attachmentsNeedingPreviews
            .Where(a => IsPreviewableType(a.FileType!, previewSettings))
            .ToList();
```

- [ ] **Step 3: Update IsPreviewableType to check settings**

Replace the `IsPreviewableType` method (lines 271-287) with:

```csharp
    private static bool IsPreviewableType(string contentType, PreviewGenerationSettings settings)
    {
        // Check if the category is enabled in settings
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return settings.Images;
        }

        if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            return settings.Video;
        }

        if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return settings.Pdf;
        }

        if (contentType.Equals("application/vnd.openxmlformats-officedocument.wordprocessingml.document", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/msword", StringComparison.OrdinalIgnoreCase))
        {
            return settings.Word;
        }

        if (contentType.Equals("application/vnd.openxmlformats-officedocument.presentationml.presentation", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/vnd.ms-powerpoint", StringComparison.OrdinalIgnoreCase))
        {
            return settings.PowerPoint;
        }

        return false;
    }
```

- [ ] **Step 4: Build to verify**

Run: `dotnet build Source/Collectibles.Infrastructure/Collectibles.Infrastructure.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Run tests**

Run: `dotnet test Test/Collectibles.Application.Tests/Collectibles.Application.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add Source/Collectibles.Infrastructure/Services/AttachmentPreviewBackgroundService.cs
git commit -m "feat: filter disabled preview types in background service"
```

---

### Task 5: Update Configuration.md documentation

**Files:**
- Modify: `Docs/Configuration.md`

- [ ] **Step 1: Add PreviewGeneration section**

Insert a new section after the `Storage` section (after line 109 — after the Storage JSON example closing `}`) and before the `HashIds` section. Add:

```markdown
---

## PreviewGeneration

Controls which attachment types have preview thumbnails generated. Disable categories to skip preview processing for specific file types (e.g., disable Video on servers without FFmpeg, or disable PDF where pdfium is unavailable). When a category is disabled, attachments of that type have no preview image.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Images` | bool | `true` | Generate thumbnail previews for image attachments (JPEG, PNG, GIF, WebP, BMP, TIFF). |
| `Pdf` | bool | `true` | Generate rendered first-page previews for PDF attachments. Requires pdfium native library. |
| `Video` | bool | `true` | Generate frame-capture previews for video attachments. Requires FFmpeg installed on the server. |
| `Word` | bool | `true` | Generate text-extract previews for Word documents (.doc, .docx). |
| `PowerPoint` | bool | `true` | Generate text-extract previews for PowerPoint documents (.ppt, .pptx). |

```json
{
  "PreviewGeneration": {
    "Images": true,
    "Pdf": true,
    "Video": true,
    "Word": true,
    "PowerPoint": true
  }
}
```
```

- [ ] **Step 2: Commit**

```bash
git add Docs/Configuration.md
git commit -m "docs: add PreviewGeneration settings to configuration reference"
```
