import { test, expect } from '@playwright/test';
import { adminLogin, orderFixture } from './helpers';

test('failed cash lookup blocks payment; retry permits a three-cent cash payment', async ({ page }) => {
  await adminLogin(page);
  let sessionAvailable = false;
  let paid = false;
  let sales = 0;
  await page.route('**/api/comandas/branch/1', route => route.fulfill({ json: [{ id: 1, code: '001', comandaStatusId: 2 }] }));
  await page.route('**/api/orders/open/branch/1', route => route.fulfill({ json: paid ? [] : [{ ...orderFixture, comandaId: 1, orderStatusId: 3 }] }));
  await page.route('**/api/orders/14', route => route.fulfill({ json: { ...orderFixture, comandaId: 1, orderStatusId: paid ? 4 : 3 } }));
  await page.route('**/api/cash/registers/*/open-session', route => sessionAvailable
    ? route.fulfill({ json: { id: 8, cashRegisterId: 1, cashSessionStatusId: 1 } })
    : route.fulfill({ status: 500, json: { detail: 'Unavailable' } }));
  await page.route('**/api/sales', route => {
    const body = route.request().postDataJSON();
    expect(body.cashSessionId).toBe(8);
    expect(body.payments[0].amount).toBe(0.03);
    expect(body.payments[0].paymentMethodId).toBe(1);
    sales++;
    paid = true;
    return route.fulfill({ json: 20 });
  });
  await page.goto('/');
  await page.getByTestId('comanda-tile-1').click();
  await expect(page.getByText('Não foi possível verificar o caixa. Tente novamente antes de confirmar o pagamento.')).toBeVisible();
  await expect(page.getByTestId('btn-confirm-payment')).toHaveCount(0);
  expect(sales).toBe(0);
  sessionAvailable = true;
  await page.getByRole('button', { name: 'Verificar caixa novamente' }).click();
  await page.getByTestId('input-payment-amount-0').fill('0,03');
  await page.getByTestId('btn-confirm-payment').click();
  await expect(page.getByTestId('payment-panel')).toHaveCount(0);
  expect(sales).toBe(1);
});
