import { test, expect, Page } from '@playwright/test';
import { authFile } from '../helpers/auth';
import { uniqueName } from '../helpers/names';
import { createShowcase } from '../helpers/showcases';
import {
  alternateImageFixture,
  canonicalImageFixture,
  choosePreviewImage,
  createItemForAttachments,
  documentFixture,
  expectSelectedPreviewImage,
  expectUploadedFileNames,
  uploadItemAttachments,
} from '../helpers/attachments';

test.use({ storageState: authFile('user') });

async function getItemCardPreviewSrc(page: Page, itemName: string): Promise<string> {
  const image = page.getByRole('img', { name: itemName });
  await expect(image).toHaveCount(1, { timeout: 15000 });
  await expect(image).toBeVisible({ timeout: 15000 });

  const src = await image.getAttribute('src');
  expect(src).toBeTruthy();
  return src!;
}

function firstAttachmentImageAfterHeading(page: Page, heading: string) {
  return page.getByRole('heading', { name: heading }).locator('xpath=following::img[1]');
}

test('regular user can upload item attachments and see them on detail', async ({ page }) => {
  const showcaseName = uniqueName('PW Attachments Showcase');
  const showcaseHash = await createShowcase(page, showcaseName, true);
  const itemName = uniqueName('PW Attachment Item');

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
  await expect(page.getByRole('heading', { name: itemName })).toBeVisible({ timeout: 15000 });
  await expect(page.getByText(/2 attachments/)).toBeVisible({ timeout: 15000 });
  await expect(page.getByRole('heading', { name: 'Pictures' })).toBeVisible({ timeout: 15000 });
  await expect(page.getByRole('heading', { name: 'Documents & Files' })).toBeVisible({ timeout: 15000 });
  await expect(page.getByText('README.md', { exact: true })).toBeVisible({ timeout: 15000 });
  await expect(page.getByRole('img', { name: /lotus_123\.png/i })).toBeVisible({ timeout: 15000 });
});

test('regular user can feature an image and inspect its details from the item surface', async ({ page }) => {
  const showcaseName = uniqueName('PW Featured Showcase');
  const showcaseHash = await createShowcase(page, showcaseName, true);
  const itemName = uniqueName('PW Featured Item');

  await createItemForAttachments(page, showcaseHash, itemName);
  await page.getByText(itemName, { exact: true }).click();
  await expect(page).toHaveURL(/\/item\/[^/]+/);
  await expect(page.getByRole('heading', { name: itemName })).toBeVisible({ timeout: 15000 });

  await page.getByRole('link', { name: 'Edit' }).click();
  await expect(page).toHaveURL(/\/item\/[^/]+\/edit$/);
  await expect(page.getByRole('heading', { name: 'Edit Collectible Item' })).toBeVisible({ timeout: 15000 });

  await uploadItemAttachments(page, [canonicalImageFixture(), alternateImageFixture()]);
  await expectUploadedFileNames(page, ['lotus_123.png', 'windows_31_desktop.jpg']);

  await page.getByRole('button', { name: 'Save Changes' }).click();
  await expect(page).toHaveURL(/\/item\/[^/]+$/);
  await expect(page.getByRole('heading', { name: itemName })).toBeVisible({ timeout: 15000 });

  await expect(page.getByRole('heading', { name: 'Pictures' })).toBeVisible({ timeout: 15000 });
  await expect(firstAttachmentImageAfterHeading(page, 'Pictures')).toHaveAttribute('alt', /lotus_123\.png/i, {
    timeout: 15000,
  });
  await expect(page.getByRole('heading', { name: 'Featured', exact: true })).toHaveCount(0);

  await page.getByTitle('Add to featured').first().click();

  await expect(page.getByRole('heading', { name: 'Featured' })).toBeVisible({ timeout: 15000 });
  await expect(firstAttachmentImageAfterHeading(page, 'Featured')).toHaveAttribute('alt', /lotus_123\.png/i, {
    timeout: 15000,
  });
  await expect(firstAttachmentImageAfterHeading(page, 'Pictures')).toHaveAttribute('alt', /windows_31_desktop\.jpg/i, {
    timeout: 15000,
  });

  await firstAttachmentImageAfterHeading(page, 'Featured').click();
  await expect(page.getByTitle('View Details')).toBeVisible({ timeout: 15000 });
  await page.getByTitle('View Details').click();

  const detailModal = page.locator('.modal.show').filter({ has: page.getByText('Original File:', { exact: true }) });
  await expect(detailModal.getByRole('heading', { name: 'lotus_123.png' })).toBeVisible({ timeout: 15000 });
  await expect(detailModal.getByText('Original File:', { exact: true })).toBeVisible({ timeout: 15000 });
  await expect(detailModal.getByTitle('lotus_123.png')).toBeVisible({ timeout: 15000 });
  await expect(detailModal.getByText('Category:', { exact: true })).toBeVisible({ timeout: 15000 });
  await expect(detailModal.getByText('Image', { exact: true })).toBeVisible({ timeout: 15000 });
  await expect(detailModal.getByText('image/png', { exact: true })).toBeVisible({ timeout: 15000 });

  await detailModal.locator('button.btn-close').click();
  await expect(detailModal).toHaveCount(0);

  await page.getByTitle('Close').click();
  await expect(page.getByRole('heading', { name: itemName })).toBeVisible({ timeout: 15000 });
  await expect(firstAttachmentImageAfterHeading(page, 'Featured')).toHaveAttribute('alt', /lotus_123\.png/i, {
    timeout: 15000,
  });
  await expect(firstAttachmentImageAfterHeading(page, 'Pictures')).toHaveAttribute('alt', /windows_31_desktop\.jpg/i, {
    timeout: 15000,
  });
});

test('regular user can choose and persist a preview image from uploaded attachments', async ({ page }) => {
  const showcaseName = uniqueName('PW Preview Showcase');
  const showcaseHash = await createShowcase(page, showcaseName, true);
  const itemName = uniqueName('PW Preview Item');

  await createItemForAttachments(page, showcaseHash, itemName);
  await page.getByText(itemName, { exact: true }).click();
  await expect(page).toHaveURL(/\/item\/[^/]+/);
  await expect(page.getByRole('heading', { name: itemName })).toBeVisible({ timeout: 15000 });

  await page.getByRole('link', { name: 'Edit' }).click();
  await expect(page).toHaveURL(/\/item\/[^/]+\/edit$/);
  await expect(page.getByRole('heading', { name: 'Edit Collectible Item' })).toBeVisible({ timeout: 15000 });

  await uploadItemAttachments(page, [canonicalImageFixture(), alternateImageFixture()]);
  await expectUploadedFileNames(page, ['lotus_123.png', 'windows_31_desktop.jpg']);

  await page.getByRole('button', { name: 'Save Changes' }).click();
  await expect(page).toHaveURL(/\/item\/[^/]+$/);
  await expect(page.getByRole('heading', { name: itemName })).toBeVisible({ timeout: 15000 });

  await page.goto(`/showcase/${showcaseHash}`, { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: showcaseName })).toBeVisible({ timeout: 15000 });
  const initialPreviewSrc = await getItemCardPreviewSrc(page, itemName);

  await page.getByText(itemName, { exact: true }).click();
  await expect(page).toHaveURL(/\/item\/[^/]+/);
  await expect(page.getByRole('heading', { name: itemName })).toBeVisible({ timeout: 15000 });

  await page.getByRole('link', { name: 'Edit' }).click();
  await expect(page).toHaveURL(/\/item\/[^/]+\/edit$/);
  await expect(page.getByRole('heading', { name: 'Edit Collectible Item' })).toBeVisible({ timeout: 15000 });

  await choosePreviewImage(page, 'windows_31_desktop.jpg');
  await expect(page.getByText('Current Preview:', { exact: true })).toBeVisible({ timeout: 15000 });

  await page.getByRole('button', { name: 'Save Changes' }).click();
  await expect(page).toHaveURL(/\/item\/[^/]+$/);
  await expect(page.getByRole('heading', { name: itemName })).toBeVisible({ timeout: 15000 });

  await page.goto(`/showcase/${showcaseHash}`, { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: showcaseName })).toBeVisible({ timeout: 15000 });
  const updatedPreviewSrc = await getItemCardPreviewSrc(page, itemName);
  expect(updatedPreviewSrc).not.toBe(initialPreviewSrc);

  await page.getByText(itemName, { exact: true }).click();
  await expect(page).toHaveURL(/\/item\/[^/]+/);
  await expect(page.getByRole('heading', { name: itemName })).toBeVisible({ timeout: 15000 });

  await page.getByRole('link', { name: 'Edit' }).click();
  await expect(page).toHaveURL(/\/item\/[^/]+\/edit$/);
  await expect(page.getByRole('heading', { name: 'Edit Collectible Item' })).toBeVisible({ timeout: 15000 });
  await expect(page.getByText('Current Preview:', { exact: true })).toBeVisible({ timeout: 15000 });
  await expectSelectedPreviewImage(page, 'windows_31_desktop.jpg');
});
