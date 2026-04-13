import path from 'path';
import { test, expect } from '@playwright/test';
import { authFile } from '../helpers/auth';
import { uniqueName } from '../helpers/names';
import { createShowcase } from '../helpers/showcases';

const zipFixturePath = path.resolve(
  __dirname,
  '../../../../Test/Example Data/Showcase Example Images/ShowcaseScreenshotsBulkZipUpload.zip'
);

test.use({ storageState: authFile('user') });

test('regular user can upload the canonical ZIP fixture and browse the imported hierarchy', async ({ page }) => {
  test.setTimeout(300_000);

  const showcaseName = uniqueName('PW ZIP Showcase');
  const showcaseHash = await createShowcase(page, showcaseName, true);
  const zipFileName = 'ShowcaseScreenshotsBulkZipUpload.zip';

  await page.goto(`/showcase/${showcaseHash}`, { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: showcaseName })).toBeVisible({ timeout: 15000 });
  await page.getByRole('button', { name: 'Zip Upload' }).click();

  await expect(page).toHaveURL(/\/zip-upload-bg-simple\?showcaseId=/);
  const zipUploadUrl = page.url();
  await expect(page.getByRole('heading', { name: /Zip Upload \(Background Processing\)/ })).toBeVisible({
    timeout: 15000,
  });
  await expect(page.locator('#showcaseSelect')).toHaveValue(/\d+/, { timeout: 15000 });
  await expect(page.locator('#showcaseSelect option:checked')).toContainText(showcaseName, { timeout: 15000 });

  await page.locator('#zipFileInput').setInputFiles(zipFixturePath);
  await expect(page.getByText(zipFileName)).toBeVisible({ timeout: 15000 });
  await page.getByRole('button', { name: 'Upload & Start Processing' }).click();

  await expect(page.locator('.alert-success')).toContainText('Processing has started in the background.', {
    timeout: 30000,
  });

  await page.getByRole('link', { name: 'My Showcases' }).click();
  await expect(page.getByRole('heading', { name: 'My Showcases' })).toBeVisible({ timeout: 15000 });

  await page.goto(zipUploadUrl, { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: /Zip Upload \(Background Processing\)/ })).toBeVisible({
    timeout: 15000,
  });

  const completedJobsHeading = page.getByRole('heading', { name: 'Completed Jobs' });
  await expect(completedJobsHeading).toBeVisible({ timeout: 180000 });

  const completedRow = page.locator('tbody tr').filter({ hasText: zipFileName }).first();
  await expect(completedRow).toBeVisible({ timeout: 180000 });
  await expect(completedRow.getByText('Success', { exact: true })).toBeVisible({ timeout: 180000 });
  await expect(completedRow.getByRole('button', { name: 'View' })).toBeVisible({ timeout: 15000 });
  await completedRow.getByRole('button', { name: 'View' }).click();

  await expect(page).toHaveURL(/\/showcase\/[^/]+$/);
  await expect(page.getByRole('heading', { name: showcaseName })).toBeVisible({ timeout: 15000 });
  await expect(page.getByText('Computers', { exact: true }).first()).toBeVisible({ timeout: 15000 });
  await expect(page.getByText('Software', { exact: true }).first()).toBeVisible({ timeout: 15000 });
  await expect(page.getByText('Video Games', { exact: true }).first()).toBeVisible({ timeout: 15000 });

  await page.getByText('Computers', { exact: true }).first().click();
  await expect(page).toHaveURL(/\/item\/[^/]+$/);
  await expect(page.getByRole('heading', { name: 'Computers' })).toBeVisible({ timeout: 15000 });
  await expect(page.getByRole('heading', { name: 'Related Items' })).toBeVisible({ timeout: 15000 });
  await expect(page.getByText('Apple', { exact: true }).first()).toBeVisible({ timeout: 15000 });

  await page.goto(`/showcase/${showcaseHash}`, { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: showcaseName })).toBeVisible({ timeout: 15000 });
  await page.getByText('Software', { exact: true }).first().click();

  await expect(page).toHaveURL(/\/item\/[^/]+$/);
  await expect(page.getByRole('heading', { name: 'Software' })).toBeVisible({ timeout: 15000 });
  await expect(page.getByText(/10 attachments/)).toBeVisible({ timeout: 15000 });
  await expect(page.getByRole('img', { name: /lotus_123\.png/i })).toBeVisible({ timeout: 15000 });
  await expect(page.getByRole('img', { name: /windows_31_desktop\.jpg/i })).toBeVisible({ timeout: 15000 });
});
