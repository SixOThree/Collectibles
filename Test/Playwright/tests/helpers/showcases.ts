import { expect, Page } from '@playwright/test';

export async function createShowcase(page: Page, name: string, isPrivate: boolean): Promise<string> {
  await page.goto('/showcase/new', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Create New Showcase' })).toBeVisible({ timeout: 15000 });

  await page.getByLabel('Name').fill(name);
  await page.getByLabel('Description').fill(`${name} description`);

  const privateCheckbox = page.getByLabel(/Private showcase/);
  if (isPrivate) {
    await privateCheckbox.check();
  } else {
    await privateCheckbox.uncheck();
  }

  await page.getByRole('button', { name: 'Create Showcase' }).click();
  await expect(page).toHaveURL(/\/showcase\/[^/]+$/);
  await expect(page.getByRole('heading', { name })).toBeVisible({ timeout: 15000 });

  const match = page.url().match(/\/showcase\/([^/?#]+)/);
  if (!match) {
    throw new Error(`Unable to parse showcase hash from URL: ${page.url()}`);
  }

  return match[1];
}
