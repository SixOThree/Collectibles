import { test, expect } from '@playwright/test';
import { authFile } from '../helpers/auth';

test.describe('regular user shell', () => {
  test.use({ storageState: authFile('user') });

  test('regular user can open My Showcases', async ({ page }) => {
    await page.goto('/showcases', { waitUntil: 'domcontentloaded' });
    await expect(page.getByRole('heading', { name: 'My Showcases' })).toBeVisible({ timeout: 15000 });
  });
});

test.describe('admin shell', () => {
  test.use({ storageState: authFile('admin') });

  test('admin can open User Management', async ({ page }) => {
    await page.goto('/users', { waitUntil: 'domcontentloaded' });
    await expect(page.getByRole('heading', { name: 'User Management' })).toBeVisible({ timeout: 15000 });
  });
});
