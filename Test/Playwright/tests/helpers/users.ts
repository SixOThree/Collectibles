import { expect, Page } from '@playwright/test';

export interface ManagedUserScenario {
  displayName: string;
  email: string;
  initialPassword: string;
  nextPassword: string;
  uniqueSuffix: string;
}

export interface CreateUserOptions {
  displayName: string;
  email: string;
  isActive?: boolean;
  password: string;
  roles?: string[];
}

export function createManagedUserScenario(prefix = 'pw-account'): ManagedUserScenario {
  const uniqueSuffix = `${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
  const passwordSeed = Math.random().toString(36).replace(/[^a-z]/g, '').slice(0, 8) || 'strongpass';

  return {
    uniqueSuffix,
    email: `${prefix}-${uniqueSuffix}@collectibles.local`,
    initialPassword: `Start!${passwordSeed}Aa1`,
    nextPassword: `Next!${passwordSeed}Bb2`,
    displayName: `PW Account ${uniqueSuffix}`,
  };
}

export async function createUserAsAdmin(page: Page, options: CreateUserOptions): Promise<string> {
  await page.goto('/users/new', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Create New User' })).toBeVisible({ timeout: 15000 });

  await page.getByLabel('Email').fill(options.email);
  await page.getByLabel(/^Password$/).fill(options.password);
  await page.getByLabel(/^Confirm Password$/).fill(options.password);
  await page.getByLabel('Display Name').fill(options.displayName);

  if (options.isActive === false) {
    await page.getByLabel('Active').uncheck();
  }

  for (const role of options.roles ?? []) {
    const roleCheckbox = page.locator('.form-check').filter({ hasText: role }).locator('input[type="checkbox"]');
    await roleCheckbox.check();
  }

  await page.getByRole('button', { name: 'Create User' }).click();
  await expect(page).toHaveURL(/\/users\/[^/]+$/);
  await expect(page.getByRole('heading', { name: 'User Details' })).toBeVisible({ timeout: 15000 });
  await expect(page.locator('body')).toContainText(options.email);
  await expect(page.locator('body')).toContainText('Confirmed');

  const match = page.url().match(/\/users\/([^/?#]+)/);
  if (!match) {
    throw new Error(`Unable to parse user id from URL: ${page.url()}`);
  }

  return match[1];
}
