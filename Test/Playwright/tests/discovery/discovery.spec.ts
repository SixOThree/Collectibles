import { chromium, expect, test } from '@playwright/test';
import { authFile } from '../helpers/auth';
import { addTagsToShowcase, ensurePublicShowcaseFiltersVisible, createItemWithTags } from '../helpers/discovery';
import { uniqueName } from '../helpers/names';
import { createShowcase } from '../helpers/showcases';

test.use({ storageState: authFile('user') });

test('anonymous users can search public showcases and open a matching result', async ({ page }) => {
  const publicShowcaseName = uniqueName('PW Public Discovery');
  const secondPublicShowcaseName = uniqueName('PW Public Archive');
  const privateShowcaseName = uniqueName('PW Private Discovery');

  await createShowcase(page, publicShowcaseName, false);
  await createShowcase(page, secondPublicShowcaseName, false);
  await createShowcase(page, privateShowcaseName, true);

  const browseUrl = new URL('/showcases/public', page.url()).toString();
  const anonymousBrowser = await chromium.launch();
  const anonymousContext = await anonymousBrowser.newContext({
    storageState: { cookies: [], origins: [] },
  });

  try {
    const anonymousPage = await anonymousContext.newPage();
    await anonymousPage.goto(browseUrl, { waitUntil: 'domcontentloaded' });

    await expect(anonymousPage.getByRole('heading', { name: 'Browse All Showcases' })).toBeVisible({ timeout: 15000 });
    await expect(anonymousPage.getByText(publicShowcaseName, { exact: true }).first()).toBeVisible({ timeout: 15000 });
    await expect(anonymousPage.getByText(secondPublicShowcaseName, { exact: true }).first()).toBeVisible({
      timeout: 15000,
    });
    await expect(anonymousPage.getByText(privateShowcaseName, { exact: true })).toHaveCount(0);

    await ensurePublicShowcaseFiltersVisible(anonymousPage);
    const searchBox = anonymousPage.getByPlaceholder('Search showcases…');
    await searchBox.fill(publicShowcaseName);
    await searchBox.press('Enter');

    await expect(anonymousPage.getByText(publicShowcaseName, { exact: true }).first()).toBeVisible({ timeout: 15000 });
    await expect(anonymousPage.getByText(secondPublicShowcaseName, { exact: true })).toHaveCount(0);
    await expect(anonymousPage.getByText(privateShowcaseName, { exact: true })).toHaveCount(0);

    await anonymousPage.getByRole('button', { name: 'Reset Filters' }).click();
    await expect(anonymousPage.getByText(publicShowcaseName, { exact: true }).first()).toBeVisible({ timeout: 15000 });
    await expect(anonymousPage.getByText(secondPublicShowcaseName, { exact: true }).first()).toBeVisible({
      timeout: 15000,
    });

    await anonymousPage.getByRole('link', { name: publicShowcaseName }).first().click();
    await expect(anonymousPage.getByRole('heading', { name: publicShowcaseName })).toBeVisible({ timeout: 15000 });
    await expect(anonymousPage.getByRole('button', { name: 'Edit' })).toHaveCount(0);
    await expect(anonymousPage.getByRole('button', { name: 'Share' })).toHaveCount(0);
    await expect(anonymousPage.getByRole('button', { name: 'Add New Item' })).toHaveCount(0);
  } finally {
    await anonymousContext.close();
    await anonymousBrowser.close();
  }
});

test('regular user can search showcase items filter by tag and clear filters', async ({ page }) => {
  const showcaseName = uniqueName('PW Discovery Showcase');
  const showcaseHash = await createShowcase(page, showcaseName, true);
  const hardwareItemName = uniqueName('Hardware Rig');
  const softwareItemName = uniqueName('Software Disk');
  const hybridItemName = uniqueName('Hybrid Bundle');
  const hardwareTagName = uniqueName('PW Hardware Tag');
  const softwareTagName = uniqueName('PW Software Tag');

  await createItemWithTags(page, showcaseHash, hardwareItemName, [hardwareTagName]);
  await createItemWithTags(page, showcaseHash, softwareItemName, [softwareTagName]);
  await createItemWithTags(page, showcaseHash, hybridItemName, [hardwareTagName, softwareTagName]);
  await addTagsToShowcase(page, showcaseHash, [hardwareTagName, softwareTagName]);

  await page.goto(`/showcase/${showcaseHash}`, { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: showcaseName })).toBeVisible({ timeout: 15000 });

  const searchItems = page.getByLabel('Search items');
  await searchItems.fill('Software');

  await expect(page.getByText(softwareItemName, { exact: true })).toBeVisible({ timeout: 15000 });
  await expect(page.getByText(hardwareItemName, { exact: true })).toHaveCount(0);
  await expect(page.getByText(hybridItemName, { exact: true })).toHaveCount(0);

  await searchItems.fill('');
  await expect(page.getByText(softwareItemName, { exact: true })).toBeVisible({ timeout: 15000 });
  await expect(page.getByText(hardwareItemName, { exact: true })).toBeVisible({ timeout: 15000 });
  await expect(page.getByText(hybridItemName, { exact: true })).toBeVisible({ timeout: 15000 });

  await page.getByRole('button', { name: hardwareTagName }).click();
  await expect(page.getByText(hardwareItemName, { exact: true })).toBeVisible({ timeout: 15000 });
  await expect(page.getByText(hybridItemName, { exact: true })).toBeVisible({ timeout: 15000 });
  await expect(page.getByText(softwareItemName, { exact: true })).toHaveCount(0);

  await searchItems.fill('Software');
  await expect(page.getByText('No items match your current filters.', { exact: true })).toBeVisible({
    timeout: 15000,
  });

  await page.getByRole('button', { name: 'Clear filters' }).click();
  await expect(page.getByText(hardwareItemName, { exact: true })).toBeVisible({ timeout: 15000 });
  await expect(page.getByText(softwareItemName, { exact: true })).toBeVisible({ timeout: 15000 });
  await expect(page.getByText(hybridItemName, { exact: true })).toBeVisible({ timeout: 15000 });
});
