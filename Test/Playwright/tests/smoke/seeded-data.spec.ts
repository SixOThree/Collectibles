import { test, expect } from '@playwright/test';
import { readSeedManifest } from '../helpers/seed-manifest';

test('seeded public showcase is visible and the seeded private showcase is hidden from anonymous users', async ({ page }) => {
  const manifest = readSeedManifest();

  await page.goto('/showcases/public', { waitUntil: 'domcontentloaded' });

  await expect(page.getByRole('heading', { name: 'Browse All Showcases' })).toBeVisible({ timeout: 15000 });
  await expect(page.getByRole('link', { name: manifest.showcases.regularPublic.name })).toBeVisible({ timeout: 15000 });
  await expect(page.getByText(manifest.showcases.regularPrivate.name)).toHaveCount(0);
});

