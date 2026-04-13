# Open-Source Readiness Audit Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Audit the Collectibles codebase across 7 categories and produce a findings document with severity-rated items and concrete remediation actions, preparing the project for open-source release as a fresh repository.

**Architecture:** Each audit category is an independent task that produces findings in a consistent format. All findings are collected into a single output document. The audit is read-only -- it identifies issues but does not fix them (remediation is a separate plan).

**Tech Stack:** gitleaks (secret scanning), dotnet CLI (dependency listing), grep/ripgrep (pattern scanning), manual review

---

## File Structure

| File | Purpose |
|------|---------|
| Create: `Docs/open-source-audit-findings.md` | The single audit output document with all findings |

---

### Task 1: Set Up Findings Document and Install Tooling

**Files:**
- Create: `Docs/open-source-audit-findings.md`

- [ ] **Step 1: Install gitleaks**

Run:
```bash
winget install Gitleaks.Gitleaks
```
Expected: gitleaks installs successfully. Verify with `gitleaks version`.

If winget is unavailable, download from https://github.com/gitleaks/gitleaks/releases and add to PATH.

- [ ] **Step 2: Create the findings document skeleton**

Create `Docs/open-source-audit-findings.md` with this content:

```markdown
# Open-Source Readiness Audit Findings

**Date:** 2026-04-08
**Spec:** Docs/superpowers/specs/2026-04-08-open-source-audit-design.md

## Summary

| Severity | Count |
|----------|-------|
| CRITICAL | 0 |
| HIGH | 0 |
| MEDIUM | 0 |
| LOW | 0 |

### Files to Exclude from Open-Source Release

_(populated after audit)_

### Files to Modify

_(populated after audit)_

### Files to Create

_(populated after audit)_

---

## Findings

### Category 1: Secrets & Credentials

_(populated in Task 2)_

### Category 2: Configuration Hygiene

_(populated in Task 3)_

### Category 3: File Exclusions

_(populated in Task 4)_

### Category 4: Licensing & Dependencies

_(populated in Task 5)_

### Category 5: Security Posture

_(populated in Task 6)_

### Category 6: Documentation Readiness

_(populated in Task 7)_

### Category 7: Contributor Experience

_(populated in Task 8)_
```

- [ ] **Step 3: Commit**

```bash
git add Docs/open-source-audit-findings.md
git commit -m "chore: add audit findings document skeleton"
```

---

### Task 2: Audit Category 1 -- Secrets & Credentials

**Files:**
- Modify: `Docs/open-source-audit-findings.md`

- [ ] **Step 1: Run gitleaks scan**

Run from the repo root:
```bash
gitleaks dir . --no-git --report-format json --report-path gitleaks-report.json -v
```

Review the output. Each finding will have a description, file, line number, and match. Record any real secrets (not test/placeholder values) as CRITICAL findings.

Note: `gitleaks-report.json` is a temporary file -- do not commit it.

- [ ] **Step 2: Run regex scans for credential patterns**

Run each of these and review the output for real credentials (not empty strings or placeholders):

```bash
# Connection strings with actual values
grep -rn "Server=\|Data Source=" --include="*.json" --include="*.cs" --include="*.config" --include="*.yml" --include="*.md" --include="*.ps1" --include="*.sql" .

# Password assignments with actual values
grep -rn "Password=" --include="*.json" --include="*.cs" --include="*.config" --include="*.yml" . | grep -v '""' | grep -v "Password=\"\""

# API keys and tokens
grep -rn "ApiKey\|api_key\|Bearer \|token" --include="*.json" --include="*.cs" --include="*.config" . | grep -v '""'

# Secret patterns
grep -rn "secret\|credential" -i --include="*.json" --include="*.cs" --include="*.config" . | grep -v '""'
```

For each match, determine if it's:
- A real credential (CRITICAL)
- A test-only credential that's acceptable (document but not CRITICAL)
- An empty placeholder (no finding needed)

- [ ] **Step 3: Manual review of known sensitive files**

Read and review each of these files for credentials:
- `Source/Collectibles.Web/appsettings.json` -- verify all credential fields are empty/placeholder
- `Source/Collectibles.Web/appsettings.Playwright.json` -- known to contain real DB credentials (Server=NucOne, User Id=sqluser, Password=Dynamic123)
- `Source/Collectibles.Web/Properties/launchSettings.json` -- check for non-localhost URLs
- `Source/Collectibles.Infrastructure/Persistence/Seeders/PlaywrightScenarioSeeder.cs` -- test password
- `Source/Collectibles.Web/Components/Pages/UsersList.razor` (lines 528, 597) -- hardcoded test password

- [ ] **Step 4: Record findings in the audit document**

Update the "Category 1: Secrets & Credentials" section of `Docs/open-source-audit-findings.md`. Use this format for each finding:

```markdown
#### [SEVERITY] Short title
- **Category**: Secrets & Credentials
- **Location**: `file/path.ext:line_number`
- **Issue**: Description of what was found
- **Remediation**: Specific action to fix
```

Known findings to record (verify and add any new ones from Steps 1-3):

1. **[CRITICAL] Real database credentials in Playwright config** -- `appsettings.Playwright.json:3` contains `Server=NucOne;...User Id=sqluser;Password=Dynamic123`. Remediation: Replace with placeholder connection string using `(localdb)` or `localhost` with generic credentials.

2. **[HIGH] Test password in production code path** -- `UsersList.razor:528,597` contains hardcoded `xA&%4hTVhTDixSOO`. Remediation: Document that test accounts seeded in production would be compromisable. Consider reading password from configuration instead of hardcoding.

3. Record any additional findings from the automated scans.

- [ ] **Step 5: Clean up and commit**

```bash
rm -f gitleaks-report.json
git add Docs/open-source-audit-findings.md
git commit -m "audit: complete secrets & credentials scan (category 1)"
```

---

### Task 3: Audit Category 2 -- Configuration Hygiene

**Files:**
- Modify: `Docs/open-source-audit-findings.md`

- [ ] **Step 1: Review appsettings.json**

Read `Source/Collectibles.Web/appsettings.json` and check each section:
- All `ConnectionString` fields should be empty `""`
- All API key / password fields should be empty `""`
- Placeholder values (like `YOUR_UNIQUE_SALT_HERE`) should be clearly identifiable
- No environment-specific values (server names, domain names, IP addresses)

- [ ] **Step 2: Review appsettings.Playwright.json**

Read `Source/Collectibles.Web/appsettings.Playwright.json` and verify:
- Contains real server name `NucOne` (finding already known)
- Determine if this file should be excluded entirely or sanitized with placeholder values

- [ ] **Step 3: Review launchSettings.json**

Read `Source/Collectibles.Web/Properties/launchSettings.json` and check:
- Only `localhost` URLs
- No environment variables with real values
- No profile-specific secrets

- [ ] **Step 4: Review web.config files**

Read `Source/Collectibles.Web/web.config` and any `web.*.config` transform files. Check for:
- Hardcoded server names or paths
- Credentials
- Environment-specific values

- [ ] **Step 5: Review .gitignore**

Read `.gitignore` and check:
- Line 374: `!/Source/Collectibles.Web/appsettings.Playwright.json` -- this exception un-ignores the file containing real credentials
- Verify all other sensitive file patterns are properly excluded
- Check for any missing exclusions (e.g., `.env`, `secrets.json`, user-specific files)

- [ ] **Step 6: Record findings**

Update "Category 2: Configuration Hygiene" in `Docs/open-source-audit-findings.md`.

Known findings to record:
1. **[CRITICAL] .gitignore exception exposes sensitive config** -- `.gitignore:374` un-ignores `appsettings.Playwright.json` which contains real credentials. Remediation: Remove the exception line, or sanitize the file.
2. Record any additional findings from Steps 1-4.

- [ ] **Step 7: Commit**

```bash
git add Docs/open-source-audit-findings.md
git commit -m "audit: complete configuration hygiene review (category 2)"
```

---

### Task 4: Audit Category 3 -- File Exclusions

**Files:**
- Modify: `Docs/open-source-audit-findings.md`

- [ ] **Step 1: Generate top-level file listing**

```bash
# List all top-level files and directories
ls -la

# List Docs directory structure
find Docs/ -type f | head -60

# List Scripts directory
find Scripts/ -type f

# List any CI/CD files
find . -maxdepth 1 -name "*.yml" -o -name "*.yaml" -o -name "Dockerfile*" -o -name "docker-compose*" | head -20
```

- [ ] **Step 2: Review each candidate for exclusion**

Check each of these against the decisions in the spec:

| File/Directory | Decision | Rationale |
|---------------|----------|-----------|
| `azure-pipelines.yml` | EXCLUDE | Contains private deployment info (ReadyOkRetro, IIS paths). Keep `azure-pipelines-example.yml` |
| `Docs/superpowers/` | EXCLUDE | Internal planning docs, contain test passwords |
| `appsettings.Playwright.json` | EXCLUDE or SANITIZE | Contains real DB credentials |
| `.claude/` | Check `.gitignore` | Should already be excluded by git |

Also scan for anything else that should be excluded:
```bash
# Look for any files with personal/company references
grep -rl "ReadyOkRetro\|NucOne\|readyokretro" --include="*.yml" --include="*.yaml" --include="*.json" --include="*.ps1" --include="*.md" .
```

- [ ] **Step 3: Record findings**

Update "Category 3: File Exclusions" in `Docs/open-source-audit-findings.md`.

Create a complete exclusion list:
```markdown
#### [HIGH] Files to exclude from open-source release
- **Category**: File Exclusions
- **Location**: Repository root
- **Issue**: The following files/directories contain private deployment info or internal artifacts
- **Remediation**: Do not include these in the fresh repository

**Exclusion list:**
1. `azure-pipelines.yml` -- private deployment configuration
2. `Docs/superpowers/` -- internal planning docs with test credentials
3. `Source/Collectibles.Web/appsettings.Playwright.json` -- real database credentials (or sanitize)
4. (add any others found in Step 2)
```

- [ ] **Step 4: Commit**

```bash
git add Docs/open-source-audit-findings.md
git commit -m "audit: complete file exclusions review (category 3)"
```

---

### Task 5: Audit Category 4 -- Licensing & Dependencies

**Files:**
- Modify: `Docs/open-source-audit-findings.md`

- [ ] **Step 1: Verify LICENSE file**

Read `LICENSE` and confirm:
- MIT license text is correct and complete
- Copyright line is appropriate: "Copyright (c) 2024 Collectibles contributors"
- Year is reasonable (2024 is when the project started)

- [ ] **Step 2: List all direct dependencies**

```bash
dotnet list package
```

This uses central package management (`Directory.Packages.props`). Record the output.

- [ ] **Step 3: Cross-reference package licenses**

Review each package from `Directory.Packages.props` and classify its license. The packages are:

| Package | Known License | Compatible with MIT? |
|---------|--------------|---------------------|
| AspNetCore.HealthChecks.SqlServer | Apache 2.0 | Yes |
| AutoFixture / AutoFixture.AutoMoq | MIT | Yes |
| Azure.Communication.Email | MIT | Yes |
| Azure.Storage.Blobs | MIT | Yes |
| Blazor.Bootstrap | Apache 2.0 | Yes |
| CommunityToolkit.Mvvm | MIT | Yes |
| DocumentFormat.OpenXml | MIT | Yes |
| FFMpegCore | MIT | Yes |
| FluentAssertions | Apache 2.0 | Yes |
| FluentValidation | Apache 2.0 | Yes |
| **Hangfire.AspNetCore / Core / SqlServer** | **LGPL v3** | **Check** |
| Hashids.net | MIT | Yes |
| MailKit | MIT | Yes |
| MediatR | Apache 2.0 | Yes |
| Microsoft.* (all) | MIT | Yes |
| MiniProfiler.* | MIT | Yes |
| Moq | BSD 3-Clause | Yes |
| Newtonsoft.Json | MIT | Yes |
| PDFtoImage | MIT | Yes |
| Polly | BSD 3-Clause | Yes |
| QRCoder | MIT | Yes |
| SendGrid | MIT | Yes |
| Serilog.* | Apache 2.0 | Yes |
| SixLabors.ImageSharp | Apache 2.0 | Yes |
| SixLabors.ImageSharp.Drawing | Apache 2.0 | Yes |
| StyleCop.Analyzers | MIT | Yes |
| xunit.v3 / xunit.runner.visualstudio | Apache 2.0 | Yes |
| coverlet.collector | MIT | Yes |

Verify the Hangfire license specifically. As of v1.8.x, Hangfire uses **LGPL v3** (not AGPL as initially suspected). LGPL v3 is compatible with MIT for projects that use Hangfire as a library (via NuGet) without modifying its source code. Confirm this by checking the Hangfire GitHub repo license or NuGet page.

- [ ] **Step 4: Record findings**

Update "Category 4: Licensing & Dependencies" in `Docs/open-source-audit-findings.md`.

```markdown
#### [MEDIUM] Hangfire LGPL license requires documentation
- **Category**: Licensing & Dependencies
- **Location**: `Directory.Packages.props` (Hangfire packages)
- **Issue**: Hangfire v1.8.x uses LGPL v3. While compatible with MIT when used as a NuGet dependency, this should be documented so users understand the licensing implications.
- **Remediation**: Add a "Third-Party Licenses" section to README or a THIRD-PARTY-NOTICES file noting Hangfire's LGPL v3 license.
```

Record any other license issues found.

- [ ] **Step 5: Commit**

```bash
git add Docs/open-source-audit-findings.md
git commit -m "audit: complete licensing & dependencies review (category 4)"
```

---

### Task 6: Audit Category 5 -- Security Posture

**Files:**
- Modify: `Docs/open-source-audit-findings.md`

- [ ] **Step 1: Scan for security-relevant comments**

```bash
# TODO/FIXME/HACK comments (excluding test data like "Torch XXX" computer name)
grep -rn "TODO\|FIXME\|HACK" --include="*.cs" --include="*.razor" . | grep -v "VintageComputerTagSeeder" | grep -v "Torch XXX"
```

Review each match. For this codebase, the known results are:
- `AttachmentPreviewBackgroundService.cs:173` -- `TODO: Consider modifying IFileProcessingService to accept file paths for videos` -- this is a performance improvement suggestion, NOT a security concern. No finding needed.

- [ ] **Step 2: Check for DEBUG-conditional code**

```bash
grep -rn "#if DEBUG\|#ifdef\|Debugger.IsAttached\|IsDebug" --include="*.cs" --include="*.razor" .
```

Review any matches for code that could leak sensitive information in debug builds.

- [ ] **Step 3: Review MiniProfiler configuration**

Read the MiniProfiler setup in `Source/Collectibles.Web/Extensions/ServiceCollectionExtensions.cs` (around line 89) and `Source/Collectibles.Web/Program.cs` (around line 213).

Verify:
- MiniProfiler is only enabled when `ASPNETCORE_ENVIRONMENT == "Development"` (line 90 of ServiceCollectionExtensions.cs checks this)
- `app.UseMiniProfiler()` is only called in the development branch of the `if` statement (Program.cs line 215, inside `if (app.Environment.IsDevelopment())`)

If both checks confirm development-only, no finding needed. If either is missing, record as HIGH.

- [ ] **Step 4: Review test account usage in production code**

Read `Source/Collectibles.Web/Components/Pages/UsersList.razor` around lines 528 and 597.

Determine:
- What context is the test password used in? (e.g., seeding test users via admin UI?)
- Is there a warning to admins that these are publicly known credentials?
- Could this code path be triggered in production?

- [ ] **Step 5: Check sensitive data logging settings**

Verify in `Source/Collectibles.Web/appsettings.json`:
- `EntityFramework.EnableSensitiveDataLogging` is `false` (line 92)
- `EntityFramework.EnableDetailedErrors` is `false` (line 93)

- [ ] **Step 6: Record findings**

Update "Category 5: Security Posture" in `Docs/open-source-audit-findings.md`.

Known findings:
1. **[HIGH] Test password hardcoded in production UI component** -- `UsersList.razor:528,597`. When open-sourced, anyone can see this password. If test accounts are seeded in a production database, they are immediately compromisable. Remediation: Add a prominent warning in the UI and documentation that test accounts should never exist in production, or move the password to configuration.

Record any additional findings from Steps 1-5.

- [ ] **Step 7: Commit**

```bash
git add Docs/open-source-audit-findings.md
git commit -m "audit: complete security posture review (category 5)"
```

---

### Task 7: Audit Category 6 -- Documentation Readiness

**Files:**
- Modify: `Docs/open-source-audit-findings.md`

- [ ] **Step 1: Review README.md from new-user perspective**

Read `README.md` and check:
- Does it explain what the project is?
- Are prerequisites listed (SQL Server, .NET 10, etc.)?
- Are setup instructions complete and accurate?
- Are there references to private infrastructure (server names, internal URLs)?
- Is the test credential section clear about security implications?
- Are all links valid (no internal/private links)?

- [ ] **Step 2: Review Docs/ directory**

```bash
find Docs/ -type f -name "*.md" | grep -v superpowers
```

Read each documentation file and check for:
- References to private servers, domains, or infrastructure
- Outdated or inaccurate information
- Missing setup steps a new user would need

- [ ] **Step 3: Review agent_docs/ directory**

```bash
find agent_docs/ -type f
```

Read each file and check for:
- Private references (server names, internal URLs, personal info)
- Whether the content is useful for open-source contributors using Claude Code
- Any credentials or secrets

- [ ] **Step 4: Check for broken links**

```bash
# Find all markdown links and check for internal/private references
grep -rn "](http\|](https" --include="*.md" . | grep -v "github.com\|localhost\|example.com\|collectibles.com\|microsoft.com\|nuget.org"
```

Review any non-standard URLs for private/internal references.

- [ ] **Step 5: Record findings**

Update "Category 6: Documentation Readiness" in `Docs/open-source-audit-findings.md`.

Potential findings:
1. **[MEDIUM] README test credentials need security warning** -- `README.md:181-188` lists test credentials without warning that they're visible in source code and should not be used in production.
2. Any other documentation gaps or private references found.

- [ ] **Step 6: Commit**

```bash
git add Docs/open-source-audit-findings.md
git commit -m "audit: complete documentation readiness review (category 6)"
```

---

### Task 8: Audit Category 7 -- Contributor Experience

**Files:**
- Modify: `Docs/open-source-audit-findings.md`

- [ ] **Step 1: Check for standard OSS community files**

Check for the existence of each of these files:

```bash
ls -la CONTRIBUTING.md CODE_OF_CONDUCT.md CODEOWNERS SECURITY.md 2>/dev/null
ls -la .github/ISSUE_TEMPLATE/ .github/PULL_REQUEST_TEMPLATE.md .github/FUNDING.yml 2>/dev/null
```

- [ ] **Step 2: Gap analysis**

For each missing file, determine if it's needed:

| File | Purpose | Needed? |
|------|---------|---------|
| `CONTRIBUTING.md` | How to contribute (dev setup, PR process, coding standards) | Yes -- essential for OSS |
| `CODE_OF_CONDUCT.md` | Community behavior expectations | Yes -- standard for OSS |
| `SECURITY.md` | How to report security vulnerabilities | Yes -- important since this handles user data |
| `CODEOWNERS` | Who reviews which areas | Optional -- useful if multiple maintainers |
| `.github/ISSUE_TEMPLATE/bug_report.md` | Structured bug reports | Yes -- improves issue quality |
| `.github/ISSUE_TEMPLATE/feature_request.md` | Structured feature requests | Yes -- improves issue quality |
| `.github/PULL_REQUEST_TEMPLATE.md` | PR checklist | Yes -- ensures PR quality |

- [ ] **Step 3: Record findings**

Update "Category 7: Contributor Experience" in `Docs/open-source-audit-findings.md`.

```markdown
#### [MEDIUM] Missing CONTRIBUTING.md
- **Category**: Contributor Experience
- **Location**: Repository root
- **Issue**: No contribution guidelines exist. New contributors won't know how to set up the dev environment, what coding standards to follow, or how to submit PRs.
- **Remediation**: Create CONTRIBUTING.md covering: prerequisites, dev setup, coding conventions, PR process, and testing requirements.

#### [MEDIUM] Missing CODE_OF_CONDUCT.md
- **Category**: Contributor Experience
- **Location**: Repository root
- **Issue**: No code of conduct. Standard expectation for open-source projects.
- **Remediation**: Adopt Contributor Covenant (https://www.contributor-covenant.org/) or similar.

#### [MEDIUM] Missing SECURITY.md
- **Category**: Contributor Experience
- **Location**: Repository root
- **Issue**: No security vulnerability reporting process. This application handles user data and file uploads -- responsible disclosure is important.
- **Remediation**: Create SECURITY.md with instructions for reporting vulnerabilities privately.

#### [LOW] Missing GitHub issue and PR templates
- **Category**: Contributor Experience
- **Location**: `.github/`
- **Issue**: No templates for bug reports, feature requests, or pull requests.
- **Remediation**: Create `.github/ISSUE_TEMPLATE/bug_report.md`, `.github/ISSUE_TEMPLATE/feature_request.md`, and `.github/PULL_REQUEST_TEMPLATE.md`.
```

- [ ] **Step 4: Commit**

```bash
git add Docs/open-source-audit-findings.md
git commit -m "audit: complete contributor experience review (category 7)"
```

---

### Task 9: Finalize Audit Document

**Files:**
- Modify: `Docs/open-source-audit-findings.md`

- [ ] **Step 1: Count findings by severity**

Review all findings recorded in Tasks 2-8. Count the totals for each severity level.

- [ ] **Step 2: Update the summary section**

Update the summary table at the top of `Docs/open-source-audit-findings.md` with the actual counts:

```markdown
| Severity | Count |
|----------|-------|
| CRITICAL | X |
| HIGH | X |
| MEDIUM | X |
| LOW | X |
```

- [ ] **Step 3: Populate the file action lists**

Fill in the three summary lists:

**Files to Exclude from Open-Source Release:**
- `azure-pipelines.yml`
- `Docs/superpowers/`
- `Source/Collectibles.Web/appsettings.Playwright.json` (or sanitize)
- (any others found during audit)

**Files to Modify:**
- `.gitignore` (remove Playwright exception)
- `README.md` (add security warning for test credentials)
- `appsettings.Playwright.json` (sanitize if keeping, otherwise exclude)
- (any others found during audit)

**Files to Create:**
- `CONTRIBUTING.md`
- `CODE_OF_CONDUCT.md`
- `SECURITY.md`
- `.github/ISSUE_TEMPLATE/bug_report.md`
- `.github/ISSUE_TEMPLATE/feature_request.md`
- `.github/PULL_REQUEST_TEMPLATE.md`
- (any others identified during audit)

- [ ] **Step 4: Final review of the complete document**

Read through the entire findings document. Check:
- Every finding has severity, category, location, issue, and remediation
- No duplicate findings
- Severity ratings are consistent (same type of issue gets same severity)
- Remediation actions are specific and actionable

- [ ] **Step 5: Commit**

```bash
git add Docs/open-source-audit-findings.md
git commit -m "audit: finalize findings document with summary and action lists"
```
