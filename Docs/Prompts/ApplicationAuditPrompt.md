# Comprehensive Collectibles Application Audit

## Role

Act as a principal .NET engineer performing a comprehensive, evidence-based audit of the Collectibles repository. Audit the implementation for correctness, data integrity, duplication, inconsistency, convention drift, architectural weaknesses, security, performance, reliability, maintainability, test quality, and departures from current .NET engineering practices.

This is a read-only audit. Do not fix findings. Produce a prioritized remediation plan in a self-contained HTML report.

## Repository Context

The repository is a .NET 10 application centered on a Blazor Server web application. Its declared architecture includes Domain, Application, Infrastructure, Web, and SyncTool projects; CQRS with MediatR; EF Core with SQL Server; Hangfire; FluentValidation; Serilog; resource-based authorization; pluggable storage and email; media processing; Playwright-based browser automation; xUnit tests; and a separate Node Playwright end-to-end suite.

Treat that description as orientation, not truth. Establish the actual architecture, projects, dependencies, and runtime paths from the repository. Explicitly reconcile documentation with the solution and implementation. Verify documented project existence, agreement among analyzer configurations, and alignment between test project names and actual dependency boundaries without presuming an issue.

## Required Outcome

Create exactly one final report at:

`Docs/Audits/YYYY-MM-DD-application-audit.html`

Use the current local date in the filename. Before writing it, check whether the canonical date-only path already exists. Do not silently overwrite an existing report; if it exists, stop and obtain explicit user authorization before replacing it. Keep this canonical date-only path and do not invent a suffix without approval. The report must be understandable without this prompt or access to your working notes.

The final HTML report is the sole permitted tracked repository/documentation write. Do not create fixes, commits, pull requests, migrations, snapshots, dependency updates, formatting changes, or rewritten tests. Do not modify existing application source, configuration, tests, documentation, or dependencies. Under the repository root, no files may be changed except the required final report, or an existing canonical audit report replaced only when the user explicitly authorized that replacement during the output-collision preflight, and permitted ignored or temporary diagnostic artifacts. Necessary writes to OS-temporary locations and external package or tool caches caused by approved diagnostics (for example the global NuGet cache) are permitted. Uncontrolled external-service or persistent data writes remain prohibited.

## Authority and Evidence

Use these sources in descending order of authority:

1. Observable behavior, executable diagnostics, and traced runtime paths.
2. Domain invariants and security or data-integrity requirements expressed by the implementation.
3. Repository instructions, solution configuration, tests, and documented conventions.
4. Current official Microsoft documentation and official documentation for the exact library version or supported version family.
5. Established engineering principles when the repository and primary documentation are silent.

When sources disagree, report the disagreement and explain which source should govern. Do not silently choose whichever source supports a finding.

Version-sensitive claims must be checked against current primary documentation. Do not rely on remembered framework behavior when it could differ in .NET 10, EF Core 10, ASP.NET Core 10, Blazor, MediatR, Hangfire, or another installed dependency.

## Non-Negotiable Guardrails

1. Inspect the complete defined scope. Do not extrapolate a repository-wide conclusion from a sample.
2. Preserve uncertainty. Separate proven behavior from likely risk and design opportunity.
3. Trace suspected correctness defects through callers, dependencies, persistence, authorization, and tests before confirming them.
4. Treat analyzer output as evidence to triage, not as a ready-made finding list.
5. Treat textual similarity as a lead, not proof of harmful duplication. Compare intent, behavior, and expected evolution.
6. Do not report generated migrations, vendored code, minified assets, source maps, or intentional boundary-local repetition as duplication merely because they repeat content.
7. Do not label personal style preferences as defects. Identify the violated invariant, repository rule, observable behavior, primary guidance, or concrete maintenance cost.
8. Consolidate repeated symptoms under their shared root cause and include representative examples plus an occurrence count.
9. Redact secrets, tokens, connection details, personal data, filesystem details outside the repository, and other sensitive values from notes and the report.
10. Continue through recoverable diagnostic failures. Record limitations rather than treating unverified behavior as correct.
11. Do not stop after finding several serious issues. Complete every phase and reconcile the coverage ledger.

## Phase 1: Establish Ground Truth

Read all applicable repository instruction files before auditing code. Inspect the solution, project files, central build and package configuration, analyzer configuration, global SDK selection, application configuration templates, deployment configuration, CI/CD, and architectural documentation.

Build an actual project and dependency map. Identify entry points, composition roots, storage boundaries, background execution paths, authorization boundaries, external integrations, and test boundaries. Compare the result with the documented architecture and commands.

Define a coverage universe before recording findings:

1. Define the starting universe as tracked files plus non-ignored untracked files under the current repository root, using source-control metadata when available.
2. Explicitly exclude VCS internals, external or nested worktrees, `bin`, `obj`, `node_modules`, vendored dependency trees, tool caches, generated output, and temporary directories from content-level review. Inventory and assess provenance of relevant excluded artifacts, but do not line-audit generated or third-party content.
3. Group files into named audit units by project, feature, integration, or delivery concern.
4. Content-inspect all included custom C#, Razor, TypeScript, JavaScript, CSS, PowerShell, configuration, project, CI/CD, test, migration, and documentation files.
5. Maintain a coverage ledger with unit, scope, status, files or file count, diagnostics, limitations, relevant finding identifiers, and inclusion or exclusion decisions.

## Phase 2: Capture Baseline Diagnostics

Before executing repository-controlled builds, tests, analyzers, or scripts, capture the baseline source-control state and inspect custom MSBuild targets, scripts, test configuration, launch settings, and external-service configuration sufficiently to identify side effects. Restore, build, test, and analyzer commands can execute repository-controlled code and are not inherently side-effect free. Execute tests or scripts only when database, storage, email, browser, and network dependencies are isolated and disposable; never allow uncontrolled writes to persistent databases, storage, email, external services, or production-like systems. Record a safety skip when isolation cannot be established.

Run each applicable listed diagnostic from the repository root. Prefer existing repository commands and adapt only when the actual solution requires it. A command may be skipped only for a recorded incompatibility, safety concern, missing prerequisite, or environmental limitation.

```powershell
dotnet --info
dotnet restore Collectibles.sln
dotnet build Collectibles.sln --no-restore
dotnet test Collectibles.sln --no-build --logger "console;verbosity=normal"
dotnet format Collectibles.sln --verify-no-changes --no-restore
dotnet package list --project Collectibles.sln --vulnerable --include-transitive --no-restore
dotnet package list --project Collectibles.sln --deprecated --no-restore
```

Run a second analyzer-focused build when useful, without editing project configuration. Collect test coverage if the existing test setup supports it without source changes. Inspect the Node Playwright package configuration and run applicable package or static diagnostics that do not update lockfiles or dependencies. Do not run end-to-end tests against an uncontrolled or non-isolated environment.

For every diagnostic, record the exact command, exit status, material output, and interpretation. Distinguish environment/setup failures from repository failures. After diagnostics, capture the final source-control state. If unexpected tracked or non-ignored untracked repository changes appear, stop, report them, and do not revert user changes. Do not paste large raw logs into the report; summarize them and include only evidence needed to support findings.

## Phase 3: Inspect Every Subsystem

Audit every in-scope unit and update the coverage ledger as it is completed. Include:

1. Domain entities, value objects, invariants, interfaces, enums, and domain services.
2. Application commands, queries, handlers, validators, behaviors, mappings, DTOs, authorization requirements, and application services.
3. Infrastructure persistence, EF Core configuration, repositories, unit-of-work behavior, migrations, background services, file storage, media processing, browser automation, email, HTTP clients, logging, and external integrations.
4. Web startup, middleware, endpoints, authentication, authorization handlers, Blazor components, forms, state, JavaScript interop, static assets, and error handling.
5. SyncTool composition, view models, services, HTTP behavior, state management, and WPF lifecycle.
6. Domain, application, infrastructure, web, and end-to-end tests, including helpers, fixtures, builders, mocks, assertions, isolation, and coverage gaps.
7. Build configuration, analyzers, central packages, SDK selection, CI/CD, deployment transforms, environment configuration, scripts, and operational documentation.

Within each unit, review both individual implementation quality and consistency with equivalent units elsewhere in the repository.

## Phase 4: Cross-Cutting Audit Passes

After subsystem inspection, perform all of these repository-wide passes.

### Correctness and Data Integrity

Inspect domain invariants, state transitions, null and empty handling, boundary values, partial failure, exception paths, inconsistent validation, stale data, transaction boundaries, concurrency conflicts, soft-delete behavior, cascading behavior, audit history, time zones, identifier conversions, and behavior that differs between UI, application, and persistence layers.

Trace representative end-to-end workflows, including setup and accounts, authentication and authorization, showcases and sharing, collectible items, templates or content definitions, attachments and uploads, ZIP processing, QR codes, sync operations, storage providers, email, media processing, external link capture, maintenance, and background jobs where present.

### Duplication and Consistency

Find copy-pasted or independently evolved validation, authorization, mapping, query, storage, upload, error-handling, UI, configuration, and test logic. Distinguish harmful knowledge duplication from deliberate separation. Look for parallel implementations that accept different inputs, apply different limits, omit checks, use different defaults, or produce inconsistent outcomes.

Also inspect dead code, unused abstractions, obsolete files, orphaned configuration, unused package references, duplicate package responsibilities, and stale compatibility code.

### Architecture and Boundaries

Assess dependency direction, project references, layer leakage, framework dependencies in inner layers, business rules in UI or persistence, anemic or overburdened types, misplaced interfaces, inappropriate repository or unit-of-work abstractions, CQRS consistency, handler size and cohesion, circular conceptual dependencies, service-locator behavior, hidden coupling, and needless indirection.

Check whether declared patterns solve current problems or create ceremony, duplication, and navigation cost. Recommend simplification when evidence shows a clearer boundary or direct implementation would be safer. Do not propose wholesale architectural replacement without a concrete migration benefit.

### Modern C# and .NET

Inspect nullability, API contracts, exception semantics, disposal and async disposal, stream ownership, cancellation propagation, immutable data, records and value semantics where appropriate, collection exposure, equality, parsing, culture, date/time types, pattern matching, asynchronous APIs, blocking work, `ConfigureAwait` assumptions, fire-and-forget calls, source-generated options or logging opportunities when materially useful, and consistency with enabled language and framework features.

Prefer correctness, clarity, and measured performance over novelty. A newer language feature is not automatically better.

### Dependency Injection and Configuration

Inspect service lifetimes, captive dependencies, scoped services in hosted jobs, factory use, duplicate registrations, missing registrations, registration order, options binding and validation, startup failure behavior, environment overrides, default values, secret handling, and configuration keys that are documented but unused or used but undocumented.

### Async, Concurrency, and Background Work

Inspect unawaited work, synchronous blocking, cancellation, thread safety, shared mutable state, Blazor circuit concurrency, job idempotency, retries, duplicate execution, transaction boundaries across jobs, shutdown behavior, scope creation, exception observation, progress reporting, and cleanup after cancellation or failure.

### EF Core and SQL Server

Inspect tracking choices, query projection, N+1 access, cartesian expansion, filtered includes, client evaluation assumptions, multiple enumeration, pagination stability, indexes, uniqueness enforcement, foreign keys, cascade rules, concurrency tokens, transactions, save behavior, execution strategies, raw SQL, migrations, global filters, soft deletion, seeding, test-provider differences, and date/time precision.

Compare database constraints with application validation. Identify invariants that can race because they are enforced only in memory.

### Blazor and Frontend

Inspect component lifecycle, event handling, async callbacks, disposal, subscriptions, circuit state, prerendering assumptions, navigation, forms, validation, authorization visibility versus authorization enforcement, error boundaries, loading and empty states, duplicate UI logic, JavaScript interop, DOM ownership, accessibility, keyboard behavior, semantic markup, responsive behavior, and sensitive information rendered to clients.

### Security and Privacy

Inspect authentication, account setup, session and cookie configuration, resource authorization, endpoint and handler enforcement, insecure direct object access, reliance on HashIds as access control, validation, overposting, mass assignment, CSRF, XSS, unsafe markup, SSRF, redirects, URL handling, path traversal, ZIP slip, archive bombs, upload size and type validation, media parsers, file permissions, secrets, logging, error disclosure, dependency vulnerabilities, data protection, rate limiting, and administrative functionality.

Treat security claims as high-stakes. Verify current framework behavior and primary guidance before reporting them.

### External I/O and Resilience

Inspect HTTP client creation, timeouts, retries, circuit breaking, DNS and connection reuse, streaming, buffering, temporary files, atomic writes, filename collisions, cleanup, partial output, storage consistency, email delivery, browser process lifecycle, media-tool process execution, cancellation, backpressure, and behavior when an external dependency is slow or unavailable.

### Performance and Scalability

Inspect database round trips, query size, allocations, repeated serialization, reflection-heavy hot paths, sync-over-async, unbounded collections, caching correctness, cache invalidation, large-file buffering, image or video memory use, background queue growth, Blazor circuit resource usage, excessive rendering, repeated remote calls, and algorithmic complexity. Do not report micro-optimizations without a credible hot path or measurable impact.

### Tests and Testability

Inspect whether tests prove behavior rather than implementation details. Find missing high-risk paths, weak or absent assertions, tests that cannot fail meaningfully, brittle ordering or timing, excessive mocking, unrealistic EF InMemory behavior, shared-state leakage, non-determinism, untested authorization, missing negative cases, missing concurrency and failure cases, duplicated test setup, and divergence between documented features and executable coverage.

Do not equate line coverage with confidence. Use coverage only to locate unexamined behavior.

### Reliability and Observability

Inspect exception handling, swallowed failures, log levels, structured properties, correlation, sensitive values, duplicate logging, actionable context, audit events, health checks, readiness versus liveness, startup validation, operational visibility, failure recovery, graceful shutdown, and whether a production operator could diagnose important failures.

### Build, Delivery, and Repository Hygiene

Inspect solution membership, project configuration, target frameworks, analyzer conflicts, warning suppression, central package consistency, vulnerable or deprecated packages, transitive pinning, restore determinism, CI/CD parity, deployment transforms, environment-specific behavior, publish output, source-controlled build artifacts, backup files, stale assets, scripts, commands, and documentation drift.

## Phase 5: Consolidate and Verify Findings

Before writing the report:

1. Merge duplicate symptoms into root-cause findings.
2. Recheck every Critical and High finding against relevant callers, authorization, persistence, tests, and configuration.
3. Confirm that severity describes impact and confidence describes evidence strength.
4. Downgrade or reclassify claims that remain uncertain.
5. Ensure every finding has actionable evidence and a future verification method.
6. Reconcile completed coverage units with the original coverage universe.
7. Record uninspected files, skipped diagnostics, and environmental limitations explicitly.

## Finding Taxonomy

Use stable identifiers in the form `F-001`, `F-002`, and so on.

Classify each finding as:

- `Confirmed defect`: evidence demonstrates incorrect, unsafe, or contract-violating behavior.
- `Probable risk`: evidence demonstrates a credible failure mode, but runtime confirmation is unavailable or conditional.
- `Improvement opportunity`: no current defect is proven, but a concrete change would reduce complexity, duplication, operational risk, or future defect likelihood.

Assign severity independently:

- `Critical`: severe consequence and broad blast radius, such as compromise, unrecoverable data loss or corruption, or broad production unavailability.
- `High`: material consequence and substantial blast radius affecting correctness, security, authorization, integrity, or reliability.
- `Medium`: meaningful consequence or bounded blast radius involving operational, performance, maintainability, or change risk.
- `Low`: localized consequence and limited blast radius involving convention, hygiene, documentation, or low-impact quality concerns.

Assign confidence independently as `High`, `Medium`, or `Low`.

Severity describes consequence magnitude and blast radius only. Confidence describes evidence strength. Exposure and trigger likelihood belong in the triggering-conditions narrative, not in Severity or Confidence.

Every finding must include:

1. Identifier and concise title.
2. Classification, severity, confidence, category, and affected subsystem.
3. Repository-relative file and line evidence.
4. Affected behavior and traced execution path.
5. Root cause.
6. Impact and triggering conditions.
7. The violated invariant, repository rule, observable contract, or primary technical guidance.
8. Recommended correction with enough specificity to plan later, but no implementation during this audit.
9. Verification method for a future fix.
10. Related findings, representative examples, and occurrence count where applicable.

Where repository file-and-line evidence or a traced runtime path cannot exist, an explicitly justified `Not applicable` is acceptable. Acceptable alternate evidence includes the exact command and material output, file-level evidence, an absence established by a documented search, configuration key or symbol references, or artifact provenance. Never invent or include irrelevant file, line, or path evidence.

## Remediation Roadmap

Create a phased roadmap that:

1. Addresses Critical and High correctness, security, authorization, and data-integrity issues first.
2. Identifies prerequisite characterization tests or diagnostics before risky changes.
3. Groups systemic fixes so repeated symptoms are corrected once at the appropriate boundary.
4. Sequences database, API, UI, deployment, and documentation changes safely.
5. Separates quick, low-risk corrections from architectural work.
6. Identifies changes that should not be combined because they would obscure behavior or increase rollback risk.
7. Defines the validation expected after each phase.

Do not estimate calendar time. Use relative scope and dependency information instead.

## HTML Report Requirements

Generate one self-contained HTML5 document at the required path. Use inline CSS and vanilla JavaScript only. Do not use a CDN, remote font, remote image, frontend framework, build step, or external runtime asset. The report must open directly through a `file:` URL.

Use a deliberate, professional visual system suitable for a serious engineering assessment. Define CSS custom properties. Provide strong information hierarchy, restrained color, readable typography, clear severity indicators that do not rely on color alone, and responsive layouts for desktop and mobile.

Include these sections:

1. Executive Summary.
2. Scope, Assumptions, and Limitations.
3. Baseline Diagnostics.
4. Actual Architecture and Documentation Drift.
5. Findings.
6. Systemic Duplication and Inconsistencies.
7. Test and Behavioral Coverage Gaps.
8. Dependencies, Build, Configuration, and Delivery.
9. Remediation Roadmap.
10. Coverage Ledger.
11. Diagnostic Commands and Primary References.

The top dashboard must show severity counts, classification counts, confidence distribution, affected subsystems, diagnostic status, and coverage. Do not calculate or display an overall quality score.

Provide sticky navigation plus search and combinable filters for severity, classification, confidence, category, subsystem, and remediation phase. Each finding must be addressable by a stable fragment link. Use semantic HTML and accessible form labels. Support keyboard operation, visible focus, sufficient contrast, reduced motion, and print styling.

Render every finding in the document by default. JavaScript may filter, search, expand, summarize, and preserve navigation state, but it must not fetch or create report content. Hide or disable JavaScript-only controls until successful initialization. Include a clear no-script explanation. Without JavaScript, all evidence must remain accessible through normal HTML, including expandable sections that a reader can open manually. Browser-state persistence for `file:` origins is optional and must fail safely; storage failures must never block filtering or navigation.

Escape all repository excerpts and diagnostic content before inserting them. Never concatenate unescaped source text into executable HTML or JavaScript. If structured finding data is embedded, encode it safely and render untrusted strings with `textContent`.

For printing, hide interactive controls, show full finding evidence, preserve severity labels, avoid splitting a finding across pages where practical, and include the report date and repository name.

## Completion Gate

Do not declare the audit complete until all of these are true:

1. The coverage universe and coverage ledger reconcile.
2. Every in-scope subsystem and every cross-cutting pass has a recorded status.
3. Diagnostics and limitations are recorded with exact commands and outcomes.
4. Critical and High findings have been independently rechecked.
5. Duplicate symptoms have been consolidated.
6. Every finding contains all required evidence fields.
7. The remediation roadmap includes dependencies, sequencing, and verification.
8. The HTML report is self-contained, opens successfully from disk, works at desktop and mobile widths, prints readably, and remains usable without JavaScript.
9. Sensitive evidence is redacted.
10. Under the repository root, no files were changed except the required final report, or an existing canonical audit report replaced only when the user explicitly authorized that replacement during the output-collision preflight, and permitted ignored or temporary diagnostic artifacts; necessary OS-temporary or external package/tool cache writes caused by approved diagnostics are separately permitted. Existing application source, configuration, dependency, migration, test, and documentation files were not intentionally changed. Uncontrolled external-service or persistent data writes did not occur.

## Final Response

When finished, respond with:

1. A link to the HTML report.
2. Counts by severity and classification.
3. A concise list of Critical and High finding titles.
4. Diagnostic or coverage limitations that materially affect confidence.
5. Confirmation that under the repository root no files changed except the required final report, or an existing canonical audit report replaced only when the user explicitly authorized that replacement during the output-collision preflight, and permitted ignored or temporary diagnostic artifacts; necessary OS-temporary or external package/tool cache writes caused by approved diagnostics are separately permitted. Confirm that no existing application source, configuration, dependency, migration, test, or documentation files were intentionally changed and that no uncontrolled external-service or persistent data writes occurred.

Do not reproduce the full report in the response.
