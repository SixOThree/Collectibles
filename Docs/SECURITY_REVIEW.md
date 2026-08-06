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
