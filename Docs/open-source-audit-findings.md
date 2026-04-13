# Open-Source Readiness Audit Findings

**Date:** 2026-04-08
**Spec:** Docs/superpowers/specs/2026-04-08-open-source-audit-design.md

## Summary

| Severity | Count |
|----------|-------|
| CRITICAL | 2 |
| HIGH | 3 |
| MEDIUM | 14 |
| LOW | 7 |
| **Total** | **26** |

### Files to Exclude from Open-Source Release

1. `azure-pipelines.yml`
2. `Docs/superpowers/`
3. `Source/Collectibles.Web/appsettings.Playwright.json`
4. `Test/Example Data/Showcase Example Images/`
5. `Workspace/`
6. `agents.md`
7. `agent_docs/`
8. `Documentation/`
9. `CLAUDE.md` (or replace with minimal version)

### Files to Modify

1. `.gitignore` -- remove Playwright exception (line 374), add `.env` / `.env.*` exclusion
2. `Source/Collectibles.Web/appsettings.json` -- replace `smtp.gmail.com` with `smtp.example.com`
3. `Source/Collectibles.Web/appsettings.Playwright.json` -- sanitize connection string (or exclude entirely)
4. `Source/Collectibles.Web/web.config` -- change `ASPNETCORE_ENVIRONMENT` default from `Development` to `Production`
5. `Source/Collectibles.Infrastructure/Services/HashIdsService.cs` -- remove fallback salt, throw on missing/placeholder value
6. `README.md` -- reformat Quick Start as numbered code blocks, add `dotnet-ef` prerequisite, add security warning to test credentials, fix broken `#first-run-setup` anchor link
7. `Docs/DEVELOPER_README.md` -- update EF Core version to 10, fix Playwright test instructions
8. `Docs/LargeFileUploads.md` -- reformat as proper markdown
9. `Docs/Playwright in production.md` -- reformat or remove

### Files to Create

1. `CONTRIBUTING.md` -- contribution guidelines, dev setup, PR process
2. `CODE_OF_CONDUCT.md` -- Contributor Covenant v2.1
3. `SECURITY.md` -- vulnerability disclosure process
4. `THIRD-PARTY-NOTICES` -- Hangfire LGPL v3 and SixLabors Split License disclosures
5. `.github/ISSUE_TEMPLATE/bug_report.md` -- structured bug report template
6. `.github/ISSUE_TEMPLATE/feature_request.md` -- structured feature request template
7. `.github/PULL_REQUEST_TEMPLATE.md` -- PR checklist template

---

## Findings

### Category 1: Secrets & Credentials

**Scan methodology:** gitleaks 8.30.1 `detect --source . --no-git` (no findings); manual regex scans for connection strings, password assignments, API keys, and secret patterns; manual file review of known sensitive files.

#### [CRITICAL] Real database credentials in Playwright config
- **Category**: Secrets & Credentials
- **Location**: `Source/Collectibles.Web/appsettings.Playwright.json:3`
- **Issue**: Connection string contains a real SQL Server hostname (`Server=NucOne`), a real SQL login (`User Id=sqluser`), and a real password (`Password=Dynamic123`). This file is committed to the repository and would be public after the open-source release.
- **Remediation**: Replace the connection string with a placeholder using `(localdb)\MSSQLLocalDB` or `localhost` with generic credentials, matching the pattern used in `Docs/Configuration.md`. Developers should configure their own connection string via `appsettings.Development.json` (which is git-ignored) or user secrets.

#### [HIGH] Hardcoded test password in production code path
- **Category**: Secrets & Credentials
- **Location**: `Source/Collectibles.Web/Components/Pages/UsersList.razor:528,597`
- **Issue**: The test password `xA&%4hTVhTDixSOO` is hardcoded in two `CreateTestAdminUser` and `CreateTestRegularUser` methods inside a `#if DEBUG` block. The same password appears in `Source/Collectibles.Infrastructure/Persistence/Seeders/PlaywrightScenarioSeeder.cs:13`, `agent_docs/claude/playwright-testing.md`, and `README.md`. While only active in DEBUG builds, the password is publicly visible in source code and documentation. If a developer inadvertently runs a DEBUG build against a production database, those test accounts would be created with a publicly known password.
- **Remediation**: Accept the risk as documented (owner decision). Optionally, read the password from configuration (`IConfiguration`) rather than hardcoding it, so operators can override it per environment.

#### [MEDIUM] Hardcoded fallback HashIds salt in source code
- **Category**: Secrets & Credentials
- **Location**: `Source/Collectibles.Infrastructure/Services/HashIdsService.cs:13`
- **Issue**: The `HashIdsService` constructor falls back to the literal string `"collectibles-default-salt-change-in-production"` when no `HashIds:Salt` config value is provided. Any deployment that omits this setting (or uses the `appsettings.json` placeholder `YOUR_UNIQUE_SALT_HERE`) will use a well-known default salt, making HashIds trivially reversible.
- **Remediation**: Remove the fallback default entirely and throw an `InvalidOperationException` if the salt is missing or is the placeholder value, matching the behaviour of the `DefaultConnection` check in `ServiceCollectionExtensions.cs`. Document the required configuration in `Docs/Configuration.md`.

#### [MEDIUM] Hardcoded example SMTP host in appsettings.json
- **Category**: Secrets & Credentials
- **Location**: `Source/Collectibles.Web/appsettings.json:61`
- **Issue**: `appsettings.json` ships with `"Host": "smtp.gmail.com"` as the SMTP host. While no credentials are present, this is an opinionated default that implies Gmail is the expected provider and may mislead operators who copy the config without reading it.
- **Remediation**: Replace the hardcoded host with an empty string or a comment-style placeholder such as `"smtp.example.com"` to make it clear this must be configured.

### Category 2: Configuration Hygiene

**Scan methodology:** Manual review of `appsettings.json`, `appsettings.Playwright.json`, `launchSettings.json`, `web.config`, `web.Production.config`, `web.Release.config`, and `.gitignore`.

#### [CRITICAL] .gitignore exception exposes sensitive config file
- **Category**: Configuration Hygiene
- **Location**: `.gitignore:374`
- **Issue**: The line `!/Source/Collectibles.Web/appsettings.Playwright.json` negates the `appsettings.*.json` wildcard ignore on line 373, causing the Playwright config file (which contains real server credentials -- see Category 1) to be committed to the repository. Any developer who clones the repo receives the real credentials.
- **Remediation**: Remove line 374. The Playwright config should be git-ignored like all other environment-specific appsettings files. Developers running Playwright tests locally should provide their own connection string via `appsettings.Development.json` or an environment variable, and a sanitized `appsettings.Playwright.json.example` file should be added to the repo as a reference.

#### [HIGH] Base web.config hardcodes `Development` environment
- **Category**: Configuration Hygiene
- **Location**: `Source/Collectibles.Web/web.config:29`
- **Issue**: The committed `web.config` sets `ASPNETCORE_ENVIRONMENT` to `Development` via an IIS environment variable. Any deployment that uses the base `web.config` without applying a transform (e.g., a manual IIS deploy, a CI/CD pipeline that skips config transforms, or a developer who copies the file) will run in Development mode on a production server. Development mode enables detailed exception pages, relaxed security settings, and additional diagnostic output.
- **Remediation**: Change the default value to `Production` in the base `web.config`. The `web.Release.config` and `web.Production.config` transforms already set `Production` correctly, so this change makes the base file safe-by-default and the transforms become no-ops for that setting. Alternatively, remove the environment variable from the base config entirely and rely solely on the transforms or server-level environment variable configuration.

#### [MEDIUM] `appsettings.json` ships with opinionated `HashIds.Salt` placeholder
- **Category**: Configuration Hygiene
- **Location**: `Source/Collectibles.Web/appsettings.json:36`
- **Issue**: The `HashIds:Salt` field contains `"YOUR_UNIQUE_SALT_HERE"`. While clearly a placeholder, there is no runtime guard preventing the application from starting with this value. A developer who skips this step will silently use a well-known salt (see also Category 1, MEDIUM finding on the fallback default in `HashIdsService.cs`). The configuration file itself does not warn operators of the consequences.
- **Remediation**: Add a comment in `Docs/Configuration.md` clearly marking this as a required field. The companion code-level fix (throwing if the salt is the placeholder) is tracked in Category 1. Both fixes together close the gap.

#### [LOW] `.gitignore` does not exclude `.env` files
- **Category**: Configuration Hygiene
- **Location**: `.gitignore` (no existing entry)
- **Issue**: The `.gitignore` has no rule for `.env` or `.env.*` files. While this project does not currently use `.env` files, contributors familiar with other ecosystems may create one to hold local environment variables, and it would be committed accidentally. The standard .NET `.gitignore` template also does not include this, but it is a commonly recommended addition.
- **Remediation**: Add `.env` and `.env.*` to `.gitignore` as a precautionary measure.

### Category 3: File Exclusions

**Scan methodology:** Top-level directory listing; `find` scans of `Docs/`, `Scripts/`, and root; grep scan for private identifiers (`ReadyOkRetro`, `NucOne`, `readyokretro`) across all committed text files; manual review of each candidate.

#### [HIGH] Files and directories to exclude from open-source release
- **Category**: File Exclusions
- **Location**: Repository root and subdirectories
- **Issue**: The following files and directories contain private deployment information, internal planning artifacts, real credentials, or sample data that is inappropriate to include in a public open-source repository.
- **Remediation**: Do not copy these into the fresh repository. Where a sanitized or generic equivalent is useful, create a replacement (e.g., `azure-pipelines-example.yml` already exists as the intended public reference for the pipeline file).

**Exclusion list:**

1. `azure-pipelines.yml` -- Private deployment pipeline. Contains the internal pool name `ReadyOkDevAgentPool`, IIS path `C:\inetpub\ReadyOkRetro`, app pool name `ReadyOkRetro`, and warmup URL `https://readyokretro.com`. The generic equivalent `azure-pipelines-example.yml` should be kept.

2. `Docs/superpowers/` -- Internal AI-assisted planning artifacts (specs and implementation plans). These reference internal test credentials (e.g., `Server=NucOne`, `Password=Dynamic123`) and are not meaningful documentation for external contributors. The `plans/` and `specs/` subdirectories under this path should be excluded in their entirety.

3. `Source/Collectibles.Web/appsettings.Playwright.json` -- Contains real SQL Server credentials (`Server=NucOne;User Id=sqluser;Password=Dynamic123`). This file must be excluded or replaced with a sanitized placeholder version. (See also Category 1 CRITICAL finding and Category 2 CRITICAL finding on the `.gitignore` exception that causes this file to be committed.)

4. `Test/Example Data/Showcase Example Images/` -- Sample image collection used for local development seeding. Contains ~88 MB of images plus a bulk zip upload (`ShowcaseScreenshotsBulkZipUpload.zip`, ~85 MB). This is local seed data, not application source code, and is not appropriate to ship in a public repository.

5. `Workspace/` -- Internal scratchpad directory containing AI-generated migration plans and security task tracking (`AUTOMAPPER_MIGRATION_PLAN.md`, `MAUI_BLAZOR_HYBRID_MIGRATION_CHECKLIST_Claude.md`, `SECURITY_TASKS.md`, etc.). These are internal working documents not suitable for a public release.

6. `agents.md` -- Internal AI agent commit policy file. References the private branch name `ReadyOk` as a protected branch and contains internal workflow conventions intended for private use. Not relevant to external contributors.

7. `agent_docs/` -- Internal Claude AI operational docs (`git-workflow.md`, `playwright-testing.md`, `code-style.md`, etc.) that reference the private `ReadyOk` branch and contain Claude Code-specific agent instructions. External contributors do not need these files; a public `CONTRIBUTING.md` should replace them.

8. `Documentation/` -- Internal operational documentation for the private deployment (`SQL-Server-Startup-Solution.md`, `Showcase-Deletion-Attachment-Behavior.md`). These describe infrastructure-specific configuration for the private hosting environment and are not useful to external contributors in their current form.

#### [LOW] `CLAUDE.md` references internal tooling paths
- **Category**: File Exclusions
- **Location**: `CLAUDE.md`
- **Issue**: `CLAUDE.md` is an AI agent configuration file for the Claude Code CLI. It references internal guide paths (`agent_docs/claude/`) that will not exist in the fresh repository if `agent_docs/` is excluded. It is not part of the application and is unnecessary for external contributors.
- **Remediation**: Exclude `CLAUDE.md` from the open-source repository, or replace it with a minimal version pointing only to files that will be present (e.g., `CONTRIBUTING.md`).

### Category 4: Licensing & Dependencies

**Scan methodology:** LICENSE file manual review; `dotnet list package` against all projects; `Directory.Packages.props` review; per-package license classification cross-referenced against NuGet metadata, GitHub repositories, and known license changes.

#### LICENSE file verification

The `LICENSE` file contains valid, complete MIT license text. The copyright line ("Copyright (c) 2024 Collectibles contributors") is appropriate -- 2024 is the project start year and "contributors" is the correct attribution form for an open-source project. One cosmetic note: line 18 uses lowercase "authors" where the canonical MIT template uses "AUTHORS OR COPYRIGHT HOLDERS" -- this is legally equivalent and requires no change.

#### Dependency license inventory

**Production dependencies (shipped with the application):**

| Package | Version | License | MIT-Compatible? |
|---------|---------|---------|----------------|
| Hangfire.AspNetCore | 1.8.23 | LGPL v3 | Yes (as NuGet dependency) |
| Hangfire.Core | 1.8.23 | LGPL v3 | Yes (as NuGet dependency) |
| Hangfire.SqlServer | 1.8.23 | LGPL v3 | Yes (as NuGet dependency) |
| SixLabors.ImageSharp | 3.1.12 | Apache 2.0 (OSS) / Six Labors Split License v1.0 (commercial) | Yes for OSS release |
| SixLabors.ImageSharp.Drawing | 2.1.7 | Apache 2.0 (OSS) / Six Labors Split License v1.0 (commercial) | Yes for OSS release |
| FFMpegCore | 5.4.0 | MIT | Yes |
| PDFtoImage | 5.2.0 | MIT | Yes |
| FluentValidation | 12.1.1 | Apache 2.0 | Yes |
| FluentValidation.DependencyInjectionExtensions | 12.1.1 | Apache 2.0 | Yes |
| MediatR | 14.1.0 | Apache 2.0 | Yes |
| Polly | 8.6.6 | BSD 3-Clause | Yes |
| MailKit | 4.15.1 | MIT | Yes |
| SendGrid | 9.29.3 | MIT | Yes |
| QRCoder | 1.7.0 | MIT | Yes |
| Hashids.net | 1.7.0 | MIT | Yes |
| Serilog | 4.3.1 | Apache 2.0 | Yes |
| Serilog.AspNetCore | 10.0.0 | Apache 2.0 | Yes |
| Serilog.Sinks.EventLog | 4.0.0 | Apache 2.0 | Yes |
| Serilog.Sinks.File | 7.0.0 | Apache 2.0 | Yes |
| Blazor.Bootstrap | 3.5.0 | Apache 2.0 | Yes |
| MiniProfiler.AspNetCore.Mvc | 4.5.4 | MIT | Yes |
| MiniProfiler.EntityFrameworkCore | 4.5.4 | MIT | Yes |
| Azure.Storage.Blobs | 12.27.0 | MIT | Yes |
| Azure.Communication.Email | 1.1.0 | MIT | Yes |
| DocumentFormat.OpenXml | 3.5.1 | MIT | Yes |
| AspNetCore.HealthChecks.SqlServer | 9.0.0 | Apache 2.0 | Yes |
| Newtonsoft.Json | 13.0.3 | MIT | Yes |
| Microsoft.IdentityModel.Tokens | 8.14.0 | MIT | Yes |
| Microsoft.Web.Xdt | 3.2.5 | MIT | Yes |
| Microsoft.* (all) | 10.0.5 | MIT | Yes |
| CommunityToolkit.Mvvm | 8.4.0 | MIT | Yes |
| StyleCop.Analyzers | 1.2.0-beta.556 | Apache 2.0 | Yes (build-time only, not shipped) |

**Test-only dependencies (not shipped):**

| Package | License | Notes |
|---------|---------|-------|
| AutoFixture / AutoFixture.AutoMoq | MIT | Test-only |
| FluentAssertions | Apache 2.0 | Test-only |
| Moq | BSD 3-Clause | Test-only |
| xunit.v3 / xunit.runner.visualstudio | Apache 2.0 | Test-only |
| coverlet.collector | MIT | Test-only |
| Microsoft.Playwright | MIT | Test-only |
| Microsoft.EntityFrameworkCore.InMemory | MIT | Test-only |
| Microsoft.NET.Test.Sdk | MIT | Test-only |

**No incompatible licenses were found.** All dependencies use MIT, Apache 2.0, BSD 3-Clause, or LGPL v3, all of which are compatible with distributing this project under MIT.

#### [MEDIUM] Hangfire LGPL license requires documentation
- **Category**: Licensing & Dependencies
- **Location**: `Directory.Packages.props` (Hangfire packages)
- **Issue**: Hangfire v1.8.x (Community Edition) uses LGPL v3. While LGPL v3 is compatible with MIT when used as a NuGet dependency (i.e., dynamically linked, no source modification), this should be documented so users understand the licensing implications. Operators who modify the Hangfire source code and redistribute would need to comply with LGPL terms.
- **Remediation**: Add a "Third-Party Licenses" section to `README.md` or create a `THIRD-PARTY-NOTICES` file noting Hangfire's LGPL v3 license and the conditions under which it applies.

#### [MEDIUM] SixLabors.ImageSharp dual-license model requires awareness
- **Category**: Licensing & Dependencies
- **Location**: `Directory.Packages.props` (`SixLabors.ImageSharp` v3.1.12, `SixLabors.ImageSharp.Drawing` v2.1.7)
- **Issue**: SixLabors changed its license model in 2023. Version 3.x uses the "Six Labors Split License v1.0": Apache 2.0 applies to open-source projects, but commercial use requires a paid license. As an open-source project distributed under MIT, this application qualifies for the Apache 2.0 terms. However, anyone who forks this project and uses it commercially (without contributing back or obtaining a SixLabors commercial license) would be subject to the commercial license terms. This is not a problem for the project itself but should be disclosed to downstream users.
- **Remediation**: Add a note in `THIRD-PARTY-NOTICES` (see Hangfire finding) that SixLabors.ImageSharp and SixLabors.ImageSharp.Drawing use the Six Labors Split License v1.0 and that commercial users must obtain a SixLabors commercial license. Link to https://sixlabors.com/pricing/ for details.

### Category 5: Security Posture

**Scan methodology:** grep scan of all `.cs` and `.razor` files for `TODO`, `FIXME`, and `HACK` comments; grep scan for `#if DEBUG`, `Debugger.IsAttached`, and `IsDebug` patterns; manual review of MiniProfiler registration and middleware placement; manual review of test account code paths in `UsersList.razor`; manual review of `appsettings.json` EF logging flags.

#### [MEDIUM] MiniProfiler active in production if ASPNETCORE_ENVIRONMENT is misconfigured
- **Category**: Security Posture
- **Location**: `Source/Collectibles.Web/Extensions/ServiceCollectionExtensions.cs:90`, `Source/Collectibles.Web/Program.cs:215`
- **Issue**: MiniProfiler is correctly gated behind a development environment check in both service registration and middleware placement. However, if `ASPNETCORE_ENVIRONMENT` is set to `Development` in a production deployment (which the base `web.config` does by default -- see Category 2 HIGH finding), MiniProfiler will be active. When active, MiniProfiler exposes a profiler UI at its configured route that reveals SQL query text, query execution times, and server-side timing data to any user who can reach it. There is no authentication guard on the MiniProfiler route in the current configuration.
- **Remediation**: Fix the `web.config` default value as described in the Category 2 HIGH finding -- that fix closes this exposure automatically. As a defense-in-depth measure, consider adding `options.ResultsAuthorize` and `options.ResultsListAuthorize` callbacks in the MiniProfiler configuration to restrict the profiler UI to authenticated administrators only, even in development.

#### No findings: TODO/FIXME comments
- Two TODO comments were found (`AttachmentPreviewBackgroundService.cs:173` and `CreateShowcase.razor:129`). Neither indicates a security concern -- one is a refactoring note about API design and the other is a UX improvement note about surfacing error messages.

#### No findings: Test account code path
- The `CreateTestAdminUser` and `CreateTestRegularUser` methods in `UsersList.razor` are fully enclosed in `#if DEBUG` preprocessor blocks (lines 524--end of file). The public-facing handler methods (`HandleCreateTestAdminUser`, `HandleCreateTestRegularUser`) compile to `Task.CompletedTask` stubs in non-DEBUG builds. The hardcoded test password is covered as a credentials finding in Category 1.

#### No findings: EF sensitive data logging
- `appsettings.json` confirms `EntityFramework.EnableSensitiveDataLogging: false` and `EntityFramework.EnableDetailedErrors: false` (lines 92--93). Query parameter values and detailed EF errors are not logged in any build configuration.

### Category 6: Documentation Readiness

**Scan methodology:** Full read of `README.md`, all files under `Docs/` (excluding `superpowers/`), and all files under `agent_docs/` from a new-user perspective; grep scan of all `.md` files for external URLs; checked for private infrastructure references, version accuracy, missing setup steps, and polish.

#### [MEDIUM] README Quick Start uses inconsistent and incomplete formatting
- **Category**: Documentation Readiness
- **Location**: `README.md:71-88`
- **Issue**: The Quick Start section mixes a markdown bullet list (`- git clone`, `- cd`, `- dotnet restore`) with freestanding shell commands (the `dotnet ef database update` and `dotnet run` lines appear outside any code block). The bullet list format does not render as copyable commands. A new user following the README would have to guess which lines to run and in what order.
- **Remediation**: Rewrite the Quick Start section as a single numbered sequence of `bash` code blocks. All commands (clone, restore, ef migrate, run) should appear in fenced code blocks so they render consistently on GitHub and can be copied directly.

#### [MEDIUM] README does not warn that `dotnet ef` tool must be installed separately
- **Category**: Documentation Readiness
- **Location**: `README.md:127`
- **Issue**: The Quick Start instructs the reader to run `dotnet ef database update ...` but does not mention that the `dotnet-ef` global tool is a separate installation step. New users who do not already have the tool installed will receive a cryptic "No executable found matching command 'dotnet-ef'" error with no explanation.
- **Remediation**: Add a prerequisite step: `dotnet tool install --global dotnet-ef` (or reference the Microsoft EF Core Tools documentation). Alternatively, add it to the Prerequisites list alongside .NET SDK and SQL Server.

#### [MEDIUM] README test credentials section lacks a security context callout
- **Category**: Documentation Readiness
- **Location**: `README.md:183-189`
- **Issue**: The "Test User (Debug Only)" section presents the test email/password pair (`xA&%4hTVhTDixSOO`) without any callout making clear that these credentials are public knowledge for anyone reading the repository, and that the accounts they create should never exist in a production database. A developer who runs a DEBUG build against a production server would create accounts with a password visible to anyone on GitHub.
- **Remediation**: Add a warning callout (e.g., a markdown blockquote prefixed with `> **Warning:**`) explaining that these credentials are publicly known, that the test user creation feature is compiled out of Release builds, and that it must never be used against a production database.

#### [MEDIUM] `agent_docs/git-workflow.md` exposes private branch names
- **Category**: Documentation Readiness
- **Location**: `agent_docs/claude/git-workflow.md:9`
- **Issue**: The protected branches list includes `ReadyOk`, which is a private internal branch name not present in the public repository. If `agent_docs/` is retained in the open-source release (rather than excluded as Category 3 recommends), this reference reveals internal workflow structure and would confuse external contributors who will not have this branch.
- **Remediation**: If `agent_docs/` is included in the public release, remove `ReadyOk` from the protected branch list and replace it with standard branch names. If `agent_docs/` is excluded (per the Category 3 recommendation), no action is needed.

#### [MEDIUM] `agent_docs/database.md` and `Docs/DEVELOPER_README.md` state wrong EF Core version
- **Category**: Documentation Readiness
- **Location**: `agent_docs/database.md:4`, `Docs/DEVELOPER_README.md:115`
- **Issue**: Both files state "Entity Framework Core 8" as the version in use. The project targets .NET 10 and uses EF Core 10 (as reflected in `Directory.Packages.props` and `Docs/Configuration.md` which correctly documents the stack). The version mismatch could send contributors to the wrong version of EF Core documentation.
- **Remediation**: Update both files to read "Entity Framework Core 10". If `agent_docs/` is excluded from the release, only `DEVELOPER_README.md` requires correction.

#### [LOW] `Docs/LargeFileUploads.md` is raw notes, not formatted documentation
- **Category**: Documentation Readiness
- **Location**: `Docs/LargeFileUploads.md`
- **Issue**: The file uses ASCII box-drawing characters for tables and plain text bullet syntax (`-`), suggesting it was generated or copied from a terminal or chat session rather than authored as markdown. It lacks a top-level heading and uses a mix of indentation styles. The content itself is accurate, but it does not match the presentation quality of other files in `Docs/`.
- **Remediation**: Reformat the file with standard markdown: add an `#` heading, convert the ASCII table to a markdown pipe table, and use proper bullet list syntax.

#### [LOW] `Docs/Playwright in production.md` is raw notes without markdown structure
- **Category**: Documentation Readiness
- **Location**: `Docs/Playwright in production.md`
- **Issue**: The file contains no markdown headings, no code fencing, and appears to be unedited notes or chat output. It is also not linked from `README.md` or the Further Documentation section. As raw notes it could confuse contributors who expect polished documentation.
- **Remediation**: Either reformat the file as a proper markdown guide with headings and fenced code blocks and link it from `README.md`, or remove it from the public release if the content is covered adequately by the Playwright testing section in `README.md` and `agent_docs/claude/playwright-testing.md`.

#### [LOW] README `#first-run-setup` anchor link points to a section that does not exist in README
- **Category**: Documentation Readiness
- **Location**: `README.md:136`
- **Issue**: The text "follow the [First-Run Setup](#first-run-setup) instructions" links to a `#first-run-setup` heading anchor, but no such heading exists in `README.md`. The first-run setup content is described in `Docs/Features.md` under the Security section. The link will silently fail to navigate on GitHub (the page will not scroll).
- **Remediation**: Either add a `## First-Run Setup` section to `README.md` with the setup token steps, or change the link to point to the correct location in `Docs/Features.md` (e.g., `Docs/Features.md#first-run-setup`).

#### [LOW] `Docs/DEVELOPER_README.md` Playwright instructions do not match actual test setup
- **Category**: Documentation Readiness
- **Location**: `Docs/DEVELOPER_README.md:168-174`
- **Issue**: The developer README states "the Playwright config does not currently launch the web server automatically" and instructs developers to start the app manually in a separate terminal before running tests. This contradicts `README.md` and `agent_docs/claude/playwright-testing.md`, which describe a self-contained Playwright environment that resets and reseeds its own database. The discrepancy could confuse contributors about the actual test setup.
- **Remediation**: Update `DEVELOPER_README.md` to accurately describe the current Playwright setup: the suite uses `ASPNETCORE_ENVIRONMENT=Playwright`, loads from `appsettings.Playwright.json`, and manages its own database reset/reseed. Remove or correct the instruction to start the app manually.

### Category 7: Contributor Experience

**Scan methodology:** Directory listing of repository root and `.github/` for standard OSS community files (`CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`, `CODEOWNERS`, `.github/ISSUE_TEMPLATE/`, `.github/PULL_REQUEST_TEMPLATE.md`, `.github/FUNDING.yml`). None were found -- the project has been private and these files were never created.

#### [MEDIUM] Missing CONTRIBUTING.md
- **Category**: Contributor Experience
- **Location**: Repository root (file does not exist)
- **Issue**: There is no contribution guide. External contributors have no documented path for setting up a development environment, understanding the coding conventions, submitting pull requests, or knowing what is expected before a PR will be reviewed. Without this file, the barrier to first contribution is unnecessarily high and maintainers will receive inconsistent PRs.
- **Remediation**: Create `CONTRIBUTING.md` covering: prerequisites (.NET SDK version, SQL Server, `dotnet-ef` tool), local dev setup steps, coding conventions (Clean Architecture layer boundaries, CQRS command/query structure, entity design rules), branch naming and PR process, and how to run the test suite including Playwright.

#### [MEDIUM] Missing CODE_OF_CONDUCT.md
- **Category**: Contributor Experience
- **Location**: Repository root (file does not exist)
- **Issue**: There is no code of conduct. GitHub surfaces the absence of a code of conduct on the repository's community health page, and many potential contributors (especially those from organizations with open-source policies) will not engage with a project that has none. It also leaves the project without any stated basis for moderating behavior in issues and pull requests.
- **Remediation**: Adopt the Contributor Covenant (https://www.contributor-covenant.org/). Add `CODE_OF_CONDUCT.md` at the repository root using the standard v2.1 template with the project maintainer contact email filled in.

#### [MEDIUM] Missing SECURITY.md
- **Category**: Contributor Experience
- **Location**: Repository root (file does not exist)
- **Issue**: There is no documented security vulnerability disclosure process. This application handles user authentication, stores attachments, and exposes a public showcase-sharing feature -- making it a meaningful target. Without a `SECURITY.md`, well-intentioned security researchers have no channel to report vulnerabilities privately, and may fall back to opening a public GitHub issue (which would disclose the vulnerability before a fix is available).
- **Remediation**: Create `SECURITY.md` with: a statement of supported versions, instructions for private disclosure (e.g., GitHub's private vulnerability reporting feature or a dedicated security contact email), an expected response time commitment, and a note on responsible disclosure.

#### [LOW] Missing GitHub issue and PR templates
- **Category**: Contributor Experience
- **Location**: `.github/` directory (does not exist)
- **Issue**: There are no `.github/ISSUE_TEMPLATE/` templates for bug reports or feature requests, and no `.github/PULL_REQUEST_TEMPLATE.md`. Without templates, issues arrive without reproduction steps or environment details, and PRs arrive without a description of what was changed or how it was tested. This increases triage burden on the maintainer and leads to back-and-forth to gather basic information.
- **Remediation**: Create the following files: `.github/ISSUE_TEMPLATE/bug_report.md` (fields: description, steps to reproduce, expected vs. actual behavior, environment), `.github/ISSUE_TEMPLATE/feature_request.md` (fields: problem statement, proposed solution, alternatives considered), and `.github/PULL_REQUEST_TEMPLATE.md` (checklist: description of change, type of change, testing performed, breaking changes, checklist for coding standards).
