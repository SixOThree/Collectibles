# Playwright E2E Milestone 5 Templates And Structured Data Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Playwright coverage for template CRUD, showcase-scoped template assignment, template-backed item editing, and the structured-data surfaces that render those values back to the user.

**Architecture:** Reuse the existing regular-user auth state plus the showcase and naming helpers, then drive the real `/templates`, `/templates/new`, `/templates/{id}/edit`, `/showcase/{hash}/item/new`, `/item/{hash}/edit`, and `/showcase/{hash}/templates/items` pages through the shared `TemplateSelector`, `DynamicFieldsEditor`, `MultiEntryEditor`, and display components. Start with a single-entry dynamic-fields slice that verifies create, edit, detail rendering, and template-items table behavior; then extend the same helper coverage to multi-entry templates in a follow-up task.

**Tech Stack:** Playwright Test, TypeScript, ASP.NET Core Blazor Server, shared template components, local Playwright storage

---

## File Structure

- Create: `Test/Playwright/tests/templates/templates.spec.ts`
- Create: `Test/Playwright/tests/helpers/templates.ts`
- Modify: `agent_docs/claude/playwright-testing.md`
- Create: `Docs/superpowers/plans/2026-04-08-playwright-e2e-milestone-5-templates.md`

### Task 1: Add Template CRUD Coverage

**Files:**
- Create: `Test/Playwright/tests/helpers/templates.ts`
- Create: `Test/Playwright/tests/templates/templates.spec.ts`

- [ ] **Step 1: Write the failing template CRUD spec**

Create `Test/Playwright/tests/templates/templates.spec.ts` with a test that:

```ts
test('regular user can create and edit a showcase template', async ({ page }) => {
  const showcaseName = uniqueName('PW Template Showcase');
  const showcaseHash = await createShowcase(page, showcaseName, true);
  const templateName = uniqueName('PW Card Template');

  await page.goto('/templates/new', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Create New Template' })).toBeVisible();

  await page.getByLabel(/Template Name/).fill(templateName);
  await page.getByLabel(/Description/).fill(`${templateName} description`);
  await page.getByLabel(/Select Showcase/).selectOption({ label: showcaseName });
  await page.getByRole('button', { name: 'Add Field' }).click();
  await page.getByLabel('Field Name').fill('manufacturer');
  await page.getByLabel('Display Label').fill('Manufacturer');
  await page.getByRole('button', { name: 'Create Template' }).click();

  await expect(page).toHaveURL('/templates');
  await expect(page.getByText(templateName, { exact: true })).toBeVisible();
});
```

- [ ] **Step 2: Run the template spec and verify it fails**

Run from `Test/Playwright`:

```powershell
npx playwright test --project=chromium tests/templates/templates.spec.ts
```

Expected: FAIL because the `templates` spec and helper do not exist yet.

- [ ] **Step 3: Implement the helper and stable CRUD assertions**

Create `Test/Playwright/tests/helpers/templates.ts` with a helper that:

- navigates to `/templates/new`
- fills the showcase-scoped template form
- supports adding text and dropdown fields
- returns the created template name so later tests can reuse it

In `tests/templates/templates.spec.ts`, assert:

- the created template appears on `/templates`
- the card shows the field count
- the user can open `Edit`
- editing the template updates the description and adds a second field

- [ ] **Step 4: Re-run the CRUD spec and verify it passes**

Run:

```powershell
npx playwright test --project=chromium tests/templates/templates.spec.ts
```

Expected: PASS for template create/list/edit coverage.

### Task 2: Cover Single-Entry Dynamic Fields Across Item Create, Edit, Detail, And Template Items

**Files:**
- Modify: `Test/Playwright/tests/helpers/templates.ts`
- Modify: `Test/Playwright/tests/templates/templates.spec.ts`

- [ ] **Step 1: Add a failing template-backed item test**

Extend `Test/Playwright/tests/templates/templates.spec.ts` with a test that:

```ts
test('regular user can use a showcase template on an item and review it in template items', async ({ page }) => {
  const showcaseName = uniqueName('PW Structured Showcase');
  const showcaseHash = await createShowcase(page, showcaseName, true);
  const templateName = uniqueName('PW Structured Template');

  await createShowcaseTemplate(page, {
    showcaseName,
    templateName,
    fields: [
      { name: 'manufacturer', label: 'Manufacturer', type: 'Text', required: true },
      { name: 'condition', label: 'Condition', type: 'Dropdown', options: ['Mint', 'Played'] },
      { name: 'sealed', label: 'Sealed', type: 'Boolean' },
    ],
  });

  await page.goto(`/showcase/${showcaseHash}/item/new`);
  await page.getByLabel('Template').selectOption({ label: templateName });
  await page.getByLabel(/^Name/).fill(itemName);
  await page.getByLabel('Manufacturer').fill('Nintendo');
  await page.getByLabel('Condition').selectOption('Mint');
  await page.getByLabel('Sealed').check();
  await page.getByRole('button', { name: 'Create Item' }).click();

  await expect(page.getByText('Template:')).toBeVisible();
  await expect(page.getByText(templateName, { exact: true })).toBeVisible();
  await expect(page.getByText('Nintendo')).toBeVisible();
  await expect(page.getByText('Mint')).toBeVisible();

  await page.getByRole('link', { name: 'Edit' }).click();
  await page.getByLabel('Condition').selectOption('Played');
  await page.getByRole('button', { name: 'Save Changes' }).click();

  await page.goto(`/showcase/${showcaseHash}`);
  await page.getByRole('button', { name: 'Template Items' }).click();
  await page.getByLabel('Template').selectOption({ label: templateName });
  await page.getByRole('button', { name: 'Search' }).click();
  await expect(page.getByRole('table')).toContainText(itemName);
  await expect(page.getByRole('table')).toContainText('Played');
});
```

- [ ] **Step 2: Run the template spec and verify the new assertions fail**

Run:

```powershell
npx playwright test --project=chromium tests/templates/templates.spec.ts
```

Expected: FAIL until the shared helper and selectors match the dynamic field UI.

- [ ] **Step 3: Make the single-entry flow pass**

Keep the assertions user-facing and stable:

- the item form exposes dynamic fields after template selection
- the item detail page shows the template name and structured values
- editing the item updates the rendered values
- the showcase `Template Items` table lists the created item with its structured data

- [ ] **Step 4: Re-run the template spec and verify it passes**

Run:

```powershell
npx playwright test --project=chromium tests/templates/templates.spec.ts
```

Expected: PASS for both template CRUD and single-entry structured-data coverage.

### Task 3: Cover Multi-Entry Template Data

**Files:**
- Modify: `Test/Playwright/tests/helpers/templates.ts`
- Modify: `Test/Playwright/tests/templates/templates.spec.ts`

- [ ] **Step 1: Add a failing multi-entry template test**

Extend the helper and spec so a test creates a template with `Allow Multiple Entries` enabled, then:

- selects that template on the new-item page
- adds at least two entries with different values through `MultiEntryEditor`
- saves the item
- verifies item detail renders `MultiEntryDisplay`
- verifies `Template Items` shows one row per entry or the entry badge/entry count surface

- [ ] **Step 2: Run the template spec and verify the multi-entry assertions fail**

Run:

```powershell
npx playwright test --project=chromium tests/templates/templates.spec.ts
```

Expected: FAIL until the multi-entry controls and assertions are tuned.

- [ ] **Step 3: Tune the multi-entry selectors and assertions until they pass**

Prefer row- and label-based selectors over CSS-only hooks, and assert:

- `Add Entry`
- required-entry validation or visible entry rows
- rendered entry count in detail/table views

- [ ] **Step 4: Re-run the template spec and verify it passes**

Run:

```powershell
npx playwright test --project=chromium tests/templates/templates.spec.ts
```

Expected: PASS for template CRUD, single-entry structured data, and multi-entry coverage.

### Task 4: Update Docs And Run Milestone Verification

**Files:**
- Modify: `agent_docs/claude/playwright-testing.md`

- [ ] **Step 1: Document the template milestone coverage**

Update `agent_docs/claude/playwright-testing.md` to mention:

- the new `tests/templates` folder
- template CRUD and structured-data coverage
- the targeted command:

```powershell
npx playwright test --project=chromium tests/templates
```

- [ ] **Step 2: Run Milestone 0-5 verification**

Run:

```powershell
npx playwright test --project=chromium tests/smoke tests/showcases tests/items tests/attachments tests/zip-upload tests/templates tests/authorization
```

Expected: PASS for the Milestone 0-5 Chromium suite.
