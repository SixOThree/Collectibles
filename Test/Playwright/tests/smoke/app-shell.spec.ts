import { test, expect } from '@playwright/test';

test('login page loads from the local Collectibles app', async ({ page }) => {
  await page.goto('/Account/Login');

  await expect(page).toHaveURL(/\/Account\/Login/);
  await expect(page.getByRole('heading', { level: 1, name: 'Log in' })).toBeVisible();
  await expect(page.getByLabel('Email')).toBeVisible();
  await expect(page.getByLabel('Password')).toBeVisible();
});

