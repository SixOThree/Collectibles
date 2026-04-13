import { test, expect } from '@playwright/test';
import { readSeedManifest } from '../helpers/seed-manifest';

test('anonymous users can browse the seeded public showcase but not the seeded private showcase', async ({ page }) => {
  const manifest = readSeedManifest();

  await page.goto('/showcases/public', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('link', { name: manifest.showcases.regularPublic.name })).toBeVisible({ timeout: 15000 });
  await expect(page.getByText(manifest.showcases.regularPrivate.name)).toHaveCount(0);
});
