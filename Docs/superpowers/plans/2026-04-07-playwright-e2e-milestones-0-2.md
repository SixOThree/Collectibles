# Playwright E2E Milestones 0-2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a deterministic Playwright harness plus the first high-value end-to-end coverage for login, showcases, collectible items, and authorization boundaries.

**Architecture:** Run the Blazor app in a dedicated `Playwright` ASP.NET environment that boots against a local SQL Server database, resets and seeds deterministic data on each run, writes a seed manifest into `App_Data`, and exposes only Chromium at first. Organize Playwright tests by domain, reuse saved auth states for the admin and regular users, and use helper modules so Milestones 3+ can add coverage without reworking the harness.

**Tech Stack:** ASP.NET Core Blazor Server, EF Core, SQL Server LocalDB, Node Playwright Test, TypeScript, PowerShell, local file storage

---

## File Structure

### Application files

- Modify: `.gitignore`
- Create: `Source/Collectibles.Web/appsettings.Playwright.json`
- Modify: `Source/Collectibles.Infrastructure/Common/ConfigureServices.cs`
- Modify: `Source/Collectibles.Infrastructure/Services/DatabaseInitializerService.cs`
- Create: `Source/Collectibles.Infrastructure/Persistence/Seeders/PlaywrightSeedManifest.cs`
- Create: `Source/Collectibles.Infrastructure/Persistence/Seeders/PlaywrightScenarioSeeder.cs`

### Playwright harness files

- Modify: `Test/Playwright/.gitignore`
- Modify: `Test/Playwright/package.json`
- Modify: `Test/Playwright/playwright.config.ts`
- Create: `Test/Playwright/tsconfig.json`
- Delete: `Test/Playwright/tests/example.spec.ts`
- Delete: `Test/Playwright/tests-examples/demo-todo-app.spec.ts`
- Create: `Test/Playwright/tests/helpers/auth.ts`
- Create: `Test/Playwright/tests/helpers/names.ts`
- Create: `Test/Playwright/tests/helpers/seed-manifest.ts`
- Create: `Test/Playwright/tests/helpers/showcases.ts`
- Create: `Test/Playwright/tests/helpers/items.ts`
- Create: `Test/Playwright/tests/setup/auth.setup.ts`

### Playwright specs

- Create: `Test/Playwright/tests/smoke/app-shell.spec.ts`
- Create: `Test/Playwright/tests/smoke/seeded-data.spec.ts`
- Create: `Test/Playwright/tests/smoke/authenticated-shell.spec.ts`
- Create: `Test/Playwright/tests/showcases/showcases.spec.ts`
- Create: `Test/Playwright/tests/showcases/public-showcases.spec.ts`
- Create: `Test/Playwright/tests/items/items.spec.ts`
- Create: `Test/Playwright/tests/authorization/ownership.spec.ts`
- Create: `Test/Playwright/tests/authorization/admin-access.spec.ts`

### Documentation files

- Modify: `agent_docs/claude/playwright-testing.md`
- Modify: `README.md`

## Preflight

Before Task 1:

- Run `git branch --show-current`
- If the current branch is `dev`, `main`, `test`, `prod`, or `ReadyOK`, create a feature branch first:

```powershell
git checkout -b feature/playwright-e2e-m0-m2
```

- From `Test/Playwright`, install dependencies and the Chromium browser once:

```powershell
npm install
npx playwright install chromium
```

## Task 1: Replace The Starter Playwright Project With A Local-App Harness

**Files:**
- Create: `Source/Collectibles.Web/appsettings.Playwright.json`
- Modify: `.gitignore`
- Modify: `Test/Playwright/.gitignore`
- Modify: `Test/Playwright/package.json`
- Modify: `Test/Playwright/playwright.config.ts`
- Create: `Test/Playwright/tsconfig.json`
- Create: `Test/Playwright/tests/smoke/app-shell.spec.ts`
- Delete: `Test/Playwright/tests/example.spec.ts`
- Delete: `Test/Playwright/tests-examples/demo-todo-app.spec.ts`

- [ ] **Step 1: Write the failing anonymous smoke test**

Create `Test/Playwright/tests/smoke/app-shell.spec.ts`:

```ts
import { test, expect } from '@playwright/test';

test('login page loads from the local Collectibles app', async ({ page }) => {
  await page.goto('/Account/Login');

  await expect(page).toHaveURL(/\/Account\/Login/);
  await expect(page.getByRole('heading', { name: 'Log in' })).toBeVisible();
  await expect(page.getByLabel('Email')).toBeVisible();
  await expect(page.getByLabel('Password')).toBeVisible();
});
```

- [ ] **Step 2: Run the smoke test and verify it fails**

Run from `Test/Playwright`:

```powershell
npx playwright test tests/smoke/app-shell.spec.ts
```

Expected: FAIL because the current Playwright project still targets the starter example configuration and has no `baseURL` or app web server.

- [ ] **Step 3: Point Playwright at the Collectibles app and remove the starter examples**

Replace `Test/Playwright/package.json` with:

```json
{
  "name": "playwright",
  "version": "1.0.0",
  "private": true,
  "type": "commonjs",
  "scripts": {
    "test": "playwright test",
    "test:headed": "playwright test --headed",
    "test:ui": "playwright test --ui",
    "report": "playwright show-report"
  },
  "devDependencies": {
    "@playwright/test": "^1.52.0",
    "@types/node": "^22.15.29"
  }
}
```

Replace `Test/Playwright/playwright.config.ts` with:

```ts
import { defineConfig, devices } from '@playwright/test';

const port = 5115;
const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? `http://127.0.0.1:${port}`;

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [['html', { open: 'never' }]],
  use: {
    baseURL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure'
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] }
    }
  ],
  webServer: {
    command: `pwsh -NoProfile -Command "Set-Location '..\\..\\Source\\Collectibles.Web'; $env:ASPNETCORE_ENVIRONMENT='Playwright'; dotnet run --no-launch-profile --urls ${baseURL}"`,
    url: baseURL,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000
  }
});
```

Create `Test/Playwright/tsconfig.json`:

```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "commonjs",
    "moduleResolution": "node",
    "types": ["node", "@playwright/test"],
    "strict": true,
    "esModuleInterop": true,
    "resolveJsonModule": true,
    "skipLibCheck": true
  },
  "include": ["playwright.config.ts", "tests/**/*.ts"]
}
```

Append this line to `Test/Playwright/.gitignore`:

```gitignore
/playwright/.auth/
```

Append this line to `.gitignore`:

```gitignore
/Source/Collectibles.Web/App_Data/playwright/
```

Create `Source/Collectibles.Web/appsettings.Playwright.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=Collectibles_Playwright;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Storage": {
    "Provider": "LocalFileSystem",
    "LocalFileSystem": {
      "BasePath": "App_Data/playwright/uploads",
      "UseAbsolutePath": false
    },
    "DirectUpload": {
      "Enabled": false,
      "ThresholdBytes": 52428800,
      "SasExpiryMinutes": 30
    }
  },
  "EmailSettings": {
    "Provider": "NULL",
    "EnableEmailLogging": false
  },
  "ExternalLinks": {
    "Enabled": false,
    "CachingEnabled": false
  },
  "Serilog": {
    "EnableDatabaseLogging": false
  },
  "CrawlerBlocking": {
    "Enabled": false
  },
  "SecurityScanBlocking": {
    "Enabled": false
  },
  "PlaywrightSeed": {
    "ResetDatabaseOnStartup": true,
    "ResetStorageOnStartup": true,
    "SeedManifestPath": "App_Data/playwright/seed-manifest.json"
  }
}
```

Delete:

- `Test/Playwright/tests/example.spec.ts`
- `Test/Playwright/tests-examples/demo-todo-app.spec.ts`

- [ ] **Step 4: Run the smoke test and verify the local app bootstraps**

Run from `Test/Playwright`:

```powershell
npm test -- tests/smoke/app-shell.spec.ts
```

Expected: PASS with one Chromium test proving the local app starts under `ASPNETCORE_ENVIRONMENT=Playwright` and serves `/Account/Login`.

- [ ] **Step 5: Commit the harness bootstrap**

```powershell
git add .gitignore Source/Collectibles.Web/appsettings.Playwright.json Test/Playwright/.gitignore Test/Playwright/package.json Test/Playwright/playwright.config.ts Test/Playwright/tsconfig.json Test/Playwright/tests/smoke/app-shell.spec.ts
git rm Test/Playwright/tests/example.spec.ts Test/Playwright/tests-examples/demo-todo-app.spec.ts
git commit -m "Set up local Playwright harness

Point the Playwright project at the Collectibles app with a dedicated Playwright environment, local database settings, and a smoke test so the suite runs against the application instead of the starter examples.

~"
```

## Task 2: Reset And Seed Deterministic Playwright Data On Every Run

**Files:**
- Modify: `Source/Collectibles.Infrastructure/Common/ConfigureServices.cs`
- Modify: `Source/Collectibles.Infrastructure/Services/DatabaseInitializerService.cs`
- Create: `Source/Collectibles.Infrastructure/Persistence/Seeders/PlaywrightSeedManifest.cs`
- Create: `Source/Collectibles.Infrastructure/Persistence/Seeders/PlaywrightScenarioSeeder.cs`
- Create: `Test/Playwright/tests/helpers/seed-manifest.ts`
- Create: `Test/Playwright/tests/smoke/seeded-data.spec.ts`

- [ ] **Step 1: Write the failing seeded-data smoke test**

Create `Test/Playwright/tests/helpers/seed-manifest.ts`:

```ts
import fs from 'fs';
import path from 'path';

export type SeedManifest = {
  users: {
    admin: { email: string; password: string; displayName: string };
    regular: { email: string; password: string; displayName: string };
    otherOwner: { email: string; password: string; displayName: string };
  };
  showcases: {
    regularPrivate: { name: string; hash: string };
    regularPublic: { name: string; hash: string };
    otherPrivate: { name: string; hash: string };
  };
  items: {
    regularRoot: { name: string; hash: string };
    regularChild: { name: string; hash: string };
    otherPrivate: { name: string; hash: string };
  };
};

export function readSeedManifest(): SeedManifest {
  const manifestPath = path.resolve(
    __dirname,
    '../../../../Source/Collectibles.Web/App_Data/playwright/seed-manifest.json'
  );

  return JSON.parse(fs.readFileSync(manifestPath, 'utf8')) as SeedManifest;
}
```

Create `Test/Playwright/tests/smoke/seeded-data.spec.ts`:

```ts
import { test, expect } from '@playwright/test';
import { readSeedManifest } from '../helpers/seed-manifest';

test('seeded public showcase is visible and the seeded private showcase is hidden from anonymous users', async ({ page }) => {
  const manifest = readSeedManifest();

  await page.goto('/showcases/public');

  await expect(page.getByRole('heading', { name: 'Browse All Showcases' })).toBeVisible();
  await expect(page.getByRole('link', { name: manifest.showcases.regularPublic.name })).toBeVisible();
  await expect(page.getByText(manifest.showcases.regularPrivate.name)).toHaveCount(0);
});
```

- [ ] **Step 2: Run the seeded-data smoke test and verify it fails**

Run from `Test/Playwright`:

```powershell
npm test -- tests/smoke/seeded-data.spec.ts
```

Expected: FAIL because the app does not yet generate `seed-manifest.json` and does not seed the named Playwright users, showcases, and items.

- [ ] **Step 3: Implement Playwright reset, seed, and manifest generation**

Create `Source/Collectibles.Infrastructure/Persistence/Seeders/PlaywrightSeedManifest.cs`:

```csharp
namespace Collectibles.Infrastructure.Persistence.Seeders;

public sealed class PlaywrightSeedManifest
{
    public required SeedUsers Users { get; init; }
    public required SeedShowcases Showcases { get; init; }
    public required SeedItems Items { get; init; }
}

public sealed class SeedUsers
{
    public required SeedUser Admin { get; init; }
    public required SeedUser Regular { get; init; }
    public required SeedUser OtherOwner { get; init; }
}

public sealed class SeedUser
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string DisplayName { get; init; }
}

public sealed class SeedShowcases
{
    public required SeedReference RegularPrivate { get; init; }
    public required SeedReference RegularPublic { get; init; }
    public required SeedReference OtherPrivate { get; init; }
}

public sealed class SeedItems
{
    public required SeedReference RegularRoot { get; init; }
    public required SeedReference RegularChild { get; init; }
    public required SeedReference OtherPrivate { get; init; }
}

public sealed class SeedReference
{
    public required string Name { get; init; }
    public required string Hash { get; init; }
}
```

Create `Source/Collectibles.Infrastructure/Persistence/Seeders/PlaywrightScenarioSeeder.cs`:

```csharp
using System.Text.Json;
using Collectibles.Application.Services;
using Collectibles.Domain.Entities;
using Collectibles.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Collectibles.Infrastructure.Persistence.Seeders;

public sealed class PlaywrightScenarioSeeder
{
    private const string DefaultPassword = "xA&%4hTVhTDixSOO";

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHashIdsService _hashIdsService;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public PlaywrightScenarioSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IHashIdsService hashIdsService,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _hashIdsService = hashIdsService;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await ResetStorageAsync();

        var admin = await CreateUserAsync("test.admin@collectibles.local", "Playwright Admin", new[] { "Administrator" });
        var regular = await CreateUserAsync("test.user@collectibles.local", "Playwright User", Array.Empty<string>());
        var otherOwner = await CreateUserAsync("test.owner@collectibles.local", "Other Private Owner", Array.Empty<string>());

        var regularPrivate = new Showcase { Name = "PW Seed Private Showcase", Description = "Private showcase owned by the regular user.", IsPrivate = true, UserId = regular.Id };
        var regularPublic = new Showcase { Name = "PW Seed Public Showcase", Description = "Public showcase visible on the browse page.", IsPrivate = false, UserId = regular.Id };
        var otherPrivate = new Showcase { Name = "PW Seed Other User Private Showcase", Description = "Private showcase owned by another user.", IsPrivate = true, UserId = otherOwner.Id };

        _context.Showcases.AddRange(regularPrivate, regularPublic, otherPrivate);
        await _context.SaveChangesAsync(cancellationToken);

        var regularRoot = new CollectibleItem
        {
            Name = "PW Seed Root Item",
            DetailedDescription = "Root seeded item used by the item tests."
        };
        regularRoot.Showcases.Add(regularPrivate);

        var regularChild = new CollectibleItem
        {
            Name = "PW Seed Child Item",
            DetailedDescription = "Child seeded item used for breadcrumb and parent checks.",
            Parent = regularRoot
        };
        regularChild.Showcases.Add(regularPrivate);

        var otherPrivateItem = new CollectibleItem
        {
            Name = "PW Seed Other User Private Item",
            DetailedDescription = "Private item owned by another seeded user."
        };
        otherPrivateItem.Showcases.Add(otherPrivate);

        _context.CollectibleItems.AddRange(regularRoot, regularChild, otherPrivateItem);
        await _context.SaveChangesAsync(cancellationToken);

        var manifest = new PlaywrightSeedManifest
        {
            Users = new SeedUsers
            {
                Admin = new SeedUser { Email = admin.Email!, Password = DefaultPassword, DisplayName = admin.DisplayName ?? admin.Email! },
                Regular = new SeedUser { Email = regular.Email!, Password = DefaultPassword, DisplayName = regular.DisplayName ?? regular.Email! },
                OtherOwner = new SeedUser { Email = otherOwner.Email!, Password = DefaultPassword, DisplayName = otherOwner.DisplayName ?? otherOwner.Email! }
            },
            Showcases = new SeedShowcases
            {
                RegularPrivate = new SeedReference { Name = regularPrivate.Name, Hash = _hashIdsService.Encode(regularPrivate.Id) },
                RegularPublic = new SeedReference { Name = regularPublic.Name, Hash = _hashIdsService.Encode(regularPublic.Id) },
                OtherPrivate = new SeedReference { Name = otherPrivate.Name, Hash = _hashIdsService.Encode(otherPrivate.Id) }
            },
            Items = new SeedItems
            {
                RegularRoot = new SeedReference { Name = regularRoot.Name!, Hash = _hashIdsService.Encode(regularRoot.Id) },
                RegularChild = new SeedReference { Name = regularChild.Name!, Hash = _hashIdsService.Encode(regularChild.Id) },
                OtherPrivate = new SeedReference { Name = otherPrivateItem.Name!, Hash = _hashIdsService.Encode(otherPrivateItem.Id) }
            }
        };

        var manifestPath = _configuration["PlaywrightSeed:SeedManifestPath"] ?? "App_Data/playwright/seed-manifest.json";
        var fullManifestPath = Path.Combine(_environment.ContentRootPath, manifestPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullManifestPath)!);
        await File.WriteAllTextAsync(fullManifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
    }

    private async Task<ApplicationUser> CreateUserAsync(string email, string displayName, IEnumerable<string> roles)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            FirstName = displayName.Split(' ')[0],
            LastName = displayName.Split(' ').Last(),
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, DefaultPassword);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(error => error.Description)));
        }

        foreach (var role in roles)
        {
            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(error => error.Description)));
            }
        }

        return user;
    }

    private Task ResetStorageAsync()
    {
        if (!_configuration.GetValue<bool>("PlaywrightSeed:ResetStorageOnStartup"))
        {
            return Task.CompletedTask;
        }

        var basePath = _configuration["Storage:LocalFileSystem:BasePath"] ?? "App_Data/playwright/uploads";
        var fullPath = Path.Combine(_environment.ContentRootPath, basePath);

        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, recursive: true);
        }

        Directory.CreateDirectory(fullPath);
        return Task.CompletedTask;
    }
}
```

Modify `Source/Collectibles.Infrastructure/Common/ConfigureServices.cs` to register the seeder:

```csharp
services.AddScoped<PlaywrightScenarioSeeder>();
```

Modify `Source/Collectibles.Infrastructure/Services/DatabaseInitializerService.cs`:

```csharp
using Microsoft.Extensions.Configuration;
```

```csharp
private readonly IConfiguration _configuration;
```

```csharp
public DatabaseInitializerService(
    IServiceProvider serviceProvider,
    ILogger<DatabaseInitializerService> logger,
    IHostEnvironment environment,
    IConfiguration configuration)
{
    _serviceProvider = serviceProvider;
    _logger = logger;
    _environment = environment;
    _configuration = configuration;
}
```

```csharp
var isPlaywright = _environment.IsEnvironment("Playwright");
if (isPlaywright && _configuration.GetValue<bool>("PlaywrightSeed:ResetDatabaseOnStartup"))
{
    _logger.LogInformation("Resetting Playwright database before migrations");
    await context.Database.EnsureDeletedAsync(cancellationToken);
}

await context.Database.MigrateAsync(cancellationToken);
```

```csharp
await CreateRolesAsync(roleManager, sysLogService);
await SeedVintageComputerTagsAsync(context, sysLogService);
await SeedTemplatesAsync(context, sysLogService);

if (isPlaywright)
{
    var playwrightScenarioSeeder = scope.ServiceProvider.GetRequiredService<PlaywrightScenarioSeeder>();
    await playwrightScenarioSeeder.SeedAsync(cancellationToken);

    _logger.LogInformation("Playwright database initialization completed successfully");
    await sysLogService.LogInformationAsync("Playwright database initialization completed successfully", "Application.Startup", cancellationToken: cancellationToken);
    return;
}

if (await setupTokenService.IsSetupRequiredAsync())
{
    await setupTokenService.GenerateSetupTokenAsync();
    _logger.LogWarning("No administrators found. Setup token has been generated for initial configuration.");
    await sysLogService.LogWarningAsync("Initial setup required - no administrators found", "Security.Setup", new Dictionary<string, object> { ["SetupTokenGenerated"] = true }, cancellationToken);
}
else
{
    _logger.LogInformation("Administrator accounts found. System is configured.");
}
```

- [ ] **Step 4: Run the seeded-data smoke test and verify it passes**

Run from `Test/Playwright`:

```powershell
npm test -- tests/smoke/seeded-data.spec.ts
```

Expected: PASS, and `Source/Collectibles.Web/App_Data/playwright/seed-manifest.json` exists for the running environment.

- [ ] **Step 5: Commit the deterministic seed foundation**

```powershell
git add Source/Collectibles.Infrastructure/Common/ConfigureServices.cs Source/Collectibles.Infrastructure/Services/DatabaseInitializerService.cs Source/Collectibles.Infrastructure/Persistence/Seeders/PlaywrightSeedManifest.cs Source/Collectibles.Infrastructure/Persistence/Seeders/PlaywrightScenarioSeeder.cs Test/Playwright/tests/helpers/seed-manifest.ts Test/Playwright/tests/smoke/seeded-data.spec.ts
git commit -m "Seed deterministic Playwright test data

Reset the Playwright database and file storage on startup, create stable admin and user fixtures, and write a seed manifest that the Playwright suite can use for repeatable routing and authorization checks.

~"
```

## Task 3: Add Reusable Admin And User Auth States

**Files:**
- Modify: `Test/Playwright/playwright.config.ts`
- Create: `Test/Playwright/tests/helpers/auth.ts`
- Create: `Test/Playwright/tests/helpers/names.ts`
- Create: `Test/Playwright/tests/setup/auth.setup.ts`
- Create: `Test/Playwright/tests/smoke/authenticated-shell.spec.ts`

- [ ] **Step 1: Write the failing authenticated-shell smoke test**

Create `Test/Playwright/tests/smoke/authenticated-shell.spec.ts`:

```ts
import { test, expect } from '@playwright/test';
import { authFile } from '../helpers/auth';

test.describe('regular user shell', () => {
  test.use({ storageState: authFile('user') });

  test('regular user can open My Showcases', async ({ page }) => {
    await page.goto('/showcases');
    await expect(page.getByRole('heading', { name: 'My Showcases' })).toBeVisible();
  });
});

test.describe('admin shell', () => {
  test.use({ storageState: authFile('admin') });

  test('admin can open User Management', async ({ page }) => {
    await page.goto('/users');
    await expect(page.getByRole('heading', { name: 'User Management' })).toBeVisible();
  });
});
```

- [ ] **Step 2: Run the authenticated-shell smoke test and verify it fails**

Run from `Test/Playwright`:

```powershell
npm test -- tests/smoke/authenticated-shell.spec.ts
```

Expected: FAIL because no auth-state files exist yet and there is no setup project to create them.

- [ ] **Step 3: Implement reusable auth-state setup**

Create `Test/Playwright/tests/helpers/auth.ts`:

```ts
import path from 'path';

export type AuthRole = 'admin' | 'user';

export const authDir = path.resolve(__dirname, '../../playwright/.auth');

export function authFile(role: AuthRole): string {
  return path.join(authDir, `${role}.json`);
}
```

Create `Test/Playwright/tests/helpers/names.ts`:

```ts
export function uniqueName(prefix: string): string {
  const id = `${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
  return `${prefix} ${id}`;
}
```

Create `Test/Playwright/tests/setup/auth.setup.ts`:

```ts
import fs from 'fs';
import { test as setup, expect, Page } from '@playwright/test';
import { authDir, authFile } from '../helpers/auth';
import { readSeedManifest } from '../helpers/seed-manifest';

async function signIn(page: Page, email: string, password: string) {
  await page.goto('/Account/Login');
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Log in' }).click();
  await expect(page).toHaveURL(/\/showcases/);
}

setup.beforeAll(() => {
  fs.mkdirSync(authDir, { recursive: true });
});

setup('authenticate regular user', async ({ page }) => {
  const manifest = readSeedManifest();
  await signIn(page, manifest.users.regular.email, manifest.users.regular.password);
  await page.context().storageState({ path: authFile('user') });
});

setup('authenticate admin user', async ({ page }) => {
  const manifest = readSeedManifest();
  await signIn(page, manifest.users.admin.email, manifest.users.admin.password);
  await page.context().storageState({ path: authFile('admin') });
});
```

Replace `Test/Playwright/playwright.config.ts` with:

```ts
import { defineConfig, devices } from '@playwright/test';

const port = 5115;
const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? `http://127.0.0.1:${port}`;

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [['html', { open: 'never' }]],
  use: {
    baseURL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure'
  },
  projects: [
    {
      name: 'setup',
      testMatch: /tests\/setup\/.*\.setup\.ts/,
      use: { ...devices['Desktop Chrome'] }
    },
    {
      name: 'chromium',
      dependencies: ['setup'],
      use: { ...devices['Desktop Chrome'] }
    }
  ],
  webServer: {
    command: `pwsh -NoProfile -Command "Set-Location '..\\..\\Source\\Collectibles.Web'; $env:ASPNETCORE_ENVIRONMENT='Playwright'; dotnet run --no-launch-profile --urls ${baseURL}"`,
    url: baseURL,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000
  }
});
```

- [ ] **Step 4: Run the authenticated-shell smoke test and verify it passes**

Run from `Test/Playwright`:

```powershell
npm test -- --project=chromium tests/smoke/authenticated-shell.spec.ts
```

Expected: PASS, with the `setup` project writing `.auth/user.json` and `.auth/admin.json` before the Chromium tests run.

- [ ] **Step 5: Commit the auth-state setup**

```powershell
git add Test/Playwright/playwright.config.ts Test/Playwright/tests/helpers/auth.ts Test/Playwright/tests/helpers/names.ts Test/Playwright/tests/setup/auth.setup.ts Test/Playwright/tests/smoke/authenticated-shell.spec.ts
git commit -m "Add reusable Playwright auth states

Create setup-time login flows for the seeded admin and regular users so milestone tests can reuse stable storage states instead of repeating the login form in every spec.

~"
```

## Task 4: Cover The Core Showcase Journeys

**Files:**
- Create: `Test/Playwright/tests/helpers/showcases.ts`
- Create: `Test/Playwright/tests/showcases/showcases.spec.ts`
- Create: `Test/Playwright/tests/showcases/public-showcases.spec.ts`

- [ ] **Step 1: Write the failing showcase specs**

Create `Test/Playwright/tests/helpers/showcases.ts`:

```ts
import { expect, Page } from '@playwright/test';

export async function createShowcase(page: Page, name: string, isPrivate: boolean): Promise<string> {
  await page.goto('/showcase/new');
  await expect(page.getByRole('heading', { name: 'Create New Showcase' })).toBeVisible();

  await page.getByLabel('Name').fill(name);
  await page.getByLabel('Description').fill(`${name} description`);

  const privateCheckbox = page.getByLabel(/Private showcase/);
  if (isPrivate) {
    await privateCheckbox.check();
  } else {
    await privateCheckbox.uncheck();
  }

  await page.getByRole('button', { name: 'Create Showcase' }).click();
  await expect(page).toHaveURL(/\/showcase\/[^/]+$/);

  const match = page.url().match(/\/showcase\/([^/?#]+)/);
  if (!match) {
    throw new Error(`Unable to parse showcase hash from URL: ${page.url()}`);
  }

  return match[1];
}
```

Create `Test/Playwright/tests/showcases/showcases.spec.ts`:

```ts
import { test, expect } from '@playwright/test';
import { authFile } from '../helpers/auth';
import { uniqueName } from '../helpers/names';
import { createShowcase } from '../helpers/showcases';

test.use({ storageState: authFile('user') });

test('regular user can create a new private showcase', async ({ page }) => {
  const showcaseName = uniqueName('PW Showcase');

  await createShowcase(page, showcaseName, true);

  await expect(page.getByRole('heading', { name: showcaseName })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Info' })).toBeVisible();
});

test('regular user can edit showcase details and make the showcase public', async ({ page }) => {
  const showcaseName = uniqueName('PW Editable Showcase');
  await createShowcase(page, showcaseName, true);

  await page.getByRole('button', { name: 'Edit' }).click();
  await expect(page).toHaveURL(/\/showcase\/[^/]+\/edit$/);

  await page.getByLabel(/^Description$/).fill('Updated by Playwright');
  await page.getByLabel('Private showcase').uncheck();
  await page.getByRole('button', { name: 'Save Changes' }).click();

  await expect(page.getByRole('heading', { name: showcaseName })).toBeVisible();
  await page.getByRole('button', { name: 'Info' }).click();
  await expect(page.getByText('Public')).toBeVisible();
  await expect(page.getByText('Updated by Playwright')).toBeVisible();
});
```

Create `Test/Playwright/tests/showcases/public-showcases.spec.ts`:

```ts
import { test, expect } from '@playwright/test';
import { readSeedManifest } from '../helpers/seed-manifest';

test('anonymous users can browse the seeded public showcase but not the seeded private showcase', async ({ page }) => {
  const manifest = readSeedManifest();

  await page.goto('/showcases/public');
  await expect(page.getByRole('link', { name: manifest.showcases.regularPublic.name })).toBeVisible();
  await expect(page.getByText(manifest.showcases.regularPrivate.name)).toHaveCount(0);
});
```

- [ ] **Step 2: Run the showcase specs and verify they fail**

Run from `Test/Playwright`:

```powershell
npm test -- --project=chromium tests/showcases/showcases.spec.ts tests/showcases/public-showcases.spec.ts
```

Expected: FAIL because the helper modules and showcase journey coverage do not exist yet.

- [ ] **Step 3: Make the showcase helpers and specs pass**

Use the file contents from Step 1 exactly.

- [ ] **Step 4: Run the showcase specs and verify they pass**

Run from `Test/Playwright`:

```powershell
npm test -- --project=chromium tests/showcases/showcases.spec.ts tests/showcases/public-showcases.spec.ts
```

Expected: PASS for showcase creation, editing, and public/private browsing.

- [ ] **Step 5: Commit the showcase milestone**

```powershell
git add Test/Playwright/tests/helpers/showcases.ts Test/Playwright/tests/showcases/showcases.spec.ts Test/Playwright/tests/showcases/public-showcases.spec.ts
git commit -m "Add Playwright coverage for showcase journeys

Cover the first end-user showcase flows by testing creation, editing, and public visibility behavior with the seeded regular user and anonymous browse scenarios.

~"
```

## Task 5: Cover The Collectible Item Lifecycle

**Files:**
- Create: `Test/Playwright/tests/helpers/items.ts`
- Create: `Test/Playwright/tests/items/items.spec.ts`

- [ ] **Step 1: Write the failing item lifecycle spec**

Create `Test/Playwright/tests/helpers/items.ts`:

```ts
import { expect, Page } from '@playwright/test';

export async function createItem(page: Page, showcaseHash: string, name: string, parentHash?: string): Promise<string> {
  const url = parentHash
    ? `/showcase/${showcaseHash}/item/new?parent=${parentHash}`
    : `/showcase/${showcaseHash}/item/new`;

  await page.goto(url);
  await expect(page.getByRole('heading', { name: 'Add New Collectible Item' })).toBeVisible();

  await page.getByLabel(/^Name/).fill(name);
  await page.getByLabel('Description').fill(`${name} description`);
  await page.getByRole('button', { name: 'Create Item' }).click();

  await expect(page).toHaveURL(new RegExp(`/showcase/${showcaseHash}$`));
  await page.getByText(name, { exact: true }).click();
  await expect(page).toHaveURL(/\/item\/[^/]+/);

  const match = page.url().match(/\/item\/([^/?#]+)/);
  if (!match) {
    throw new Error(`Unable to parse item hash from URL: ${page.url()}`);
  }

  return match[1];
}
```

Create `Test/Playwright/tests/items/items.spec.ts`:

```ts
import { test, expect } from '@playwright/test';
import { authFile } from '../helpers/auth';
import { uniqueName } from '../helpers/names';
import { createShowcase } from '../helpers/showcases';
import { createItem } from '../helpers/items';

test.use({ storageState: authFile('user') });

test('regular user can create and edit a collectible item', async ({ page }) => {
  const showcaseHash = await createShowcase(page, uniqueName('PW Item Showcase'), true);
  const itemName = uniqueName('PW Root Item');

  await createItem(page, showcaseHash, itemName);

  await expect(page.getByRole('heading', { name: itemName })).toBeVisible();
  await page.getByRole('link', { name: 'Edit' }).click();
  await expect(page).toHaveURL(/\/item\/[^/]+\/edit$/);

  const updatedName = `${itemName} Updated`;
  await page.getByLabel(/^Name/).fill(updatedName);
  await page.getByRole('button', { name: 'Save Changes' }).click();

  await expect(page.getByRole('heading', { name: updatedName })).toBeVisible();
});

test('regular user can create a child item and see the breadcrumb trail', async ({ page }) => {
  const showcaseHash = await createShowcase(page, uniqueName('PW Hierarchy Showcase'), true);
  const parentName = uniqueName('PW Parent Item');
  const childName = uniqueName('PW Child Item');

  const parentHash = await createItem(page, showcaseHash, parentName);
  await createItem(page, showcaseHash, childName, parentHash);

  await expect(page.getByRole('heading', { name: childName })).toBeVisible();
  await expect(page.getByRole('link', { name: parentName })).toBeVisible();
  await expect(page.getByText(parentName, { exact: true })).toBeVisible();
});
```

- [ ] **Step 2: Run the item lifecycle spec and verify it fails**

Run from `Test/Playwright`:

```powershell
npm test -- --project=chromium tests/items/items.spec.ts
```

Expected: FAIL because the item helper module and lifecycle spec do not exist yet.

- [ ] **Step 3: Make the item lifecycle spec pass**

Use the file contents from Step 1 exactly.

- [ ] **Step 4: Run the item lifecycle spec and verify it passes**

Run from `Test/Playwright`:

```powershell
npm test -- --project=chromium tests/items/items.spec.ts
```

Expected: PASS for item creation, item editing, child-item creation, and breadcrumb behavior.

- [ ] **Step 5: Commit the item milestone**

```powershell
git add Test/Playwright/tests/helpers/items.ts Test/Playwright/tests/items/items.spec.ts
git commit -m "Add Playwright coverage for collectible item flows

Exercise the core collectible item lifecycle by covering item creation, item editing, parent-child hierarchy creation, and breadcrumb visibility within a user-owned showcase.

~"
```

## Task 6: Cover Ownership And Admin Access Boundaries

**Files:**
- Create: `Test/Playwright/tests/authorization/ownership.spec.ts`
- Create: `Test/Playwright/tests/authorization/admin-access.spec.ts`

- [ ] **Step 1: Write the failing authorization specs**

Create `Test/Playwright/tests/authorization/ownership.spec.ts`:

```ts
import { test, expect } from '@playwright/test';
import { authFile } from '../helpers/auth';
import { readSeedManifest } from '../helpers/seed-manifest';

test.use({ storageState: authFile('user') });

test('regular user cannot open another user private showcase', async ({ page }) => {
  const manifest = readSeedManifest();

  await page.goto(`/showcase/${manifest.showcases.otherPrivate.hash}`);

  await expect(page.getByRole('heading', { name: 'Access Denied' })).toBeVisible();
  await expect(page.getByText("This showcase is private and you don't have permission to view it.")).toBeVisible();
});

test('regular user cannot add items to another user showcase', async ({ page }) => {
  const manifest = readSeedManifest();

  await page.goto(`/showcase/${manifest.showcases.otherPrivate.hash}/item/new`);

  await expect(page.getByRole('heading', { name: 'Unauthorized' })).toBeVisible();
  await expect(page.getByText("You don't have permission to add items to this showcase.")).toBeVisible();
});

test('regular user cannot open another user private item', async ({ page }) => {
  const manifest = readSeedManifest();

  await page.goto(`/item/${manifest.items.otherPrivate.hash}`);

  await expect(page.getByRole('heading', { name: 'Access Denied' })).toBeVisible();
  await expect(page.getByText("This item is in a private showcase and you don't have permission to view it.")).toBeVisible();
});
```

Create `Test/Playwright/tests/authorization/admin-access.spec.ts`:

```ts
import { test, expect } from '@playwright/test';
import { authFile } from '../helpers/auth';

test.describe('regular user admin boundary', () => {
  test.use({ storageState: authFile('user') });

  test('regular user is redirected away from User Management', async ({ page }) => {
    await page.goto('/users');

    await expect(page).toHaveURL(/\/Account\/AccessDenied/);
    await expect(page.getByRole('heading', { name: 'Access Restricted' })).toBeVisible();
  });
});

test.describe('admin access', () => {
  test.use({ storageState: authFile('admin') });

  test('admin can open User Management', async ({ page }) => {
    await page.goto('/users');

    await expect(page).toHaveURL(/\/users$/);
    await expect(page.getByRole('heading', { name: 'User Management' })).toBeVisible();
  });
});
```

- [ ] **Step 2: Run the authorization specs and verify they fail**

Run from `Test/Playwright`:

```powershell
npm test -- --project=chromium tests/authorization/ownership.spec.ts tests/authorization/admin-access.spec.ts
```

Expected: FAIL because the authorization specs do not exist yet.

- [ ] **Step 3: Make the authorization specs pass**

Use the file contents from Step 1 exactly.

- [ ] **Step 4: Run the authorization specs and verify they pass**

Run from `Test/Playwright`:

```powershell
npm test -- --project=chromium tests/authorization/ownership.spec.ts tests/authorization/admin-access.spec.ts
```

Expected: PASS for regular-user ownership denials and admin-page access behavior.

- [ ] **Step 5: Commit the authorization milestone**

```powershell
git add Test/Playwright/tests/authorization/ownership.spec.ts Test/Playwright/tests/authorization/admin-access.spec.ts
git commit -m "Add Playwright authorization boundary coverage

Protect the early E2E suite against security regressions by covering private-resource ownership checks and the admin-only User Management surface for regular and admin users.

~"
```

## Task 7: Update The Human-Facing Playwright Docs

**Files:**
- Modify: `agent_docs/claude/playwright-testing.md`
- Modify: `README.md`

- [ ] **Step 1: Update the testing docs to match the new workflow**

Update `agent_docs/claude/playwright-testing.md` to document:

- `ASPNETCORE_ENVIRONMENT=Playwright`
- the local Playwright database and reset-and-seed behavior
- the canonical admin and regular test users
- `cd Test/Playwright`
- `npm install`
- `npx playwright install chromium`
- `npm test`

Update `README.md` to add a short "Playwright E2E" subsection under the existing testing commands with the same commands and the fact that the suite uses `appsettings.Playwright.json`.

- [ ] **Step 2: Verify there are no remaining starter-example references**

Run:

```powershell
rg -n "playwright.dev|demo-todo-app|example.spec" Test/Playwright README.md agent_docs/claude/playwright-testing.md
```

Expected: no matches.

- [ ] **Step 3: Commit the documentation updates**

```powershell
git add agent_docs/claude/playwright-testing.md README.md
git commit -m "Document the Playwright E2E workflow

Explain how to run the new Playwright harness, which environment it uses, and which seeded test users and reset behavior the suite relies on during local execution.

~"
```

## Final Verification

- [ ] **Step 1: Run the Milestone 0-2 suite end to end**

Run from `Test/Playwright`:

```powershell
npm test -- --project=chromium tests/smoke tests/showcases tests/items tests/authorization
```

Expected: PASS for the full Milestone 0-2 suite.

- [ ] **Step 2: Check the generated report**

Run from `Test/Playwright`:

```powershell
npm run report
```

Expected: the HTML report opens and shows green coverage for smoke, showcase, item, and authorization specs.

- [ ] **Step 3: Review the final diff**

Run from the repo root:

```powershell
git status --short
git diff -- README.md agent_docs/claude/playwright-testing.md Source/Collectibles.Web/appsettings.Playwright.json Source/Collectibles.Infrastructure/Common/ConfigureServices.cs Source/Collectibles.Infrastructure/Services/DatabaseInitializerService.cs Source/Collectibles.Infrastructure/Persistence/Seeders/PlaywrightSeedManifest.cs Source/Collectibles.Infrastructure/Persistence/Seeders/PlaywrightScenarioSeeder.cs Test/Playwright
```

Expected: only the planned Playwright harness, seed, spec, and documentation changes are present.
