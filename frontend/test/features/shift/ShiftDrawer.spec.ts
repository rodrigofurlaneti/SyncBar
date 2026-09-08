import { test, expect } from '@playwright/test';
import { adminLogin } from '../orders/helpers';

test('route not found must not offer opening a shift', async ({ page }) => {
  await adminLogin(page);
  await page.route('**/api/shift-closing/open?*', route => route.fulfill({ status: 404, body: '' }));
  await page.getByRole('button', { name: 'Turno', exact: true }).click();
  await expect(page.getByTestId('shift-query-error')).toBeVisible();
  await expect(page.getByTestId('open-shift-btn')).toHaveCount(0);
  await expect(page.getByText('Nenhum turno aberto nesta filial.')).toHaveCount(0);
});

test('missing open shift permits creation and refreshes its state', async ({ page }) => {
  await adminLogin(page);
  let opened = false;
  await page.route('**/api/shift-closing/open?*', route => opened
    ? route.fulfill({ json: { id: 42, branchId: 1, periodStart: '2026-09-08T12:00:00Z' } })
    : route.fulfill({ status: 404, json: { title: 'ShiftClosing.NotFound', detail: 'No open shift for this branch.' } }));
  await page.route('**/api/shift-closing', route => {
    expect(route.request().method()).toBe('POST');
    expect(route.request().postDataJSON()).toMatchObject({ branchId: 1, openedByEmployeeId: 1 });
    opened = true;
    return route.fulfill({ status: 201, json: 42 });
  });
  await page.getByRole('button', { name: 'Turno', exact: true }).click();
  await page.getByTestId('open-shift-btn').click();
  await expect(page.getByTestId('close-shift-view')).toContainText('#42');
  await expect(page.getByTestId('open-shift-view')).toHaveCount(0);
});
