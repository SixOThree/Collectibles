import { test, expect, Page } from '@playwright/test';
import { authFile } from '../helpers/auth';
import { uniqueName } from '../helpers/names';
import { createShowcase } from '../helpers/showcases';
import {
  addTagToOpenTagSelector,
  canonicalImageFixture,
  createItemForAttachments,
  documentFixture,
  expectUploadedFileNames,
  uploadItemAttachments,
} from '../helpers/attachments';

test.use({ storageState: authFile('user') });

function formatDateForInput(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');

  return `${year}-${month}-${day}`;
}

async function createAttachmentData(page: Page): Promise<void> {
  const showcaseHash = await createShowcase(page, uniqueName('PW Library Showcase'), true);
  const itemName = uniqueName('PW Library Item');

  await createItemForAttachments(page, showcaseHash, itemName);
  await page.getByText(itemName, { exact: true }).click();
  await expect(page).toHaveURL(/\/item\/[^/]+/);
  await expect(page.getByRole('heading', { name: itemName })).toBeVisible({ timeout: 15000 });

  await page.getByRole('link', { name: 'Edit' }).click();
  await expect(page).toHaveURL(/\/item\/[^/]+\/edit$/);
  await expect(page.getByRole('heading', { name: 'Edit Collectible Item' })).toBeVisible({ timeout: 15000 });

  await uploadItemAttachments(page, [canonicalImageFixture(), documentFixture()]);
  await expectUploadedFileNames(page, ['lotus_123.png', 'README.md']);

  await page.getByRole('button', { name: 'Save Changes' }).click();
  await expect(page).toHaveURL(/\/item\/[^/]+$/);
}

test('regular user can search filter and inspect attachment details from the attachments page', async ({ page }) => {
  await createAttachmentData(page);

  await page.goto('/attachments', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Attachments' })).toBeVisible({ timeout: 15000 });

  const searchBox = page.getByPlaceholder('Search by name or filename...');
  await searchBox.click();
  await searchBox.pressSequentially('README.md');

  const readmeResult = page.getByRole('img', { name: 'README.md' });
  await expect(readmeResult.first()).toBeVisible({ timeout: 15000 });
  await readmeResult.first().click();

  await expect(page.getByText('Original File:', { exact: true })).toBeVisible({ timeout: 15000 });
  await expect(page.getByRole('heading', { name: 'README.md' })).toBeVisible({ timeout: 15000 });
  await expect(page.getByTitle('README.md')).toBeVisible({ timeout: 15000 });
  await expect(page.getByText('Category:', { exact: true })).toBeVisible({ timeout: 15000 });
  await page.getByRole('button', { name: 'Close' }).click();
  await expect(page.getByText('Original File:', { exact: true })).not.toBeVisible({ timeout: 15000 });

  await page.getByRole('button', { name: 'Details' }).click();
  await expect(page.getByRole('button', { name: 'View' }).first()).toBeVisible({ timeout: 15000 });

  await searchBox.click();
  await page.keyboard.press('Control+A');
  await page.keyboard.press('Backspace');
  await page.waitForTimeout(500);

  await page.getByRole('combobox').selectOption({ label: 'Image' });
  await page.getByRole('button', { name: 'Thumbnails' }).click();

  const imageResult = page.getByRole('img', { name: /lotus_123\.png/i });
  await expect(imageResult.first()).toBeVisible({ timeout: 15000 });
  await imageResult.first().click();

  await expect(page.getByText('Original File:', { exact: true })).toBeVisible({ timeout: 15000 });
  await expect(page.getByRole('heading', { name: 'lotus_123.png' })).toBeVisible({ timeout: 15000 });
  await expect(page.getByTitle('lotus_123.png')).toBeVisible({ timeout: 15000 });
  await expect(page.getByText('image/png', { exact: true })).toBeVisible({ timeout: 15000 });
});

test('regular user can clear attachment filters after empty search and future date constraints', async ({ page }) => {
  await createAttachmentData(page);

  await page.goto('/attachments', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Attachments' })).toBeVisible({ timeout: 15000 });

  const searchBox = page.getByPlaceholder('Search by name or filename...');
  const clearButton = page.getByRole('button', { name: /Clear/ });
  const noResults = page.getByText('No attachments found');
  const createdFromInput = page
    .locator('div')
    .filter({ has: page.getByText('Created From', { exact: true }) })
    .locator('input[type="date"]')
    .first();

  await searchBox.click();
  await searchBox.pressSequentially(uniqueName('PW Missing Attachment'));
  await expect(noResults).toBeVisible({ timeout: 15000 });

  await clearButton.click();
  await expect(noResults).not.toBeVisible({ timeout: 15000 });
  await page.getByRole('button', { name: 'Details' }).click();
  await expect(page.getByRole('img', { name: /README\.md/i }).first()).toBeVisible({ timeout: 15000 });
  await expect(page.getByRole('img', { name: /lotus_123\.png/i }).first()).toBeVisible({ timeout: 15000 });

  const tomorrow = new Date();
  tomorrow.setDate(tomorrow.getDate() + 1);

  await createdFromInput.fill(formatDateForInput(tomorrow));
  await createdFromInput.press('Tab');
  await expect(noResults).toBeVisible({ timeout: 15000 });

  await clearButton.click();
  await expect(noResults).not.toBeVisible({ timeout: 15000 });

  await page.getByRole('combobox').selectOption({ label: 'Image' });
  await expect(page.getByRole('img', { name: /lotus_123\.png/i }).first()).toBeVisible({ timeout: 15000 });
  await expect(page.getByRole('img', { name: /README\.md/i })).toHaveCount(0);

  await clearButton.click();
  await expect(page.getByRole('img', { name: /README\.md/i }).first()).toBeVisible({ timeout: 15000 });
});

test('regular user can tag an attachment from the modal and review the tag on the detail page', async ({ page }) => {
  await createAttachmentData(page);

  const attachmentTagName = uniqueName('PW Attachment Tag');

  await page.goto('/attachments', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Attachments' })).toBeVisible({ timeout: 15000 });

  const searchBox = page.getByPlaceholder('Search by name or filename...');
  await searchBox.click();
  await searchBox.pressSequentially('README.md');

  await page.getByRole('img', { name: /README\.md/i }).first().click();
  await expect(page.getByRole('heading', { name: 'README.md' })).toBeVisible({ timeout: 15000 });

  await page.getByRole('button', { name: 'Tags' }).click();
  await expect(page.getByRole('heading', { name: `Manage Tags for README.md` })).toBeVisible({ timeout: 15000 });

  await addTagToOpenTagSelector(page, attachmentTagName);
  await page.getByRole('button', { name: 'Save Tags' }).click();
  await expect(page.getByText(attachmentTagName, { exact: true })).toBeVisible({ timeout: 15000 });

  const downloadHref = await page.getByRole('link', { name: 'Download' }).getAttribute('href');
  const attachmentHashMatch = downloadHref?.match(/\/api\/attachments\/([^/]+)\/download$/);
  if (!attachmentHashMatch) {
    throw new Error(`Could not determine the attachment hash from href "${downloadHref}".`);
  }

  await page.getByRole('button', { name: 'Close' }).click();
  await page.goto(`/attachments/${attachmentHashMatch[1]}`, { waitUntil: 'domcontentloaded' });

  await expect(page).toHaveURL(/\/attachments\/[^/]+$/);
  await expect(page.getByText('Tags:', { exact: true })).toBeVisible({ timeout: 15000 });
  await expect(page.getByText(attachmentTagName, { exact: true })).toBeVisible({ timeout: 15000 });
});
