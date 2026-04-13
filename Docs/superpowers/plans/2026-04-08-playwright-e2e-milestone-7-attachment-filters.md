# Playwright E2E Milestone 7 Attachment Filters Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend Milestone 7 with coverage for the user-facing attachments discovery surface: empty-result search, date-based filtering, and clear-filter reset behavior on `/attachments`.

**Architecture:** Reuse the existing attachment upload helpers to create a small deterministic library item for the authenticated user, then drive the attachments page through visible search, type, date, and clear controls without depending on seeded attachment counts.

**Tech Stack:** Playwright Test, TypeScript, ASP.NET Core Blazor Server, Playwright LocalDB configuration from `appsettings.Playwright.json`

---

## File Structure

- Modify: `Test/Playwright/tests/attachments/attachments-list.spec.ts`
- Modify: `agent_docs/claude/playwright-testing.md`

### Task 1: Cover Empty Search And Clear Reset

**Files:**
- Modify: `Test/Playwright/tests/attachments/attachments-list.spec.ts`

- [ ] **Step 1: Add a failing attachments filter test**

Create a test that:

- uploads a known image and document through the item-edit surface
- opens `/attachments`
- searches for a guaranteed-missing term
- verifies the `No attachments found` empty state
- clears filters and verifies the uploaded files become visible again

- [ ] **Step 2: Run the attachments spec and verify the new test fails**

Run:

```powershell
npx playwright test --project=chromium tests/attachments/attachments-list.spec.ts
```

Expected: FAIL until the selectors match the live attachments filter UI.

### Task 2: Cover Date Filtering And Reset

**Files:**
- Modify: `Test/Playwright/tests/attachments/attachments-list.spec.ts`

- [ ] **Step 1: Extend the same test with a future `Created From` value**

Use the visible date filter controls to:

- set `Created From` to a future day
- verify the empty state returns
- clear filters and verify uploaded files return
- re-apply the existing `Image` type filter and verify document results disappear

- [ ] **Step 2: Re-run the attachments spec and verify both attachment tests pass**

Run:

```powershell
npx playwright test --project=chromium tests/attachments/attachments-list.spec.ts
```

Expected: PASS for search, type, date, and clear-filter attachment coverage.

### Task 3: Update Playwright Docs

**Files:**
- Modify: `agent_docs/claude/playwright-testing.md`

- [ ] **Step 1: Document the attachment-filter slice**

Update the Playwright testing guide to mention:

- attachments-page empty-state coverage
- future-date filter coverage
- clear-filter reset behavior
- the targeted spec command for `attachments-list.spec.ts`

