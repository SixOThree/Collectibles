import { chromium, test, expect, Page } from '@playwright/test';
import {
  canonicalImageFixture,
  documentFixture,
  expectUploadedFileNames,
  uploadItemAttachments,
} from '../helpers/attachments';
import { authFile } from '../helpers/auth';
import { createItem } from '../helpers/items';
import { uniqueName } from '../helpers/names';
import { createShowcase } from '../helpers/showcases';
import { generateShareLink, revokeShareLink } from '../helpers/sharing';

test.use({ storageState: authFile('user') });

test('regular user can share a private showcase with anonymous read-only access and revoke the link', async ({ page }) => {
  const showcaseName = uniqueName('PW Shared Showcase');
  const showcaseHash = await createShowcase(page, showcaseName, true);
  const itemName = uniqueName('PW Shared Item');
  await createItem(page, showcaseHash, itemName);

  await page.getByRole('link', { name: 'Edit' }).click();
  await expect(page.getByRole('heading', { name: 'Edit Collectible Item' })).toBeVisible({ timeout: 15000 });

  await uploadItemAttachments(page, [canonicalImageFixture(), documentFixture()]);
  await expectUploadedFileNames(page, ['lotus_123.png', 'README.md']);

  await page.getByRole('button', { name: 'Save Changes' }).click();
  await expect(page).toHaveURL(/\/item\/[^/]+$/);
  await expect(page.getByRole('heading', { name: itemName })).toBeVisible({ timeout: 15000 });

  const shareNote = uniqueName('PW Share Note');
  const shareUrl = await generateShareLink(page, showcaseHash, shareNote);
  expect(shareUrl).toContain('/share/');

  const anonymousBrowser = await chromium.launch();
  const anonymousContext = await anonymousBrowser.newContext();
  try {
    const anonymousPage = await anonymousContext.newPage();

    await anonymousPage.goto(shareUrl, { waitUntil: 'domcontentloaded' });
    await expect(anonymousPage.getByRole('heading', { name: showcaseName })).toBeVisible({ timeout: 15000 });
    await expect(anonymousPage.getByRole('heading', { name: 'Items in this Showcase' })).toBeVisible({ timeout: 15000 });
    await expect(anonymousPage.getByText(itemName, { exact: true })).toBeVisible({ timeout: 15000 });
    await expect(anonymousPage.getByText(`${itemName} description`, { exact: true })).toBeVisible({ timeout: 15000 });
    await expect(anonymousPage.getByText('lotus_123.png', { exact: true })).toBeVisible({ timeout: 15000 });
    await expect(anonymousPage.getByText('README.md', { exact: true })).toBeVisible({ timeout: 15000 });
    await expect(
      anonymousPage.getByText('This showcase is being shared with you via a private link.', { exact: true })
    ).toBeVisible({ timeout: 15000 });
    await expect(anonymousPage.getByRole('button', { name: 'Edit' })).toHaveCount(0);
    await expect(anonymousPage.getByRole('button', { name: 'Share' })).toHaveCount(0);

    await revokeShareLink(page, shareNote);

    await anonymousPage.goto(shareUrl, { waitUntil: 'domcontentloaded' });
    await expect(anonymousPage.getByRole('heading', { name: 'Unable to Load Showcase' })).toBeVisible({
      timeout: 15000,
    });
    await expect(
      anonymousPage.getByText('This link may be expired, invalid, or the showcase may no longer be available.', {
        exact: true,
      })
    ).toBeVisible({ timeout: 15000 });
  } finally {
    await anonymousContext.close();
    await anonymousBrowser.close();
  }
});

test('regular user can assign a QR code and anonymous users are redirected to the public item', async ({ page }) => {
  const showcaseName = uniqueName('PW QR Showcase');
  const showcaseHash = await createShowcase(page, showcaseName, false);
  const itemName = uniqueName('PW QR Item');
  const itemHash = await createItem(page, showcaseHash, itemName);

  const qrCode = await generateQrCode(page);

  await page.goto(`/item/${itemHash}/edit`, { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Edit Collectible Item' })).toBeVisible({ timeout: 15000 });

  const qrInput = page.getByPlaceholder('Enter QR code or scan');
  await qrInput.fill(qrCode);
  await qrInput.press('Tab');
  await expect(page.getByRole('button', { name: 'Assign' })).toBeEnabled({ timeout: 15000 });
  await page.getByRole('button', { name: 'Assign' }).click();

  await expect(page.locator('.alert-info')).toContainText(qrCode);
  await expect(page.getByRole('button', { name: 'Unassign' })).toBeVisible({ timeout: 15000 });

  const anonymousBrowser = await chromium.launch();
  const anonymousContext = await anonymousBrowser.newContext();
  try {
    const anonymousPage = await anonymousContext.newPage();

    await anonymousPage.goto(`/qr/${qrCode}`, { waitUntil: 'domcontentloaded' });
    await anonymousPage.waitForURL(new RegExp(`/item/${itemHash}$`), { timeout: 30000 });
    await expect(anonymousPage.getByRole('heading', { name: itemName })).toBeVisible({ timeout: 15000 });

    await anonymousPage.goto('/qr/PLAYWRIGHT-UNKNOWN-CODE', { waitUntil: 'domcontentloaded' });
    await expect(anonymousPage.getByRole('heading', { name: 'QR Code Error' })).toBeVisible({ timeout: 15000 });
    await expect(
      anonymousPage.getByText('This QR code is not recognized. It may not have been registered in the system.', {
        exact: true,
      })
    ).toBeVisible({ timeout: 15000 });
  } finally {
    await anonymousContext.close();
    await anonymousBrowser.close();
  }
});

async function generateQrCode(page: Page): Promise<string> {
  await page.goto('/qrcodes', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'QR Code Management' })).toBeVisible({ timeout: 15000 });

  await page.getByLabel('Quantity').fill('1');
  await page.getByRole('button', { name: 'Generate QR Codes' }).click();

  await expect(page.getByText('Generated 1 QR codes.', { exact: false })).toBeVisible({ timeout: 15000 });

  const recentCodesCard = page.locator('.card').filter({ has: page.getByRole('heading', { name: 'Recently Generated Codes' }) }).first();
  const qrCode = (await recentCodesCard.locator('code').first().innerText()).trim();

  if (!qrCode) {
    throw new Error('Unable to read a generated QR code from the Recently Generated Codes table.');
  }

  return qrCode;
}
