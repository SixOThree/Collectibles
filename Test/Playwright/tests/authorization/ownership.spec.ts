import { test, expect } from '@playwright/test';
import { authFile } from '../helpers/auth';
import { readSeedManifest } from '../helpers/seed-manifest';

test.use({ storageState: authFile('user') });

test('regular user cannot open another user private showcase', async ({ page }) => {
  const manifest = readSeedManifest();

  await page.goto(`/showcase/${manifest.showcases.otherPrivate.hash}`, { waitUntil: 'domcontentloaded' });

  await expect(page.getByRole('heading', { name: 'Showcase not found' })).toBeVisible({ timeout: 15000 });
  await expect(page.getByText("The showcase you're looking for doesn't exist or has been removed.")).toBeVisible({ timeout: 15000 });
});

test('regular user cannot add items to another user showcase', async ({ page }) => {
  const manifest = readSeedManifest();

  await page.goto(`/showcase/${manifest.showcases.otherPrivate.hash}/item/new`, { waitUntil: 'domcontentloaded' });

  await expect(page.getByRole('heading', { name: 'Showcase not found' })).toBeVisible({ timeout: 15000 });
  await expect(page.getByText("The showcase you're trying to add an item to doesn't exist.")).toBeVisible({ timeout: 15000 });
});

test('regular user cannot open another user private item', async ({ page }) => {
  const manifest = readSeedManifest();

  await page.goto(`/item/${manifest.items.otherPrivate.hash}`, { waitUntil: 'domcontentloaded' });

  await expect(page.getByRole('heading', { name: 'Item Not Found' })).toBeVisible({ timeout: 15000 });
  await expect(page.getByText('The collectible item you are looking for could not be found.')).toBeVisible({ timeout: 15000 });
});
