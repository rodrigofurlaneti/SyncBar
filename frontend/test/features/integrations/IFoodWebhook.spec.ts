import { test, expect } from '@playwright/test';
import { adminLogin } from '../orders/helpers';

test('selects and persists iFood event delivery mode and shows its exclusive webhook URL', async ({ page }) => {
  await adminLogin(page);
  let mode = 'Polling';
  const writes: string[] = [];
  await page.route('**/api/integrations/ifood/company/1', route => route.fulfill({ json: {
    hasCredentials: true, clientId: 'test-client', enabled: true, ifoodCustomerId: null,
    lastConnectionTestAt: null, lastConnectionTestSucceeded: null, eventDeliveryMode: mode,
  } }));
  await page.route('**/api/integrations/ifood', route => {
    expect(route.request().method()).toBe('PUT');
    mode = route.request().postDataJSON().eventDeliveryMode;
    writes.push(mode);
    return route.fulfill({ status: 204 });
  });
  await page.goto('/integracoes/ifood');
  const selection = page.getByLabel('Recebimento de eventos');
  await expect(selection).toHaveValue('Polling');
  await selection.selectOption('Webhook');
  await expect(page.getByLabel('URL do webhook iFood')).toHaveValue('http://localhost:5173/api/webhook/ifood');
  await expect(page.getByText('Este endereço usa HTTP.', { exact: false })).toBeVisible();
  await page.getByRole('button', { name: 'Salvar credenciais', exact: true }).click();
  await expect.poll(() => writes).toEqual(['Webhook']);
  await page.reload();
  await expect(selection).toHaveValue('Webhook');
  await selection.selectOption('Polling');
  await page.getByRole('button', { name: 'Salvar credenciais', exact: true }).click();
  await expect.poll(() => writes).toEqual(['Webhook', 'Polling']);
  await expect(page.getByLabel('URL do webhook iFood')).toHaveCount(0);
});

test('shows the API validation message when credentials cannot be saved', async ({ page }) => {
  await adminLogin(page);
  await page.route('**/api/integrations/ifood/company/1', route => route.fulfill({ json: {
    hasCredentials: true, clientId: 'test-client', enabled: true, eventDeliveryMode: 'Polling',
  } }));
  await page.route('**/api/integrations/ifood', route => route.fulfill({ status: 400, json: {
    title: 'Ifood.InvalidCredentials', detail: 'Confira o Client ID informado.',
  } }));
  await page.goto('/integracoes/ifood');
  await expect(page.getByLabel('Client ID', { exact: true })).toHaveValue('test-client');
  await page.getByRole('button', { name: 'Salvar credenciais', exact: true }).click();
  await expect(page.getByText('Confira o Client ID informado.', { exact: true })).toBeVisible();
});
