import { expect, Locator, Page, test } from '@playwright/test';
import { authFile } from '../helpers/auth';
import { uniqueName } from '../helpers/names';
import { createShowcase } from '../helpers/showcases';
import { addTemplateField, createShowcaseTemplate } from '../helpers/templates';

test.use({ storageState: authFile('user') });

test('regular user can create and edit a showcase template', async ({ page }) => {
  const showcaseName = uniqueName('PW Template Showcase');
  await createShowcase(page, showcaseName, true);

  const templateName = uniqueName('PW Card Template');
  await createShowcaseTemplate(page, {
    showcaseName,
    templateName,
    fields: [
      {
        name: 'manufacturer',
        label: 'Manufacturer',
        required: true,
        placeholder: 'Enter the manufacturer',
      },
    ],
  });

  const createdCard = getTemplateCard(page, templateName);
  await expect(createdCard).toContainText('1 field');

  await createdCard.getByRole('button', { name: 'Edit' }).click();
  await expect(page.getByRole('heading', { name: 'Edit Template' })).toBeVisible({ timeout: 15000 });

  const updatedDescription = `${templateName} updated description`;
  await page.getByLabel(/^Description$/).fill(updatedDescription);
  await addTemplateField(page, {
    name: 'series',
    label: 'Series',
  });

  await page.getByRole('button', { name: 'Save Changes' }).click();
  await expect(page).toHaveURL(/\/templates$/);

  const updatedCard = getTemplateCard(page, templateName);
  await expect(updatedCard).toContainText(updatedDescription);
  await expect(updatedCard).toContainText('2 fields');
});

test('regular user can use a showcase template on an item and review it in template items', async ({ page }) => {
  const showcaseName = uniqueName('PW Structured Showcase');
  const showcaseHash = await createShowcase(page, showcaseName, true);
  const templateName = uniqueName('PW Structured Template');
  const itemName = uniqueName('PW Structured Item');

  await createShowcaseTemplate(page, {
    showcaseName,
    templateName,
    fields: [
      {
        name: 'manufacturer',
        label: 'Manufacturer',
        required: true,
      },
      {
        name: 'releaseYear',
        label: 'Release Year',
        type: 'number',
      },
      {
        name: 'condition',
        label: 'Condition',
        type: 'dropdown',
        options: ['Mint', 'Played'],
      },
    ],
  });

  await page.goto(`/showcase/${showcaseHash}/item/new`, { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Add New Collectible Item' })).toBeVisible({ timeout: 15000 });

  await page.getByLabel('Template').selectOption({ label: templateName });
  await page.getByLabel(/^Name/).fill(itemName);
  await page.getByLabel('Description').fill(`${itemName} description`);
  await page.getByLabel('Manufacturer').fill('Nintendo');
  await page.getByLabel('Release Year').fill('1996');
  await selectLabeledDropdownOption(page, 'Condition', 'Mint');
  await page.getByRole('button', { name: 'Create Item' }).click();

  await expect(page).toHaveURL(new RegExp(`/showcase/${showcaseHash}$`));
  await page.getByText(itemName, { exact: true }).click();
  await expect(page.getByRole('heading', { name: itemName })).toBeVisible({ timeout: 15000 });

  await expect(page.locator('body')).toContainText('Template:');
  await expect(page.locator('body')).toContainText(templateName);
  await expect(page.locator('body')).toContainText('Manufacturer');
  await expect(page.locator('body')).toContainText('Nintendo');
  await expect(page.locator('body')).toContainText('Release Year');
  await expect(page.locator('body')).toContainText('1996');
  await expect(page.locator('body')).toContainText('Condition');
  await expect(page.locator('body')).toContainText('Mint');

  await page.getByRole('link', { name: 'Edit' }).click();
  await expect(page).toHaveURL(/\/item\/[^/]+\/edit$/);

  await page.getByLabel('Release Year').fill('1998');
  await selectLabeledDropdownOption(page, 'Condition', 'Played');
  await page.getByRole('button', { name: 'Save Changes' }).click();

  await expect(page.getByRole('heading', { name: itemName })).toBeVisible({ timeout: 15000 });
  await expect(page.locator('body')).toContainText('1998');
  await expect(page.locator('body')).toContainText('Played');

  await page.goto(`/showcase/${showcaseHash}`, { waitUntil: 'domcontentloaded' });
  await page.getByRole('button', { name: 'Template Items' }).click();
  await expect(page.getByRole('heading', { name: 'Template Items' })).toBeVisible({ timeout: 15000 });

  const templateFilter = page.locator('.filters-section .col-md-3').locator('select');
  await expect(templateFilter.locator('option').nth(1)).toContainText(templateName);
  await templateFilter.selectOption({ index: 1 });
  await page.getByRole('button', { name: 'Search' }).click();

  const table = page.getByRole('table');
  await expect(table).toContainText(itemName);
  await expect(table).toContainText('Nintendo');
  await expect(table).toContainText('1,998');
  await expect(table).toContainText('Played');
});

test('regular user can create and edit a multi-entry template item', async ({ page }) => {
  const showcaseName = uniqueName('PW Multi Entry Showcase');
  const showcaseHash = await createShowcase(page, showcaseName, true);
  const templateName = uniqueName('PW Multi Entry Template');
  const itemName = uniqueName('PW Multi Entry Item');

  await createShowcaseTemplate(page, {
    showcaseName,
    templateName,
    allowMultipleEntries: true,
    fields: [
      {
        name: 'issue',
        label: 'Issue',
        required: true,
      },
      {
        name: 'format',
        label: 'Format',
        type: 'dropdown',
        options: ['DVD', 'Blu-ray'],
      },
      {
        name: 'notes',
        label: 'Notes',
      },
    ],
  });

  await page.goto(`/showcase/${showcaseHash}/item/new`, { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Add New Collectible Item' })).toBeVisible({ timeout: 15000 });

  await page.getByLabel('Template').selectOption({ label: templateName });
  await page.getByLabel(/^Name/).fill(itemName);
  await page.getByLabel('Description').fill(`${itemName} description`);

  const multiEntryEditor = page.locator('.multi-entry-editor');
  await expect(multiEntryEditor).toBeVisible({ timeout: 15000 });
  await multiEntryEditor.getByRole('button', { name: 'Add Entry' }).click();
  await multiEntryEditor.getByRole('button', { name: 'Add Entry' }).click();
  await expect(multiEntryEditor.locator('tbody tr')).toHaveCount(2);

  await fillMultiEntryCell(multiEntryEditor, 0, 'Issue', 'Issue One');
  await fillMultiEntryCell(multiEntryEditor, 0, 'Format', 'DVD');
  await fillMultiEntryCell(multiEntryEditor, 0, 'Notes', 'Shelf A');
  await fillMultiEntryCell(multiEntryEditor, 1, 'Issue', 'Issue Two');
  await fillMultiEntryCell(multiEntryEditor, 1, 'Format', 'Blu-ray');
  await fillMultiEntryCell(multiEntryEditor, 1, 'Notes', 'Shelf B');

  await page.getByRole('button', { name: 'Create Item' }).click();

  await expect(page).toHaveURL(new RegExp(`/showcase/${showcaseHash}$`));
  await page.getByText(itemName, { exact: true }).click();
  await expect(page.getByRole('heading', { name: itemName })).toBeVisible({ timeout: 15000 });
  await expect(page.locator('body')).toContainText(`${templateName} Entries`);
  await expect(page.locator('body')).toContainText('2 entries');
  await expect(page.locator('body')).toContainText('Issue One');
  await expect(page.locator('body')).toContainText('Issue Two');
  await expect(page.locator('body')).toContainText('Shelf A');
  await expect(page.locator('body')).toContainText('Shelf B');
  await expect(page.locator('body')).toContainText('Blu-ray');

  await page.getByRole('link', { name: 'Edit' }).click();
  await expect(page).toHaveURL(/\/item\/[^/]+\/edit$/);
  await expect(page.locator('.multi-entry-editor tbody tr')).toHaveCount(2);

  await fillMultiEntryCell(page.locator('.multi-entry-editor'), 1, 'Format', 'DVD');
  await fillMultiEntryCell(page.locator('.multi-entry-editor'), 1, 'Notes', 'Shelf C');
  await page.getByRole('button', { name: 'Save Changes' }).click();

  await expect(page.getByRole('heading', { name: itemName })).toBeVisible({ timeout: 15000 });
  await expect(page.locator('body')).toContainText('Shelf C');
  await expect(page.locator('body')).not.toContainText('Shelf B');

  await page.goto(`/showcase/${showcaseHash}`, { waitUntil: 'domcontentloaded' });
  await page.getByRole('button', { name: 'Template Items' }).click();
  await expect(page.getByRole('heading', { name: 'Template Items' })).toBeVisible({ timeout: 15000 });

  const templateFilter = page.locator('.filters-section .col-md-3').locator('select');
  await expect(templateFilter.locator('option').nth(1)).toContainText(templateName);
  await templateFilter.selectOption({ index: 1 });
  await page.getByRole('button', { name: 'Search' }).click();

  const table = page.getByRole('table');
  await expect(table.locator('tbody tr')).toHaveCount(2);
  await expect(table).toContainText('Issue One');
  await expect(table).toContainText('Issue Two');
  await expect(table).toContainText('Shelf A');
  await expect(table).toContainText('Shelf C');
});

function getTemplateCard(page: Page, templateName: string): Locator {
  return page.locator('.template-card').filter({ hasText: templateName }).first();
}

async function fillMultiEntryCell(editor: Locator, rowIndex: number, columnLabel: string, value: string): Promise<void> {
  const columnIndex = await getMultiEntryColumnIndex(editor, columnLabel);
  const cell = editor.locator('tbody tr').nth(rowIndex).locator('td').nth(columnIndex);

  const select = cell.locator('select');
  if ((await select.count()) > 0) {
    await select.selectOption({ label: value });
    return;
  }

  const textInput = cell.locator('input:not([type="checkbox"]), textarea');
  if ((await textInput.count()) > 0) {
    await textInput.fill(value);
    return;
  }

  throw new Error(`Unable to find editable control for column "${columnLabel}" in row ${rowIndex + 1}.`);
}

async function getMultiEntryColumnIndex(editor: Locator, columnLabel: string): Promise<number> {
  const headers = editor.locator('thead th');
  const headerCount = await headers.count();

  for (let index = 0; index < headerCount; index += 1) {
    const text = (await headers.nth(index).innerText()).trim();
    if (text.includes(columnLabel)) {
      return index;
    }
  }

  throw new Error(`Unable to find multi-entry column "${columnLabel}".`);
}

async function selectLabeledDropdownOption(page: Page, label: string, optionText: string): Promise<void> {
  const dropdown = page.getByLabel(label);
  await expect(dropdown.locator('option').filter({ hasText: optionText })).toHaveCount(1);
  await dropdown.selectOption({ label: optionText });
}
