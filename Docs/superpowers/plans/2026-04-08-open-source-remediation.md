# Open-Source Remediation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix all 26 findings from `Docs/open-source-audit-findings.md` so the codebase is ready for public release as a fresh GitHub repository.

**Architecture:** Tasks are grouped by dependency order: security-critical fixes first, then config hygiene, then code changes with tests, then documentation, then new community files. Each task is independently committable. File exclusions (Category 3) are not implemented here -- they're handled when creating the fresh repository by simply not copying those files.

**Tech Stack:** .NET 10, C# 14, xUnit, Markdown

---

## File Structure

| Action | File | Responsibility |
|--------|------|---------------|
| Modify | `.gitignore` | Remove Playwright exception, add `.env` exclusion |
| Modify | `Source/Collectibles.Web/appsettings.json` | Replace smtp.gmail.com with placeholder |
| Modify | `Source/Collectibles.Web/appsettings.Playwright.json` | Sanitize connection string |
| Modify | `Source/Collectibles.Web/web.config` | Change default environment to Production |
| Modify | `Source/Collectibles.Infrastructure/Services/HashIdsService.cs` | Remove fallback salt, add startup validation |
| Create | `Test/Collectibles.Application.Tests/Services/HashIdsServiceTests.cs` | Test HashIdsService validation |
| Modify | `README.md` | Quick Start reformat, prerequisites, security warning, fix link |
| Modify | `Docs/DEVELOPER_README.md` | Fix EF Core version, fix Playwright instructions |
| Modify | `Docs/LargeFileUploads.md` | Reformat as proper markdown |
| Modify | `Docs/Playwright in production.md` | Reformat as proper markdown |
| Create | `CONTRIBUTING.md` | Contribution guidelines |
| Create | `CODE_OF_CONDUCT.md` | Contributor Covenant v2.1 |
| Create | `SECURITY.md` | Vulnerability disclosure process |
| Create | `THIRD-PARTY-NOTICES` | Hangfire LGPL + SixLabors license notices |
| Create | `.github/ISSUE_TEMPLATE/bug_report.md` | Bug report template |
| Create | `.github/ISSUE_TEMPLATE/feature_request.md` | Feature request template |
| Create | `.github/PULL_REQUEST_TEMPLATE.md` | PR checklist template |

---

### Task 1: Fix CRITICAL -- Sanitize Playwright Config and .gitignore

**Findings addressed:** CRITICAL (appsettings.Playwright.json credentials), CRITICAL (.gitignore exception), LOW (.env exclusion)

**Files:**
- Modify: `Source/Collectibles.Web/appsettings.Playwright.json`
- Modify: `.gitignore:371-374`

- [ ] **Step 1: Sanitize the Playwright config connection string**

Replace the real credentials in `Source/Collectibles.Web/appsettings.Playwright.json` line 3. Change:

```json
"DefaultConnection": "Server=NucOne;Database=Collectibles_Playwright;User Id=sqluser;Password=Dynamic123;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

To:

```json
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=Collectibles_Playwright;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

- [ ] **Step 2: Fix .gitignore -- remove Playwright exception, add .env exclusion**

In `.gitignore`, replace lines 371-374:

```gitignore
# Application settings with sensitive data
appsettings.Development.json
appsettings.*.json
!/Source/Collectibles.Web/appsettings.Playwright.json
```

With:

```gitignore
# Application settings with sensitive data
appsettings.Development.json
appsettings.*.json

# Environment files
.env
.env.*
```

- [ ] **Step 3: Verify changes**

Run:
```bash
git diff
```

Confirm:
- `appsettings.Playwright.json` no longer contains `NucOne`, `sqluser`, or `Dynamic123`
- `.gitignore` no longer has the `!/Source/Collectibles.Web/appsettings.Playwright.json` exception
- `.gitignore` now includes `.env` and `.env.*`

- [ ] **Step 4: Commit**

```bash
git add Source/Collectibles.Web/appsettings.Playwright.json .gitignore
git commit -m "fix: sanitize Playwright config and remove .gitignore exception

Replace real database credentials with localdb placeholder.
Remove the .gitignore exception that forced the Playwright config
to be tracked. Add .env exclusion as a precaution."
```

---

### Task 2: Fix HIGH -- web.config Default Environment

**Findings addressed:** HIGH (web.config Development default), MEDIUM (MiniProfiler exposure -- resolved by this fix)

**Files:**
- Modify: `Source/Collectibles.Web/web.config:29`

- [ ] **Step 1: Change the default environment to Production**

In `Source/Collectibles.Web/web.config` line 29, change:

```xml
<environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Development" />
```

To:

```xml
<environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
```

- [ ] **Step 2: Verify the transform files are consistent**

Read `Source/Collectibles.Web/web.Production.config` and `Source/Collectibles.Web/web.Release.config`. Both should already set `Production`. This change makes the base file match, so transforms are now no-ops for this setting (which is correct -- safe by default).

- [ ] **Step 3: Commit**

```bash
git add Source/Collectibles.Web/web.config
git commit -m "fix: change web.config default environment to Production

The base web.config previously defaulted to Development, which could
expose MiniProfiler and detailed error pages if deployed without
config transforms. Now defaults to Production (safe by default)."
```

---

### Task 3: Fix MEDIUM -- HashIdsService Fallback Salt

**Findings addressed:** MEDIUM (HashIdsService fallback salt), MEDIUM (appsettings.json placeholder with no runtime guard)

**Files:**
- Modify: `Source/Collectibles.Infrastructure/Services/HashIdsService.cs:11-17`
- Create: `Test/Collectibles.Application.Tests/Services/HashIdsServiceTests.cs`

- [ ] **Step 1: Write failing tests**

Create `Test/Collectibles.Application.Tests/Services/HashIdsServiceTests.cs`:

```csharp
using Collectibles.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace Collectibles.Application.Tests.Services;

public class HashIdsServiceTests
{
    [Fact]
    public void Constructor_WithValidSalt_CreatesService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HashIds:Salt"] = "my-unique-salt-value",
            })
            .Build();

        var service = new HashIdsService(config);

        var encoded = service.Encode(42);
        Assert.NotNull(encoded);
        Assert.NotEmpty(encoded);
    }

    [Fact]
    public void Constructor_WithMissingSalt_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() => new HashIdsService(config));
        Assert.Contains("HashIds:Salt", ex.Message);
    }

    [Fact]
    public void Constructor_WithPlaceholderSalt_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HashIds:Salt"] = "YOUR_UNIQUE_SALT_HERE",
            })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() => new HashIdsService(config));
        Assert.Contains("placeholder", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WithOldFallbackSalt_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HashIds:Salt"] = "collectibles-default-salt-change-in-production",
            })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() => new HashIdsService(config));
        Assert.Contains("placeholder", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test Test/Collectibles.Application.Tests --filter "FullyQualifiedName~HashIdsServiceTests" -v minimal
```

Expected: 3 of 4 tests fail (the valid-salt test passes, the three validation tests fail because the constructor doesn't throw yet).

- [ ] **Step 3: Implement the validation**

Replace the constructor in `Source/Collectibles.Infrastructure/Services/HashIdsService.cs` (lines 11-17):

```csharp
    public HashIdsService(IConfiguration configuration)
    {
        var salt = configuration["HashIds:Salt"];

        if (string.IsNullOrWhiteSpace(salt))
        {
            throw new InvalidOperationException(
                "HashIds:Salt configuration is required. " +
                "Set a unique salt value in appsettings.json or user secrets.");
        }

        string[] placeholderValues =
        [
            "YOUR_UNIQUE_SALT_HERE",
            "collectibles-default-salt-change-in-production",
        ];

        if (placeholderValues.Contains(salt, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "HashIds:Salt is still set to a placeholder value. " +
                "Replace it with a unique, secret string in appsettings.json or user secrets.");
        }

        var minHashLength = configuration.GetValue<int?>("HashIds:MinHashLength") ?? 8;
        var alphabet = configuration["HashIds:Alphabet"] ?? "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";

        _hashids = new Hashids(salt, minHashLength, alphabet);
    }
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test Test/Collectibles.Application.Tests --filter "FullyQualifiedName~HashIdsServiceTests" -v minimal
```

Expected: All 4 tests pass.

- [ ] **Step 5: Run full test suite to check for regressions**

```bash
dotnet test
```

Expected: All tests pass. If any existing tests fail because they use `HashIdsService` without providing a salt, update those tests to provide a valid salt in their test configuration.

- [ ] **Step 6: Commit**

```bash
git add Source/Collectibles.Infrastructure/Services/HashIdsService.cs Test/Collectibles.Application.Tests/Services/HashIdsServiceTests.cs
git commit -m "fix: remove HashIdsService fallback salt, add startup validation

The service now throws InvalidOperationException if the salt is
missing or set to a known placeholder value, preventing deployments
with trivially reversible HashIds."
```

---

### Task 4: Fix MEDIUM -- Sanitize appsettings.json SMTP Host

**Findings addressed:** MEDIUM (hardcoded smtp.gmail.com)

**Files:**
- Modify: `Source/Collectibles.Web/appsettings.json:61`

- [ ] **Step 1: Replace the SMTP host**

In `Source/Collectibles.Web/appsettings.json` line 61, change:

```json
"Host": "smtp.gmail.com",
```

To:

```json
"Host": "",
```

- [ ] **Step 2: Commit**

```bash
git add Source/Collectibles.Web/appsettings.json
git commit -m "fix: remove opinionated smtp.gmail.com default from appsettings

Replace with empty string so operators must explicitly configure
their SMTP provider."
```

---

### Task 5: Fix README -- Quick Start, Prerequisites, Security Warning, Broken Link

**Findings addressed:** MEDIUM (Quick Start formatting), MEDIUM (missing dotnet-ef prerequisite), MEDIUM (test credentials need security warning), LOW (broken #first-run-setup link)

**Files:**
- Modify: `README.md:63-68, 69-131, 180-189, 136`

- [ ] **Step 1: Rewrite Prerequisites section to include dotnet-ef**

In `README.md`, replace lines 63-67:

```markdown
### Prerequisites

- .NET 10 SDK
- SQL Server (LocalDB, Express, or full instance)
- Node.js 18+ (for Playwright E2E tests)
```

With:

```markdown
### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, Express, or full instance)
- [EF Core CLI tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`
- Node.js 18+ (for Playwright E2E tests only)
```

- [ ] **Step 2: Rewrite Quick Start as numbered code blocks**

Replace lines 69-131 (from `### Quick Start` through the `dotnet run` command) with:

```markdown
### Quick Start

1. **Clone and restore**

   ```bash
   git clone https://github.com/SixOThree/Collectibles.git
   cd Collectibles
   dotnet restore
   ```

2. **Configure the database connection**

   Copy the example connection string into a local settings file (git-ignored):

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

   On first launch with no admin accounts, the app generates a setup token saved to `App_Data/setup-token.txt`. Navigate to `/Setup` and enter the token to create the first administrator account. See [Features > First-Run Setup](Docs/Features.md#first-run-setup) for details.
```

- [ ] **Step 3: Remove the old Configuration section**

The old inline Configuration section (lines 76-124 in the original with JSON examples for Database, Storage, Email, HashIds) is now redundant with the Quick Start rewrite and the existing [Configuration Reference](Docs/Configuration.md) link in Further Documentation. Remove it. Keep the `### Playwright Browsers` section that follows.

- [ ] **Step 4: Fix the broken #first-run-setup anchor link**

If line 136 still contains the old broken link, it should have been removed by Step 2's rewrite (which includes a working link to `Docs/Features.md#first-run-setup`). Verify the old `[First-Run Setup](#first-run-setup)` link no longer exists.

- [ ] **Step 5: Add security warning to test credentials sections**

After the Playwright seeded users (around line 180-182), add a warning:

```markdown
Seeded Playwright users:
- `test.admin@collectibles.local` / `xA&%4hTVhTDixSOO` (Administrator)
- `test.user@collectibles.local` / `xA&%4hTVhTDixSOO` (regular user)

> **Warning:** These test credentials are publicly visible in the source code. The test user creation feature is compiled out of Release builds. Never run a DEBUG build against a production database -- the test accounts would be created with a password that anyone can read in this repository.
```

Add the same warning after the Debug mode test user section (around line 184-188):

```markdown
### Test User (Debug Mode Only)

When running in DEBUG with the Development environment, you can still create manual test users from the user management page:
- **Admin:** `test.admin@collectibles.local` / `xA&%4hTVhTDixSOO`
- **Regular:** `test.user@collectibles.local` / `xA&%4hTVhTDixSOO`

> **Warning:** These credentials are publicly known. Test accounts must never exist in a production database.
```

- [ ] **Step 6: Verify README renders correctly**

Skim the full README to ensure the new sections flow logically and no orphaned content remains from the old Quick Start.

- [ ] **Step 7: Commit**

```bash
git add README.md
git commit -m "docs: rewrite Quick Start, add prerequisites, add security warnings

- Reformat Quick Start as numbered steps with code blocks
- Add dotnet-ef tool to prerequisites
- Add security warnings to test credential sections
- Fix broken #first-run-setup anchor link"
```

---

### Task 6: Fix DEVELOPER_README.md -- EF Core Version and Playwright Instructions

**Findings addressed:** MEDIUM (wrong EF Core version), LOW (outdated Playwright instructions)

**Files:**
- Modify: `Docs/DEVELOPER_README.md:115, 168-174`

- [ ] **Step 1: Fix EF Core version**

In `Docs/DEVELOPER_README.md` line 115, change:

```markdown
- Version: 8.0
```

To:

```markdown
- Version: 10.0
```

- [ ] **Step 2: Fix Playwright instructions**

Replace lines 168-178 (the section about starting the app manually):

```markdown
If your tests need the local app, start the site in a separate terminal first because the Playwright config does not currently launch the web server automatically:

```bash
dotnet run --project Source/Collectibles.Web
```

Then run the E2E tests from `Test/Playwright/`:

```bash
# Run the full suite
npx playwright test
```
```

With:

```markdown
The Playwright test suite is self-contained. It starts the application automatically with `ASPNETCORE_ENVIRONMENT=Playwright`, loads configuration from `appsettings.Playwright.json`, and resets/reseeds its own dedicated database on each run.

Run the E2E tests from `Test/Playwright/`:

```bash
# Run the full suite
npx playwright test
```
```

- [ ] **Step 3: Commit**

```bash
git add Docs/DEVELOPER_README.md
git commit -m "docs: fix EF Core version and Playwright instructions in DEVELOPER_README

Update EF Core version from 8 to 10. Replace outdated manual-start
instructions with accurate description of the self-contained
Playwright test environment."
```

---

### Task 7: Reformat Raw Documentation Files

**Findings addressed:** LOW (LargeFileUploads.md raw notes), LOW (Playwright in production.md raw notes)

**Files:**
- Modify: `Docs/LargeFileUploads.md`
- Modify: `Docs/Playwright in production.md`

- [ ] **Step 1: Reformat LargeFileUploads.md**

Read the full file and rewrite it as proper markdown. Replace the ASCII box-drawing table with a markdown pipe table. Add a top-level heading. Keep all content accurate. The reformatted file should start:

```markdown
# Large File Upload Feature

The application implements a dual-mode upload system designed to handle files of any size.

## Upload Modes

| File Size | Upload Method |
|-----------|--------------|
| Under 50 MB | Standard upload |
| 50 MB - 2 GB | Direct Azure Blob upload via SAS tokens |
| Over 2 GB | Chunked upload (10 MB chunks) |

## Key Components

### 1. Chunked Upload Pipeline
```

Continue reformatting the rest of the file, converting all ASCII art to markdown tables, adding proper headings, and ensuring code references are in backticks.

- [ ] **Step 2: Reformat Playwright in production.md**

Read the full file and rewrite it as proper markdown. Add a top-level heading, fenced code blocks for all PowerShell/bash commands, and proper section headings:

```markdown
# Playwright Browser Setup for Production (IIS)

When deploying with IIS, Playwright browsers must be installed in a location accessible to the IIS application pool's service account.

## Recommended: Explicit Browser Path

The most reliable approach is to set the `PLAYWRIGHT_BROWSERS_PATH` environment variable:

1. In IIS Manager, select your site > Configuration Editor
2. Navigate to `system.webServer/aspNetCore` > `environmentVariables`
3. Add a new environment variable:
   - **Name:** `PLAYWRIGHT_BROWSERS_PATH`
   - **Value:** `C:\ProgramData\playwright-browsers`
4. Install browsers to that location:

   ```powershell
   $env:PLAYWRIGHT_BROWSERS_PATH = "C:\ProgramData\playwright-browsers"
   cd "C:\path\to\your\published\app"
   .\playwright.ps1 install chromium
   ```

5. Restart your application pool
```

Continue reformatting the rest of the file similarly.

- [ ] **Step 3: Commit**

```bash
git add "Docs/LargeFileUploads.md" "Docs/Playwright in production.md"
git commit -m "docs: reformat LargeFileUploads and Playwright production guides

Convert ASCII art tables to markdown, add headings, add code fences.
Content unchanged, formatting only."
```

---

### Task 8: Create THIRD-PARTY-NOTICES

**Findings addressed:** MEDIUM (Hangfire LGPL documentation), MEDIUM (SixLabors dual-license)

**Files:**
- Create: `THIRD-PARTY-NOTICES`

- [ ] **Step 1: Create the notices file**

Create `THIRD-PARTY-NOTICES` at the repository root:

```
THIRD-PARTY SOFTWARE NOTICES AND INFORMATION

This project incorporates components from the projects listed below.
The original copyright notices and the licenses under which Collectibles
received such components are set forth below.

===========================================================================

1. Hangfire (https://www.hangfire.io/)

   Copyright (c) 2013-2024 Hangfire OU
   License: LGPL v3 (GNU Lesser General Public License v3.0)
   https://github.com/HangfireIO/Hangfire/blob/main/LICENSE.md

   This project uses Hangfire Community Edition as a NuGet package
   dependency. Under LGPL v3, you may use this library without restriction
   as long as you do not modify and redistribute the Hangfire source code
   itself. If you modify the Hangfire source and redistribute it, you must
   comply with the full LGPL v3 terms.

   A commercial license is also available from Hangfire OU for users who
   prefer not to be bound by LGPL terms.

===========================================================================

2. SixLabors.ImageSharp (https://sixlabors.com/)

   Copyright (c) Six Labors
   License: Six Labors Split License v1.0
   https://github.com/SixLabors/ImageSharp/blob/main/LICENSE

   SixLabors.ImageSharp and SixLabors.ImageSharp.Drawing use the
   Six Labors Split License:

   - Open-source projects (like this one): Apache License 2.0
   - Commercial/proprietary use: Requires a paid commercial license

   If you fork this project for commercial use, you must obtain a
   SixLabors commercial license. See https://sixlabors.com/pricing/
   for details.

===========================================================================
```

- [ ] **Step 2: Commit**

```bash
git add THIRD-PARTY-NOTICES
git commit -m "docs: add THIRD-PARTY-NOTICES for Hangfire LGPL and SixLabors license

Document Hangfire's LGPL v3 terms and SixLabors' split license model
for downstream users and commercial forkers."
```

---

### Task 9: Create CONTRIBUTING.md

**Findings addressed:** MEDIUM (missing CONTRIBUTING.md)

**Files:**
- Create: `CONTRIBUTING.md`

- [ ] **Step 1: Create the file**

Create `CONTRIBUTING.md` at the repository root:

```markdown
# Contributing to Collectibles

Thank you for your interest in contributing! This guide will help you get started.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, Express, or full instance)
- [EF Core CLI tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`
- Node.js 18+ (for Playwright E2E tests)

## Development Setup

1. Fork and clone the repository
2. Copy `Source/Collectibles.Web/appsettings.json` to `Source/Collectibles.Web/appsettings.Development.json`
3. Configure your database connection string in `appsettings.Development.json`
4. Apply migrations: `dotnet ef database update -p Source/Collectibles.Infrastructure -s Source/Collectibles.Web`
5. Run the app: `dotnet run --project Source/Collectibles.Web`
6. Run tests: `dotnet test`

## Architecture

This project follows **Clean Architecture** with CQRS:

- `Collectibles.Domain` -- Entities, enums, value objects (no external dependencies)
- `Collectibles.Application` -- Commands, queries, validators, DTOs, service interfaces
- `Collectibles.Infrastructure` -- EF Core, repositories, external service implementations
- `Collectibles.Kernel` -- Cross-cutting constants and utilities
- `Collectibles.Web` -- Blazor Server UI, API endpoints, authorization

Dependencies flow inward: Web -> Application -> Domain. Infrastructure implements Application interfaces.

## Coding Conventions

- Always use braces for control structures
- PascalCase for public members, `_camelCase` for private fields
- Use `long` for entity primary keys (never `int`)
- Never expose database IDs externally -- use HashIds at all boundaries
- Constants belong in `ApplicationConstants`, no magic numbers
- One type per file, filename matches the type name
- Follow existing patterns in the codebase

## Pull Request Process

1. Create a feature branch from `main`
2. Make your changes in small, focused commits
3. Ensure all tests pass: `dotnet test`
4. Update documentation if your change affects user-facing behavior
5. Open a pull request with a clear description of what and why

## Running Tests

```bash
# Unit and integration tests
dotnet test

# Playwright E2E tests (requires Node.js)
cd Test/Playwright
npm install
npx playwright install chromium
npm test
```

## Reporting Issues

- Use the [bug report template](.github/ISSUE_TEMPLATE/bug_report.md) for bugs
- Use the [feature request template](.github/ISSUE_TEMPLATE/feature_request.md) for ideas
- Check existing issues before creating a new one

## Code of Conduct

This project follows the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.
```

- [ ] **Step 2: Commit**

```bash
git add CONTRIBUTING.md
git commit -m "docs: add CONTRIBUTING.md with dev setup and PR guidelines"
```

---

### Task 10: Create CODE_OF_CONDUCT.md

**Findings addressed:** MEDIUM (missing CODE_OF_CONDUCT.md)

**Files:**
- Create: `CODE_OF_CONDUCT.md`

- [ ] **Step 1: Create the file**

Create `CODE_OF_CONDUCT.md` using Contributor Covenant v2.1. The full text is available at https://www.contributor-covenant.org/version/2/1/code_of_conduct/

Use the standard template with these placeholders filled in:
- Contact method: GitHub Issues (or a dedicated email if the maintainer provides one)

```markdown
# Contributor Covenant Code of Conduct

## Our Pledge

We as members, contributors, and leaders pledge to make participation in our
community a harassment-free experience for everyone, regardless of age, body
size, visible or invisible disability, ethnicity, sex characteristics, gender
identity and expression, level of experience, education, socio-economic status,
nationality, personal appearance, race, caste, color, religion, or sexual
identity and orientation.

We pledge to act and interact in ways that contribute to an open, welcoming,
diverse, inclusive, and healthy community.

## Our Standards

Examples of behavior that contributes to a positive environment for our
community include:

* Demonstrating empathy and kindness toward other people
* Being respectful of differing opinions, viewpoints, and experiences
* Giving and gracefully accepting constructive feedback
* Accepting responsibility and apologizing to those affected by our mistakes,
  and learning from the experience
* Focusing on what is best not just for us as individuals, but for the overall
  community

Examples of unacceptable behavior include:

* The use of sexualized language or imagery, and sexual attention or advances of
  any kind
* Trolling, insulting or derogatory comments, and personal or political attacks
* Public or private harassment
* Publishing others' private information, such as a physical or email address,
  without their explicit permission
* Other conduct which could reasonably be considered inappropriate in a
  professional setting

## Enforcement Responsibilities

Community leaders are responsible for clarifying and enforcing our standards of
acceptable behavior and will take appropriate and fair corrective action in
response to any behavior that they deem inappropriate, threatening, offensive,
or harmful.

## Scope

This Code of Conduct applies within all community spaces, and also applies when
an individual is officially representing the community in public spaces.

## Enforcement

Instances of abusive, harassing, or otherwise unacceptable behavior may be
reported to the project maintainers via [GitHub Issues](https://github.com/SixOThree/Collectibles/issues).

All complaints will be reviewed and investigated promptly and fairly.

## Attribution

This Code of Conduct is adapted from the [Contributor Covenant](https://www.contributor-covenant.org/),
version 2.1, available at
[https://www.contributor-covenant.org/version/2/1/code_of_conduct.html](https://www.contributor-covenant.org/version/2/1/code_of_conduct.html).
```

- [ ] **Step 2: Commit**

```bash
git add CODE_OF_CONDUCT.md
git commit -m "docs: add Contributor Covenant Code of Conduct v2.1"
```

---

### Task 11: Create SECURITY.md

**Findings addressed:** MEDIUM (missing SECURITY.md)

**Files:**
- Create: `SECURITY.md`

- [ ] **Step 1: Create the file**

Create `SECURITY.md` at the repository root:

```markdown
# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| Latest  | Yes       |

## Reporting a Vulnerability

If you discover a security vulnerability, please report it responsibly. **Do not open a public GitHub issue.**

### How to Report

1. Use [GitHub's private vulnerability reporting](https://github.com/SixOThree/Collectibles/security/advisories/new) to submit a report
2. Include a description of the vulnerability, steps to reproduce, and potential impact

### What to Expect

- **Acknowledgment** within 48 hours of your report
- **Status update** within 7 days with an assessment and timeline
- **Credit** in the fix announcement (unless you prefer to remain anonymous)

### Scope

This policy covers the Collectibles application code, its dependencies, and its default configuration. It does not cover vulnerabilities in third-party services (Azure, SQL Server, etc.) -- please report those to the respective vendors.

## Security Best Practices for Deployers

- Never use the default `HashIds:Salt` placeholder -- generate a unique value
- Always deploy with `ASPNETCORE_ENVIRONMENT=Production`
- Keep the .NET runtime and all NuGet packages up to date
- Review `Docs/Configuration.md` for all security-relevant settings
```

- [ ] **Step 2: Commit**

```bash
git add SECURITY.md
git commit -m "docs: add SECURITY.md with vulnerability disclosure process"
```

---

### Task 12: Create GitHub Issue and PR Templates

**Findings addressed:** LOW (missing GitHub templates)

**Files:**
- Create: `.github/ISSUE_TEMPLATE/bug_report.md`
- Create: `.github/ISSUE_TEMPLATE/feature_request.md`
- Create: `.github/PULL_REQUEST_TEMPLATE.md`

- [ ] **Step 1: Create bug report template**

Create `.github/ISSUE_TEMPLATE/bug_report.md`:

```markdown
---
name: Bug Report
about: Report a bug to help us improve
title: ''
labels: bug
assignees: ''
---

## Description

A clear and concise description of what the bug is.

## Steps to Reproduce

1. Go to '...'
2. Click on '...'
3. Scroll down to '...'
4. See error

## Expected Behavior

What you expected to happen.

## Actual Behavior

What actually happened.

## Environment

- OS: [e.g., Windows 11, Ubuntu 24.04]
- Browser: [e.g., Chrome 120, Firefox 121]
- .NET Version: [e.g., 10.0]
- Database: [e.g., SQL Server 2022, LocalDB]

## Additional Context

Add any other context, screenshots, or log output about the problem here.
```

- [ ] **Step 2: Create feature request template**

Create `.github/ISSUE_TEMPLATE/feature_request.md`:

```markdown
---
name: Feature Request
about: Suggest an idea for this project
title: ''
labels: enhancement
assignees: ''
---

## Problem Statement

A clear description of the problem this feature would solve. Ex: "I'm always frustrated when..."

## Proposed Solution

Describe what you'd like to happen.

## Alternatives Considered

Describe any alternative solutions or features you've considered.

## Additional Context

Add any other context, mockups, or examples about the feature request here.
```

- [ ] **Step 3: Create PR template**

Create `.github/PULL_REQUEST_TEMPLATE.md`:

```markdown
## Description

Brief description of what this PR does and why.

## Type of Change

- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update
- [ ] Refactoring

## Testing

- [ ] All existing tests pass (`dotnet test`)
- [ ] New tests added for new functionality
- [ ] Manual testing performed

## Checklist

- [ ] Code follows existing project conventions
- [ ] No database IDs exposed (HashIds used at boundaries)
- [ ] No magic numbers (constants in `ApplicationConstants`)
- [ ] Documentation updated if user-facing behavior changed
```

- [ ] **Step 4: Commit**

```bash
git add .github/
git commit -m "docs: add GitHub issue and PR templates"
```
