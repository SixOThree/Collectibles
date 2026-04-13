# Preview Generation Settings Design

## Scope

Add per-category configuration to control which attachment types have preview thumbnails generated. This allows deployments to disable preview generation for specific file types (e.g., disable video previews on servers without FFmpeg, or disable PDF previews where pdfium is unavailable).

## Configuration

New top-level section in `appsettings.json`:

```json
"PreviewGeneration": {
    "Images": true,
    "Pdf": true,
    "Video": true,
    "Word": true,
    "PowerPoint": true
}
```

All properties default to `true` so existing deployments are unaffected. When a category is `false`, `GeneratePreviewAsync` returns `null` for that content type (no placeholder image), and the background service skips fetching files for that category.

## Approach

### Settings class

Create `PreviewGenerationSettings` in `Domain/Configuration/` following the existing `StorageSettings` pattern: a plain class with a `SectionName` constant and boolean properties defaulting to `true`.

### DI registration

Bind the new section in `ConfigureServices` (Infrastructure layer) alongside the other `IOptions<T>` registrations.

### FileProcessingService

Inject `IOptions<PreviewGenerationSettings>`. At the top of `GeneratePreviewAsync`, check the relevant boolean before dispatching to each handler. When disabled, return `null` immediately. This affects both upload-time preview generation and background catch-up processing since both call through `GeneratePreviewAsync`.

### AttachmentPreviewBackgroundService

Update `IsPreviewableType` to accept `PreviewGenerationSettings` and exclude disabled categories. This prevents the background service from fetching file bytes for types whose preview generation is turned off.

### Documentation

Update `Docs/Configuration.md` with a PreviewGeneration section documenting the new settings.

## Files to create or modify

| File | Action |
|------|--------|
| `Source/Collectibles.Domain/Configuration/PreviewGenerationSettings.cs` | Create settings class |
| `Source/Collectibles.Infrastructure/Common/ConfigureServices.cs` | Bind settings |
| `Source/Collectibles.Infrastructure/FileProcessing/FileProcessingService.cs` | Inject settings, gate each category |
| `Source/Collectibles.Domain/Interfaces/IFileProcessingService.cs` | No change needed (settings are internal to implementation) |
| `Source/Collectibles.Infrastructure/Services/AttachmentPreviewBackgroundService.cs` | Filter by settings |
| `Source/Collectibles.Web/appsettings.json` | Add `PreviewGeneration` section |
| `Docs/Configuration.md` | Add PreviewGeneration section |

## Testing

Existing tests mock `IFileProcessingService` so they are unaffected. The settings gate is internal to the infrastructure implementation and can be verified by inspecting the configuration binding and the guard logic.

## Notes

The interface `IFileProcessingService` stays unchanged. Settings are an implementation detail of `FileProcessingService`, keeping the domain layer clean. Callers that pass a content type to `GeneratePreviewAsync` do not need to know which categories are enabled.
