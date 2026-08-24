import { test, expect } from '@playwright/test';

test('login form works', async ({ page }) => {
  await page.goto('/login');

  await page.getByLabel('Email').fill('test@example.com');
  await page.getByLabel('Password').fill('Password123!');

  await page.getByRole('button', {
    name: 'Sign in',
    exact: true,
  }).click();

  await expect(page).toHaveURL('/');
});