import { expect, Page } from '@playwright/test';

export async function ensurePublicShowcaseFiltersVisible(page: Page): Promise<void> {
  const searchBox = page.getByPlaceholder('Search showcases…');
  if ((await searchBox.count()) > 0) {
    return;
  }

  const showFiltersButton = page.getByRole('button', { name: 'Show Filters' });
  await expect(showFiltersButton).toBeVisible({ timeout: 15000 });
  await showFiltersButton.click();
  await expect(page.getByPlaceholder('Search showcases…')).toBeVisible({ timeout: 15000 });
}

export async function createItemWithTags(
  page: Page,
  showcaseHash: string,
  itemName: string,
  tags: string[]
): Promise<void> {
  await page.goto(`/showcase/${showcaseHash}/item/new`, { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Add New Collectible Item' })).toBeVisible({ timeout: 15000 });

  await page.getByLabel(/^Name/).fill(itemName);
  await page.getByLabel('Description').fill(`${itemName} description`);

  for (const tag of tags) {
    await addTagToCurrentItem(page, tag);
  }

  await page.getByRole('button', { name: 'Create Item' }).click();
  await expect(page).toHaveURL(new RegExp(`/showcase/${showcaseHash}$`));
  await expect(page.getByText(itemName, { exact: true })).toBeVisible({ timeout: 15000 });
}

export async function addTagsToShowcase(page: Page, showcaseHash: string, tags: string[]): Promise<void> {
  await page.goto(`/showcase/${showcaseHash}/edit`, { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Edit Showcase' })).toBeVisible({ timeout: 15000 });

  for (const tag of tags) {
    await addTagToCurrentSelector(page, tag);
  }

  await page.getByRole('button', { name: 'Save Changes' }).click();
  await expect(page).toHaveURL(new RegExp(`/showcase/${showcaseHash}$`));
}

async function addTagToCurrentItem(page: Page, tagName: string): Promise<void> {
  await addTagToCurrentSelector(page, tagName);
}

async function addTagToCurrentSelector(page: Page, tagName: string): Promise<void> {
  const tagInput = page.getByPlaceholder('Search or add tags...');
  await tagInput.click();
  await tagInput.fill('');
  await tagInput.pressSequentially(tagName);

  const createTagButton = page.locator('.tag-dropdown .create-tag-item');
  const tagAction = await waitForTagAction(page, tagName);
  if (tagAction === 'create') {
    await createTagButton.click();
  } else {
    const existingTagButton = page.locator('.tag-dropdown').getByRole('button', { name: tagName, exact: true });
    await expect(existingTagButton).toBeVisible({ timeout: 5000 });
    await existingTagButton.click();
  }

  await expect(page.locator('.selected-tags').getByText(tagName, { exact: true })).toBeVisible({ timeout: 15000 });
}

async function waitForTagAction(page: Page, tagName: string): Promise<'create' | 'existing'> {
  const searchStartedAt = Date.now();
  const dropdownItems = page.locator('.tag-dropdown .dropdown-item');
  const createTagButton = page.locator('.tag-dropdown .create-tag-item');
  let createActionAvailable = false;

  while (Date.now() - searchStartedAt < 5000) {
    const optionTexts = await dropdownItems.evaluateAll((elements) =>
      elements.map((element) => (element.textContent ?? '').replace(/\s+/g, ' ').trim())
    );

    if (optionTexts.includes(tagName)) {
      return 'existing';
    }

    if (await createTagButton.isVisible().catch(() => false)) {
      createActionAvailable = true;
    }

    await page.waitForTimeout(100);
  }

  if (createActionAvailable) {
    return 'create';
  }

  throw new Error(`Timed out waiting for the tag dropdown to resolve "${tagName}".`);
}
