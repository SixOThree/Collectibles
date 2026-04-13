# Open-Source Readiness Audit

**Date:** 2026-04-08
**Status:** Approved
**Approach:** Structured audit with automated scanning (Approach B)

## Context

The Collectibles application is being prepared for open-source release. A fresh repository will be created (no history scrubbing needed). This audit identifies everything that must be fixed, excluded, or added before the codebase is safe and welcoming for public consumption.

## Decisions

- **Repository strategy**: Fresh repo, no git history carried over
- **CI/CD**: Remove `azure-pipelines.yml`, keep `azure-pipelines-example.yml` as a generic reference
- **Internal docs**: Exclude `Docs/superpowers/` entirely (planning specs, implementation plans)
- **SyncTool**: Include `Collectibles.SyncTool` in the release
- **Test credentials**: The hardcoded test password is not a personal or production credential. Keep it but document the risk -- if test accounts are seeded in a production database, they'd be compromisable by anyone who reads the source.

## Audit Categories

### 1. Secrets & Credentials

**Goal:** Ensure no real passwords, connection strings, API keys, or tokens are present in code, config, scripts, or documentation.

**Method:**
- Automated scan with `gitleaks` against the codebase snapshot
- Regex scan for patterns: `password\s*=`, `connectionstring`, `apikey`, `Bearer `, `secret`, `token`
- Manual review of all config files, seeders, and razor pages

**Known findings:**
- `appsettings.Playwright.json` contains a real database connection string with server name, username, and password
- Test password `xA&%4hTVhTDixSOO` appears in seeder, `UsersList.razor`, README, and agent docs

### 2. Configuration Hygiene

**Goal:** All config files are safe for public consumption with proper placeholders and no environment-specific values.

**Method:**
- Inspect all `appsettings*.json`, `launchSettings.json`, `web.config`
- Verify placeholder values are clearly marked (e.g., `YOUR_UNIQUE_SALT_HERE`)
- Check `.gitignore` for correctness

**Known findings:**
- `.gitignore` line 374 has an exception that un-ignores `appsettings.Playwright.json`
- `appsettings.json` has empty credential fields (good) but should have clear comments for new users

### 3. File Exclusions

**Goal:** Identify files and directories that should NOT be in the open-source release.

**Method:**
- Generate full file listing
- Flag CI/CD configs, internal docs, IDE-specific files, environment-specific artifacts
- Produce an explicit exclusion list for the fresh repo

**Known exclusions:**
- `azure-pipelines.yml` (keep `azure-pipelines-example.yml`)
- `Docs/superpowers/` (internal planning docs)
- `appsettings.Playwright.json` (or sanitize it)

### 4. Licensing & Dependencies

**Goal:** Verify license compatibility and document third-party dependencies.

**Method:**
- Verify `LICENSE` file (currently MIT)
- Run `dotnet list package` to enumerate all direct dependencies
- Cross-reference each package's license for compatibility with MIT
- Flag any restrictive licenses (AGPL, GPL, commercial-only)

**Known findings:**
- Hangfire uses AGPL for its free version -- needs documentation about license implications
- All other known dependencies appear MIT/Apache 2.0 compatible

### 5. Security Posture

**Goal:** Ensure no security weaknesses are exposed by open-sourcing, and that debug/profiling tools aren't leaking in production.

**Method:**
- Grep for `TODO`, `FIXME`, `HACK`, `XXX` comments that might reveal security concerns
- Scan for `#if DEBUG` patterns that could leak sensitive behavior
- Review MiniProfiler configuration for production exposure
- Review test account usage in production code paths (e.g., `UsersList.razor`)
- Check `EnableSensitiveDataLogging`, `EnableDetailedErrors` settings

**Known findings:**
- MiniProfiler referenced in `Collectibles.Web.csproj` -- verify it's disabled in production
- `UsersList.razor` has hardcoded test password in production code (lines 528, 597)

### 6. Documentation Readiness

**Goal:** README and setup docs are accurate and complete for someone discovering the project for the first time.

**Method:**
- Read README from a "new contributor" perspective
- Verify setup instructions work
- Check for broken internal links or references to private infrastructure
- Ensure configuration docs explain all required settings

**Known findings:**
- README is well-structured but references test credentials that should include a security note
- `agent_docs/` contains AI agent instructions for Claude Code -- review for any private references, but likely appropriate to include since contributors may use Claude Code

### 7. Contributor Experience

**Goal:** The repo has the standard files and structures that open-source contributors expect.

**Method:**
- Gap analysis against standard OSS project files
- Review existing templates and automation

**Expected gaps:**
- `CONTRIBUTING.md` -- contribution guidelines, development setup, PR process
- `CODEOWNERS` -- who reviews what
- Issue templates -- bug reports, feature requests
- Code of conduct
- PR template

## Findings Format

Each finding is documented as:

```
### [SEVERITY] Short title
- **Category**: Which audit category
- **Location**: File path(s) and line number(s)
- **Issue**: What's wrong
- **Remediation**: Specific action to take
```

Severity levels:
- **CRITICAL**: Must fix before release. Exposed real credentials, security vulnerabilities.
- **HIGH**: Should fix before release. Hardcoded environment-specific data, missing security documentation.
- **MEDIUM**: Recommended to fix. Improves contributor experience or code quality.
- **LOW**: Nice to have. Polish items.

## Output

The audit produces a single findings document with:
- Summary of total findings by severity
- List of files to exclude from the open-source release
- List of files to modify
- List of files/docs to create
- Detailed findings organized by category

## Scope

**In scope:**
- All source code in the current working tree
- All configuration files, scripts, and documentation
- NuGet package license review (direct dependencies)
- Automated secret scanning with `gitleaks`
- Dependency listing with `dotnet list package`
- Security-relevant code patterns
- Gap analysis for open-source community files

**Out of scope:**
- Full transitive dependency license audit
- SBOM generation
- Formal threat modeling
- Penetration testing
- Legal review
- Git history scrubbing (fresh repo, not needed)

If in-scope scanning reveals something that warrants pulling in an out-of-scope item, it will be flagged rather than silently skipped.
