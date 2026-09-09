import { test, expect, type Page } from '@playwright/test';
import { mockMenu, openIdentification } from './helpers';
import { fillNewCustomer, newCustomer } from './customerFactory';

async function setup(page: Page) {
  await mockMenu(page);
  await page.route('**/api/branch-payment-method-settings/branch/1', route => route.fulfill({ json: {
    id: 1, branchId: 1, isActive: true, enablePix: true, enableBoleto: true,
    enableCreditCard: true, enableDebitCard: true, enableCashMachine: true,
  } }));
  await page.route('https://viacep.com.br/**', route => route.fulfill({ json: {
    logradouro: 'Praça da Sé', bairro: 'Sé', localidade: 'São Paulo', uf: 'SP',
  } }));
}

async function register(page: Page) {
  const customer = newCustomer();
  const registrations: unknown[] = [];
  const addresses: unknown[] = [];
  await page.route('**/api/storefront/branches/*/customers', route => {
    registrations.push(route.request().postDataJSON());
    return route.fulfill({ json: { id: 901, companyId: 1 } });
  });
  await page.route('**/api/storefront/customer/addresses', route => {
    addresses.push(route.request().postDataJSON());
    return route.fulfill({ json: { id: 902 } });
  });
  await openIdentification(page, 1);
  await fillNewCustomer(page, customer);
  await page.getByRole('button', { name: 'Cadastrar e Enviar Pedido' }).click();
  await expect(page.getByTestId('storefront-auth-modal')).toHaveCount(0);
  expect(registrations).toEqual([expect.objectContaining({ userName: customer.name, cpf: customer.cpf, email: customer.email })]);
  expect(addresses).toEqual([expect.objectContaining({ customerId: 901, zipCode: customer.zipCode })]);
  await page.getByTestId('btn-submit-order').click();
  await page.getByRole('button', { name: /Retirar no Balcão/ }).click();
  return customer;
}

for (const method of ['PIX', 'BOLETO', 'MAQUININHA', 'CREDITO', 'DEBITO'] as const) {
  test(`new customer, address, pickup order and ${method}`, async ({ page }) => {
    await setup(page);
    const orders: any[] = [];
    await page.route('**/api/storefront/branches/1/orders', route => {
      orders.push(route.request().postDataJSON());
      return route.fulfill({ json: { orderId: 903 } });
    });
    const payments: any[] = [];
    await page.route('**/api/checkout/*', route => {
      payments.push(route.request().postDataJSON());
      return route.fulfill({ json: {
        paymentId: 904, asaasPaymentId: 'pay_test', status: method === 'CREDITO' ? 'CONFIRMED' : 'PENDING', value: 20,
        invoiceUrl: 'https://example.com/test-payment', cardBrand: 'VISA', last4Digits: '4242',
        pixPayload: 'PIX-SINTETICO-E2E', pixQrCodeBase64: null,
        identificationField: 'BOLETO-SINTETICO-E2E', bankSlipUrl: 'https://example.com/boleto',
      } });
    });
    await page.route('**/api/asaas/payments/order/903', route => route.fulfill({ json: { id: 904, status: 'CONFIRMED' } }));
    const customer = await register(page);
    const label = { PIX: 'PIX (Online)', BOLETO: 'Boleto', MAQUININHA: 'Maquininha', CREDITO: 'Cartão de Crédito', DEBITO: 'Cartão de Débito (Asaas)' }[method];
    await page.getByRole('group', { name: 'Forma de Pagamento', exact: true }).getByRole('button', { name: label, exact: true }).click();
    if (method === 'CREDITO') {
      await page.getByLabel('Nome no cartão', { exact: true }).fill(customer.name);
      await page.getByPlaceholder('0000 0000 0000 0000').fill('4242424242424242');
      await page.getByPlaceholder('MM', { exact: true }).fill('12');
      await page.getByPlaceholder('AAAA', { exact: true }).fill(String(new Date().getFullYear() + 2));
      await page.getByLabel('CVV', { exact: true }).fill('123');
    }
    await page.getByTestId('btn-submit-order').click();
    await expect.poll(() => orders.length).toBe(1);
    expect(orders[0]).toMatchObject({ customerId: 901, customerName: customer.name, deliveryType: 'PICKUP', items: [{ productId: 1, quantity: 1 }] });
    if (method === 'MAQUININHA') {
      await expect(page.getByText('Pedido Solicitado!', { exact: true })).toBeVisible();
      expect(payments).toHaveLength(0);
    } else {
      await expect(page.getByText('✓ Pago com sucesso', { exact: true })).toBeVisible({ timeout: 15000 });
      expect(payments).toHaveLength(1);
      expect(payments[0]).toMatchObject({ customerOrderId: 903 });
      if (method === 'CREDITO') expect(payments[0].card).toMatchObject({ holderName: customer.name, number: '4242424242424242' });
    }
  });
}

test('registration API failure keeps the form and never creates an address or order', async ({ page }) => {
  await setup(page);
  let downstream = 0;
  await page.route('**/api/storefront/branches/*/customers', route => route.fulfill({ status: 409, json: { title: 'Customer.Exists', detail: 'Cliente já cadastrado.' } }));
  await page.route('**/api/storefront/customer/addresses', route => { downstream++; return route.fulfill({ json: { id: 1, companyId: 1 } }); });
  await page.route('**/api/storefront/branches/1/orders', route => { downstream++; return route.fulfill({ json: { orderId: 1 } }); });
  await openIdentification(page, 1);
  await fillNewCustomer(page, newCustomer());
  await page.getByRole('button', { name: 'Cadastrar e Enviar Pedido' }).click();
  await expect(page.getByText('Cliente já cadastrado.', { exact: true })).toBeVisible();
  await expect(page.getByTestId('storefront-auth-modal')).toBeVisible();
  expect(downstream).toBe(0);
});

test('delivery uses the newly registered customer address', async ({ page }) => {
  await setup(page);
  await page.route('**/api/storefront/customer/addresses/customer/901', route => route.fulfill({ json: [
    { id: 902, customerId: 901, street: 'Praça da Sé', number: '100', zipCode: '01001000', isActive: true },
  ] }));
  let order: any;
  await page.route('**/api/storefront/branches/1/orders', route => {
    order = route.request().postDataJSON();
    return route.fulfill({ json: { orderId: 903 } });
  });
  await register(page);
  await page.getByRole('button', { name: /Enviar com Motoboy/ }).click();
  await page.getByRole('radio', { name: /Praça da Sé/ }).click();
  await page.getByRole('button', { name: 'Maquininha', exact: true }).click();
  await page.getByTestId('btn-submit-order').click();
  await expect(page.getByText('Pedido Solicitado!', { exact: true })).toBeVisible();
  expect(order).toMatchObject({ customerId: 901, deliveryType: 'DELIVERY', addressId: 902 });
});

for (const failure of ['order', 'payment'] as const) {
  test(`${failure} failure does not duplicate an order or report payment success`, async ({ page }) => {
    await setup(page);
    let orders = 0;
    let payments = 0;
    await page.route('**/api/storefront/branches/1/orders', route => {
      orders++;
      return failure === 'order'
        ? route.fulfill({ status: 500, json: { detail: 'Falha de teste ao criar pedido.' } })
        : route.fulfill({ json: { orderId: 903 } });
    });
    await page.route('**/api/checkout/pix', route => {
      payments++;
      return route.fulfill({ status: 502, json: { detail: 'Falha de teste no pagamento.' } });
    });
    await register(page);
    await page.getByTestId('btn-submit-order').click();
    await expect(page.getByText(failure === 'order' ? 'Ops!' : 'Pagamento não concluído', { exact: true })).toBeVisible();
    expect(orders).toBe(1);
    expect(payments).toBe(failure === 'order' ? 0 : 1);
    await expect(page.getByText('✓ Pago com sucesso', { exact: true })).toHaveCount(0);
  });
}

test('menu API failure displays an error instead of a usable cart', async ({ page }) => {
  await page.route('**/api/storefront/branches/1/menu', route => route.fulfill({ status: 503, json: {} }));
  await page.goto('/cardapio/1');
  await expect(page.getByTestId('error-menu')).toBeVisible();
  await expect(page.getByTestId('btn-open-cart')).toHaveCount(0);
});

test('search and removing the last item leave an empty cart', async ({ page }) => {
  await setup(page);
  await page.goto('/cardapio/1');
  await page.getByTestId('input-menu-search').fill('Lanche');
  await page.getByTestId('btn-add-item-1').click();
  await page.getByTestId('btn-open-cart').click();
  await expect(page.getByTestId('cart-item-1')).toBeVisible();
  await page.getByTestId('btn-remove-1').click();
  await expect(page.getByTestId('empty-cart-msg')).toBeVisible();
  await expect(page.getByTestId('btn-submit-order')).toHaveCount(0);
});
