import type { Page } from '@playwright/test';

export async function mockMenu(page: Page) {
  await page.route('**/api/storefront/branches/*/menu', route => route.fulfill({ json: {
    companyId: 1,
    items: [{ id: 1, name: 'Lanche', salePrice: 20, categoryName: 'Lanches', complementGroups: [] }],
  } }));
  await page.route('**/api/customeraddresses/customer/*', route => route.fulfill({ json: [] }));
}

export async function openIdentification(page: Page, branchId = 7) {
  await page.goto(`/cardapio/${branchId}`);
  await page.getByTestId('btn-add-item-1').click();
  await page.getByTestId('btn-open-cart').click();
  await page.getByTestId('btn-submit-order').click();
}

export async function enterCheckout(page: Page, branchId = 7) {
  await page.route('**/api/auth/customer-login', route => route.fulfill({ json: { customerId: 1, userName: 'Cliente' } }));
  await openIdentification(page, branchId);
  const auth = page.getByTestId('storefront-auth-modal');
  await auth.getByLabel('E-mail', { exact: true }).fill('cliente@example.com');
  await auth.getByLabel('Senha', { exact: true }).fill('123456');
  await auth.getByRole('button', { name: 'Entrar e Finalizar Pedido' }).click();
  await page.getByTestId('btn-submit-order').click();
}
