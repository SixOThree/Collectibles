import { test, expect } from '@playwright/test';
import { authFile } from '../helpers/auth';
import { uniqueName } from '../helpers/names';
import { createShowcase } from '../helpers/showcases';

test.use({ storageState: authFile('user') });

test('regular user can create a new private showcase', async ({ page }) => {
  const showcaseName = uniqueName('PW Showcase');

  await createShowcase(page, showcaseName, true);

  await expect(page.getByRole('heading', { name: showcaseName })).toBeVisible({ timeout: 15000 });
  await expect(page.getByRole('button', { name: 'Info' })).toBeVisible({ timeout: 15000 });
});

test('regular user can edit showcase details and make the showcase public', async ({ page }) => {
  const showcaseName = uniqueName('PW Editable Showcase');
  await createShowcase(page, showcaseName, true);

  await page.getByRole('button', { name: 'Edit' }).click();
  await expect(page).toHaveURL(/\/showcase\/[^/]+\/edit$/);
  await expect(page.getByRole('heading', { name: 'Edit Showcase' })).toBeVisible({ timeout: 15000 });

  await page.getByLabel(/^Description$/).fill('Updated by Playwright');
  await page.getByLabel('Private showcase').uncheck();
  await page.getByRole('button', { name: 'Save Changes' }).click();

  await expect(page.getByRole('heading', { name: showcaseName })).toBeVisible({ timeout: 15000 });
  await page.getByRole('button', { name: 'Info' }).click();
  await expect(page.getByText('Public')).toBeVisible({ timeout: 15000 });
  await expect(page.getByText('Updated by Playwright')).toBeVisible({ timeout: 15000 });
});
