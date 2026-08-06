# Codebase Refactoring Plan

Based on a full architectural scan of the codebase and execution of the unit test suite (177/177 passing tests), here is the structured plan for refactoring the application.

---

## 🏗️ Phase 1: Codebase Hygiene & Dead Code Cleanup
1. **Remove Stale Project / Directory Artifacts**
   - **`Collectibles.Kernel`**: Unused project containing no source code. Remove project references from `Collectibles.Application.csproj` and `Collectibles.Web.csproj` and clean solution file.
   - **`Collectibles.Maui`**: Remove abandoned directory containing unused user files.
   - **`Collectibles.Web/Program.cs`**: Clean commented-out legacy IIS configuration blocks.

---

## ⚡ Phase 2: C# 12 & .NET 10 Modernization
1. **Adopt `Convert.ToBase64UrlString`**
   - Refactor `ApiKeyService.cs` to use native BCL `.NET 8+` Base64Url conversion instead of manual string replacements.
2. **Primary Constructors across Services & MediatR Handlers**
   - Convert standard constructor boilerplate to C# 12 primary constructors in:
     - `UserManagementService.cs`
     - `CollectibleItemPreviewService.cs`
     - `ZipUploadJobService.cs`
     - MediatR Handlers in `Collectibles.Application`
3. **Collection Expressions `[...]`**
   - Replace verbose `new[] { ... }` and `new List<T>()` initializations across `Program.cs`, `SyncEndpoints.cs`, and `AttachmentEndpoints.cs`.

---

## 🏛️ Phase 3: Web Layer & Architecture Cleanup
1. **`Program.cs` Decomposition**
   - Extract Serilog theme configuration into `SerilogThemeExtensions.cs`.
   - Extract Health Checks and Middleware setup into extension methods.
2. **Authorization & Helper Method Deduplication in `AttachmentEndpoints.cs`**
   - Centralize duplicate HttpContext user ID resolution blocks into a unified helper.
   - Streamline `GetAttachmentPreview` and `GetAttachmentThumbnail` handler logic.

---

## 🧪 Verification Strategy
- Run `dotnet build` after each phase to guarantee zero compilation errors.
- Run `dotnet test` (177 tests) after each phase to ensure full regression testing.
