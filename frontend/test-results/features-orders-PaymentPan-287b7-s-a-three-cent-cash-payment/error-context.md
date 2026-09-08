# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: features\orders\PaymentPanel.spec.ts >> failed cash lookup blocks payment; retry permits a three-cent cash payment
- Location: test\features\orders\PaymentPanel.spec.ts:4:1

# Error details

```
Error: expect(locator).toBeVisible() failed

Locator: getByText('Não foi possível verificar o caixa. Tente novamente antes de confirmar o pagamento.')
Expected: visible
Timeout: 5000ms
Error: element(s) not found

Call log:
  - Expect "toBeVisible" with timeout 5000ms
  - waiting for getByText('Não foi possível verificar o caixa. Tente novamente antes de confirmar o pagamento.')

```

```yaml
- alert:
  - text: Algo deu errado
  - paragraph: A tela travou por um erro inesperado. Recarregar a página costuma resolver — se persistir, avise o suporte.
  - button "Recarregar"
```

# Test source

```ts
  1  | import { test, expect } from '@playwright/test';
  2  | import { adminLogin, orderFixture } from './helpers';
  3  | 
  4  | test('failed cash lookup blocks payment; retry permits a three-cent cash payment', async ({ page }) => {
  5  |   await adminLogin(page);
  6  |   let sessionAvailable = false;
  7  |   let paid = false;
  8  |   let sales = 0;
  9  |   await page.route('**/api/comandas/branch/1', route => route.fulfill({ json: [{ id: 1, code: '001', comandaStatusId: 2 }] }));
  10 |   await page.route('**/api/orders/open/branch/1', route => route.fulfill({ json: paid ? [] : [{ ...orderFixture, comandaId: 1, orderStatusId: 3 }] }));
  11 |   await page.route('**/api/orders/14', route => route.fulfill({ json: { ...orderFixture, comandaId: 1, orderStatusId: paid ? 4 : 3 } }));
  12 |   await page.route('**/api/cash/registers/*/open-session', route => sessionAvailable
  13 |     ? route.fulfill({ json: { id: 8, cashRegisterId: 1, cashSessionStatusId: 1 } })
  14 |     : route.fulfill({ status: 500, json: { detail: 'Unavailable' } }));
  15 |   await page.route('**/api/sales', route => {
  16 |     const body = route.request().postDataJSON();
  17 |     expect(body.cashSessionId).toBe(8);
  18 |     expect(body.payments[0].amount).toBe(0.03);
  19 |     expect(body.payments[0].paymentMethodId).toBe(1);
  20 |     sales++;
  21 |     paid = true;
  22 |     return route.fulfill({ json: 20 });
  23 |   });
  24 |   await page.goto('/');
  25 |   await page.getByTestId('comanda-tile-1').click();
> 26 |   await expect(page.getByText('Não foi possível verificar o caixa. Tente novamente antes de confirmar o pagamento.')).toBeVisible();
     |                                                                                                                       ^ Error: expect(locator).toBeVisible() failed
  27 |   await expect(page.getByTestId('btn-confirm-payment')).toHaveCount(0);
  28 |   expect(sales).toBe(0);
  29 |   sessionAvailable = true;
  30 |   await page.getByRole('button', { name: 'Verificar caixa novamente' }).click();
  31 |   await page.getByTestId('input-payment-amount-0').fill('0,03');
  32 |   await page.getByTestId('btn-confirm-payment').click();
  33 |   await expect(page.getByTestId('payment-panel')).toHaveCount(0);
  34 |   expect(sales).toBe(1);
  35 | });
  36 | 
```