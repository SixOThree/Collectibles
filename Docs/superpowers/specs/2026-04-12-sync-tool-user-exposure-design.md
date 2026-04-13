# Sync Tool User Exposure

Expose the existing WPF desktop sync tool to end users by replacing the single static API key with per-user keys, adding admin and user self-service controls, and gating the feature behind configuration.

## Feature Gating

### Global Setting

New `SyncTool` section in `appsettings.json`, replacing the current `ApiKey` section:

```json
"SyncTool": {
    "Enabled": false
}
```

When `false`, sync API endpoints are not registered in the routing table at all. The old `ApiKey` section is removed entirely.

### Per-User Admin Flag

`ApplicationUser` gets a `SyncToolEnabled` boolean field (default `false`). An administrator must enable this on the user edit page before the user can access sync functionality. Disabling it is a hard cutoff — auth fails regardless of whether the user has an active key.

### User Self-Service Key Control

Users can revoke their own API key at any time, setting `ApiKeyHash` to `null` and immediately disabling sync access. They can generate a new key when ready. This gives users a self-service safety mechanism without requiring admin involvement.

### Gating Summary

All three tiers must pass for sync access:

1. **Global** — `SyncTool:Enabled` is `true`
2. **Admin** — `SyncToolEnabled` is `true` on the user
3. **User** — User has an active API key (non-null `ApiKeyHash`)

## ApplicationUser Changes

Two new fields on `ApplicationUser`:

```csharp
public bool SyncToolEnabled { get; set; }    // Admin-granted flag, default false
public string? ApiKeyHash { get; set; }       // SHA256 hash of user's API key
```

## API Key Authentication Rework

The existing `ApiKeyAuthenticationHandler` is reworked to support per-user keys:

1. Receive `X-Api-Key` header
2. SHA256 hash the provided key
3. Query the database for an `ApplicationUser` where `ApiKeyHash` matches the hash
4. Verify the matched user has `SyncToolEnabled == true` and `IsActive == true`
5. Build claims principal from that user (NameIdentifier, Name, AuthenticationMethod)

### Key Generation

- Generated using `RandomNumberGenerator.GetBytes(32)`, Base64url-encoded (43 characters, URL-safe)
- Raw key shown to the user exactly once at generation time
- Only the SHA256 hash is stored in `ApplicationUser.ApiKeyHash`

### Key Operations

- **Generate**: Create new key, hash it, store hash, return raw key
- **Regenerate**: Same as generate — overwrites existing hash, old key immediately invalid
- **Revoke**: Set `ApiKeyHash` to `null`, sync access immediately disabled

### Removed

- `ApiKeySettings` configuration class
- `IOptions<ApiKeySettings>` injection
- Static user caching fields (`_cachedUserId`, `_cachedUserEmail`, `_cachedUserName`)
- Constant-time raw key comparison (unnecessary when comparing hashes)

## Admin User Edit Page

The existing `EditUser.razor` page (`/users/{UserId}/edit`, policy `"UserManager"`) gets:

- **SyncToolEnabled checkbox** bound to `UpdateUserCommand.SyncToolEnabled`
- Only visible when global `SyncTool:Enabled` is `true`
- Admins control the permission toggle; users manage their own keys
- Disabling the checkbox revokes sync access but leaves the key hash in the database

## User Account — Sync Tool Page

New page at `/Account/Manage/SyncTool`, added to `ManageNavMenu.razor`. Only visible when:
- Global `SyncTool:Enabled` is `true`
- User's `SyncToolEnabled` is `true`

### Page Content

**Connection Details panel:**
- Server URL (current site base URL, copy button)
- API Key display:
  - If no key exists: "Generate Key" button
  - If key exists: masked display (`••••••••••••`) with active status indicator
  - On generation/regeneration: raw key shown in highlighted box with copy button and "Copy this key now — it won't be shown again" message
- Regenerate button (warning that old key dies immediately)
- Revoke button (confirmation prompt: "This will immediately disable sync access. You can generate a new key later.")

**Download panel:**
- Link to GitHub releases page for the sync tool
- Brief note about Windows requirement / .NET 10

**Quick Start instructions (inline):**
1. Download the sync tool from GitHub releases
2. Enter the server URL
3. Paste your API key
4. Select a showcase
5. Select a local folder
6. Sync

## Sync Tool Desktop App Changes

### Remove SkipTlsValidation

- Remove `SkipTlsValidation` from `SyncSettings.cs` and `SettingsDto`
- Remove the checkbox from `MainWindow.xaml`
- Remove any `HttpClientHandler` logic that disables certificate validation
- Existing `settings.json` files with the old field are handled gracefully (JSON deserialization ignores unknown properties)

### No Other Client Changes

The sync tool already accepts server URL and API key. Per-user keys are transparent to the client — the same `X-Api-Key` header is sent regardless.

## Database Migration

- Add `SyncToolEnabled` column: `bool`, default `false`
- Add `ApiKeyHash` column: `nvarchar(max)`, nullable

Applied to the `AspNetUsers` table via EF Core migration.

## Configuration Cleanup

- Remove `ApiKey` section from `appsettings.json` and `appsettings.Development.json`
- Add `SyncTool: { Enabled: false }` section
- Remove `ApiKeySettings` class from `Collectibles.Domain.Configuration`
- Remove `IOptions<ApiKeySettings>` service registration

## Authorization Policy

The `"ApiKeyOrCookie"` policy and scheme name remain unchanged. Only the handler's internal logic changes (database lookup instead of static config comparison).

## Endpoint Registration

`MapSyncEndpoints()` in `EndpointExtensions.cs` becomes conditional on `SyncTool:Enabled`. When disabled, no sync routes are registered.

## Files Affected

### Web Project
- `Authentication/ApiKeyAuthenticationHandler.cs` — rework to per-user key lookup
- `Endpoints/SyncEndpoints.cs` — no changes (auth policy unchanged)
- `Extensions/EndpointExtensions.cs` — conditional sync endpoint registration
- `Extensions/ServiceCollectionExtensions.cs` — remove ApiKeySettings registration, add SyncTool config
- `Components/Pages/EditUser.razor` — add SyncToolEnabled toggle
- `Components/Account/Pages/Manage/SyncTool.razor` — new page
- `Components/Account/Shared/ManageNavMenu.razor` — add conditional nav link
- `appsettings.json` — remove ApiKey, add SyncTool section
- `appsettings.Development.json` — same

### Domain Project
- `Configuration/ApiKeySettings.cs` — remove
- `Configuration/SyncToolSettings.cs` — new (just `Enabled` bool)

### Application Project
- `Features/Users/Commands/UpdateUserCommand.cs` — add SyncToolEnabled field
- New commands/handlers for API key generation, regeneration, and revocation (MediatR commands following existing CQRS pattern)

### Infrastructure Project
- `Persistence/ApplicationUser.cs` — add SyncToolEnabled, ApiKeyHash fields
- New EF Core migration

### SyncTool Project
- `Models/SyncSettings.cs` — remove SkipTlsValidation
- `Services/SettingsService.cs` — remove SkipTlsValidation from SettingsDto
- `MainWindow.xaml` — remove SkipTlsValidation checkbox
- Related HttpClientHandler TLS bypass logic — remove
