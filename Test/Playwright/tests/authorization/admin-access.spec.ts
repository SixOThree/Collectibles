import { test, expect } from '@playwright/test';
import { authFile } from '../helpers/auth';

test.describe('regular user admin boundary', () => {
  test.use({ storageState: authFile('user') });

  test('regular user is redirected away from User Management', async ({ page }) => {
    await page.goto('/users', { waitUntil: 'domcontentloaded' });

    await expect(page).toHaveURL(/\/Account\/AccessDenied/);
    await expect(page.getByRole('heading', { name: 'Access Restricted' })).toBeVisible({ timeout: 15000 });
  });
});

test.describe('admin access', () => {
  test.use({ storageState: authFile('admin') });

  test('admin can open User Management', async ({ page }) => {
    await page.goto('/users', { waitUntil: 'domcontentloaded' });

    await expect(page).toHaveURL(/\/users$/);
    await expect(page.getByRole('heading', { name: 'User Management' })).toBeVisible({ timeout: 15000 });
  });
});
