import fs from 'fs';
import { test as setup, expect, Page } from '@playwright/test';
import { authDir, authFile } from '../helpers/auth';
import { readSeedManifest } from '../helpers/seed-manifest';

async function signIn(page: Page, email: string, password: string) {
  await page.goto('/showcases');
  await expect(page).toHaveURL(/\/Account\/Login/);

  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Log in' }).click();

  await expect(page).toHaveURL(/\/showcases$/);
  await expect(page.getByRole('heading', { name: 'My Showcases' })).toBeVisible();
}

setup.beforeAll(() => {
  fs.mkdirSync(authDir, { recursive: true });
});

setup('authenticate regular user', async ({ page }) => {
  const manifest = readSeedManifest();
  await signIn(page, manifest.users.regular.email, manifest.users.regular.password);
  await page.context().storageState({ path: authFile('user') });
});

setup('authenticate admin user', async ({ page }) => {
  const manifest = readSeedManifest();
  await signIn(page, manifest.users.admin.email, manifest.users.admin.password);
  await page.context().storageState({ path: authFile('admin') });
});
