import { test, expect } from '@playwright/test';
import { mockMenu, openIdentification } from './helpers';
import { fillNewCustomer, newCustomer } from './customerFactory';

test('CEP network failure allows manual address entry and registration', async ({ page }) => {
  await mockMenu(page);
  await page.route('**/api/branch-payment-method-settings/branch/7', route => route.fulfill({ json: {
    id: 1, branchId: 7, isActive: true, enablePix: false, enableBoleto: false,
    enableCreditCard: false, enableDebitCard: false, enableCashMachine: true,
  } }));
  let registrations = 0;
  let address: any;
  let order: any;
  await page.route('**/api/storefront/branches/7/customers', route => {
    registrations++;
    return route.fulfill({ json: { id: 901, companyId: 1 } });
  });
  await page.route('**/api/storefront/customer/addresses', route => {
    address = route.request().postDataJSON();
    expect(route.request().headers().authorization).toBe('Bearer customer-test-token');
    return route.fulfill({ json: { id: 902 } });
  });
  await page.route('**/api/storefront/branches/7/orders', route => {
    order = route.request().postDataJSON();
    return route.fulfill({ json: { orderId: 903 } });
  });
  await page.route('https://viacep.com.br/**', route => route.abort('failed'));
  await openIdentification(page);
  const auth = page.getByTestId('storefront-auth-modal');
  await auth.getByRole('tab', { name: 'Novo Cliente' }).click();
  const customer = newCustomer();
  await auth.getByLabel('Nome Completo').fill(customer.name);
  await auth.getByLabel('CPF', { exact: true }).fill(customer.cpf);
  await auth.getByLabel('E-mail', { exact: true }).fill(customer.email);
  await auth.getByLabel('Senha', { exact: true }).fill(customer.password);
  await auth.getByLabel('Confirmar Senha', { exact: true }).fill(customer.password);
  await auth.getByLabel('CEP', { exact: true }).fill('01001000');
  await expect(auth.getByLabel('Rua / Avenida')).toBeEditable();
  await expect(auth.getByLabel('Bairro / Cidade')).toBeEditable();
  await expect(auth.getByRole('status')).toContainText('Preencha o endereço manualmente');
  await auth.getByLabel('Rua / Avenida').fill('Rua Manual');
  await auth.getByLabel('Bairro / Cidade').fill('Centro, São Paulo - SP');
  await auth.getByLabel('Número', { exact: true }).fill('123');
  await auth.getByRole('button', { name: 'Cadastrar e Enviar Pedido' }).click();
  await expect(auth).toHaveCount(0);
  expect(registrations).toBe(1);
  expect(address).toMatchObject({ customerId: 901, street: 'Rua Manual', zipCode: '01001000' });
  await page.getByTestId('btn-submit-order').click();
  await page.getByRole('button', { name: /Retirar no Balcão/ }).click();
  await page.getByRole('button', { name: 'Maquininha', exact: true }).click();
  await page.getByTestId('btn-submit-order').click();
  await expect(page.getByText('Pedido Solicitado!', { exact: true })).toBeVisible();
  expect(order).toMatchObject({ customerId: 901, items: [{ productId: 1, quantity: 1 }] });
});

test('registration API errors are readable above the modal on small screens', async ({ page }) => {
  await mockMenu(page);
  await page.route('https://viacep.com.br/**', route => route.fulfill({ json: {
    logradouro: 'Rua Teste', bairro: 'Centro', localidade: 'São Paulo', uf: 'SP',
  } }));
  await page.route('**/api/storefront/branches/*/customers', route => route.fulfill({
    status: 409, json: { detail: 'CPF já cadastrado.' },
  }));
  await openIdentification(page);
  await fillNewCustomer(page, newCustomer());
  await page.getByRole('button', { name: 'Cadastrar e Enviar Pedido' }).click();
  const error = page.getByText('CPF já cadastrado.', { exact: true });
  await expect(error).toBeVisible();
  await expect(error).toBeFocused();
  await expect(error).toBeInViewport();
  // Visibility alone does not detect a dialog painted behind another fixed overlay.
  await expect.poll(() => error.evaluate(element => {
    const bounds = element.getBoundingClientRect();
    const hit = document.elementFromPoint(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2);
    return element.contains(hit);
  })).toBe(true);
});

test('a stalled CEP lookup times out and restores the submit button', async ({ page }) => {
  await mockMenu(page);
  await page.route('https://viacep.com.br/**', () => {});
  await openIdentification(page);
  const auth = page.getByTestId('storefront-auth-modal');
  await auth.getByRole('tab', { name: 'Novo Cliente' }).click();
  await page.clock.install();
  await auth.getByLabel('CEP', { exact: true }).fill('01001000');
  await expect(auth.getByRole('button', { name: 'Processando...' })).toBeDisabled();
  await page.clock.fastForward(8100);
  await expect(auth.getByRole('status')).toContainText('Não foi possível consultar o CEP');
  await expect(auth.getByRole('button', { name: 'Cadastrar e Enviar Pedido' })).toBeEnabled();
  await expect(auth.getByLabel('Número', { exact: true })).toBeEditable();
});

test('changing CEP cancels the previous lookup without overwriting the new address', async ({ page }) => {
  await mockMenu(page);
  let releaseFirst!: () => void;
  const firstResponse = new Promise<void>(resolve => { releaseFirst = resolve; });
  let firstStarted = false;
  await page.route('https://viacep.com.br/**', async route => {
    if (route.request().url().includes('01001000')) {
      firstStarted = true;
      await firstResponse;
      await route.fulfill({ json: { logradouro: 'Rua Antiga', bairro: 'Antigo' } }).catch(() => {});
    } else {
      await route.fulfill({ json: { logradouro: 'Rua Nova', bairro: 'Novo', localidade: 'São Paulo', uf: 'SP' } });
    }
  });
  await openIdentification(page);
  const auth = page.getByTestId('storefront-auth-modal');
  await auth.getByRole('tab', { name: 'Novo Cliente' }).click();
  await auth.getByLabel('CEP', { exact: true }).fill('01001000');
  await expect.poll(() => firstStarted).toBe(true);
  await auth.getByLabel('CEP', { exact: true }).fill('02002000');
  await expect(auth.getByLabel('Rua / Avenida')).toHaveValue('Rua Nova');
  releaseFirst();
  await expect(auth.getByLabel('Bairro / Cidade')).toHaveValue('Novo, São Paulo - SP');
  await expect(auth.getByLabel('Rua / Avenida')).toHaveValue('Rua Nova');
});

test('manual entry can cancel a stalled CEP lookup immediately', async ({ page }) => {
  await mockMenu(page);
  await page.route('https://viacep.com.br/**', () => {});
  await openIdentification(page);
  const auth = page.getByTestId('storefront-auth-modal');
  await auth.getByRole('tab', { name: 'Novo Cliente' }).click();
  await auth.getByLabel('CEP', { exact: true }).fill('01001000');
  await auth.getByRole('button', { name: 'Preencher endereço manualmente' }).click();
  await expect(auth.getByRole('button', { name: 'Cadastrar e Enviar Pedido' })).toBeEnabled();
  await auth.getByLabel('Rua / Avenida').fill('Rua Manual');
  await expect(auth.getByLabel('Rua / Avenida')).toHaveValue('Rua Manual');
});

test('required field errors receive focus inside a short viewport', async ({ page }) => {
  await mockMenu(page);
  await openIdentification(page);
  await page.setViewportSize({ width: 390, height: 360 });
  const auth = page.getByTestId('storefront-auth-modal');
  await auth.getByRole('tab', { name: 'Novo Cliente' }).click();
  await auth.getByLabel('Senha', { exact: true }).fill('abcdef');
  await auth.getByLabel('Confirmar Senha', { exact: true }).fill('abcdef');
  await auth.getByRole('button', { name: 'Cadastrar e Enviar Pedido' }).click();
  await expect(auth.getByRole('alert')).toContainText('Preencha nome');
  await expect(auth.getByRole('alert')).toBeFocused();
  await expect(auth.getByRole('alert')).toBeInViewport();
});

test('registration has independent password toggles and only sends matching passwords of at least six characters', async ({ page }) => {
  await mockMenu(page);
  await page.route('**/api/branch-payment-method-settings/branch/7', route => route.fulfill({ json: null }));
  await page.route('**/api/branch-payment-method-settings/resolve*', route => route.fulfill({ json: null }));
  let requests = 0;
  let submittedPassword = '';
  await page.route('**/api/storefront/branches/*/customers', route => {
    requests++;
    submittedPassword = route.request().postDataJSON().password;
    return route.fulfill({ json: { id: 1, companyId: 1 } });
  });
  await page.route('**/api/storefront/customer/addresses', route => route.fulfill({ json: { id: 1, companyId: 1 } }));
  await page.route('https://viacep.com.br/**', route => route.fulfill({ json: { logradouro: 'Rua Teste', bairro: 'Centro', localidade: 'São Paulo', uf: 'SP' } }));
  await openIdentification(page);
  const auth = page.getByTestId('storefront-auth-modal');
  await auth.getByRole('tab', { name: 'Novo Cliente' }).click();
  const password = auth.getByLabel('Senha', { exact: true });
  const confirmation = auth.getByLabel('Confirmar Senha', { exact: true });
  await password.fill('abcdef');
  await confirmation.fill('abcdef ');
  await auth.getByRole('button', { name: 'Mostrar senha', exact: true }).click();
  await expect(password).toHaveAttribute('type', 'text');
  await expect(confirmation).toHaveAttribute('type', 'password');
  await auth.getByRole('button', { name: 'Mostrar confirmar senha', exact: true }).click();
  await expect(confirmation).toHaveAttribute('type', 'text');
  await auth.getByRole('button', { name: 'Ocultar senha', exact: true }).click();
  await expect(password).toHaveAttribute('type', 'password');
  const submit = auth.getByRole('button', { name: 'Cadastrar e Enviar Pedido' });
  await submit.click();
  await expect(auth.getByRole('alert')).toHaveText('As senhas não coincidem.');
  expect(requests).toBe(0);
  await password.fill('12345');
  await confirmation.fill('12345');
  await submit.click();
  await expect(auth.getByRole('alert')).toHaveText('As senhas devem ter no mínimo 6 caracteres.');
  expect(requests).toBe(0);
  await password.fill('123456');
  await confirmation.fill('123456');
  await auth.getByLabel('Nome Completo').fill('Cliente Teste');
  await auth.getByLabel('CPF', { exact: true }).fill('12345678909');
  await auth.getByLabel('E-mail', { exact: true }).fill('cliente@example.com');
  await auth.getByLabel('CEP', { exact: true }).fill('01001000');
  await expect(auth.getByLabel('Rua / Avenida')).toHaveValue('Rua Teste');
  await auth.getByLabel('Número', { exact: true }).fill('12');
  await submit.click();
  await expect(auth).toHaveCount(0);
  expect(requests).toBe(1);
  expect(submittedPassword).toBe('123456');
});
