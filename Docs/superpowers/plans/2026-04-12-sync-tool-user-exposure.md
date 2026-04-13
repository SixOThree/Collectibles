# Sync Tool User Exposure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expose the WPF sync tool to end users with per-user API keys, three-tier feature gating, and self-service key management.

**Architecture:** Replace the single static API key with per-user SHA256-hashed keys stored on ApplicationUser. Gate sync access behind a global config switch and an admin-granted per-user flag. Add a user-facing account page for key management. Remove SkipTlsValidation from the desktop sync tool.

**Tech Stack:** .NET 8 (web) / .NET 10 (sync tool), Blazor Server, EF Core, ASP.NET Identity, MediatR, xUnit + Moq + FluentAssertions

---

## File Map

### New Files
- `Source/Collectibles.Domain/Configuration/SyncToolSettings.cs` — global feature config
- `Source/Collectibles.Application/Interfaces/IApiKeyService.cs` — key management interface
- `Source/Collectibles.Infrastructure/Services/ApiKeyService.cs` — key generation, hashing, validation
- `Source/Collectibles.Web/Components/Account/Pages/Manage/SyncTool.razor` — user account page
- `Test/Collectibles.Application.Tests/Features/Sync/ApiKeyServiceTests.cs` — key service tests
- `Test/Collectibles.Application.Tests/Features/Sync/ApiKeyAuthenticationHandlerTests.cs` — auth handler tests

### Modified Files
- `Source/Collectibles.Infrastructure/Persistence/ApplicationUser.cs` — add SyncToolEnabled, ApiKeyHash
- `Source/Collectibles.Web/Authentication/ApiKeyAuthenticationHandler.cs` — rework to per-user lookup
- `Source/Collectibles.Web/Extensions/ServiceCollectionExtensions.cs` — swap config, register service
- `Source/Collectibles.Web/Extensions/EndpointExtensions.cs` — conditional sync registration
- `Source/Collectibles.Web/Components/Pages/EditUser.razor` — add SyncToolEnabled toggle
- `Source/Collectibles.Web/Components/Account/Shared/ManageNavMenu.razor` — add sync tool nav link
- `Source/Collectibles.Application/Features/Users/Commands/UpdateUserCommand.cs` — add SyncToolEnabled
- `Source/Collectibles.Application/Interfaces/IUserManagementService.cs` — add SyncToolEnabled param
- `Source/Collectibles.Infrastructure/Services/UserManagementService.cs` — persist SyncToolEnabled
- `Source/Collectibles.Web/appsettings.json` — remove ApiKey, add SyncTool section
- `Source/Collectibles.Web/appsettings.Development.json` — same
- `Source/Collectibles.SyncTool/Models/SyncSettings.cs` — remove SkipTlsValidation
- `Source/Collectibles.SyncTool/Services/SettingsService.cs` — remove SkipTlsValidation from DTO
- `Source/Collectibles.SyncTool/MainWindow.xaml` — remove SkipTlsValidation checkbox
- `Source/Collectibles.SyncTool/ViewModels/MainViewModel.cs` — remove SkipTlsValidation property

### Deleted Files
- `Source/Collectibles.Domain/Configuration/ApiKeySettings.cs`

---

### Task 1: SyncToolSettings Configuration Class

**Files:**
- Create: `Source/Collectibles.Domain/Configuration/SyncToolSettings.cs`
- Delete: `Source/Collectibles.Domain/Configuration/ApiKeySettings.cs`

- [ ] **Step 1: Create SyncToolSettings class**

```csharp
// Source/Collectibles.Domain/Configuration/SyncToolSettings.cs
namespace Collectibles.Domain.Configuration;

public class SyncToolSettings
{
    public bool Enabled { get; set; }
}
```

- [ ] **Step 2: Delete ApiKeySettings class**

Delete `Source/Collectibles.Domain/Configuration/ApiKeySettings.cs` entirely.

- [ ] **Step 3: Build to verify**

Run: `dotnet build Source/Collectibles.Domain`
Expected: SUCCESS (the class is not yet referenced)

Note: This will break `ServiceCollectionExtensions.cs` and `ApiKeyAuthenticationHandler.cs` which reference `ApiKeySettings`. Those are fixed in later tasks. Do NOT fix them now — the build of the full solution will fail until Task 6.

- [ ] **Step 4: Commit**

```bash
git add Source/Collectibles.Domain/Configuration/SyncToolSettings.cs
git add Source/Collectibles.Domain/Configuration/ApiKeySettings.cs
git commit -m "feat: add SyncToolSettings, remove ApiKeySettings"
```

---

### Task 2: ApplicationUser — Add Sync Fields

**Files:**
- Modify: `Source/Collectibles.Infrastructure/Persistence/ApplicationUser.cs`

- [ ] **Step 1: Add SyncToolEnabled and ApiKeyHash fields**

Add these two properties to `ApplicationUser`, after the existing `ModifiedBy` property:

```csharp
public bool SyncToolEnabled { get; set; }
public string? ApiKeyHash { get; set; }
```

The full properties section should read:

```csharp
public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
    public bool SyncToolEnabled { get; set; }
    public string? ApiKeyHash { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
```

- [ ] **Step 2: Build Infrastructure project**

Run: `dotnet build Source/Collectibles.Infrastructure`
Expected: SUCCESS

- [ ] **Step 3: Commit**

```bash
git add Source/Collectibles.Infrastructure/Persistence/ApplicationUser.cs
git commit -m "feat: add SyncToolEnabled and ApiKeyHash to ApplicationUser"
```

---

### Task 3: EF Core Migration

**Files:**
- Creates new migration files in `Source/Collectibles.Infrastructure/Migrations/`

- [ ] **Step 1: Generate migration**

Run from the solution root:

```bash
dotnet ef migrations add AddSyncToolUserFields \
  --project Source/Collectibles.Infrastructure \
  --startup-project Source/Collectibles.Web
```

Expected: Migration files created in `Source/Collectibles.Infrastructure/Migrations/` with name pattern `*_AddSyncToolUserFields.cs`

- [ ] **Step 2: Verify the migration Up/Down methods**

Read the generated migration file. The `Up` method should contain:

```csharp
migrationBuilder.AddColumn<bool>(
    name: "SyncToolEnabled",
    table: "AspNetUsers",
    type: "bit",
    nullable: false,
    defaultValue: false);

migrationBuilder.AddColumn<string>(
    name: "ApiKeyHash",
    table: "AspNetUsers",
    type: "nvarchar(max)",
    nullable: true);
```

The `Down` method should drop both columns. If the migration doesn't look right, delete it and regenerate.

- [ ] **Step 3: Apply migration**

Run:

```bash
dotnet ef database update \
  --project Source/Collectibles.Infrastructure \
  --startup-project Source/Collectibles.Web
```

Expected: Database updated successfully.

- [ ] **Step 4: Commit**

```bash
git add Source/Collectibles.Infrastructure/Migrations/
git commit -m "feat: add migration for SyncToolEnabled and ApiKeyHash columns"
```

---

### Task 4: API Key Service

**Files:**
- Create: `Source/Collectibles.Application/Interfaces/IApiKeyService.cs`
- Create: `Source/Collectibles.Infrastructure/Services/ApiKeyService.cs`
- Create: `Test/Collectibles.Application.Tests/Features/Sync/ApiKeyServiceTests.cs`

- [ ] **Step 1: Create IApiKeyService interface**

```csharp
// Source/Collectibles.Application/Interfaces/IApiKeyService.cs
namespace Collectibles.Application.Interfaces;

public record ApiKeyGenerationResult(string RawKey, string KeyHash);

public interface IApiKeyService
{
    ApiKeyGenerationResult GenerateKey();
    string HashKey(string rawKey);
}
```

- [ ] **Step 2: Write failing tests for ApiKeyService**

```csharp
// Test/Collectibles.Application.Tests/Features/Sync/ApiKeyServiceTests.cs
using Collectibles.Infrastructure.Services;
using FluentAssertions;

namespace Collectibles.Application.Tests.Features.Sync;

public class ApiKeyServiceTests
{
    private readonly ApiKeyService _service = new();

    [Fact]
    public void GenerateKeyShouldReturnBase64UrlSafeKey()
    {
        var result = _service.GenerateKey();

        result.RawKey.Should().NotBeNullOrWhiteSpace();
        result.RawKey.Should().HaveLength(43);
        result.RawKey.Should().NotContain("+");
        result.RawKey.Should().NotContain("/");
        result.RawKey.Should().NotContain("=");
    }

    [Fact]
    public void GenerateKeyShouldReturnMatchingHash()
    {
        var result = _service.GenerateKey();

        var reHashed = _service.HashKey(result.RawKey);
        reHashed.Should().Be(result.KeyHash);
    }

    [Fact]
    public void GenerateKeyShouldProduceUniqueKeys()
    {
        var key1 = _service.GenerateKey();
        var key2 = _service.GenerateKey();

        key1.RawKey.Should().NotBe(key2.RawKey);
        key1.KeyHash.Should().NotBe(key2.KeyHash);
    }

    [Fact]
    public void HashKeyShouldBeConsistentForSameInput()
    {
        var rawKey = "test-key-value";

        var hash1 = _service.HashKey(rawKey);
        var hash2 = _service.HashKey(rawKey);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashKeyShouldReturnHexString()
    {
        var result = _service.GenerateKey();

        result.KeyHash.Should().MatchRegex("^[0-9A-F]{64}$");
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test Test/Collectibles.Application.Tests --filter "FullyQualifiedName~ApiKeyServiceTests" -v n`
Expected: FAIL — `ApiKeyService` class does not exist yet.

- [ ] **Step 4: Implement ApiKeyService**

```csharp
// Source/Collectibles.Infrastructure/Services/ApiKeyService.cs
using System.Security.Cryptography;
using System.Text;
using Collectibles.Application.Interfaces;

namespace Collectibles.Infrastructure.Services;

public class ApiKeyService : IApiKeyService
{
    public ApiKeyGenerationResult GenerateKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var rawKey = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var keyHash = HashKey(rawKey);
        return new ApiKeyGenerationResult(rawKey, keyHash);
    }

    public string HashKey(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test Test/Collectibles.Application.Tests --filter "FullyQualifiedName~ApiKeyServiceTests" -v n`
Expected: All 5 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add Source/Collectibles.Application/Interfaces/IApiKeyService.cs
git add Source/Collectibles.Infrastructure/Services/ApiKeyService.cs
git add Test/Collectibles.Application.Tests/Features/Sync/ApiKeyServiceTests.cs
git commit -m "feat: add IApiKeyService with key generation and SHA256 hashing"
```

---

### Task 5: Rework ApiKeyAuthenticationHandler

**Files:**
- Modify: `Source/Collectibles.Web/Authentication/ApiKeyAuthenticationHandler.cs`
- Create: `Test/Collectibles.Application.Tests/Features/Sync/ApiKeyAuthenticationHandlerTests.cs`

- [ ] **Step 1: Write failing tests for the reworked handler**

```csharp
// Test/Collectibles.Application.Tests/Features/Sync/ApiKeyAuthenticationHandlerTests.cs
using System.Security.Claims;
using System.Text.Encodings.Web;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Configuration;
using Collectibles.Infrastructure.Persistence;
using Collectibles.Web.Authentication;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Collectibles.Application.Tests.Features.Sync;

public class ApiKeyAuthenticationHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IApiKeyService> _apiKeyServiceMock;
    private readonly Mock<IOptionsMonitor<SyncToolSettings>> _syncToolSettingsMock;

    public ApiKeyAuthenticationHandlerTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _apiKeyServiceMock = new Mock<IApiKeyService>();
        _syncToolSettingsMock = new Mock<IOptionsMonitor<SyncToolSettings>>();
        _syncToolSettingsMock.Setup(x => x.CurrentValue)
            .Returns(new SyncToolSettings { Enabled = true });
    }

    [Fact]
    public async Task ShouldReturnNoResultWhenNoApiKeyHeader()
    {
        var context = new DefaultHttpContext();
        var handler = await CreateAndInitializeHandler(context);

        var result = await handler.AuthenticateAsync();

        result.None.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldFailWhenSyncToolDisabledGlobally()
    {
        _syncToolSettingsMock.Setup(x => x.CurrentValue)
            .Returns(new SyncToolSettings { Enabled = false });

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = "some-key";
        var handler = await CreateAndInitializeHandler(context);

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldFailWhenNoUserMatchesKeyHash()
    {
        _apiKeyServiceMock.Setup(x => x.HashKey("test-key")).Returns("HASH123");

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = "test-key";
        var handler = await CreateAndInitializeHandler(context);

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldFailWhenUserSyncToolDisabled()
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            Email = "test@example.com",
            DisplayName = "Test User",
            SyncToolEnabled = false,
            IsActive = true,
            ApiKeyHash = "HASH123"
        };
        _apiKeyServiceMock.Setup(x => x.HashKey("test-key")).Returns("HASH123");
        SetupUserLookup(user);

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = "test-key";
        var handler = await CreateAndInitializeHandler(context);

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldFailWhenUserIsInactive()
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            Email = "test@example.com",
            DisplayName = "Test User",
            SyncToolEnabled = true,
            IsActive = false,
            ApiKeyHash = "HASH123"
        };
        _apiKeyServiceMock.Setup(x => x.HashKey("test-key")).Returns("HASH123");
        SetupUserLookup(user);

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = "test-key";
        var handler = await CreateAndInitializeHandler(context);

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldSucceedForValidKeyWithEnabledActiveUser()
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            Email = "test@example.com",
            DisplayName = "Test User",
            SyncToolEnabled = true,
            IsActive = true,
            ApiKeyHash = "HASH123"
        };
        _apiKeyServiceMock.Setup(x => x.HashKey("test-key")).Returns("HASH123");
        SetupUserLookup(user);

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = "test-key";
        var handler = await CreateAndInitializeHandler(context);

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        result.Principal!.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be("user-1");
        result.Principal!.FindFirst(ClaimTypes.Name)!.Value.Should().Be("Test User");
    }

    private void SetupUserLookup(ApplicationUser user)
    {
        _userManagerMock.Setup(x => x.Users)
            .Returns(new[] { user }.AsQueryable());
    }

    private async Task<ApiKeyAuthenticationHandler> CreateAndInitializeHandler(HttpContext context)
    {
        var options = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        options.Setup(x => x.Get(It.IsAny<string>()))
            .Returns(new AuthenticationSchemeOptions());
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
        var encoder = UrlEncoder.Default;

        var handler = new ApiKeyAuthenticationHandler(
            options.Object,
            loggerFactory.Object,
            encoder,
            _userManagerMock.Object,
            _apiKeyServiceMock.Object,
            _syncToolSettingsMock.Object);

        var scheme = new AuthenticationScheme(
            ApiKeyAuthenticationHandler.SchemeName,
            null,
            typeof(ApiKeyAuthenticationHandler));

        await handler.InitializeAsync(scheme, context);
        return handler;
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Test/Collectibles.Application.Tests --filter "FullyQualifiedName~ApiKeyAuthenticationHandlerTests" -v n`
Expected: FAIL — constructor signature doesn't match yet.

- [ ] **Step 3: Rewrite ApiKeyAuthenticationHandler**

Replace the entire content of `Source/Collectibles.Web/Authentication/ApiKeyAuthenticationHandler.cs`:

```csharp
using System.Security.Claims;
using System.Text.Encodings.Web;
using Collectibles.Application.Interfaces;
using Collectibles.Domain.Configuration;
using Collectibles.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Serilog;

namespace Collectibles.Web.Authentication;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiKey";
    private const string ApiKeyHeaderName = "X-Api-Key";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApiKeyService _apiKeyService;
    private readonly IOptionsMonitor<SyncToolSettings> _syncToolSettings;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        UserManager<ApplicationUser> userManager,
        IApiKeyService apiKeyService,
        IOptionsMonitor<SyncToolSettings> syncToolSettings)
        : base(options, logger, encoder)
    {
        _userManager = userManager;
        _apiKeyService = apiKeyService;
        _syncToolSettings = syncToolSettings;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedKey = apiKeyHeader.ToString();
        if (string.IsNullOrWhiteSpace(providedKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!_syncToolSettings.CurrentValue.Enabled)
        {
            Log.Warning("API key authentication attempted but sync tool is disabled globally");
            return Task.FromResult(AuthenticateResult.Fail("Sync tool is not enabled."));
        }

        var keyHash = _apiKeyService.HashKey(providedKey);
        var user = _userManager.Users
            .FirstOrDefault(u => u.ApiKeyHash == keyHash);

        if (user == null)
        {
            Log.Warning("Invalid API key provided from {RemoteIp}", Context.Connection.RemoteIpAddress);
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        if (!user.IsActive)
        {
            Log.Warning("API key authentication attempted for inactive user {UserId}", user.Id);
            return Task.FromResult(AuthenticateResult.Fail("User account is inactive."));
        }

        if (!user.SyncToolEnabled)
        {
            Log.Warning("API key authentication attempted for user {UserId} without sync tool access", user.Id);
            return Task.FromResult(AuthenticateResult.Fail("Sync tool access is not enabled for this user."));
        }

        var displayName = user.DisplayName ?? user.Email ?? user.UserName ?? "API User";
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, displayName),
            new Claim(ClaimTypes.AuthenticationMethod, SchemeName),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        Log.Debug("API key authentication successful for user {UserId}", user.Id);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Test/Collectibles.Application.Tests --filter "FullyQualifiedName~ApiKeyAuthenticationHandlerTests" -v n`
Expected: All 6 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add Source/Collectibles.Web/Authentication/ApiKeyAuthenticationHandler.cs
git add Test/Collectibles.Application.Tests/Features/Sync/ApiKeyAuthenticationHandlerTests.cs
git commit -m "feat: rework ApiKeyAuthenticationHandler for per-user key lookup"
```

---

### Task 6: Service Configuration & Registration Updates

**Files:**
- Modify: `Source/Collectibles.Web/Extensions/ServiceCollectionExtensions.cs`
- Modify: `Source/Collectibles.Web/Extensions/EndpointExtensions.cs`
- Modify: `Source/Collectibles.Web/appsettings.json`
- Modify: `Source/Collectibles.Web/appsettings.Development.json`

- [ ] **Step 1: Update appsettings.json**

In `Source/Collectibles.Web/appsettings.json`, replace the `"ApiKey"` section:

```json
"ApiKey": {
    "Key": "",
    "UserEmail": ""
},
```

with:

```json
"SyncTool": {
    "Enabled": false
},
```

- [ ] **Step 2: Update appsettings.Development.json**

In `Source/Collectibles.Web/appsettings.Development.json`, remove any `"ApiKey"` section if present, and add:

```json
"SyncTool": {
    "Enabled": true
},
```

Read the file first to find the exact location of the ApiKey section to replace.

- [ ] **Step 3: Update ServiceCollectionExtensions.cs**

In `Source/Collectibles.Web/Extensions/ServiceCollectionExtensions.cs`:

Replace the ApiKeySettings registration line:

```csharp
services.Configure<ApiKeySettings>(configuration.GetSection("ApiKey"));
```

with:

```csharp
services.Configure<SyncToolSettings>(configuration.GetSection("SyncTool"));
services.AddSingleton<IApiKeyService, ApiKeyService>();
```

Update the using statements at the top of the file — replace:

```csharp
using Collectibles.Domain.Configuration;
```

Keep it (SyncToolSettings is also in this namespace). Add if not present:

```csharp
using Collectibles.Application.Interfaces;
using Collectibles.Infrastructure.Services;
```

- [ ] **Step 4: Make sync endpoint registration conditional**

In `Source/Collectibles.Web/Extensions/EndpointExtensions.cs`, the `MapApiEndpoints` method currently calls `app.MapSyncEndpoints()` unconditionally. Change it to be conditional:

Replace:

```csharp
app.MapSyncEndpoints();
```

with:

```csharp
var syncToolSettings = app.Configuration.GetSection("SyncTool").Get<SyncToolSettings>();
if (syncToolSettings?.Enabled == true)
{
    app.MapSyncEndpoints();
}
```

Add the required using at the top:

```csharp
using Collectibles.Domain.Configuration;
```

Note: `EndpointExtensions.cs` receives a `WebApplication` (which has `.Configuration`). Read the file to confirm the exact method signature and parameter name.

- [ ] **Step 5: Build and verify**

Run: `dotnet build`
Expected: SUCCESS — all references to `ApiKeySettings` should now be gone and replaced with `SyncToolSettings`.

If there are remaining references to `ApiKeySettings` in other files, track them down and update. The auth handler was updated in Task 5. If `Program.cs` or any other file references it, update those too.

- [ ] **Step 6: Run all existing tests**

Run: `dotnet test`
Expected: All tests pass. No existing functionality broken.

- [ ] **Step 7: Commit**

```bash
git add Source/Collectibles.Web/Extensions/ServiceCollectionExtensions.cs
git add Source/Collectibles.Web/Extensions/EndpointExtensions.cs
git add Source/Collectibles.Web/appsettings.json
git add Source/Collectibles.Web/appsettings.Development.json
git commit -m "feat: register SyncToolSettings, conditional sync endpoints, remove ApiKey config"
```

---

### Task 7: UpdateUserCommand — Add SyncToolEnabled

**Files:**
- Modify: `Source/Collectibles.Application/Features/Users/Commands/UpdateUserCommand.cs`
- Modify: `Source/Collectibles.Application/Interfaces/IUserManagementService.cs`
- Modify: `Source/Collectibles.Infrastructure/Services/UserManagementService.cs`

- [ ] **Step 1: Add SyncToolEnabled to UpdateUserCommand**

In `Source/Collectibles.Application/Features/Users/Commands/UpdateUserCommand.cs`, add to the command's properties:

```csharp
public bool SyncToolEnabled { get; set; }
```

In the `UpdateUserCommandHandler.Handle` method, find where `_userManagementService.UpdateUserAsync()` is called and add `request.SyncToolEnabled` as a new parameter. The call should become:

```csharp
await _userManagementService.UpdateUserAsync(
    request.Id,
    request.Email,
    request.FirstName,
    request.LastName,
    request.ProfilePictureUrl,
    request.IsActive,
    request.Roles,
    request.SyncToolEnabled,
    cancellationToken);
```

Read the file first to see the exact current call and adjust accordingly.

- [ ] **Step 2: Add SyncToolEnabled parameter to IUserManagementService**

In `Source/Collectibles.Application/Interfaces/IUserManagementService.cs`, update the `UpdateUserAsync` signature:

```csharp
Task UpdateUserAsync(
    string userId,
    string email,
    string? firstName,
    string? lastName,
    string? profilePictureUrl,
    bool isActive,
    List<string> roles,
    bool syncToolEnabled,
    CancellationToken cancellationToken = default);
```

- [ ] **Step 3: Update UserManagementService implementation**

In `Source/Collectibles.Infrastructure/Services/UserManagementService.cs`, update the `UpdateUserAsync` method:

Add `bool syncToolEnabled` parameter to the method signature.

In the method body, where other user properties are being set (e.g., `user.FirstName = firstName;`), add:

```csharp
user.SyncToolEnabled = syncToolEnabled;
```

Read the file first to find the exact location within the method.

- [ ] **Step 4: Build and test**

Run: `dotnet build && dotnet test`
Expected: SUCCESS — all tests pass.

- [ ] **Step 5: Commit**

```bash
git add Source/Collectibles.Application/Features/Users/Commands/UpdateUserCommand.cs
git add Source/Collectibles.Application/Interfaces/IUserManagementService.cs
git add Source/Collectibles.Infrastructure/Services/UserManagementService.cs
git commit -m "feat: add SyncToolEnabled to UpdateUserCommand and user management service"
```

---

### Task 8: Admin EditUser.razor — SyncToolEnabled Toggle

**Files:**
- Modify: `Source/Collectibles.Web/Components/Pages/EditUser.razor`

- [ ] **Step 1: Add SyncTool configuration injection**

At the top of `EditUser.razor`, add this injection after the existing `@inject` lines:

```razor
@inject IOptions<SyncToolSettings> SyncToolSettingsOptions
```

Add the required using:

```razor
@using Collectibles.Domain.Configuration
@using Microsoft.Extensions.Options
```

Read the file first to check which usings already exist.

- [ ] **Step 2: Add SyncToolEnabled checkbox to the form**

In `EditUser.razor`, find the `IsActive` checkbox section. It follows this pattern:

```html
<div class="form-check mb-3">
    <InputCheckbox id="isActive" class="form-check-input" @bind-Value="_model.IsActive" />
    <label class="form-check-label" for="isActive">Active</label>
</div>
```

Add the following immediately after that block, wrapped in a conditional:

```razor
@if (SyncToolSettingsOptions.Value.Enabled)
{
    <div class="form-check mb-3">
        <InputCheckbox id="syncToolEnabled" class="form-check-input" @bind-Value="_model.SyncToolEnabled" />
        <label class="form-check-label" for="syncToolEnabled">Sync Tool Access</label>
    </div>
}
```

- [ ] **Step 3: Ensure model population loads SyncToolEnabled**

In the `@code` block, find where `_model` is populated from the loaded user data (in `OnInitializedAsync` or similar). Ensure `SyncToolEnabled` is mapped. Look for where `_model.IsActive` is set and add nearby:

```csharp
_model.SyncToolEnabled = user.SyncToolEnabled;
```

Read the file's code section carefully — the mapping might be done differently (e.g., via automapper or manual property assignment). Match the existing pattern.

- [ ] **Step 4: Build and verify**

Run: `dotnet build`
Expected: SUCCESS

- [ ] **Step 5: Manual test**

Run: `dotnet run --project Source/Collectibles.Web`

1. Log in as an admin
2. Navigate to `/users` and click Edit on a user
3. With `SyncTool:Enabled = true` in appsettings.Development.json, verify the "Sync Tool Access" checkbox appears
4. Toggle it and save — verify it persists on page reload
5. Set `SyncTool:Enabled = false` in config, restart, verify the checkbox is hidden

- [ ] **Step 6: Commit**

```bash
git add Source/Collectibles.Web/Components/Pages/EditUser.razor
git commit -m "feat: add SyncToolEnabled toggle to admin user edit page"
```

---

### Task 9: User Account — Sync Tool Page

**Files:**
- Create: `Source/Collectibles.Web/Components/Account/Pages/Manage/SyncTool.razor`

- [ ] **Step 1: Create the SyncTool.razor page**

```razor
@page "/Account/Manage/SyncTool"
@using Collectibles.Application.Interfaces
@using Collectibles.Domain.Configuration
@using Collectibles.Infrastructure.Persistence
@using Microsoft.AspNetCore.Identity
@using Microsoft.Extensions.Options

@inject UserManager<ApplicationUser> UserManager
@inject IdentityUserAccessor UserAccessor
@inject IdentityRedirectManager RedirectManager
@inject IApiKeyService ApiKeyService
@inject IOptions<SyncToolSettings> SyncToolSettingsOptions
@inject NavigationManager NavigationManager

<PageTitle>Sync Tool</PageTitle>

<h3>Sync Tool</h3>
<StatusMessage />

@if (!SyncToolSettingsOptions.Value.Enabled || !_user?.SyncToolEnabled == true)
{
    <div class="alert alert-warning">
        Sync tool access is not available for your account. Contact an administrator for access.
    </div>
}
else
{
    <div class="card mb-4">
        <div class="card-header">
            <h5 class="mb-0">Connection Details</h5>
        </div>
        <div class="card-body">
            <div class="mb-3">
                <label class="form-label fw-semibold">Server URL</label>
                <div class="input-group">
                    <input type="text" class="form-control" value="@_serverUrl" readonly />
                </div>
            </div>

            <div class="mb-3">
                <label class="form-label fw-semibold">API Key</label>
                @if (!string.IsNullOrEmpty(_newlyGeneratedKey))
                {
                    <div class="alert alert-success">
                        <strong>Copy this key now — it won't be shown again.</strong>
                        <div class="input-group mt-2">
                            <input type="text" class="form-control font-monospace" value="@_newlyGeneratedKey" readonly />
                        </div>
                    </div>
                }
                else if (_hasActiveKey)
                {
                    <div class="input-group">
                        <input type="text" class="form-control" value="••••••••••••••••••••••••" readonly />
                        <span class="input-group-text text-success">Active</span>
                    </div>
                }
                else
                {
                    <p class="text-muted">No API key generated.</p>
                }
            </div>

            <div class="d-flex gap-2">
                @if (_hasActiveKey)
                {
                    <form @formname="regenerate-key" @onsubmit="RegenerateKeyAsync" method="post">
                        <AntiforgeryToken />
                        <button type="submit" class="btn btn-warning">Regenerate Key</button>
                    </form>
                    @if (!_showRevokeConfirmation)
                    {
                        <button type="button" class="btn btn-outline-danger" @onclick="ShowRevokeConfirmation">Revoke Key</button>
                    }
                    else
                    {
                        <div class="d-flex gap-2 align-items-center">
                            <span class="text-danger">This will immediately disable sync access.</span>
                            <form @formname="revoke-key" @onsubmit="RevokeKeyAsync" method="post">
                                <AntiforgeryToken />
                                <button type="submit" class="btn btn-danger">Confirm Revoke</button>
                            </form>
                            <button type="button" class="btn btn-secondary" @onclick="CancelRevoke">Cancel</button>
                        </div>
                    }
                }
                else
                {
                    <form @formname="generate-key" @onsubmit="GenerateKeyAsync" method="post">
                        <AntiforgeryToken />
                        <button type="submit" class="btn btn-primary">Generate Key</button>
                    </form>
                }
            </div>
        </div>
    </div>

    <div class="card mb-4">
        <div class="card-header">
            <h5 class="mb-0">Download</h5>
        </div>
        <div class="card-body">
            <p>
                The Collectibles Sync Tool is a Windows desktop application.
                Download the latest release from GitHub:
            </p>
            <a href="https://github.com/SixOThree/Collectibles-SyncTool/releases"
               target="_blank" rel="noopener noreferrer" class="btn btn-outline-primary">
                <i class="bi bi-download me-2"></i>Download Sync Tool
            </a>
            <p class="text-muted mt-2 mb-0">
                <small>Requires Windows with .NET 10 runtime (or use the self-contained release).</small>
            </p>
        </div>
    </div>

    <div class="card mb-4">
        <div class="card-header">
            <h5 class="mb-0">Quick Start</h5>
        </div>
        <div class="card-body">
            <ol>
                <li>Download and install the Sync Tool from the link above.</li>
                <li>Open the Sync Tool and enter the <strong>Server URL</strong> shown above.</li>
                <li>Paste your <strong>API Key</strong> into the Sync Tool.</li>
                <li>Select a <strong>showcase</strong> from the dropdown.</li>
                <li>Choose a <strong>local folder</strong> containing your files.</li>
                <li>Click <strong>Compare</strong> to see what will be synced, then upload.</li>
            </ol>
        </div>
    </div>
}

@code {
    private ApplicationUser? _user;
    private string _serverUrl = string.Empty;
    private bool _hasActiveKey;
    private string? _newlyGeneratedKey;
    private bool _showRevokeConfirmation;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        _user = await UserAccessor.GetRequiredUserAsync(HttpContext);
        _serverUrl = NavigationManager.BaseUri.TrimEnd('/');
        _hasActiveKey = !string.IsNullOrEmpty(_user.ApiKeyHash);
    }

    private async Task GenerateKeyAsync()
    {
        _user = await UserAccessor.GetRequiredUserAsync(HttpContext);
        var result = ApiKeyService.GenerateKey();
        _user.ApiKeyHash = result.KeyHash;
        await UserManager.UpdateAsync(_user);
        _newlyGeneratedKey = result.RawKey;
        _hasActiveKey = true;
    }

    private async Task RegenerateKeyAsync()
    {
        await GenerateKeyAsync();
    }

    private void ShowRevokeConfirmation()
    {
        _showRevokeConfirmation = true;
    }

    private void CancelRevoke()
    {
        _showRevokeConfirmation = false;
    }

    private async Task RevokeKeyAsync()
    {
        _user = await UserAccessor.GetRequiredUserAsync(HttpContext);
        _user.ApiKeyHash = null;
        await UserManager.UpdateAsync(_user);
        _hasActiveKey = false;
        _newlyGeneratedKey = null;
        _showRevokeConfirmation = false;
        RedirectManager.RedirectToCurrentPageWithStatus("API key revoked. Sync access is now disabled.", HttpContext);
    }
}
```

Note: The GitHub releases URL (`https://github.com/SixOThree/Collectibles-SyncTool/releases`) is a placeholder — update to the actual repository URL if different. Check the existing repo organization.

- [ ] **Step 2: Build and verify**

Run: `dotnet build`
Expected: SUCCESS

- [ ] **Step 3: Manual test**

Run: `dotnet run --project Source/Collectibles.Web`

1. Log in as a user who has `SyncToolEnabled = true` (set via admin edit page from Task 8)
2. Navigate to `/Account/Manage/SyncTool`
3. Verify server URL shows correctly
4. Click "Generate Key" — verify raw key appears with copy warning
5. Navigate away and back — verify key shows as masked (`••••`) with "Active" badge
6. Click "Regenerate Key" — verify new key appears
7. Click "Revoke Key" — verify confirmation appears, confirm, verify key is cleared
8. Test with a user who does NOT have `SyncToolEnabled` — verify the access warning shows

- [ ] **Step 4: Commit**

```bash
git add Source/Collectibles.Web/Components/Account/Pages/Manage/SyncTool.razor
git commit -m "feat: add Sync Tool account page with key management"
```

---

### Task 10: ManageNavMenu — Conditional Sync Tool Link

**Files:**
- Modify: `Source/Collectibles.Web/Components/Account/Shared/ManageNavMenu.razor`

- [ ] **Step 1: Add injections and sync tool nav link**

Read the file first for exact structure. Add to the injection section:

```razor
@using Collectibles.Domain.Configuration
@using Collectibles.Infrastructure.Persistence
@using Microsoft.Extensions.Options
@inject IOptions<SyncToolSettings> SyncToolSettingsOptions
@inject UserManager<ApplicationUser> UserManager
@inject IdentityUserAccessor UserAccessor
```

Some of these may already be present (like `UserManager`). Only add what's missing.

In the `<ul>` nav list, add this entry after the existing nav links (before the closing `</ul>`):

```razor
@if (_showSyncToolLink)
{
    <li class="nav-item">
        <NavLink class="nav-link" href="Account/Manage/SyncTool" Match="NavLinkMatch.All">
            <i class="bi bi-arrow-repeat me-2"></i>Sync Tool
        </NavLink>
    </li>
}
```

In the `@code` block, add:

```csharp
private bool _showSyncToolLink;

// In OnInitializedAsync (or the existing initialization method):
if (SyncToolSettingsOptions.Value.Enabled)
{
    var user = await UserAccessor.GetRequiredUserAsync(HttpContext);
    _showSyncToolLink = user.SyncToolEnabled;
}
```

Read the file to see how the existing `@code` block is structured (it may use `OnInitializedAsync` or a different pattern) and adapt accordingly. The existing code already retrieves auth schemes for the external logins conditional — follow that pattern.

- [ ] **Step 2: Build and verify**

Run: `dotnet build`
Expected: SUCCESS

- [ ] **Step 3: Manual test**

1. Log in as a user with `SyncToolEnabled = true` — verify "Sync Tool" appears in the account sidebar nav
2. Log in as a user with `SyncToolEnabled = false` — verify "Sync Tool" does NOT appear
3. Set `SyncTool:Enabled = false` in config, restart — verify "Sync Tool" nav link is hidden for all users

- [ ] **Step 4: Commit**

```bash
git add Source/Collectibles.Web/Components/Account/Shared/ManageNavMenu.razor
git commit -m "feat: add conditional Sync Tool link to account nav menu"
```

---

### Task 11: SyncTool Desktop — Remove SkipTlsValidation

**Files:**
- Modify: `Source/Collectibles.SyncTool/Models/SyncSettings.cs`
- Modify: `Source/Collectibles.SyncTool/Services/SettingsService.cs`
- Modify: `Source/Collectibles.SyncTool/MainWindow.xaml`
- Modify: `Source/Collectibles.SyncTool/ViewModels/MainViewModel.cs`

- [ ] **Step 1: Remove from SyncSettings.cs**

In `Source/Collectibles.SyncTool/Models/SyncSettings.cs`, remove the line:

```csharp
public bool SkipTlsValidation { get; set; }
```

- [ ] **Step 2: Remove from SettingsService.cs**

In `Source/Collectibles.SyncTool/Services/SettingsService.cs`:

Remove `SkipTlsValidation` from the `SettingsDto` class:

```csharp
public bool SkipTlsValidation { get; set; }
```

In the `Load()` method, remove any mapping of `SkipTlsValidation` from DTO to SyncSettings.

In the `Save()` method, remove any mapping of `SkipTlsValidation` from SyncSettings to DTO.

Read the file first to find exact lines.

- [ ] **Step 3: Remove from MainViewModel.cs**

In `Source/Collectibles.SyncTool/ViewModels/MainViewModel.cs`, remove:

```csharp
[ObservableProperty] private bool _skipTlsValidation;
```

And remove the property changed handler:

```csharp
partial void OnSkipTlsValidationChanged(bool value) => SaveSettings();
```

Also search for and remove any references to `SkipTlsValidation` in `LoadSettings()` or `SaveSettings()` methods.

- [ ] **Step 4: Remove from MainWindow.xaml**

In `Source/Collectibles.SyncTool/MainWindow.xaml`, remove the checkbox:

```xml
<CheckBox Content="Skip TLS" IsChecked="{Binding SkipTlsValidation}"
          Foreground="#585B70" VerticalAlignment="Center" Margin="0,0,12,4" FontSize="11" />
```

- [ ] **Step 5: Search for and remove any TLS bypass HttpClientHandler code**

Search the SyncTool project for `ServerCertificateCustomValidationCallback`, `SslPolicyErrors`, or `HttpClientHandler`. If any code bypasses TLS validation based on the `SkipTlsValidation` setting, remove it.

Check these files specifically:
- `Source/Collectibles.SyncTool/App.xaml.cs`
- `Source/Collectibles.SyncTool/Services/CollectiblesApiClient.cs`
- Any DI setup or HttpClient factory configuration

If found, remove the bypass and ensure the HttpClient uses default (secure) TLS validation.

- [ ] **Step 6: Build the SyncTool project**

Run: `dotnet build Source/Collectibles.SyncTool`
Expected: SUCCESS — no remaining references to SkipTlsValidation.

- [ ] **Step 7: Commit**

```bash
git add Source/Collectibles.SyncTool/Models/SyncSettings.cs
git add Source/Collectibles.SyncTool/Services/SettingsService.cs
git add Source/Collectibles.SyncTool/MainWindow.xaml
git add Source/Collectibles.SyncTool/ViewModels/MainViewModel.cs
git commit -m "feat: remove SkipTlsValidation from sync tool"
```

---

### Task 12: Final Verification

- [ ] **Step 1: Full build**

Run: `dotnet build`
Expected: SUCCESS with no warnings related to ApiKey or SkipTlsValidation.

- [ ] **Step 2: Run all tests**

Run: `dotnet test`
Expected: All tests pass.

- [ ] **Step 3: Search for leftover references**

Search the entire codebase for any remaining references to:
- `ApiKeySettings` (should be zero hits outside of git history)
- `SkipTlsValidation` (should be zero hits outside of git history)
- `"ApiKey"` in appsettings files (should be zero — note: SendGrid has an `ApiKey` field, that's fine and unrelated)

- [ ] **Step 4: End-to-end manual test**

Run the web app: `dotnet run --project Source/Collectibles.Web`

1. As admin: edit a user, enable "Sync Tool Access", save
2. As that user: navigate to Account > Manage > Sync Tool
3. Generate an API key, copy it
4. Open the Sync Tool desktop app, enter the server URL and API key
5. Verify the sync tool can fetch the manifest for one of the user's showcases
6. Back in the web app: revoke the API key
7. Verify the sync tool can no longer authenticate (gets 401)
8. Set `SyncTool:Enabled = false` in appsettings, restart the web app
9. Verify the sync API endpoints return 404 (not registered)

- [ ] **Step 5: Commit any final adjustments**

If any issues were found and fixed during verification:

```bash
git add -A
git commit -m "fix: address issues found during final verification"
```
