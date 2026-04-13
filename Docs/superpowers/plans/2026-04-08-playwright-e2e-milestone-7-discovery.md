# Playwright E2E Milestone 7 Discovery Filters And Tags Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Playwright coverage for the Milestone 7 discovery slice: public browse visibility, showcase item search, tag-driven item filtering, and filter reset behavior.

**Architecture:** Reuse the existing seeded public/private showcase data for anonymous browse assertions, then create a focused tagged showcase through the regular-user UI so search and tag filtering can be asserted against exact item names without depending on hidden seed details. Keep the first Milestone 7 slice in a dedicated `tests/discovery` area with one helper file only if tag assignment or repeated setup logic becomes noisy.

**Tech Stack:** Playwright Test, TypeScript, ASP.NET Core Blazor Server, existing auth storage state, shell-only `HashIds__Salt` override for local Playwright execution

---

## File Structure

- Create: `Test/Playwright/tests/discovery/discovery.spec.ts`
- Create: `Test/Playwright/tests/helpers/discovery.ts`
- Modify: `agent_docs/claude/playwright-testing.md`
- Create: `Docs/superpowers/plans/2026-04-08-playwright-e2e-milestone-7-discovery.md`

### Task 1: Cover Public Browse Visibility And Navigation

**Files:**
- Create: `Test/Playwright/tests/discovery/discovery.spec.ts`

- [ ] **Step 1: Write the failing anonymous discovery spec**

Add a test that opens `/showcases/public` and verifies:

- the seeded public showcase is visible
- the seeded private showcase is absent
- following the public showcase link lands on the correct showcase detail page
- anonymous users can browse visible item cards there without owner-only controls

Use the seeded manifest rather than hard-coded names.

- [ ] **Step 2: Run the discovery spec and verify it fails**

Run from `Test/Playwright` with a shell-only salt override:

```powershell
$env:HashIds__Salt='playwright-local-salt-m7-2026-04-08'
npx playwright test --project=chromium tests/discovery/discovery.spec.ts
```

Expected: FAIL because the discovery spec does not exist yet.

- [ ] **Step 3: Implement stable public browse assertions**

In `tests/discovery/discovery.spec.ts`, keep assertions user-facing and stable:

- the public browse list shows the seeded public showcase name
- the public showcase detail heading is visible after navigation
- owner-only buttons such as `Edit` and `Share` are absent for anonymous users

- [ ] **Step 4: Re-run the discovery spec and verify the public browse test passes**

Run:

```powershell
$env:HashIds__Salt='playwright-local-salt-m7-2026-04-08'
npx playwright test --project=chromium tests/discovery/discovery.spec.ts
```

Expected: PASS for the anonymous public-browse slice.

### Task 2: Cover Showcase Search Tag Filters And Reset Behavior

**Files:**
- Create: `Test/Playwright/tests/helpers/discovery.ts`
- Modify: `Test/Playwright/tests/discovery/discovery.spec.ts`

- [ ] **Step 1: Add a failing tagged-showcase discovery test**

Create a regular-user test that:

- creates a private showcase
- creates at least three items with distinct names
- assigns overlapping tags so one item matches `Hardware`, one matches `Software`, and one matches both or neither as needed
- uses the showcase search box and tag buttons on `/showcase/{hash}`
- verifies filtered results shrink to the expected visible cards
- clears filters and verifies the full item set returns

- [ ] **Step 2: Run the discovery spec and verify the new assertions fail**

Run:

```powershell
$env:HashIds__Salt='playwright-local-salt-m7-2026-04-08'
npx playwright test --project=chromium tests/discovery/discovery.spec.ts
```

Expected: FAIL until the tag-assignment helper and showcase filter selectors match the real UI.

- [ ] **Step 3: Implement helper-backed tagged item setup and stable filter assertions**

In `tests/helpers/discovery.ts`, add only the setup helpers needed to:

- create items in a showcase
- open item edit screens when tag assignment is required
- add tags through the real tag selector in a repeatable way

In `tests/discovery/discovery.spec.ts`, prefer:

- `Search items…` textbox assertions
- visible item-card text assertions
- `tag-button` click behavior via accessible text
- `Clear filters` reset assertions
- the `No items match your current filters.` empty state where useful

- [ ] **Step 4: Re-run the discovery spec and verify both tests pass**

Run:

```powershell
$env:HashIds__Salt='playwright-local-salt-m7-2026-04-08'
npx playwright test --project=chromium tests/discovery/discovery.spec.ts
```

Expected: PASS for public browse and showcase discovery filters.

### Task 3: Update Docs And Run Milestone Verification

**Files:**
- Modify: `agent_docs/claude/playwright-testing.md`

- [ ] **Step 1: Document the discovery milestone slice**

Update `agent_docs/claude/playwright-testing.md` to mention:

- the new `tests/discovery` folder
- public browse and showcase filter coverage
- the local note that current Playwright runs require a shell-only `HashIds__Salt` override until environment configuration is updated
- the targeted command:

```powershell
$env:HashIds__Salt='playwright-local-salt-m7-2026-04-08'
npx playwright test --project=chromium tests/discovery
```

- [ ] **Step 2: Run Milestone 0-7 verification**

Run:

```powershell
$env:HashIds__Salt='playwright-local-salt-m7-2026-04-08'
npx playwright test --project=chromium tests/smoke tests/showcases tests/items tests/attachments tests/zip-upload tests/templates tests/sharing tests/discovery tests/authorization
```

Expected: PASS for the Milestone 0-7 Chromium suite.
