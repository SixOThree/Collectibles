import { expect, Page } from '@playwright/test';

export async function createItem(page: Page, showcaseHash: string, name: string, parentHash?: string): Promise<string> {
  const url = parentHash
    ? `/showcase/${showcaseHash}/item/new?parent=${parentHash}`
    : `/showcase/${showcaseHash}/item/new`;

  await page.goto(url, { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Add New Collectible Item' })).toBeVisible({ timeout: 15000 });

  await page.getByLabel(/^Name/).fill(name);
  await page.getByLabel('Description').fill(`${name} description`);
  await page.getByRole('button', { name: 'Create Item' }).click();

  await expect(page).toHaveURL(new RegExp(`/showcase/${showcaseHash}$`));

  if (parentHash) {
    await page.goto(`/item/${parentHash}?showcase=${showcaseHash}`, { waitUntil: 'domcontentloaded' });
    await expect(page.getByText(name, { exact: true })).toBeVisible({ timeout: 15000 });
  }

  await page.getByText(name, { exact: true }).click();
  await expect(page).toHaveURL(/\/item\/[^/]+/);
  await expect(page.getByRole('heading', { name })).toBeVisible({ timeout: 15000 });

  const match = page.url().match(/\/item\/([^/?#]+)/);
  if (!match) {
    throw new Error(`Unable to parse item hash from URL: ${page.url()}`);
  }

  return match[1];
}
