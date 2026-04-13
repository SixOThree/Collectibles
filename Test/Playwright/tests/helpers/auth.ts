import { expect, Page } from '@playwright/test';
import path from 'path';

export type AuthRole = 'admin' | 'user';

export const authDir = path.resolve(__dirname, '../../playwright/.auth');

export function authFile(role: AuthRole): string {
  return path.join(authDir, `${role}.json`);
}

export async function loginViaProtectedRoute(
  page: Page,
  email: string,
  password: string,
  protectedRoute = '/showcases',
): Promise<void> {
  await page.goto(protectedRoute, { waitUntil: 'domcontentloaded' });
  await expectLoginPage(page);

  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Log in' }).click();

  await expect(page).toHaveURL(new RegExp(`${escapeForRegex(protectedRoute)}$`));
}

export async function expectInvalidLogin(page: Page, email: string, password: string): Promise<void> {
  await page.goto('/Account/Login', { waitUntil: 'domcontentloaded' });
  await expectLoginPage(page);

  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Log in' }).click();

  await expect(page).toHaveURL(/\/Account\/Login/);
  await expect(page.getByText('Error: Invalid login attempt.', { exact: true })).toBeVisible();
}

export async function logout(page: Page): Promise<void> {
  const accountMenu = page.locator('ul.navbar-nav.ms-auto li.nav-item.dropdown > a.dropdown-toggle');
  await expect(accountMenu).toBeVisible({ timeout: 15000 });
  await accountMenu.click();
  await page.getByRole('button', { name: 'Logout' }).click();

  await expect(page.getByRole('link', { name: 'Login' })).toBeVisible({ timeout: 15000 });
}

export async function expectLoginPage(page: Page): Promise<void> {
  await expect(page.getByRole('heading', { name: 'Log in', exact: true })).toBeVisible({ timeout: 15000 });
}

function escapeForRegex(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
