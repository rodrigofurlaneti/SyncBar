import { test, expect, type Page } from '@playwright/test';
import { fillNewCustomer, newCustomer } from '../test/features/storeFront/customerFactory';

test('new customer and address persist and allow login at cardapio/1', async ({ page }, testInfo) => {
  const customer = newCustomer();
  async function identify(page: Page) {
    const menuResponse = page.waitForResponse(response => response.url().endsWith('/api/storefront/branches/1/menu'));
    await page.goto('/cardapio/1');
    const response = await menuResponse;
    expect(response.ok(), 'The real menu API must respond successfully').toBeTruthy();
    const menu = await response.json();
    const product = menu.items.find((item: any) => !item.complementGroups?.length);
    expect(product, 'A product without complements is required for this registration test').toBeTruthy();
    await page.getByTestId(`btn-add-item-${product.id}`).click();
    await page.getByTestId('btn-open-cart').click();
    await page.getByTestId('btn-submit-order').click();
  }
  await identify(page);
  await fillNewCustomer(page, customer);
  const registration = page.waitForResponse(response => response.request().method() === 'POST' && response.url().endsWith('/api/storefront/branches/1/customers'));
  const address = page.waitForResponse(response => response.request().method() === 'POST' && response.url().endsWith('/api/storefront/customer/addresses')).catch(() => null);
  await page.getByRole('button', { name: 'Cadastrar e Enviar Pedido' }).click();
  const result = await registration;
  expect(result.status(), 'Real customer registration status').toBeGreaterThanOrEqual(200);
  expect(result.status(), 'Real customer registration status').toBeLessThan(300);
  const registered = await result.json();
  await testInfo.attach('created-customer', { body: JSON.stringify({ name: customer.name, email: customer.email, customerId: registered.id }), contentType: 'application/json' });
  expect((await address)?.ok(), 'Real address registration status').toBeTruthy();
  await expect(page.getByTestId('storefront-auth-modal')).toHaveCount(0);

  // Reload clears the in-memory session: login must validate the persisted account.
  await identify(page);
  const auth = page.getByTestId('storefront-auth-modal');
  await auth.getByLabel('E-mail', { exact: true }).fill(customer.email);
  await auth.getByLabel('Senha', { exact: true }).fill(customer.password);
  const login = page.waitForResponse(response => response.url().endsWith('/api/auth/customer-login'));
  await auth.getByRole('button', { name: 'Entrar e Finalizar Pedido' }).click();
  expect((await login).ok(), 'The new account must authenticate against the real API').toBeTruthy();
  await expect(auth).toHaveCount(0);
  await page.getByTestId('btn-submit-order').click();
  await expect(page.getByRole('group', { name: 'Forma de Pagamento', exact: true })).toBeVisible();
});
