import { expect, test } from '@playwright/test';
import { authFile } from '../helpers/auth';
import { readSeedManifest } from '../helpers/seed-manifest';
import { createManagedUserScenario } from '../helpers/users';

test.use({ storageState: authFile('admin') });

test('admin can create inspect edit and remove a user from user management', async ({ page }) => {
  test.slow();

  const scenario = createManagedUserScenario('pw-admin-user');

  await page.goto('/users', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'User Management' })).toBeVisible({ timeout: 15000 });
  await expect(page.getByRole('button', { name: 'Create New User' })).toBeVisible();

  await page.getByRole('button', { name: 'Create New User' }).click();
  if (!/\/users\/new$/.test(page.url())) {
    await page.goto('/users/new', { waitUntil: 'domcontentloaded' });
  }
  await expect(page.getByRole('heading', { name: 'Create New User' })).toBeVisible({ timeout: 15000 });

  await page.getByLabel('Email').fill(scenario.email);
  await page.getByLabel(/^Password$/).fill(scenario.initialPassword);
  await page.getByLabel(/^Confirm Password$/).fill(scenario.initialPassword);
  await page.getByLabel('Display Name').fill(scenario.displayName);
  await page.getByRole('button', { name: 'Create User' }).click();

  await expect(page).toHaveURL(/\/users\/[^/]+$/);
  await expect(page.getByRole('heading', { name: 'User Details' })).toBeVisible({ timeout: 15000 });
  await expect(page.locator('body')).toContainText(scenario.email);
  await expect(page.getByRole('button', { name: 'Edit' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Reset Password' })).toBeVisible();

  await page.getByRole('button', { name: 'Edit' }).click();
  await expect(page.getByRole('heading', { name: 'Edit User' })).toBeVisible({ timeout: 15000 });

  const updatedDisplayName = `Updated User ${scenario.uniqueSuffix}`;
  await page.getByLabel('Display Name').fill(updatedDisplayName);
  await page.getByRole('button', { name: 'Save Changes' }).click();

  await expect(page.locator('.alert.alert-success')).toContainText('User updated successfully!');

  await page.goto('/users', { waitUntil: 'domcontentloaded' });
  await page.getByPlaceholder('Search by name, username, or email...').fill(scenario.email);
  await page.getByRole('button', { name: 'Search' }).click();
  const createdUserRow = page.locator('tbody tr').filter({ hasText: scenario.email }).first();
  await expect(createdUserRow).toBeVisible({ timeout: 15000 });

  await createdUserRow.getByTitle('View Details').click();
  await expect(page.getByRole('heading', { name: 'User Details' })).toBeVisible({ timeout: 15000 });

  await page.getByRole('button', { name: 'Delete User' }).click();
  await expect(page).toHaveURL(/\/users$/);
  await page.getByPlaceholder('Search by name, username, or email...').fill(scenario.email);
  await page.getByRole('button', { name: 'Search' }).click();
  await expect(page.getByRole('table')).not.toContainText(scenario.email);
});

test('admin can review management and diagnostics surfaces safely', async ({ page }) => {
  test.slow();

  const manifest = readSeedManifest();

  await page.goto('/management', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Management Dashboard' })).toBeVisible({ timeout: 15000 });
  await page.getByRole('button', { name: 'Refresh' }).click();
  await expect(page.getByRole('button', { name: 'Event Logs' })).toBeVisible();

  await page.goto('/management/event-logs', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Event Logs' })).toBeVisible({ timeout: 15000 });
  await page.getByRole('button', { name: 'Refresh' }).click();
  await page.getByRole('button', { name: 'Clear All' }).click();
  await expect(page.getByText('Showing', { exact: false })).toBeVisible();

  await page.goto('/management/sys-logs', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'System Logs' })).toBeVisible({ timeout: 15000 });
  await page.getByRole('button', { name: /Errors Only/ }).click();
  await page.getByRole('button', { name: 'Refresh' }).click();
  await expect(page.getByText('Showing', { exact: false })).toBeVisible();

  await page.goto('/management/email-logs', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Email Logs' })).toBeVisible({ timeout: 15000 });
  await page.getByRole('button', { name: /Sent/ }).click();
  await page.getByRole('button', { name: 'Refresh' }).click();
  await expect(page.getByText('Showing', { exact: false })).toBeVisible();

  await page.goto('/management/user-stories', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'User Stories' })).toBeVisible({ timeout: 15000 });
  await page.getByRole('button', { name: 'Refresh' }).click();
  await expect(page.getByText('Showing', { exact: false })).toBeVisible();

  await page.goto('/Management/SiteConfiguration', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Site Configuration' })).toBeVisible({ timeout: 15000 });

  const registrationMessage = page.locator('#registrationMessage');
  const originalRegistrationMessage = await registrationMessage.inputValue();
  const updatedRegistrationMessage = `Playwright registration message ${Date.now()}`;

  await registrationMessage.fill(updatedRegistrationMessage);
  await page.locator('.card').first().getByRole('button', { name: 'Save Changes' }).click();
  await expect(page.locator('.alert.alert-success')).toContainText('Configuration saved successfully.');

  await registrationMessage.fill(originalRegistrationMessage);
  await page.locator('.card').first().getByRole('button', { name: 'Save Changes' }).click();
  await expect(page.locator('.alert.alert-success')).toContainText('Configuration saved successfully.');

  await page.goto('/admin/diagnostics', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'System Diagnostics' })).toBeVisible({ timeout: 15000 });
  await page.getByRole('button', { name: 'Refresh' }).click();
  const recentDatabaseLogsHeading = page.getByRole('heading', { name: 'Recent Database Logs' });
  for (let attempt = 0; attempt < 2; attempt += 1) {
    await page.getByRole('button', { name: 'View Recent Logs' }).click();
    if (await recentDatabaseLogsHeading.isVisible().catch(() => false)) {
      break;
    }
    await page.waitForTimeout(500);
  }
  await expect(recentDatabaseLogsHeading).toBeVisible({ timeout: 15000 });
  await page.getByRole('button', { name: 'Close' }).click();

  await page.goto('/admin/showcases', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'All Showcases' })).toBeVisible({ timeout: 15000 });
  await page.getByPlaceholder('Search showcases…').fill(manifest.showcases.regularPublic.name);
  await page.getByPlaceholder('Search showcases…').press('Enter');
  await expect(page.getByRole('link', { name: manifest.showcases.regularPublic.name })).toBeVisible({ timeout: 15000 });
  await expect(page.getByText(manifest.showcases.otherPrivate.name, { exact: true })).toHaveCount(0);
  await page.getByRole('button', { name: 'Reset Filters' }).click();

  await page.goto('/admin/theme-settings', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Theme Settings' })).toBeVisible({ timeout: 15000 });
  await expect(page.locator('input[readonly]').first()).not.toHaveValue('');
  const saveSettingsButton = page.getByRole('button', { name: 'Save Settings' });
  await expect(saveSettingsButton).toBeDisabled();

  const themeRadios = page.locator('input[name="themeRadio"]');
  const themeCount = await themeRadios.count();
  let alternativeThemeIndex = -1;
  for (let index = 0; index < themeCount; index += 1) {
    if (!(await themeRadios.nth(index).isChecked())) {
      alternativeThemeIndex = index;
      break;
    }
  }

  expect(alternativeThemeIndex).toBeGreaterThanOrEqual(0);

  const alternativeTheme = themeRadios.nth(alternativeThemeIndex);
  const alternativeThemeOption = page.locator('.theme-option').nth(alternativeThemeIndex);
  for (let attempt = 0; attempt < 3; attempt += 1) {
    await alternativeThemeOption.click();
    if (await saveSettingsButton.isEnabled().catch(() => false)) {
      break;
    }
    await page.waitForTimeout(500);
  }

  await expect(alternativeThemeOption).toHaveClass(/selected/, { timeout: 15000 });
  await expect(alternativeTheme).toBeChecked({ timeout: 15000 });
  await expect(saveSettingsButton).toBeEnabled({ timeout: 15000 });
  await page.getByRole('button', { name: 'Cancel' }).click();

  await page.goto('/admin/update-file-sizes', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Update Attachment File Sizes' })).toBeVisible({ timeout: 15000 });
  await expect(page.getByRole('button', { name: 'Update File Sizes' })).toBeVisible();

  await page.goto('/admin/update-preview-images', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Update Missing Preview Images' })).toBeVisible({ timeout: 15000 });
  await expect(page.getByRole('button', { name: 'Start Update' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Clear Results' })).toBeVisible();
});
