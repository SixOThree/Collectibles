import fs from 'fs/promises';
import { expect, test } from '@playwright/test';
import { authFile, expectInvalidLogin, loginViaProtectedRoute, logout } from '../helpers/auth';
import { createManagedUserScenario, createUserAsAdmin } from '../helpers/users';

test.use({ storageState: authFile('admin') });

test('admin-created user can manage their account end to end', async ({ page }) => {
  test.slow();

  const scenario = createManagedUserScenario();

  await createUserAsAdmin(page, {
    email: scenario.email,
    password: scenario.initialPassword,
    displayName: scenario.displayName,
  });

  await logout(page);
  await loginViaProtectedRoute(page, scenario.email, scenario.initialPassword);
  await expect(page.getByRole('heading', { name: 'My Showcases' })).toBeVisible({ timeout: 15000 });

  await page.goto('/Account/Manage', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Profile' })).toBeVisible({ timeout: 15000 });
  await page.getByLabel('Display name').fill(scenario.displayName);
  await page.getByRole('button', { name: 'Save' }).click();
  await expect(page.locator('.alert.alert-success')).toContainText('Your profile has been updated');
  await expect(page.locator('ul.navbar-nav.ms-auto li.nav-item.dropdown > a.dropdown-toggle')).toContainText(
    scenario.displayName,
  );

  await page.goto('/Account/Manage/Email', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Manage email' })).toBeVisible({ timeout: 15000 });
  await expect(page.getByLabel(/^Email$/)).toHaveValue(scenario.email);
  await expect(page.getByLabel('New email')).toHaveValue(scenario.email);

  await page.goto('/Account/Manage/ChangePassword', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Change password' })).toBeVisible({ timeout: 15000 });
  await page.getByLabel('Old password').fill(scenario.initialPassword);
  await page.getByLabel('New password').fill(scenario.nextPassword);
  await page.getByLabel('Confirm password').fill(scenario.nextPassword);
  await page.getByRole('button', { name: 'Update password' }).click();
  await expect(page.locator('.alert.alert-success')).toContainText('Your password has been changed');

  await logout(page);
  await expectInvalidLogin(page, scenario.email, scenario.initialPassword);
  await loginViaProtectedRoute(page, scenario.email, scenario.nextPassword);
  await expect(page.getByRole('heading', { name: 'My Showcases' })).toBeVisible({ timeout: 15000 });

  await page.goto('/Account/Manage/PersonalData', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Personal Data' })).toBeVisible({ timeout: 15000 });

  const downloadPromise = page.waitForEvent('download');
  await page.getByRole('button', { name: 'Download' }).click();
  const download = await downloadPromise;
  expect(download.suggestedFilename()).toBe('PersonalData.json');

  const downloadPath = await download.path();
  if (!downloadPath) {
    throw new Error('Expected a personal-data download path.');
  }

  const personalData = JSON.parse(await fs.readFile(downloadPath, 'utf8')) as Record<string, string>;
  expect(personalData.Email).toBe(scenario.email);

  await page.goto('/Account/Manage/TwoFactorAuthentication', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Two-factor authentication (2FA)' })).toBeVisible({ timeout: 15000 });
  await expect(page.getByRole('link', { name: 'Add authenticator app' })).toBeVisible();

  await page.goto('/Account/Manage/DeletePersonalData', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Delete Personal Data' })).toBeVisible({ timeout: 15000 });
  await page.getByLabel('Password').fill(scenario.nextPassword);
  await page.getByRole('button', { name: 'Delete data and close my account' }).click();

  await expect(page).toHaveURL(/\/Account\/Login/);
  await expect(page.getByRole('heading', { name: 'Log in', exact: true })).toBeVisible({ timeout: 15000 });

  await expectInvalidLogin(page, scenario.email, scenario.nextPassword);
});
