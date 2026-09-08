import { test, expect } from '@playwright/test';
import { adminLogin, orderFixture } from './helpers';

for (const empty of [false, true]) {
  test(`ready for dispatch moves the order immediately (without items: ${empty})`, async ({ page }) => {
    await adminLogin(page);
    let ready = false;
    await page.route('**/api/orders/open/branch/1', route => ready ? route.abort() : route.fulfill({ json: [{ ...orderFixture, items: empty ? [] : orderFixture.items }] }));
    await page.route('**/api/orders/14/ready-for-dispatch', route => {
      expect(route.request().method()).toBe('PUT');
      ready = true;
      return route.fulfill({ status: 204 });
    });
    await page.goto('/delivery');
    await expect(page.getByTestId('kanban-column-cozinha').getByTestId('order-card-14')).toBeVisible();
    await page.getByTestId('btn-action-14').click();
    await expect(page.getByTestId('kanban-column-aguardando').getByTestId('order-card-14')).toBeVisible();
    await expect(page.getByTestId('kanban-column-cozinha').getByTestId('order-card-14')).toHaveCount(0);
  });
}
