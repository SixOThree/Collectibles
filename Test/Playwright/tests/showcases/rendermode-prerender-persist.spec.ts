import { test, expect } from '@playwright/test';
import { readSeedManifest } from '../helpers/seed-manifest';

test('showcase route returns prerendered html and hydrates without re-showing the loading message', async ({ request, page }) => {
  const manifest = readSeedManifest();
  const route = `/showcase/${manifest.showcases.regularPublic.hash}`;
  const showcaseHeading = manifest.showcases.regularPublic.name;
  const escapedHeading = showcaseHeading.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

  const response = await request.get(route);
  expect(response.ok()).toBeTruthy();

  const responseText = await response.text();
  expect(responseText).toMatch(new RegExp(`<h1[^>]*>\\s*${escapedHeading}\\s*</h1>`));
  expect(responseText).not.toContain('Loading showcase…');

  await page.goto(route, { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: showcaseHeading })).toBeVisible({ timeout: 15000 });
  await expect(page.getByText('Loading showcase…')).toHaveCount(0);
});
