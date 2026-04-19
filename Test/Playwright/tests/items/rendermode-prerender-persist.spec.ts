import { test, expect, request as playwrightRequest } from '@playwright/test';
import { authFile } from '../helpers/auth';
import { readSeedManifest } from '../helpers/seed-manifest';

test.use({ storageState: authFile('user') });

test('item route returns prerendered html for an authenticated user and hydrates to the final content', async (
  { page },
  testInfo,
) => {
  const manifest = readSeedManifest();
  const route = `/item/${manifest.items.regularRoot.hash}?showcase=${manifest.showcases.regularPrivate.hash}`;
  const itemHeading = manifest.items.regularRoot.name;
  const escapedHeading = itemHeading.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const baseURL = testInfo.project.use.baseURL;

  const apiContext = await playwrightRequest.newContext({ baseURL, storageState: authFile('user') });
  try {
    const response = await apiContext.get(route);
    expect(response.ok()).toBeTruthy();

    const responseText = await response.text();
    expect(responseText).toMatch(new RegExp(`<h1[^>]*>\\s*${escapedHeading}\\s*</h1>`));

    await page.goto(route, { waitUntil: 'domcontentloaded' });
    await expect(page.getByRole('heading', { name: itemHeading })).toBeVisible({ timeout: 15000 });
    await expect(page.getByLabel('breadcrumb')).toContainText(itemHeading);
  } finally {
    await apiContext.dispose();
  }
});
