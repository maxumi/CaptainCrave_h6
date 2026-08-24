import { test, expect } from '@playwright/test';

// tests for login first then checking if i can access cart af
test('Add to cart works', async ({ page }) => {
  await page.goto('/login');

  await page.getByLabel('Email').fill('test@example.com');
  await page.getByLabel('Password').fill('Password123!');

  await page.getByRole('button', {
    name: 'Sign in',
    exact: true,
  }).click();

  await expect(page).toHaveURL('/');

  await page.goto('/restaurants');

  const firstRestaurant = page.locator('.restaurant-card-link').first();
  await expect(firstRestaurant).toBeVisible();

  await firstRestaurant.click();

  await expect(page).toHaveURL(/\/restaurantInfo\?restaurantId=\d+/);

  const firstMenuItem = page.locator('.menu-card').first();
  await expect(firstMenuItem).toBeVisible();

  const itemName = await firstMenuItem
    .getByRole('heading', { level: 3 })
    .innerText();

  const addButton = firstMenuItem.locator('.add-btn');

  await expect(addButton).toBeEnabled();
  await addButton.click();

  await page.goto('/cart');

  const cartItem = page.locator('.cart-item').filter({
    hasText: itemName,
  });

  await expect(cartItem).toBeVisible();
  await expect(cartItem.locator('.amount-input')).toHaveValue('1');
});