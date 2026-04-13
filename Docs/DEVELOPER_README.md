# Developer README

This document provides technical guidance for developers working on the Collectibles application.

## Architecture Overview

The solution follows Clean Architecture principles with these layers:
- **Collectibles.Kernel**: Shared utilities and cross-cutting concerns
- **Collectibles.Domain**: Core business logic, entities, value objects, repository interfaces, enums
- **Collectibles.Application**: CQRS implementation with commands/queries, DTOs, application services
- **Collectibles.Infrastructure**: Data persistence with EF Core, external service implementations
- **Collectibles.Infrastructure.FileProcessing**: File processing and storage services
- **Collectibles.Web**: Blazor Server UI with Interactive Server Components

## Key Components

### Dynamic Field System

The application uses a flexible template system for defining custom fields on collectible items.

#### DynamicFieldRenderer.razor
- **Purpose**: Form input component for editing/creating field values
- **Location**: `Source/Collectibles.Web/Components/Shared/Templates/`
- **Mode**: Interactive - allows users to input and modify data
- **Usage**: Used in forms when users are entering or editing data
- **Key Features**:
  - Renders appropriate form controls (input, textarea, checkbox, dropdown, etc.) based on field type
  - Handles real-time validation and displays error messages
  - Supports both edit and read-only modes via `ReadOnly` parameter
  - Has `ValueChanged` event callback for two-way binding
  - Includes inline validation logic for required fields, patterns, min/max values
  - Special handling for complex types like InflationAdjustedPrice

#### DynamicFieldsDisplay.razor
- **Purpose**: Read-only display component for showing saved field values
- **Location**: `Source/Collectibles.Web/Components/Shared/Templates/`
- **Mode**: Presentation only - formats and displays existing data
- **Usage**: Used to display saved template data in a user-friendly format
- **Key Features**:
  - Loads template definition from database using MediatR
  - Formats values for display (dates, booleans with icons, inflation-adjusted prices)
  - Shows "Not specified" for empty fields
  - Handles multiple fields at once (displays all fields from a template)
  - Has options to show/hide empty fields and help text via parameters
  - Automatically calculates and displays inflation-adjusted values

#### DynamicFieldsEditor.razor
- **Purpose**: Container component that manages multiple DynamicFieldRenderer instances
- **Location**: `Source/Collectibles.Web/Components/Shared/Templates/`
- **Usage**: Used in forms to edit all fields of a template at once

### File Storage System

The application supports multiple storage providers with a factory pattern:

#### Storage Providers
1. **Database Storage** (Default): Files stored as binary data in SQL Server
2. **Azure Blob Storage**: Files stored in Azure Blob containers
3. **Local File System**: Files stored on disk

#### Key Interfaces
- `IFileStorage`: Core abstraction for file operations
- `FileStorageFactory`: Creates appropriate storage provider based on configuration

Configuration in `appsettings.json`:
```json
{
  "StorageSettings": {
    "Provider": 0  // 0=Database, 1=AzureBlob, 2=LocalFileSystem
  }
}
```

### Authorization System

Resource-based authorization using custom handlers:

#### Authorization Handlers
- Located in `Source/Collectibles.Web/Authorization/`
- Implements resource-based authorization for:
  - Attachments
  - CollectibleItems
  - Showcases
  - Templates

#### Key Patterns
- Users can only access their own resources
- Administrators have full access
- Shared showcases have special visibility rules

### CQRS Implementation

#### Commands
- Location: `Source/Collectibles.Application/[Feature]/Commands/`
- Naming: `[Action][Entity]Command` (e.g., `CreateCollectibleItemCommand`)
- Handlers: `[Command]Handler` classes using MediatR

#### Queries
- Location: `Source/Collectibles.Application/[Feature]/Queries/`
- Naming: `Get[Entity][Criteria]Query` (e.g., `GetCollectibleItemByIdQuery`)
- Handlers: Return DTOs, not domain entities

### ID Obfuscation

The application uses Hashids.net to obfuscate database IDs in URLs:

- Configuration: `HashIdConfiguration` class
- Usage: Entity primary keys should never be exposed directly
- Pattern: Convert IDs to/from hash strings at the application boundary
- Example: `/item/x9K2mN` instead of `/item/12345`

## Database Patterns

### Entity Framework Core
- Version: 10.0
- Database: SQL Server
- DbContext: `CollectiblesDbContext`
- Connection String: Configured in `appsettings.json`

### Migration Commands
```bash
# Add a new migration
dotnet ef migrations add [MigrationName] -p Source/Collectibles.Infrastructure -s Source/Collectibles.Web

# Update database
dotnet ef database update -p Source/Collectibles.Infrastructure -s Source/Collectibles.Web

# Remove last migration
dotnet ef migrations remove -p Source/Collectibles.Infrastructure -s Source/Collectibles.Web
```

### Entity Guidelines
- Use `long` for primary keys (not `int`)
- Include audit fields: `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`
- Implement soft delete where appropriate
- Use value objects for complex types

## Testing Strategy

### Unit Tests
- Framework: xUnit
- Assertions: FluentAssertions
- Mocking: Moq
- Test Data: AutoFixture
- Location: `Test/Collectibles.[Layer].Tests/`

### Integration Tests
- Use EF Core InMemory provider for database tests
- Test full command/query pipeline
- Location: `Test/Collectibles.Application.Tests/`

### E2E Tests (Playwright — Development Testing)

> **Note:** Playwright is used in this project for two distinct purposes. This section covers the **E2E testing** use — a Node.js Playwright test suite for development. The other use is **external link caching** in production, where the C# backend uses the `Microsoft.Playwright` NuGet package to capture screenshots of external URLs. See [Playwright in Production](/Docs/Playwright%20in%20production.md) for that use case.

- Framework: Playwright (Node.js)
- Location: `Test/Playwright/`
- Coverage folders: `tests/smoke/`, `tests/showcases/`, `tests/items/`, `tests/attachments/`, `tests/zip-upload/`, `tests/templates/`, `tests/sharing/`, `tests/discovery/`, `tests/account/`, `tests/admin/`, `tests/authorization/`

#### Playwright Test Environment

The Playwright test suite is a standalone Node.js project that starts the app automatically.

- The Playwright config launches `Source/Collectibles.Web` for you
- The app runs with `ASPNETCORE_ENVIRONMENT=Playwright`
- Settings come from `Source/Collectibles.Web/appsettings.Playwright.json`
- Each run resets and reseeds the Playwright database and local upload storage
- Seed data is written to `Source/Collectibles.Web/App_Data/playwright/seed-manifest.json`

Seeded Playwright users:

- `test.admin@collectibles.local` / `xA&%4hTVhTDixSOO` (Administrator)
- `test.user@collectibles.local` / `xA&%4hTVhTDixSOO` (regular user)
- `test.owner@collectibles.local` / `xA&%4hTVhTDixSOO` (ownership/authorization checks)

Canonical Playwright fixtures:

- `Test/Example Data/Showcase Example Images/Software/lotus_123.png`
- `Test/Example Data/Showcase Example Images/Software/windows_31_desktop.jpg`
- `Test/Example Data/Showcase Example Images/README.md`
- `Test/Example Data/Showcase Example Images/ShowcaseScreenshotsBulkZipUpload.zip`

#### Running Playwright Tests

Run Playwright from `Test/Playwright/`.

```bash
# One-time setup
cd Test/Playwright
npm install
npx playwright install chromium
```

The Playwright test suite is self-contained. It starts the application automatically with `ASPNETCORE_ENVIRONMENT=Playwright`, loads configuration from `appsettings.Playwright.json`, and resets/reseeds its own dedicated database on each run. You do not need to start `Collectibles.Web` manually.

Current milestone coverage includes:

- shell/login readiness and seeded-data visibility checks
- showcase create/edit flows
- item create and parent-child flows
- attachment upload, preview, featured-image, and detail flows
- attachment search, type/date filters, and clear-filter reset flows
- attachment tag-management flows from the detail modal and detail page
- ZIP import coverage
- template and structured-data flows
- sharing, public access, and QR flows
- public browse discovery, showcase search, and tag-filter flows
- in-app account management with admin-created throwaway users, profile/password/email/personal-data coverage, relogin checks, and self-delete cleanup
- user-management, management dashboard, log pages, diagnostics, site configuration, theme settings, and safe admin utility-page coverage
- authorization and ownership boundaries

Account/admin suite note:

- `tests/account` intentionally creates confirmed throwaway users through the real admin UI so password changes and self-delete flows do not interfere with the shared seeded Playwright users.
- `tests/admin` uses the seeded admin account and exercises user-management plus operational surfaces without running destructive maintenance actions.

```bash
# Run the full suite
npm test

# Run a single milestone area
npx playwright test --project=chromium tests/smoke
npx playwright test --project=chromium tests/showcases
npx playwright test --project=chromium tests/items
npx playwright test --project=chromium tests/attachments
npx playwright test --project=chromium tests/zip-upload
npx playwright test --project=chromium tests/templates
npx playwright test --project=chromium tests/sharing
npx playwright test --project=chromium tests/discovery
npx playwright test --project=chromium tests/account
npx playwright test --project=chromium tests/admin
npx playwright test --project=chromium tests/authorization

# Run a single spec file
npx playwright test --project=chromium tests/zip-upload/zip-upload.spec.ts
npx playwright test --project=chromium tests/attachments/attachments-list.spec.ts
npx playwright test --project=chromium tests/account/account-management.spec.ts
npx playwright test --project=chromium tests/admin/admin-operations.spec.ts

# Run in a visible browser window
npm run test:headed
```

#### Playwright UI Mode

To open the interactive Playwright test runner GUI, run this from `Test/Playwright/`:

```bash
npm run test:ui
```

What to expect in UI mode:

- Playwright starts the app automatically with the `Playwright` environment
- The `setup` project runs first to create admin and regular-user auth states
- Most test execution should be run against the `chromium` project
- You can run the full suite, a single spec, or an individual test directly from the UI
- Use the file filter to focus on folders like `tests/attachments`, `tests/templates`, `tests/sharing`, `tests/discovery`, `tests/account`, or `tests/admin`
- Failed runs expose traces, screenshots, and videos directly in the UI

Useful follow-up commands:

```bash
# Open the last HTML report
npm run report

# Open trace files for a failed retry when available
npx playwright show-trace <trace.zip>
```

## Blazor Component Patterns

### Component Organization
- Shared components: `Components/Shared/`
- Page components: `Components/Pages/`
- Layout components: `Components/Layout/`

### State Management
- Use cascading parameters for shared state
- Implement `IDisposable` for cleanup
- Use `StateHasChanged()` judiciously

### Performance Considerations
- Use `@key` directive for list rendering
- Implement virtualization for large lists
- Lazy load heavy components
- Cache frequently accessed data

## Security Considerations

### Authentication
- ASP.NET Core Identity
- Two-factor authentication support
- Account lockout policies

### Data Protection
- Never expose database IDs directly (use HashIds)
- Sanitize user input
- Use parameterized queries (handled by EF Core)
- Implement proper CORS policies

### File Upload Security
- Validate file types and sizes
- Scan for malware (when configured)
- Store files outside web root or in database
- Generate unique file names

## Performance Guidelines

### Caching Strategy
- Memory caching for frequently accessed data
- Cache keys follow pattern: `[Entity]:[Id]:[Version]`
- Default expiration: 5 minutes for lists, 15 minutes for details
- Clear cache on entity updates

### Database Optimization
- Use indexes on frequently queried columns
- Implement pagination for large result sets
- Use `AsNoTracking()` for read-only queries
- Batch operations where possible

### Front-end Optimization
- Minimize JavaScript payloads
- Use CSS isolation for component styles
- Implement lazy loading for images
- Use Bootstrap components for consistency

## Development Workflow

### Branch Strategy
- `main`: Production-ready code
- `dev`: Development integration
- `feature/*`: New features
- `bugfix/*`: Bug fixes
- `hotfix/*`: Emergency production fixes

### Code Review Checklist
- [ ] Follows C# coding conventions
- [ ] Includes unit tests
- [ ] Updates documentation if needed
- [ ] No hardcoded values (use constants)
- [ ] Proper error handling
- [ ] Security considerations addressed
- [ ] Performance impact assessed

### Debugging Tips
- Enable detailed errors in Development environment
- Use Seq for structured logging (when configured)
- Browser DevTools for Blazor debugging
- SQL Profiler for database query analysis

## Common Patterns and Solutions

### Adding a New Entity
1. Create domain entity in `Domain/Entities/`
2. Add repository interface in `Domain/[Feature]/`
3. Implement repository in `Infrastructure/Repositories/`
4. Create DTOs in `Application/[Feature]/DTOs/`
5. Add commands/queries in `Application/[Feature]/`
6. Create EF configuration in `Infrastructure/Configurations/`
7. Add migration
8. Create Blazor components for UI

### Adding a New Field Type
1. Add enum value to `FieldType` in Domain
2. Update `DynamicFieldRenderer` with rendering logic
3. Update `DynamicFieldsDisplay` with display formatting
4. Add validation rules if needed
5. Update field definition UI in admin area
6. Add migration if database changes needed

### Implementing a New Storage Provider
1. Implement `IFileStorage` interface
2. Add provider to `StorageProvider` enum
3. Update `FileStorageFactory` to handle new provider
4. Add configuration settings
5. Add provider-specific configuration class
6. Write integration tests

## Troubleshooting

### Common Issues

#### Migration Failures
- Ensure database connection string is correct
- Check for pending migrations
- Verify SQL Server is running
- Review migration SQL for conflicts

#### Blazor Rendering Issues
- Check for JavaScript errors in console
- Verify SignalR connection is established
- Look for unhandled exceptions in server logs
- Ensure components implement proper lifecycle methods

#### File Upload Problems
- Verify storage provider configuration
- Check file size limits in configuration
- Ensure proper permissions for file system storage
- Monitor available disk space

## Additional Resources

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [CQRS Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [Blazor Documentation](https://docs.microsoft.com/en-us/aspnet/core/blazor/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [MediatR](https://github.com/jbogard/MediatR)
- [Hashids.net](https://github.com/ullmark/hashids.net)
