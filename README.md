# Collectibles

A comprehensive purpose built web application for managing collectible items. Built with .NET 10, Blazor Server, and Clean Architecture. Create showcases, organize items with dynamic templates and rich tagging, attach media, share collections publicly, and track everything with a full audit trail.

See [Features](/Docs/Features.md) for a comprehensive list of features.

![Welcome Page](/Docs/Screenshots/Welcome.jpg)

---

## Technology Stack

| Layer | Technology |
|-------|-----------|
| Framework | .NET 10 / ASP.NET Core 10, C# 14 |
| UI | Blazor Server with Interactive Server Components |
| UI Components | Blazor Bootstrap 3.5 |
| Database | SQL Server with Entity Framework Core 10 (Code-First) |
| Architecture | Clean Architecture, CQRS with MediatR 14.1 |
| Background Jobs | Hangfire 1.8 |
| Validation | FluentValidation 12.0 |
| ID Obfuscation | HashIds.net 1.7 |
| Browser Automation | Playwright (external link caching — see below) |
| Logging | Serilog (file + database sinks) |
| Testing | xUnit, FluentAssertions, Moq, AutoFixture, Playwright E2E (see below) |

---

## Architecture

The solution follows **Clean Architecture** with four layers, plus a companion desktop client:

```
Source/
  Collectibles.Domain/                  # Core business entities, enums, value objects
  Collectibles.Application/             # CQRS commands/queries, validators, services, DTOs
  Collectibles.Infrastructure/          # EF Core, repositories, background services
    FileProcessing/                     # File processing helpers (folder)
    FileStorage/                        # Storage providers: Database, Azure Blob, Local FS (folder)
    Services/                           # Hosted/background services (folder)
  Collectibles.Web/                     # Blazor Server UI, API endpoints, authorization
  Collectibles.SyncTool/                # WPF desktop folder-sync client (net10.0-windows)

Test/
  Collectibles.Domain.Tests/
  Collectibles.Application.Tests/
  Playwright/                           # E2E tests (Node.js Playwright)
```

### Key Patterns

- **CQRS**: Commands and queries separated via MediatR with dedicated handlers
- **Repository + Unit of Work**: Interfaces in Domain, implementations in Infrastructure
- **Resource-based authorization**: Custom handlers per entity type (Showcase, Item, Attachment, Template)
- **Factory pattern**: Pluggable storage and email providers selected by configuration
- **Soft delete**: Most entities support logical deletion preserving data integrity
- **ID obfuscation**: HashIds at application boundaries, database IDs never exposed

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, Express, or full instance)
- [EF Core CLI tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`
- Node.js 18+ (for Playwright E2E tests only)

### Quick Start

1. **Clone and restore**

   ```bash
   git clone https://github.com/SixOThree/Collectibles.git
   cd Collectibles
   dotnet restore
   ```

2. **Configure the database connection**

   Copy the example config into a local settings file (git-ignored):

   ```bash
   cp Source/Collectibles.Web/appsettings.json Source/Collectibles.Web/appsettings.Development.json
   ```

   Edit `appsettings.Development.json` and set your `ConnectionStrings:DefaultConnection`. For LocalDB:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CollectiblesDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
     }
   }
   ```

   Also set a unique `HashIds:Salt` value -- the application will not start without one.

   See [Configuration Reference](/Docs/Configuration.md) for all available settings.

3. **Apply database migrations**

   ```bash
   dotnet ef database update -p Source/Collectibles.Infrastructure -s Source/Collectibles.Web
   ```

4. **Run the application**

   ```bash
   dotnet run --project Source/Collectibles.Web
   ```

   Access the application at:
   - **HTTP:** http://localhost:5111
   - **HTTPS:** https://localhost:7269

5. **First-run setup**

   On first launch with no admin accounts, the app generates a setup token saved to `App_Data/setup-token.txt`. Navigate to `/Setup` and enter the token to create the first administrator account. See [Features > First-Run Setup](/Docs/Features.md#first-run-setup) for details.

### Playwright

This project uses Playwright in two separate ways:

1. **External Link Caching (Production)** — The C# backend uses the `Microsoft.Playwright` NuGet package to launch a headless Chromium browser and capture screenshots and HTML snapshots of external URLs added to collectible items. This runs as a background service in the production application. Install browsers for this with:

   ```
   pwsh Scripts/playwright.ps1 install
   ```

   See [Playwright in Production](Docs/Playwright%20in%20production.md) for IIS deployment details.

2. **E2E Testing (Development)** — A separate Node.js Playwright test suite in `Test/Playwright/` provides end-to-end browser testing against an isolated test environment. This is unrelated to the link caching service. See [Developer README](Docs/DEVELOPER_README.md#e2e-tests) for setup and running instructions.

### Theme Configuration

To change themes from the admin interface, ensure the web server has write permissions to:
```
Source/Collectibles.Web/wwwroot/theme-config/
```

---

## Development

### Common Commands

```
dotnet build                                    # Build solution
dotnet run --project Source/Collectibles.Web    # Run application
dotnet test                                     # Run all tests
dotnet test Test/Collectibles.Application.Tests # Run specific test project

# Database migrations
dotnet ef migrations add <Name> -p Source/Collectibles.Infrastructure -s Source/Collectibles.Web
dotnet ef database update -p Source/Collectibles.Infrastructure -s Source/Collectibles.Web
```

### Playwright E2E (Development Testing)

The E2E test suite in `Test/Playwright/` uses the Node.js Playwright framework for browser-based testing. This is separate from the C# Playwright integration used for external link caching in production. See [Developer README](Docs/DEVELOPER_README.md#e2e-tests) for setup, test users, and running instructions.

### Basic Coding Standards

- Always use braces for control structures
- PascalCase for public members, `_camelCase` for private fields
- `long` for entity primary keys, never `int`
- Never expose database IDs; use HashIds at all boundaries
- Constants in `ApplicationConstants`, no magic numbers
- One type per file matching the type name

---

## Further Documentation

- [Features](Docs/Features.md) - Detailed feature list and capabilities
- [Configuration Reference](Docs/Configuration.md) - Complete guide to all appsettings.json options
- [Email Configuration](Docs/EmailConfiguration.md) - Configure SMTP, SendGrid, Azure Communication Services, or log-only provider
- [ZIP Upload Feature](Docs/ZipUploadFeature.md) - Upload ZIP files to create items with background processing
- [Large File Uploads](Docs/LargeFileUploads.md) - Dual-mode upload system with chunking and progress tracking
- [Azure VM Upload Optimization](Docs/Advanced/AzureVmUploadOptimization.md) - Tune IIS and network settings for faster uploads
- [Adding New Event Actions](Docs/Advanced/AddingNewEventActions.md) - Extend the event system with new actions
- [Sync Script](Docs/Advanced/Sync-Collectibles.md) - PowerShell utility for comparing local files with showcase contents

---

## License

This project is licensed under the [MIT License](LICENSE).


