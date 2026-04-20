import { test, expect } from '@playwright/test';
import { authFile } from '../helpers/auth';
import { uniqueName } from '../helpers/names';
import { createShowcase } from '../helpers/showcases';
import { createItem } from '../helpers/items';
import { createShowcaseTemplate } from '../helpers/templates';

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

test('item pages use the template preview height override for related item cards', async ({ page }) => {
  const showcaseName = uniqueName('PW Item Preview Showcase');
  const showcaseHash = await createShowcase(page, showcaseName, true);
  const templateName = uniqueName('PW Preview Height Template');
  const parentName = uniqueName('PW Preview Parent');
  const childName = uniqueName('PW Preview Child');

  await createShowcaseTemplate(page, {
    showcaseName,
    templateName,
    hideAttachments: true,
    itemDetailPreviewHeight: 320,
    fields: [],
  });

  await page.goto(`/showcase/${showcaseHash}/item/new`, { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Add New Collectible Item' })).toBeVisible({ timeout: 15000 });

  await page.getByLabel('Template').selectOption({ label: templateName });
  await page.getByLabel(/^Name/).fill(parentName);
  await page.getByLabel('Description').fill(`${parentName} description`);
  await page.getByRole('button', { name: 'Create Item' }).click();

  await expect(page).toHaveURL(new RegExp(`/showcase/${showcaseHash}$`));
  await page.getByText(parentName, { exact: true }).click();
  await expect(page.getByRole('heading', { name: parentName })).toBeVisible({ timeout: 15000 });

  const parentMatch = page.url().match(/\/item\/([^/?#]+)/);
  expect(parentMatch).not.toBeNull();
  const parentHash = parentMatch![1];

  await createItem(page, showcaseHash, childName, parentHash);

  await page.goto(`/item/${parentHash}?showcase=${showcaseHash}`, { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: parentName })).toBeVisible({ timeout: 15000 });
  await expect(page.locator('.item-placeholder').first()).toHaveAttribute('style', /320px/);
});
