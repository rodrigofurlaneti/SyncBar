import { test, expect } from '@playwright/test';
import { mockMenu, enterCheckout } from './helpers';

test('checkout respects branch flags and resets a selection disabled after reopening', async ({ page }) => {
  await mockMenu(page);
  let flags = { enablePix: true, enableBoleto: false, enableCreditCard: true, enableDebitCard: false, enableCashMachine: true };
  await page.route('**/api/branch-payment-method-settings/branch/7', route => route.fulfill({ json: { id: 1, branchId: 7, isActive: true, ...flags } }));
  await enterCheckout(page);
  const methods = page.getByRole('group', { name: 'Forma de Pagamento', exact: true });
  await expect(methods.getByRole('button', { name: 'PIX (Online)' })).toHaveAttribute('aria-pressed', 'true');
  await expect(methods.getByRole('button', { name: 'Cartão de Crédito', exact: true })).toBeVisible();
  await expect(methods.getByRole('button', { name: 'Boleto', exact: true })).toHaveCount(0);
  await expect(methods.getByRole('button', { name: /Débito/ })).toHaveCount(0);
  await methods.getByRole('button', { name: 'Cartão de Crédito', exact: true }).click();
  flags = { ...flags, enablePix: false, enableCreditCard: false };
  await page.keyboard.press('Escape');
  await page.getByTestId('btn-open-cart').click();
  await page.getByTestId('btn-submit-order').click();
  await expect(methods.getByRole('button', { name: 'Maquininha', exact: true })).toHaveAttribute('aria-pressed', 'true');
  await expect(methods.getByRole('button', { name: 'Cartão de Crédito', exact: true })).toHaveCount(0);
  await expect(methods.getByRole('button', { name: 'PIX (Online)' })).toHaveCount(0);
});

for (const scenario of ['disabled', 'error'] as const) {
  test(`checkout blocks order submission when payment methods are ${scenario}`, async ({ page }) => {
    await mockMenu(page);
    await page.route('**/api/branch-payment-method-settings/branch/7', route => scenario === 'error'
      ? route.fulfill({ status: 500, json: {} })
      : route.fulfill({ json: { isActive: true, enablePix: false, enableBoleto: false, enableCreditCard: false, enableDebitCard: false, enableCashMachine: false } }));
    await enterCheckout(page);
    await expect(page.getByTestId('btn-submit-order')).toBeDisabled();
    await expect(page.getByText(scenario === 'error' ? 'Não foi possível carregar as formas de pagamento.' : 'Nenhuma forma de pagamento disponível. Entre em contato com a loja.', { exact: false })).toBeVisible();
  });
}

