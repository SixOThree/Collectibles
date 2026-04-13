import { expect, Locator, Page } from '@playwright/test';

function shareModal(page: Page): Locator {
  return page.locator('.modal.show').filter({ has: page.getByRole('heading', { name: /^Share "/ }) }).first();
}

function shareFormCard(modal: Locator): Locator {
  return modal.locator('.card').filter({ hasText: 'Generate New Share Link' }).first();
}

function activeShareLinksTable(modal: Locator): Locator {
  return modal.locator('.card').filter({ hasText: 'Active Share Links' }).locator('table').first();
}

function activeShareRow(modal: Locator, note: string): Locator {
  return activeShareLinksTable(modal).locator('tbody tr').filter({ hasText: note }).first();
}

export async function openShareModal(page: Page, showcaseHash: string): Promise<Locator> {
  await page.goto(`/showcase/${showcaseHash}`, { waitUntil: 'domcontentloaded' });
  await expect(page).toHaveURL(new RegExp(`/showcase/${showcaseHash}$`));
  await page.getByRole('button', { name: 'Share' }).click();

  const modal = shareModal(page);
  await expect(modal.getByText('Generate New Share Link', { exact: true })).toBeVisible({ timeout: 15000 });

  return modal;
}

export async function generateShareLink(page: Page, showcaseHash: string, note: string): Promise<string> {
  const modal = await openShareModal(page, showcaseHash);
  const formCard = shareFormCard(modal);

  await formCard.locator('input[type="text"]').fill(note);
  await formCard.getByRole('button', { name: 'Generate Share Link' }).click();

  await expect(modal.getByText('Share link generated successfully!', { exact: false })).toBeVisible({ timeout: 15000 });

  const row = activeShareRow(modal, note);
  await expect(row).toBeVisible({ timeout: 15000 });

  const shareUrl = (await row.locator('input[readonly]').first().inputValue()).trim();
  if (!shareUrl) {
    throw new Error(`Unable to read generated share link for note "${note}".`);
  }

  return shareUrl;
}

export async function revokeShareLink(page: Page, note: string): Promise<void> {
  const modal = shareModal(page);
  const row = activeShareRow(modal, note);
  await expect(row).toBeVisible({ timeout: 15000 });

  await row.locator('button[title="Revoke link"]').click();

  await expect(modal.getByText('Share link revoked successfully.', { exact: true })).toBeVisible({ timeout: 15000 });
  await expect(activeShareRow(modal, note)).toHaveCount(0);
}
