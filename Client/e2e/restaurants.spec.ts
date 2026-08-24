import { test, expect } from '@playwright/test';

test('can open a restaurant page and view its information', async ({ page }) => {
  await page.goto('/restaurants');

  await expect(
    page.getByRole('heading', { level: 1 })
  ).toBeVisible();

  const firstRestaurant = page.locator('.restaurant-card-link').first();

  await expect(firstRestaurant).toBeVisible();

  const restaurantName = await firstRestaurant
    .getByRole('heading', { level: 2 })
    .innerText();

  await firstRestaurant.click();

  // Verify that the URL contains the restaurantId query parameter
  await expect(page).toHaveURL(/\/restaurantInfo\?restaurantId=\d+/);

  await expect(
    page.getByRole('heading', {
      level: 1,
      name: restaurantName,
    })
  ).toBeVisible();

  await expect(
    page.getByRole('heading', { level: 2 })
  ).toBeVisible();
});