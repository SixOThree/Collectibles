# Playwright E2E Roadmap Design

## Goal

Define a comprehensive, maintainable Playwright end-to-end testing strategy for the Collectibles application that:

- Prioritizes the most important end-user experiences first
- Expands to cover every page and major feature in the application
- Verifies both happy paths and authorization boundaries
- Runs against a dedicated Playwright environment with a reset-and-seed workflow
- Produces milestone-sized implementation plans that can be executed incrementally without losing full-coverage visibility

## Current State

The repository already contains a Node-based Playwright project at `Test/Playwright`, but it currently only includes starter example tests and a basic default config. The application itself exposes a broad Blazor surface area, including:

- Anonymous/public pages
- Logged-in end-user showcase and collectible management flows
- Media and ZIP upload workflows
- Template-driven item flows
- Sharing and QR code features
- Account-management pages
- Admin and operational pages

The application already supports:

- Environment-specific configuration through `ASPNETCORE_ENVIRONMENT`
- Database migrations on startup
- Base startup seed behavior for tags and template data
- Debug-mode test-user creation for admin and regular users

The repository also includes realistic fixture assets in `Test/Example Data/Showcase Example Images`, including a ZIP file suitable for bulk-upload testing.

## Design Decisions

The following decisions are locked in for this effort:

1. The test effort will be milestone-driven instead of using one monolithic implementation plan.
2. Early milestones will focus on the most important end-user experiences first.
3. Early milestones will cover only flows that can be exercised entirely inside the app itself.
4. Authorization and ownership checks are first-class requirements, not optional follow-up coverage.
5. Playwright tests will run against a dedicated Playwright environment rather than the normal development database and storage.
6. The Playwright environment will reset and seed its data on each run so test outcomes stay deterministic.

## Objectives

### Primary Objectives

- Build a roadmap that gives fast value on the highest-traffic user flows first.
- Maintain a master coverage view so the final suite still reaches full application coverage.
- Make test runs deterministic, isolated, and safe to execute repeatedly.
- Keep tests understandable by organizing them around domains, roles, and risks.

### Secondary Objectives

- Reuse seeded data, auth state, and fixture assets to keep the suite efficient.
- Reduce flaky tests by avoiding dependence on mutable shared environments.
- Create a planning structure that supports future additions without rewriting the full roadmap.

## Non-Goals

This design does not attempt to:

- Cover external email-driven flows in the early phases
- Turn every single UI assertion into a pixel-perfect visual test
- Validate third-party systems beyond the in-app behavior they enable
- Replace unit or integration tests for business rules that are better verified below the UI layer

## Recommended Planning Model

The recommended model is a coverage matrix plus milestone rollout.

### Why Not One Giant Plan

A single all-at-once plan for every page and feature would be easy to start but hard to keep accurate. This application already has a large route surface and meaningful domain complexity. A giant plan would become stale as soon as implementation starts.

### Why Not Pure Journey-Only Planning

User-journey-first planning is useful for prioritization, but by itself it tends to miss admin surfaces, secondary pages, and important access-control checks.

### Chosen Model

Maintain two layers:

1. A master E2E design and coverage inventory
2. One implementation plan per milestone

The master layer defines what complete coverage means. The milestone layer defines what gets built next.

## Coverage Model

Coverage should be tracked across four dimensions.

### 1. Role Coverage

- Anonymous user
- Regular user
- Administrator

### 2. Domain Coverage

- Welcome and navigation
- Public showcase browsing
- My showcases and showcase lifecycle
- Collectible item lifecycle
- Attachments and media
- ZIP upload
- Templates and dynamic fields
- Sharing and public access
- QR code workflows
- Tags, filters, and search
- In-app account management
- Admin and operations

### 3. Behavior Coverage

- Read/view flows
- Create flows
- Edit/update flows
- Delete or revoke flows
- Filtering, sorting, and navigation flows
- Background-processing visibility and completion flows
- Failure and validation states

### 4. Security Coverage

- User can access their own data
- User cannot access another user's private data
- Anonymous access works only for public or explicitly shared resources
- Regular users cannot access admin-only pages or actions
- Admin access works where intended

## Playwright Environment Design

### Dedicated Environment

Add a dedicated environment configuration file:

- `Source/Collectibles.Web/appsettings.Playwright.json`

The application should run with:

- `ASPNETCORE_ENVIRONMENT=Playwright`

This environment should use:

- A dedicated Playwright database
- Dedicated local file-storage paths
- Dedicated temporary and generated-file paths where applicable
- Test-friendly provider settings for features that do not need live external services

### Database Strategy

Each Playwright run should start from a known database state.

The design target is:

- Drop or recreate the Playwright database
- Apply migrations
- Run existing startup initialization
- Seed deterministic Playwright-specific users and domain data

The seeded data should include:

- One administrator user: `test.admin@collectibles.local`
- One regular user: `test.user@collectibles.local`
- At least one private showcase owned by the regular user
- At least one public showcase owned by the regular user
- At least one showcase owned by another seeded user for authorization testing
- Seeded items, attachments, and templates sufficient to exercise read and edit flows without requiring every test to build all data from scratch

### Storage Strategy

Playwright should use isolated local-file storage instead of sharing development uploads.

This allows tests to:

- Upload files safely
- Generate previews and derivatives without affecting development assets
- Clean up between runs
- Validate file-based features deterministically

### Fixture Assets

The roadmap should explicitly standardize on these repository fixtures:

- `Test/Example Data/Showcase Example Images/**` for attachment and image-upload scenarios
- `Test/Example Data/Showcase Example Images/ShowcaseScreenshotsBulkZipUpload.zip` for ZIP bulk-upload scenarios

These fixtures should be treated as canonical test assets, not ad hoc examples.

## Test Architecture Design

### Reusable Auth States

The suite should authenticate once per role and reuse saved auth state for most tests.

Recommended auth states:

- Anonymous default context
- Regular-user storage state
- Admin storage state

This reduces repeated login cost and improves suite stability.

### Test Organization

Tests should be organized by domain and milestone, not by one huge undifferentiated folder.

Recommended top-level organization:

- `auth/`
- `navigation/`
- `showcases/`
- `items/`
- `attachments/`
- `zip-upload/`
- `templates/`
- `sharing/`
- `qrcodes/`
- `account/`
- `admin/`
- `authorization/`

Authorization checks may live beside the relevant domain tests, but there should also be a clear inventory of denied-access scenarios.

### Test Types Inside the E2E Suite

The Playwright suite should include:

- Smoke tests for environment readiness
- Core happy-path user journeys
- CRUD coverage for major entities
- Authorization-denial tests
- Validation and failure-state tests
- Regression tests for historically risky workflows

### Assertions

Favor assertions on:

- User-visible outcomes
- Route transitions
- Presence or absence of role-relevant UI
- Persisted state after refresh/navigation
- Background-job completion visibility where the UI exposes it

Avoid over-specifying incidental markup when a more stable user-visible assertion is available.

## Milestone Rollout

### Milestone 0: Playwright Test Harness

Build the execution foundation:

- Dedicated `Playwright` environment
- Separate database and storage
- Reset-and-seed workflow
- Seeded admin and regular users
- Reusable auth states
- Basic smoke tests proving the environment is healthy

### Milestone 1: Core Logged-In User Experience

Cover the most important logged-in user flows:

- Login
- Main navigation
- My Showcases list
- Showcase creation
- Showcase detail
- Showcase editing
- Public versus private visibility behavior

### Milestone 2: Collectible Item Lifecycle

Cover the primary object model inside a showcase:

- Create item
- Edit item
- View item detail
- Parent-child hierarchy behavior
- Component relationships
- Breadcrumb and navigation behavior

### Milestone 3: Attachments and Media

Cover user-facing media workflows:

- Upload one or more files
- Use repository screenshot fixtures
- View attachment detail
- Confirm generated previews are surfaced
- Manage featured attachments and ordering
- Delete media where supported
- Use attachment list filters and navigation

### Milestone 4: ZIP Bulk Upload

Treat ZIP upload as a dedicated milestone because it is both user-facing and operationally complex:

- Upload the canonical ZIP fixture
- Observe background progress
- Validate generated item hierarchy
- Validate generated attachments
- Verify completion and resulting browse/edit behavior

### Milestone 5: Templates and Structured Data

Cover template-driven workflows:

- Template list, create, and edit
- Showcase template assignment
- Dynamic fields
- Multi-entry data flows
- Template-backed item behaviors

### Milestone 6: Sharing, Public Access, and QR Flows

Cover cross-boundary user experiences:

- Public showcase browsing
- Share-link creation and use
- Public read-only showcase views
- QR redirect behavior
- Privacy boundaries around non-shared content

### Milestone 7: Search, Filters, Tags, and Secondary User Features

Cover secondary but still user-visible discovery and organization features:

- Public browse filters
- Attachment filters
- Tag display and tag-driven navigation
- Other user-facing search and filtering experiences that depend on seeded data

### Milestone 8: In-App Account Management

Cover self-service account flows that remain entirely in-app:

- Manage account landing page
- Change password
- Personal-data pages
- Logout and relogin behavior
- Other in-app account screens that do not require email-link workflows

### Milestone 9: Admin and Operational Surfaces

Cover lower-frequency but still important admin surfaces:

- User management
- Management dashboard
- Logs and operational views
- Site configuration
- Maintenance tools
- Diagnostics
- Theme settings

## Authorization and Ownership Strategy

Authorization testing should be woven into each milestone, not deferred to the end.

Every milestone should explicitly identify:

- What anonymous users can do
- What regular users can do with their own data
- What regular users must be blocked from doing with other users' data
- What regular users must be blocked from doing on admin surfaces
- What admins are allowed to do

This should include both:

- UI-level checks such as missing navigation/actions
- Direct-route checks such as navigating to forbidden pages by URL

## Success Criteria

The roadmap is successful when:

1. The team has a master coverage document that defines what "complete Playwright coverage" means.
2. The team can implement the suite milestone by milestone without losing sight of uncovered areas.
3. Every Playwright run starts from a deterministic environment.
4. The first milestones deliver confidence in the main end-user showcase, item, media, and ZIP workflows.
5. The final roadmap clearly includes admin surfaces and authorization boundaries rather than leaving them implicit.

## Recommended Next Artifact

After this design is approved, the next step should be a detailed implementation plan for the first executable milestone sequence.

The implementation planning should begin with:

1. Milestone 0: Playwright Test Harness
2. Milestone 1: Core Logged-In User Experience
3. Milestone 2: Collectible Item Lifecycle

These three milestones establish the environment, the most important user flows, and the central domain model before expanding into deeper feature areas.
