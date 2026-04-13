# Playwright E2E Milestone 3 Attachment And Media Coverage Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the Playwright suite with high-value attachment and media coverage for regular users, focusing on the flows users encounter while managing item pictures and files.

**Architecture:** Reuse the existing `Playwright` environment, seeded auth states, and helper patterns from Milestones 0-2. Build attachment tests around real uploads from `Test/Example Data/Showcase Example Images`, favoring stable user-visible assertions on item edit/detail pages and the global attachments page.

**Fixtures:**

- Image fixture: `Test/Example Data/Showcase Example Images/Software/lotus_123.png`
- Image fixture: `Test/Example Data/Showcase Example Images/Software/windows_31_desktop.jpg`
- Document fixture: `Test/Example Data/Showcase Example Images/README.md`
- ZIP fixture reserved for Milestone 4: `Test/Example Data/Showcase Example Images/ShowcaseScreenshotsBulkZipUpload.zip`

---

## File Structure

### Plan and docs

- Create: `Docs/superpowers/plans/2026-04-07-playwright-e2e-milestone-3-attachments.md`
- Modify: `agent_docs/claude/playwright-testing.md`

### Playwright helpers

- Create: `Test/Playwright/tests/helpers/attachments.ts`

### Playwright specs

- Create: `Test/Playwright/tests/attachments/item-attachments.spec.ts`
- Create: `Test/Playwright/tests/attachments/attachments-list.spec.ts`

## Preflight

Before Task 1:

- Confirm the worktree branch is still `feature/playwright-e2e-m0-m2`
- Confirm Milestone 0-2 tests are passing or last known green
- Run attachment-focused verification from `Test/Playwright`

```powershell
npx playwright test --project=chromium tests/attachments
```

Expected before implementation: no attachment spec files exist yet.

## Task 1: Add Attachment Fixture Helpers And Upload Coverage

**Files:**
- Create: `Test/Playwright/tests/helpers/attachments.ts`
- Create: `Test/Playwright/tests/attachments/item-attachments.spec.ts`

- [ ] **Step 1: Add reusable attachment fixture helpers**

Create `Test/Playwright/tests/helpers/attachments.ts` with helpers that:

- Resolve repository fixture paths from `Test/Example Data/Showcase Example Images`
- Return a canonical image fixture, alternate image fixture, and document fixture
- Upload files through the current item edit page by targeting the hidden file input
- Wait for visible upload completion states like the selected file names and `Uploaded`

The helper should avoid brittle CSS selectors when a stable label, role, or file name assertion is available.

- [ ] **Step 2: Add a failing item attachment lifecycle spec**

Create `Test/Playwright/tests/attachments/item-attachments.spec.ts` with tests that cover:

- Regular user can create a showcase and item, upload one image and one document, save changes, and return to item detail
- Item detail shows the attachment count after save
- The uploaded document appears in `Documents & Files`
- The uploaded image appears in `Pictures` or `Featured`

Run the new spec and confirm it fails before helper logic is complete or selectors are tuned.

- [ ] **Step 3: Complete the item attachment lifecycle coverage**

Finish the helper/spec implementation so the lifecycle test passes with stable assertions on:

- Uploaded file names
- `Uploaded` status in the edit form
- `Save Changes` returning to item detail
- Visible attachment sections on the detail page

- [ ] **Step 4: Add preview-image coverage**

In `item-attachments.spec.ts`, add a second test that:

- Uploads at least two image fixtures to an item
- Uses `Choose Preview Image`
- Selects one uploaded image in the preview modal
- Saves changes
- Verifies the selected preview persists after save and changes the user-visible item card preview on the showcase page

Keep assertions user-facing. Prefer file names, headings, and visible sections over internal IDs.

- [ ] **Step 5: Verify Task 1**

Run:

```powershell
npx playwright test --project=chromium tests/attachments/item-attachments.spec.ts
```

Expected: passing Chromium attachment lifecycle and preview tests.

## Task 2: Add Featured Toggle And Attachment Detail Coverage

**Files:**
- Modify: `Test/Playwright/tests/helpers/attachments.ts`
- Modify: `Test/Playwright/tests/attachments/item-attachments.spec.ts`

- [ ] **Step 1: Add a failing featured-toggle/detail test**

Add a test that:

- Starts from an item with uploaded images
- Toggles the star button on an image attachment from the item detail page
- Verifies the image moves into or out of the `Featured` section
- Opens an attachment detail view from the item surfaces when the UI exposes one

- [ ] **Step 2: Implement the featured/detail coverage**

Finish the test so it verifies:

- The star toggle changes visible grouping between `Pictures` and `Featured`
- Attachment detail UI exposes the uploaded file name and metadata
- The user can navigate back without breaking the page state

- [ ] **Step 3: Verify Task 2**

Run:

```powershell
npx playwright test --project=chromium tests/attachments/item-attachments.spec.ts
```

Expected: all item attachment tests pass.

## Task 3: Add Global Attachments Page Coverage

**Files:**
- Create: `Test/Playwright/tests/attachments/attachments-list.spec.ts`
- Modify: `Test/Playwright/tests/helpers/attachments.ts`

- [ ] **Step 1: Add a failing attachments list spec**

Create `attachments-list.spec.ts` with tests that:

- Build their own attachment data through the UI or reuse helper-created attachments from the same test
- Visit `/attachments`
- Verify the page loads for an authenticated regular user
- Search by uploaded filename
- Filter by `Image` attachment type
- Switch between `Thumbnails` and `Details`

- [ ] **Step 2: Add detail-view coverage from the attachments page**

Add assertions that:

- Opening an attachment detail view shows the uploaded file name
- The detail UI exposes content type or category metadata
- The list remains usable after closing the modal or returning from detail

- [ ] **Step 3: Verify Task 3**

Run:

```powershell
npx playwright test --project=chromium tests/attachments/attachments-list.spec.ts
```

Expected: passing Chromium attachments-list coverage.

## Task 4: Update Documentation And Run Full Milestone Verification

**Files:**
- Modify: `agent_docs/claude/playwright-testing.md`

- [ ] **Step 1: Document the new attachment/media coverage**

Update `agent_docs/claude/playwright-testing.md` to mention:

- The new `tests/attachments` folder
- The canonical screenshot/document fixtures used by Milestone 3
- The recommended command to run attachment-only tests

- [ ] **Step 2: Run full milestone verification**

Run:

```powershell
npx playwright test --project=chromium tests/smoke tests/showcases tests/items tests/attachments tests/authorization
```

Expected: the Milestone 0-3 Chromium suite passes.

