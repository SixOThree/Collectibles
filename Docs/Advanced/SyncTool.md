# Collectibles Sync Tool

The Sync Tool is a Windows desktop companion app that compares a local folder of media against a Collectibles showcase, then uploads, downloads, deletes, moves, or copies attachments to bring the two sides into agreement.

It is composed of three coordinated parts:

1. **`Source/Collectibles.SyncTool`** — a WPF (`net10.0-windows`) desktop client that talks to the server purely over HTTP.
2. **`SyncTool` configuration in the web app's `appsettings.json`** — a global on/off switch on the server.
3. **Per-user API keys** — issued from the user's *Account → Manage → Sync Tool* page in the Blazor UI. The key is the credential the desktop client uses.

---

## 1. Server-side configuration (`appsettings.json`)

The server-side configuration lives in `Source/Collectibles.Web/appsettings.json` under the `SyncTool` section:

```json
"SyncTool": {
    "Enabled": false
}
```

This is bound to `Collectibles.Domain.Configuration.SyncToolSettings` (`Source/Collectibles.Domain/Configuration/SyncToolSettings.cs`):

```csharp
public class SyncToolSettings
{
    public bool Enabled { get; set; }
}
```

Wired up in `Source/Collectibles.Web/Extensions/ServiceCollectionExtensions.cs:328`:

```csharp
services.Configure<SyncToolSettings>(configuration.GetSection("SyncTool"));
services.AddSingleton<IApiKeyService, ApiKeyService>();
```

### What `Enabled` controls

`SyncTool:Enabled` is the **master switch** for the entire sync feature. When `false`:

- The `/api/sync/*` endpoints are not even mapped on application start. From `Source/Collectibles.Web/Extensions/EndpointExtensions.cs:28`:
  ```csharp
  var syncToolSettings = app.Configuration.GetSection("SyncTool").Get<SyncToolSettings>();
  if (syncToolSettings?.Enabled == true)
  {
      app.MapSyncEndpoints();
  }
  ```
- The API-key authentication handler still runs, but rejects every request with `"Sync tool is not enabled."` (see `ApiKeyAuthenticationHandler.cs:50-54`).
- The "Sync Tool" link is hidden from the user's *Account → Manage* nav menu (`ManageNavMenu.razor:45`).
- The **Sync Tool Access** checkbox on the *Edit User* admin page is hidden (`EditUser.razor:74`).
- Visiting `/Account/Manage/SyncTool` directly shows: *"The Sync Tool is not currently enabled on this server."*

So flipping `SyncTool:Enabled = true` is a prerequisite for *anyone* — admins or users — to do anything sync-related.

---

## 2. Per-user enablement and API keys

Even when `SyncTool:Enabled = true` globally, each user must be individually enabled and must generate their own API key.

### User fields

Two fields on `Collectibles.Infrastructure.Persistence.ApplicationUser` back the feature:

```csharp
public bool SyncToolEnabled { get; set; }
public string? ApiKeyHash { get; set; }
```

Added by migration `20260412192022_AddSyncToolUserFields`. The `ApiKeyHash` column was later constrained by `20260412194728_ConstrainApiKeyHashColumn`.

### Granting access (admin)

A user with the `Administrator` or `UserManager` role visits **`/users/{userId}/edit`**. When `SyncTool:Enabled` is true, the form shows a **Sync Tool Access** checkbox (`EditUser.razor:74-84`), bound to `UpdateUserCommand.SyncToolEnabled`. Saving toggles `ApplicationUser.SyncToolEnabled`.

### Generating a key (user)

Once `SyncToolEnabled = true`, the user visits **`/Account/Manage/SyncTool`** (`Source/Collectibles.Web/Components/Account/Pages/Manage/SyncTool.razor`). The page provides three actions:

| Action | Server effect |
|---|---|
| **Generate API Key** | Generates a new random key, stores its hash, shows the raw key once. |
| **Regenerate Key** | Same as Generate, replacing any existing hash. |
| **Revoke Key** | Sets `ApiKeyHash = null`, immediately disabling sync access. |

The page also displays the **Server URL** (the user's current `NavigationManager.BaseUri`) — this is what they paste into the desktop client.

### Key generation and hashing

`Source/Collectibles.Infrastructure/Services/ApiKeyService.cs`:

```csharp
public ApiKeyGenerationResult GenerateKey()
{
    var bytes = RandomNumberGenerator.GetBytes(32);            // 256 bits of entropy
    var rawKey = Convert.ToBase64String(bytes)
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');     // URL-safe Base64
    var keyHash = HashKey(rawKey);
    return new ApiKeyGenerationResult(rawKey, keyHash);
}

public string HashKey(string rawKey) =>
    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));
```

Important properties:

- The **raw key is shown to the user exactly once** in a green alert ("*Copy this key now — it won't be shown again*"). After page navigation it cannot be retrieved.
- Only the **SHA-256 hex hash** is persisted on `ApplicationUser.ApiKeyHash`. The server never stores the raw key.
- All key actions emit an `EventAction.AccountManagement` audit log entry tagged with `Page = "SyncTool"` and the action name.

---

## 3. Authentication flow

### The handler

`Source/Collectibles.Web/Authentication/ApiKeyAuthenticationHandler.cs` defines a custom `AuthenticationHandler<AuthenticationSchemeOptions>` registered under the scheme name `"ApiKey"`. It looks for the `X-Api-Key` request header, then enforces, in order:

1. Header present and non-empty (otherwise `NoResult` → falls through to other schemes).
2. `SyncToolSettings.Enabled` is `true` (otherwise `Fail("Sync tool is not enabled.")`).
3. The hash of the provided key matches some user's `ApiKeyHash`.
4. That user is `IsActive`.
5. That user has `SyncToolEnabled = true`.

On success it issues a `ClaimsPrincipal` with `NameIdentifier`, `Name`, and `AuthenticationMethod` claims under the `"ApiKey"` scheme.

### Registration

`ServiceCollectionExtensions.cs:143-156`:

```csharp
authBuilder.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
    ApiKeyAuthenticationHandler.SchemeName, null);

options.AddPolicy("ApiKeyOrCookie", policy =>
{
    policy.AddAuthenticationSchemes(
        ApiKeyAuthenticationHandler.SchemeName,
        IdentityConstants.ApplicationScheme);
    policy.RequireAuthenticatedUser();
});
```

The `"ApiKeyOrCookie"` policy is what every sync endpoint uses, so the same routes work both from a logged-in browser and from the desktop tool.

---

## 4. Server endpoints

All sync endpoints are mapped in `Source/Collectibles.Web/Endpoints/SyncEndpoints.cs` (only when `SyncTool:Enabled`), under the prefix `/api/sync`. Each one uses `RequireAuthorization("ApiKeyOrCookie")` and `DisableAntiforgery()`.

| Method | Path | Purpose |
|---|---|---|
| `GET` | `/api/sync/manifest/{showcaseHashId}` | Returns every attachment in the showcase as a flat list, with hash, size, type, and breadcrumb path. Used to compare against local files. |
| `POST` | `/api/sync/upload` | Initiates a sync upload: validates ownership, creates the folder hierarchy from the relative path, checks for duplicates by `ContentHash`, and (if not a duplicate) returns a SAS URL the client uploads the bytes to. |
| `POST` | `/api/sync/upload/complete` | Finalizes the upload: creates the `Attachment` row and links it to the target item. |
| `POST` | `/api/sync/attachments/{hash}/move` | Moves an existing attachment to a new relative path within the same showcase, creating folder items as needed. |

### Reused (non-`/api/sync`) endpoints

The desktop client also calls a handful of endpoints from `AttachmentEndpoints.cs` and `CollectibleItemEndpoints.cs`, all also under `"ApiKeyOrCookie"`:

| Endpoint | Used for |
|---|---|
| `POST /api/attachments/initiate-upload` / `complete-upload` | Generic Azure direct-upload pair (used by the "Copy" bulk operation). |
| `POST /api/attachments/{hash}/delete` | Delete a server-only attachment. |
| `GET /api/attachments/{hash}/context` | Fetch metadata about an attachment (used in confirmations). |
| `GET /api/attachments/{hash}/thumbnail` and `/download` | Fetch image bytes for the preview pane and for downloading server-only items. |
| `POST /api/collectible-items/{hash}/delete` | Delete an entire collectible item. |

### Manifest authorization

`GetShowcaseManifestQueryHandler` enforces that the authenticated user is the **owner** of the showcase being read (`GetShowcaseManifestQuery.cs:53-66`). The same ownership check is duplicated inside `SyncUploadCommandHandler` and `CompleteSyncUploadCommandHandler`. So even with a valid API key, a user can only sync **their own** showcases.

The query also recursively walks up to 10 levels of child items, returning each attachment's hash, size, type, and breadcrumb (`a > b > c`). Each manifest fetch emits an `EventAction.Export` audit log entry tagged with `Source = "SyncTool"`.

---

## 5. The desktop client (`Source/Collectibles.Maui` is *not* this — it's a separate scanner app)

### Project shape

- `Collectibles.SyncTool.csproj` — `WinExe`, `net10.0-windows`, `UseWPF=true`. Project references `Collectibles.Domain` and `Collectibles.Application` (only to share DTO/enum shapes — no infrastructure or DB code is pulled in).
- DI is set up in `App.xaml.cs` with a manual `ServiceCollection`: a singleton `SettingsService`, `FileHashService`, `SyncComparisonService`, two `HttpClient`s, the `CollectiblesApiClient`, and `MainViewModel`.
- UI is a single `MainWindow.xaml` using a Catppuccin-style dark theme.

### Local user settings

`Source/Collectibles.SyncTool/Services/SettingsService.cs` persists to:

```
%APPDATA%\Collectibles\SyncTool\settings.json
```

(That's `Environment.SpecialFolder.ApplicationData` → typically `C:\Users\<you>\AppData\Roaming\Collectibles\SyncTool\settings.json`.)

The settings are modeled by `SyncSettings` and serialized as a private `SettingsDto`:

| Field | Notes |
|---|---|
| `ServerUrl` | Plaintext. The Collectibles base URL (no trailing slash). |
| `EncryptedApiKey` | **DPAPI-encrypted**, Base64-encoded. Encrypted via `ProtectedData.Protect(..., DataProtectionScope.CurrentUser)`, so only the same Windows user on the same machine can decrypt it. |
| `LastShowcaseHashId` | Plaintext convenience — last showcase the user synced. |
| `LastLocalFolder` | Plaintext convenience — last local folder. |
| `MaxParallelUploads` | Defaults to `3`. |
| `IsPreviewPanelOpen`, `PreviewPanelWidth` | UI state. |

`MainViewModel` calls `SaveSettings()` from every `OnXxxChanged` partial method on the connection fields, so changes are written through immediately. Width is debounced (`OnPreviewPanelWidthChanged` uses a 500 ms timer).

### `CollectiblesApiClient`

`Source/Collectibles.SyncTool/Services/CollectiblesApiClient.cs` is the single HTTP wrapper. It owns two `HttpClient`s:

- `_httpClient` — used for all calls to the Collectibles server. Sets the `X-Api-Key` header in `Configure(baseUrl, apiKey)`.
- `_azureClient` — used to `PUT` directly to Azure Blob via SAS URL. Must **not** carry the API key header.

It exposes one method per server operation, plus two helpers for Azure uploads:

- `SingleUploadAsync` for files ≤ 200 MB — sends the whole payload in one `PUT`.
- `BlockUploadAsync` for files > 200 MB — splits into 8 MB blocks, `PUT`s each as `?comp=block`, then commits the block list as `?comp=blocklist`. Reports progress per block.

### `SyncComparisonService` — how items are classified

Given a dict of local files (relative path → hash + size + full path) and the server manifest, every local file falls into one of:

| Status | Condition |
|---|---|
| `Matched` | Hash matches a server entry whose `ItemPathSegments` exactly equal the local folder segments (case-insensitive). Falls back to filename + size match if the server entry has no hash. |
| `MovedCopied` | Hash matches a server entry, but the path is different. |
| `ToUpload` | No hash match and no fallback name+size match. |

After processing all local files, any server manifest entries that weren't matched become `ServerOnly`.

`FileHashService` computes SHA-256 over a streamed file read and caches results keyed by `(full path, length, last-write-time-utc)`, so re-running Compare on an unchanged folder skips re-hashing.

### Sync workflow (what the user sees)

1. User enters **Server URL**, **API Key**, **Showcase HashId**, **Local Folder**, and clicks **Compare**.
2. `MainViewModel.CompareAsync` calls `GetManifestAsync`, hashes local files (with progress), runs the comparison, and populates the grid.
3. The grid groups results by status; toggle buttons filter (`All`, `To Upload`, `Server Only`, `Moved/Copied`, `Matched`) plus a `Hide Matched` checkbox and a search box.
4. The bottom action bar acts on whatever is selected:
   - **Upload** (status = `ToUpload`) — three-step Azure direct upload via `/api/sync/upload` → SAS `PUT` → `/api/sync/upload/complete`. Throttled to 3 in flight via `SemaphoreSlim`. Root-level files are skipped because the server requires `segments.Length >= 2`.
   - **Download** (status = `ServerOnly`) — `GET /api/attachments/{hash}/download`, written to `LocalFolder/<breadcrumb path>/<original filename>`.
   - **Delete** (status = `ServerOnly`) — confirmation dialog, then `POST /api/attachments/{hash}/delete`.
   - **Copy** (status = `MovedCopied`) — re-uploads the local copy under the new path, leaving the server file in place.
   - **Move** (status = `MovedCopied`) — `POST /api/sync/attachments/{hash}/move` to relocate the existing server attachment.
5. After every bulk operation, `CompareAsync()` is re-run to refresh the list.

### Preview pane

Optional. For matched/server-only image rows it does a two-phase load: first the thumbnail (`/api/attachments/{hash}/thumbnail`), then the full-size image (`/api/attachments/{hash}/download`) in the background. An LRU cache (10 entries) keyed by local path or attachment hash short-circuits repeat selection.

---

## 6. End-to-end checklist

To get a brand-new user syncing:

1. **Server admin**: set `"SyncTool": { "Enabled": true }` in `appsettings.json` (or in `appsettings.Production.json` / env-var override). Restart the app — the endpoint mapping is read at startup.
2. **Server admin**: open `/users/{userId}/edit`, tick **Sync Tool Access**, save.
3. **User**: open `/Account/Manage/SyncTool`, click **Generate API Key**, copy the key from the green alert.
4. **User**: download `Collectibles.SyncTool` from GitHub Releases (linked from the same page).
5. **User**: paste the **Server URL** and **API Key** into the desktop client. They are saved to `%APPDATA%\Collectibles\SyncTool\settings.json` (key DPAPI-encrypted to the current Windows user).
6. **User**: enter a **Showcase HashId** (the slug from the showcase URL), choose a local folder, click **Compare**, then act on the results.

## 7. Related code map

| Concern | File |
|---|---|
| Global on/off switch | `Source/Collectibles.Web/appsettings.json` (`SyncTool` section) |
| Settings POCO | `Source/Collectibles.Domain/Configuration/SyncToolSettings.cs` |
| DI registration | `Source/Collectibles.Web/Extensions/ServiceCollectionExtensions.cs:328` |
| Conditional endpoint mapping | `Source/Collectibles.Web/Extensions/EndpointExtensions.cs:28` |
| API key generate / hash | `Source/Collectibles.Infrastructure/Services/ApiKeyService.cs` |
| API key auth handler | `Source/Collectibles.Web/Authentication/ApiKeyAuthenticationHandler.cs` |
| `ApiKeyOrCookie` policy | `Source/Collectibles.Web/Extensions/ServiceCollectionExtensions.cs:152` |
| User fields | `Source/Collectibles.Infrastructure/Persistence/ApplicationUser.cs` |
| User-facing key page | `Source/Collectibles.Web/Components/Account/Pages/Manage/SyncTool.razor` |
| Admin enable toggle | `Source/Collectibles.Web/Components/Pages/EditUser.razor:74` |
| Sync HTTP endpoints | `Source/Collectibles.Web/Endpoints/SyncEndpoints.cs` |
| Manifest query | `Source/Collectibles.Application/Features/Sync/Queries/GetShowcaseManifestQuery.cs` |
| Upload commands | `Source/Collectibles.Application/Features/Sync/Commands/SyncUploadCommand.cs`, `CompleteSyncUploadCommand.cs` |
| Desktop client entry | `Source/Collectibles.SyncTool/App.xaml.cs` |
| Local settings + DPAPI | `Source/Collectibles.SyncTool/Services/SettingsService.cs` |
| HTTP wrapper | `Source/Collectibles.SyncTool/Services/CollectiblesApiClient.cs` |
| Local hashing + cache | `Source/Collectibles.SyncTool/Services/FileHashService.cs` |
| Comparison logic | `Source/Collectibles.SyncTool/Services/SyncComparisonService.cs` |
| UI and commands | `Source/Collectibles.SyncTool/ViewModels/MainViewModel.cs`, `MainWindow.xaml` |
