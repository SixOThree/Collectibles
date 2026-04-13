import path from 'path';
import { expect, Locator, Page } from '@playwright/test';

const showcaseScreenshotsDir = path.resolve(__dirname, '../../../../Test/Example Data/Showcase Example Images');

function uploadedFileRow(page: Page, fileName: string): Locator {
  return page.locator('tbody tr').filter({ has: page.getByText(fileName, { exact: true }) });
}

export function showcaseScreenshotPath(...segments: string[]): string {
  return path.resolve(showcaseScreenshotsDir, ...segments);
}

export function canonicalImageFixture(): string {
  return showcaseScreenshotPath('Software', 'lotus_123.png');
}

export function alternateImageFixture(): string {
  return showcaseScreenshotPath('Software', 'windows_31_desktop.jpg');
}

export function documentFixture(): string {
  return showcaseScreenshotPath('README.md');
}

export async function createItemForAttachments(page: Page, showcaseHash: string, name: string): Promise<void> {
  await page.goto(`/showcase/${showcaseHash}/item/new`, { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Add New Collectible Item' })).toBeVisible({ timeout: 15000 });

  await page.getByLabel(/^Name/).fill(name);
  await page.getByLabel('Description').fill(`${name} description`);
  await page.getByRole('button', { name: 'Create Item' }).click();

  await page.waitForURL(new RegExp(`/showcase/${showcaseHash}$`), { timeout: 30000 });
  await expect(page.getByText(name, { exact: true })).toBeVisible({ timeout: 15000 });
}

export async function uploadItemAttachments(page: Page, filePaths: string[]): Promise<void> {
  const fileInput = page.getByLabel('Browse Files', { exact: true });
  await expect(fileInput).toBeAttached({ timeout: 15000 });
  await fileInput.setInputFiles(filePaths);
  await expect(page.getByRole('heading', { name: /Selected Files \(\d+\)/ })).toBeVisible({ timeout: 60000 });

  await waitForUploadedAttachments(
    page,
    filePaths.map((filePath) => path.basename(filePath))
  );
}

export async function waitForUploadedAttachments(page: Page, fileNames: string[]): Promise<void> {
  for (const fileName of fileNames) {
    const row = uploadedFileRow(page, fileName);
    await expect(row).toBeVisible({ timeout: 60000 });
    await expect(row.getByText('Uploaded', { exact: true })).toBeVisible({ timeout: 60000 });
  }
}

export async function choosePreviewImage(page: Page, fileName: string): Promise<void> {
  await page.getByRole('button', { name: 'Choose Preview Image' }).click();
  await expect(page.getByRole('heading', { name: 'Select Preview Image' })).toBeVisible({ timeout: 15000 });

  const option = page.getByLabel(fileName, { exact: true });
  await expect(option).toBeVisible({ timeout: 15000 });
  await option.check();
  await expect(option).toBeChecked({ timeout: 15000 });

  await page.getByRole('button', { name: 'Select' }).click();
  await expect(page.getByRole('heading', { name: 'Select Preview Image' })).not.toBeVisible({ timeout: 15000 });
}

export async function expectSelectedPreviewImage(page: Page, fileName: string): Promise<void> {
  await page.getByRole('button', { name: 'Choose Preview Image' }).click();
  await expect(page.getByRole('heading', { name: 'Select Preview Image' })).toBeVisible({ timeout: 15000 });

  const option = page.getByLabel(fileName, { exact: true });
  await expect(option).toBeVisible({ timeout: 15000 });
  await expect(option).toBeChecked({ timeout: 15000 });

  await page.getByRole('button', { name: 'Select' }).click();
  await expect(page.getByRole('heading', { name: 'Select Preview Image' })).not.toBeVisible({ timeout: 15000 });
}

export async function expectUploadedFileNames(page: Page, fileNames: string[]): Promise<void> {
  for (const fileName of fileNames) {
    await expect(page.getByText(fileName, { exact: true })).toBeVisible({ timeout: 15000 });
  }
}

export async function addTagToOpenTagSelector(page: Page, tagName: string): Promise<void> {
  const tagInput = page.getByPlaceholder('Search or add tags...');
  await tagInput.click();
  await tagInput.fill('');
  await tagInput.pressSequentially(tagName);

  const createTagButton = page.locator('.tag-dropdown .create-tag-item');
  const tagAction = await waitForTagAction(page, tagName);
  if (tagAction === 'create') {
    await createTagButton.click();
  } else {
    const existingTagButton = page.locator('.tag-dropdown').getByRole('button', { name: tagName, exact: true });
    await expect(existingTagButton).toBeVisible({ timeout: 5000 });
    await existingTagButton.click();
  }

  await expect(page.locator('.selected-tags').getByText(tagName, { exact: true })).toBeVisible({ timeout: 15000 });
}

async function waitForTagAction(page: Page, tagName: string): Promise<'create' | 'existing'> {
  const searchStartedAt = Date.now();
  const dropdownItems = page.locator('.tag-dropdown .dropdown-item');
  const createTagButton = page.locator('.tag-dropdown .create-tag-item');

  while (Date.now() - searchStartedAt < 5000) {
    const optionTexts = (await dropdownItems.allInnerTexts()).map((text) => text.trim());
    if (optionTexts.includes(tagName)) {
      return 'existing';
    }

    if (Date.now() - searchStartedAt >= 400 && (await createTagButton.isVisible().catch(() => false))) {
      return 'create';
    }

    await page.waitForTimeout(100);
  }

  throw new Error(`Timed out waiting for the tag dropdown to resolve "${tagName}".`);
}

