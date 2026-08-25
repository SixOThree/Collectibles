# Code Security & Defensive Alignment Review

A full security alignment audit of the **Collectibles** codebase was performed across data access, input validation, authentication/authorization controls, and request security controls.

---

## 1. Parameterized SQL & Data Access Security
- **Status**: ✅ **Secure**
- **Analysis**:
  - Entity Framework Core (LINQ queries) is used exclusively across repositories, queries, and background handlers.
  - In [`ZipUploadJobService.cs`](file:///C:/Development/Ready%20Ok%20Retro/Collectibles/Source/Collectibles.Infrastructure/Services/ZipUploadJobService.cs#L47-L49), EF Core `ExecuteSqlRawAsync` is used for an atomic status update:
    ```csharp
    var rowsAffected = await dbContext.Database.ExecuteSqlRawAsync(
        "UPDATE ZipUploadJobs SET Status = {0} WHERE Id = {1} AND Status = {2}",
        (int)JobStatus.Doing, jobId, (int)JobStatus.Pending);
    ```
    EF Core automatically converts `{0}`, `{1}`, and `{2}` string interpolation placeholders into parameterized `@p0`, `@p1`, `@p2` SQL parameters, protecting against SQL injection. No raw string concatenations exist.

---

## 2. Input Validation Architecture
- **Status**: ✅ **Secure & Comprehensive**
- **Analysis**:
  - **MediatR Pipeline Validation**: [`ValidationBehaviour.cs`](file:///C:/Development/Ready%20Ok%20Retro/Collectibles/Source/Collectibles.Application/Behaviors/ValidationBehaviour.cs) automatically executes FluentValidation validators before any command or query handler processes a request.
  - **Comprehensive Validators**: Dedicated `AbstractValidator<T>` rules cover all user inputs, e.g.:
    - [`CreateCollectibleItemCommandValidator.cs`](file:///C:/Development/Ready%20Ok%20Retro/Collectibles/Source/Collectibles.Application/Features/CollectibleItems/Commands/CreateCollectibleItemCommandValidator.cs) (String length bounds, required fields, template definitions).
    - [`CreateAttachmentCommandValidator.cs`](file:///C:/Development/Ready%20Ok%20Retro/Collectibles/Source/Collectibles.Application/Features/Attachments/Commands/CreateAttachmentCommand.cs#L24-L47) (Base64 encoding integrity, filename & MIME type length limits).
    - [`CustomPasswordValidator.cs`](file:///C:/Development/Ready%20Ok%20Retro/Collectibles/Source/Collectibles.Infrastructure/Services/CustomPasswordValidator.cs) (Checks against common passwords dictionary, user PII matches, and password history reuse).

---

## 3. Authentication & Authorization Patterns
- **Status**: ✅ **Secure & Robust Layering**
- **Analysis**:
  - **Authentication Schemes**: Dual authentication support configured in [`Program.cs`](file:///C:/Development/Ready%20Ok%20Retro/Collectibles/Source/Collectibles.Web/Program.cs) (ASP.NET Core Identity Cookies for Blazor Web UI, API Key handler [`ApiKeyAuthenticationHandler.cs`](file:///C:/Development/Ready%20Ok%20Retro/Collectibles/Source/Collectibles.Web/Authentication/ApiKeyAuthenticationHandler.cs) using SHA-256 hashed keys for desktop/local sync tool).
  - **Resource-Based Authorization**:
    - [`AttachmentAuthorizationHandler.cs`](file:///C:/Development/Ready%20Ok%20Retro/Collectibles/Source/Collectibles.Application/Common/Authorization/Handlers/AttachmentAuthorizationHandler.cs)
    - [`CollectibleItemAuthorizationHandler.cs`](file:///C:/Development/Ready%20Ok%20Retro/Collectibles/Source/Collectibles.Application/Common/Authorization/Handlers/CollectibleItemAuthorizationHandler.cs)
    - [`ShowcaseAuthorizationHandler.cs`](file:///C:/Development/Ready%20Ok%20Retro/Collectibles/Source/Collectibles.Application/Common/Authorization/Handlers/ShowcaseAuthorizationHandler.cs)
    - Enforces entity ownership and privacy visibility (`IsPrivate`) for non-owners.
  - **Audit Logging**: [`CustomSignInManager.cs`](file:///C:/Development/Ready%20Ok%20Retro/Collectibles/Source/Collectibles.Infrastructure/Services/CustomSignInManager.cs#L34-L60) logs every authentication event (Success, Lockout, 2FA prompt, Failure) with IP and user metadata to the persistent event log.

---

## 4. Defense-in-Depth Security Middleware
- **Status**: ✅ **Configured & Active**
- **Analysis**:
   - [`SecurityScanBlockingMiddleware.cs`](file:///C:/Development/Ready%20Ok%20Retro/Collectibles/Source/Collectibles.Web/Middleware/SecurityScanBlockingMiddleware.cs) detects aggressive request rate anomalies or suspicious scanner request patterns, automatically rate-limiting and blocking malicious IP addresses.
   - [`CrawlerBlockingMiddleware.cs`](file:///C:/Development/Ready%20Ok%20Retro/Collectibles/Source/Collectibles.Web/Middleware/CrawlerBlockingMiddleware.cs) restricts automated scrapers from harvesting non-public resources.

---

## 5. Role-Based Access Control Hardening

Server-side authorization checks were added to command and query handlers that previously relied solely on page-level `[Authorize]` attributes. In Blazor Server, the UI gate is the only gate, so every privileged operation now re-verifies the caller's role or ownership at the application layer.

### Changes

- **`/test-storage`** is now restricted to administrators and only renders its controls in the Development environment.
- **`GetAttachmentDetailQuery`** now enforces `ViewAttachmentRequirement` via `IAuthorizationService`, closing the gap where any attachment (including those in private showcases) was viewable by anyone who knew the hash.
- **User management commands** (`CreateUser`, `UpdateUser`, `DeleteUser`, `LockUnlockUser`) and **user queries** (`GetUsersList`, `GetUserById`) now verify the caller is an Administrator or UserManager. `UpdateUser` additionally blocks a non-administrator from granting themselves the Administrator role, and `DeleteUser`/`LockUnlockUser` block self-targeting.
- **`GetAllShowcasesQuery`** now requires the Administrator role, preventing private showcase data from leaking through any future caller.
- **`SetDefaultContentDefinitionCommand`** now requires the Administrator role, matching the existing checks in `UpdateContentDefinitionCommand` and `DeleteContentDefinitionCommand`.
- **Share-link commands** (`GenerateShareLink`, `RevokeShareToken`) and **`GetShareTokensQuery`** now verify showcase ownership (or Administrator), preventing users from minting or revoking share tokens for showcases they do not own.
- **Maintenance commands** (`UpdateMissingPreviewImages`, `RollbackAttachmentMigration`, `CleanupMigratedAttachments`, `CleanupOrphans`) now require the Administrator role.
- **`CanViewUsers` policy** no longer includes the Viewer role; only Administrator and UserManager can browse the user list.
- **`ICurrentUserService`** gained an `IsInRole(string)` member, implemented by `CurrentUserService`, `BlazorCurrentUserService` (with role claims cached alongside the existing identity cache), `HttpContextDataUserService`, and the captured-user service in `ScopedApplicationDbContextFactory`.

### Accepted risk: stale identity cache in Blazor circuits

`BlazorCurrentUserService` caches `UserId`, `IsAdministrator`, and role claims for `ApplicationConstants.Caching.UserCacheSeconds` (30 seconds) to avoid blocking on `AuthenticationStateProvider` from synchronous property getters. As a consequence, after a role change or account lockout, an already-connected Blazor circuit may continue to pass authorization checks for up to 30 seconds. This is a deliberate trade-off: the alternative (synchronous blocking on an async auth state provider) risks deadlocks in the SignalR circuit. The window is short, the affected operations are re-checked against the database on each request, and the cache is invalidated on a new circuit. No code change is made for this item.
