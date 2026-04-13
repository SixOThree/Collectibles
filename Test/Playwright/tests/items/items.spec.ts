import { test, expect } from '@playwright/test';
import { authFile } from '../helpers/auth';
import { uniqueName } from '../helpers/names';
import { createShowcase } from '../helpers/showcases';
import { createItem } from '../helpers/items';

test.use({ storageState: authFile('user') });

test('regular user can create and edit a collectible item', async ({ page }) => {
  const showcaseHash = await createShowcase(page, uniqueName('PW Item Showcase'), true);
  const itemName = uniqueName('PW Root Item');

  await createItem(page, showcaseHash, itemName);

  await expect(page.getByRole('heading', { name: itemName })).toBeVisible({ timeout: 15000 });
  await page.getByRole('link', { name: 'Edit' }).click();
  await expect(page).toHaveURL(/\/item\/[^/]+\/edit$/);

  const updatedName = `${itemName} Updated`;
  await page.getByLabel(/^Name/).fill(updatedName);
  await page.getByRole('button', { name: 'Save Changes' }).click();

  await expect(page.getByRole('heading', { name: updatedName })).toBeVisible({ timeout: 15000 });
});

test('regular user can create a child item and see the breadcrumb trail', async ({ page }) => {
  const showcaseHash = await createShowcase(page, uniqueName('PW Hierarchy Showcase'), true);
  const parentName = uniqueName('PW Parent Item');
  const childName = uniqueName('PW Child Item');

  const parentHash = await createItem(page, showcaseHash, parentName);
  await createItem(page, showcaseHash, childName, parentHash);

  await expect(page.getByRole('heading', { name: childName })).toBeVisible({ timeout: 15000 });
  await expect(page.getByLabel('breadcrumb').getByRole('link', { name: parentName })).toBeVisible({ timeout: 15000 });
  await expect(page.getByLabel('breadcrumb')).toContainText(parentName);
});
