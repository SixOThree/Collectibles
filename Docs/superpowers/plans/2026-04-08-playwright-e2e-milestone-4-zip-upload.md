# Playwright E2E Milestone 4 ZIP Upload Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Playwright coverage for the ZIP bulk-upload journey so the suite verifies upload, background job completion, generated item hierarchy, and imported attachments from the canonical ZIP fixture.

**Architecture:** Reuse the existing regular-user auth state, showcase-creation helpers, and Playwright environment. Drive the real `/zip-upload-bg-simple` UI with the repository ZIP fixture, wait on user-visible background-job progress and completion states, then assert the imported top-level items, child hierarchy, and attachment surfaces through the resulting showcase and item pages.

**Tech Stack:** Playwright Test, TypeScript, ASP.NET Core Blazor Server, background zip-processing jobs, local Playwright storage

---

## File Structure

- Create: `Test/Playwright/tests/zip-upload/zip-upload.spec.ts`
- Modify: `agent_docs/claude/playwright-testing.md`
- Create: `Docs/superpowers/plans/2026-04-08-playwright-e2e-milestone-4-zip-upload.md`

### Task 1: Cover ZIP Upload Queueing And Completion

**Files:**
- Create: `Test/Playwright/tests/zip-upload/zip-upload.spec.ts`

- [ ] **Step 1: Write the failing ZIP upload spec**

Create `Test/Playwright/tests/zip-upload/zip-upload.spec.ts` with a test that:

```ts
test('regular user can upload the canonical ZIP fixture and see the completed job', async ({ page }) => {
  const showcaseHash = await createShowcase(page, uniqueName('PW ZIP Showcase'), true);

  await page.goto(`/showcase/${showcaseHash}`);
  await page.getByRole('button', { name: 'Zip Upload' }).click();
  await expect(page.getByRole('heading', { name: /Zip Upload/ })).toBeVisible();

  await page.getByLabel('Select Zip File').setInputFiles(zipFixturePath);
  await page.getByRole('button', { name: 'Upload & Start Processing' }).click();

  await expect(page.getByRole('alert')).toContainText('Processing has started in the background.');
  await expect(page.getByText('ShowcaseScreenshotsBulkZipUpload.zip')).toBeVisible({ timeout: 180000 });
  await expect(page.getByText('Success')).toBeVisible({ timeout: 180000 });
});
```

- [ ] **Step 2: Run the ZIP upload spec and verify it fails**

Run from `Test/Playwright`:

```powershell
npx playwright test --project=chromium tests/zip-upload/zip-upload.spec.ts
```

Expected: FAIL because the ZIP upload spec does not exist yet.

- [ ] **Step 3: Make the ZIP upload completion spec pass**

Implement the real spec with:

```ts
const zipFixturePath = path.resolve(__dirname, '../../../Test/Example Data/Showcase Example Images/ShowcaseScreenshotsBulkZipUpload.zip');
```

Use stable assertions on:

- `Zip Upload (Background Processing)` heading
- the success alert after upload
- the completed-jobs table row for `ShowcaseScreenshotsBulkZipUpload.zip`
- the `Success` badge and `View` button

- [ ] **Step 4: Re-run the ZIP upload spec and verify it passes**

Run:

```powershell
npx playwright test --project=chromium tests/zip-upload/zip-upload.spec.ts
```

Expected: PASS for the upload-and-complete journey.

### Task 2: Assert Imported Hierarchy And Attachment Surfaces

**Files:**
- Modify: `Test/Playwright/tests/zip-upload/zip-upload.spec.ts`

- [ ] **Step 1: Add failing hierarchy and attachment assertions**

Extend the ZIP upload spec so that after clicking `View` from the completed job it asserts:

```ts
await expect(page.getByRole('heading', { name: showcaseName })).toBeVisible();
await expect(page.getByText('Computers', { exact: true })).toBeVisible();
await expect(page.getByText('Software', { exact: true })).toBeVisible();
await expect(page.getByText('Video Games', { exact: true })).toBeVisible();

await page.getByText('Computers', { exact: true }).click();
await expect(page.getByRole('heading', { name: 'Computers' })).toBeVisible();
await expect(page.getByRole('heading', { name: 'Related Items' })).toBeVisible();
await expect(page.getByText('Apple', { exact: true })).toBeVisible();

await page.goto(`/showcase/${showcaseHash}`);
await page.getByText('Software', { exact: true }).click();
await expect(page.getByRole('heading', { name: 'Software' })).toBeVisible();
await expect(page.getByText('lotus_123.png', { exact: true })).toBeVisible();
await expect(page.getByRole('img', { name: /windows_31_desktop\.jpg/i })).toBeVisible();
```

- [ ] **Step 2: Run the spec and verify it fails for the new assertions**

Run:

```powershell
npx playwright test --project=chromium tests/zip-upload/zip-upload.spec.ts
```

Expected: FAIL until the selectors and waits match the imported showcase behavior.

- [ ] **Step 3: Tune the hierarchy and attachment assertions until they pass**

Keep the assertions user-facing:

- top-level imported items visible on the showcase page
- child hierarchy exposed under `Related Items`
- imported software attachments visible on the item detail page

- [ ] **Step 4: Re-run the ZIP upload spec and verify it passes**

Run:

```powershell
npx playwright test --project=chromium tests/zip-upload/zip-upload.spec.ts
```

Expected: PASS for both the background completion and imported-data verification.

### Task 3: Update Docs And Run Milestone Verification

**Files:**
- Modify: `agent_docs/claude/playwright-testing.md`

- [ ] **Step 1: Document the ZIP milestone coverage**

Update `agent_docs/claude/playwright-testing.md` to mention:

- the new `tests/zip-upload` folder
- the canonical fixture `Test/Example Data/Showcase Example Images/ShowcaseScreenshotsBulkZipUpload.zip`
- the targeted command:

```powershell
npx playwright test --project=chromium tests/zip-upload
```

- [ ] **Step 2: Run Milestone 0-4 verification**

Run:

```powershell
npx playwright test --project=chromium tests/smoke tests/showcases tests/items tests/attachments tests/zip-upload tests/authorization
```

Expected: PASS for the Milestone 0-4 Chromium suite.
