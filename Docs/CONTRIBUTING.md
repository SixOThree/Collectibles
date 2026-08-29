# Contributing to Collectibles

Thank you for your interest in contributing. This document covers everything
you need to get started.

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 10.0+ |
| SQL Server | 2019+ (or SQL Server Express / LocalDB) |
| dotnet-ef (EF Core CLI) | Latest (`dotnet tool install -g dotnet-ef`) |
| Node.js | 18+ |

## Development Setup

1. **Fork and clone** the repository:

   ```bash
   git clone https://github.com/<your-username>/Collectibles.git
   cd Collectibles
   ```

2. **Copy application settings** and fill in your local values:

   Create `Source/Collectibles.Web/appsettings.Development.json` (it is git-ignored)
   and override the values you need from `appsettings.json`. At minimum set
   `ConnectionStrings:DefaultConnection` and `HashIds:Salt` — the application refuses to
   start while the salt is still the placeholder.

   Prefer `dotnet user-secrets` for anything secret: the Web project has a
   `UserSecretsId`, so `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."`
   keeps credentials out of the working tree entirely.

3. **Apply database migrations**:

   ```bash
   dotnet ef database update --project Source/Collectibles.Infrastructure \
                              --startup-project Source/Collectibles.Web
   ```

4. **Run the application**:

   ```bash
   dotnet run --project Source/Collectibles.Web
   ```

   The app will be available at:
   - HTTP:  http://localhost:5111
   - HTTPS: https://localhost:7269

## Architecture Overview

Collectibles follows Clean Architecture with CQRS (MediatR). The solution is
organized into the following projects:

| Layer | Project | Responsibility |
|-------|---------|----------------|
| Domain | `Collectibles.Domain` | Entities, value objects, domain events |
| Application | `Collectibles.Application` | Commands, queries, handlers, DTOs |
| Infrastructure | `Collectibles.Infrastructure` | EF Core, file storage, background jobs |
| Web | `Collectibles.Web` | Blazor Server UI, middleware, startup |

**Dependency flow:** Web → Application → Domain. Infrastructure implements
Application interfaces.

## Coding Conventions

- **Braces:** Always use Allman-style braces (opening brace on its own line).
- **Naming:** PascalCase for types and public members; camelCase for locals and
  parameters.
- **Primary keys:** Use `long` for all database primary keys. Never expose raw
  database IDs in URLs or API responses — use HashIds instead.
- **HashIds:** Encode/decode via `IHashIdsService` (`Collectibles.Application.Services`,
  implemented by `HashIdsService` in Infrastructure). Use `TryDecode` at HTTP boundaries;
  `Decode` throws on a malformed hash. The salt is configured in `appsettings.json` and
  must remain secret.
- **Constants:** Use `ApplicationConstants` (in `Collectibles.Domain.Constants`) rather
  than magic strings or numbers scattered through the codebase.
- **File layout:** One type per file. File name must match the type name.
- **StyleCop:** The solution uses StyleCop analyzers. Run `dotnet build` to
  surface any style violations before submitting a PR.

## Pull Request Process

1. **Branch from `main`:**

   ```bash
   git checkout main && git pull
   git checkout -b feature/your-feature-name
   ```

2. **Make small, focused commits.** Each commit should be buildable and leave
   the tests in a passing state.

3. **Ensure all tests pass** before pushing (see [Running Tests](#running-tests)).

4. **Update documentation** if your change affects setup, architecture, or
   public-facing behavior.

5. **Open a PR** against `main`. Fill in the PR template completely. Link any
   related issues with `Closes #123`.

6. A maintainer will review within a reasonable time. Please address feedback
   promptly. PRs with unresolved review comments for more than 30 days may be
   closed.

## Running Tests

**Unit and integration tests:**

```bash
dotnet test
```

**Playwright end-to-end tests (development testing only):**

> **Note:** Playwright is used in this project for two purposes: E2E testing (below) and external link caching in production. The E2E tests are completely separate from the link caching service.

E2E tests are a Node.js Playwright suite in `Test/Playwright` (not a dotnet test
project). They run the application under the `Playwright` environment, which resets a
LocalDB database on startup and uses the null email provider.

Copy `Source/Collectibles.Web/appsettings.Playwright.json.example` to
`appsettings.Playwright.json` and fill in the `PlaywrightSeed:*` accounts first; that file
is git-ignored. See `Docs/DEVELOPER_README.md#e2e-tests-playwright--development-testing`.

```bash
cd Test/Playwright
npm ci
npx playwright install
npm test
```

## Reporting Issues

Please use the GitHub issue templates:

- **Bug report** — for reproducible defects.
- **Feature request** — for ideas or enhancements.

Provide as much detail as possible, including reproduction steps, environment
information, and screenshots where applicable.

## Code of Conduct

This project adheres to the [Contributor Covenant Code of Conduct](../CODE_OF_CONDUCT.md).
By participating you agree to abide by its terms.
