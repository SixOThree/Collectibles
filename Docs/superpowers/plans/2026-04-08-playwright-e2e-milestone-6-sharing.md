# Playwright E2E Milestone 6 Sharing Public Access And QR Flows Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Playwright coverage for private showcase share-link generation and revocation, anonymous read-only public-share access, QR-code assignment, and QR redirect behavior.

**Architecture:** Reuse the existing authenticated regular-user state, showcase and item helpers, and real `/showcase/{hash}`, `/share/{token}`, `/qrcodes`, `/item/{hash}/edit`, and `/qr/{code}` pages instead of mocking link generation or QR assignment. Keep Milestone 6 in one dedicated `tests/sharing` slice: first prove tokenized access preserves private-content boundaries, then drive QR generation and redirect end to end through the same public browser surface anonymous users will see.

**Tech Stack:** Playwright Test, TypeScript, ASP.NET Core Blazor Server, existing auth storage state, browser secondary contexts for anonymous access

---

## File Structure

- Create: `Test/Playwright/tests/helpers/sharing.ts`
- Create: `Test/Playwright/tests/sharing/sharing.spec.ts`
- Modify: `agent_docs/claude/playwright-testing.md`
- Create: `Docs/superpowers/plans/2026-04-08-playwright-e2e-milestone-6-sharing.md`

### Task 1: Cover Private Share Link Creation Public Read-Only Access And Revocation

**Files:**
- Create: `Test/Playwright/tests/helpers/sharing.ts`
- Create: `Test/Playwright/tests/sharing/sharing.spec.ts`

- [ ] **Step 1: Add a failing share-link spec**

Create a test that:

- creates a private showcase and item as the regular user
- uploads at least one image and one file to the item so the public page renders attachment metadata
- opens the showcase `Share` modal
- generates a share link with a unique note
- opens that link in an anonymous browser context
- verifies the anonymous page can see the showcase, item name, and attachment filenames
- verifies the anonymous page does not get owner-only controls and cannot open the original private `/showcase/{hash}` or `/item/{hash}` routes directly
- revokes the link and verifies the shared URL now shows the public error state

- [ ] **Step 2: Run the new sharing spec and verify it fails**

Run from `Test/Playwright`:

```powershell
npx playwright test --project=chromium tests/sharing/sharing.spec.ts
```

Expected: FAIL because the sharing helper/spec do not exist yet.

- [ ] **Step 3: Implement stable share-modal helpers and assertions**

In `tests/helpers/sharing.ts`, add helpers that:

- open the real share modal from `/showcase/{hash}`
- generate a share link using a unique note so the test can find the correct row
- read the readonly share URL from the active-links table
- revoke that same row by note

In `tests/sharing/sharing.spec.ts`, keep assertions user-facing and stable:

- success message after generating a share link
- read-only public content at `/share/{token}`
- no owner edit/share controls on the public page
- `Access Denied` for direct private showcase and item routes without the share token
- `Unable to Load Showcase` after revocation

- [ ] **Step 4: Re-run the sharing spec and verify the share-link flow passes**

Run:

```powershell
npx playwright test --project=chromium tests/sharing/sharing.spec.ts
```

Expected: PASS for private share-link generation, anonymous read-only access, privacy boundaries, and revocation behavior.

### Task 2: Cover QR Generation Assignment And Redirect

**Files:**
- Modify: `Test/Playwright/tests/sharing/sharing.spec.ts`

- [ ] **Step 1: Add a failing QR redirect test**

Extend `tests/sharing/sharing.spec.ts` with a test that:

- creates a public showcase and item
- opens `/qrcodes`
- generates a new QR code batch with quantity `1`
- captures the generated code from `Recently Generated Codes`
- opens `/item/{hash}/edit`
- assigns that code through the real QR input
- opens `/qr/{code}` in an anonymous browser context
- verifies the redirect lands on the public item page
- verifies an unknown QR code shows the `QR Code Error` state

- [ ] **Step 2: Run the sharing spec and verify the QR assertions fail**

Run:

```powershell
npx playwright test --project=chromium tests/sharing/sharing.spec.ts
```

Expected: FAIL until the generated-code selectors and redirect assertions are tuned to the real UI.

- [ ] **Step 3: Tune the QR selectors and assertions until they pass**

Prefer heading-, button-, and form-label-based selectors over CSS-only hooks, and assert:

- `QR Code Management`
- generation success text
- assigned-code confirmation on the edit page
- final redirect to `/item/{hash}`
- the public item heading after redirect
- `QR Code Error` for an invalid code

- [ ] **Step 4: Re-run the sharing spec and verify both flows pass**

Run:

```powershell
npx playwright test --project=chromium tests/sharing/sharing.spec.ts
```

Expected: PASS for sharing and QR flows.

### Task 3: Update Docs And Run Milestone Verification

**Files:**
- Modify: `agent_docs/claude/playwright-testing.md`

- [ ] **Step 1: Document the sharing milestone coverage**

Update `agent_docs/claude/playwright-testing.md` to mention:

- the new `tests/sharing` folder
- private-share and QR redirect coverage
- the targeted command:

```powershell
npx playwright test --project=chromium tests/sharing
```

- [ ] **Step 2: Run Milestone 0-6 verification**

Run:

```powershell
npx playwright test --project=chromium tests/smoke tests/showcases tests/items tests/attachments tests/zip-upload tests/templates tests/sharing tests/authorization
```

Expected: PASS for the Milestone 0-6 Chromium suite.
