import { test, expect } from '@playwright/test';

test('edit customer binding keeps its label clear of the heading and input', async ({ page }) => {
  await page.route('**/api/**', async route => {
    const url = route.request().url();
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 200 });
    } else if (url.includes('/auth/login')) {
      await route.fulfill({ json: { accessToken: 'token', user: { id: 1, name: 'Admin', companyId: 1, branchId: 1 } } });
    } else if (url.includes('/access/my-features')) {
      await route.fulfill({ json: { canManageAccess: true, features: [] } });
    } else if (url.includes('/asaas/customers/company/')) {
      await route.fulfill({ json: [{ id: 1, customerId: 1, companyId: 1, asaasCustomerId: 'cus_000005219613', isActive: true }] });
    } else if (url.includes('/api/customers/company/')) {
      await route.fulfill({ json: [{ id: 1, name: 'Joaquim Ferreira de Albuquerque' }] });
    } else {
      await route.fulfill({ json: [] });
    }
  });
  await page.goto('/login');
  await page.getByTestId('username').fill('admin');
  await page.getByTestId('password').fill('123');
  await page.getByTestId('submit-login').click();
  await page.waitForURL('**/');
  await page.goto('/integracoes/asaas');
  await page.getByRole('tab', { name: 'Clientes', exact: true }).click();
  await page.getByRole('button', { name: 'Editar', exact: true }).click();
  const modal = page.getByRole('dialog');
  const label = modal.getByText('ID do cliente no Asaas', { exact: true });
  const input = modal.getByRole('textbox', { name: 'ID do cliente no Asaas' });
  for (const viewport of [{ width: 1865, height: 868 }, { width: 390, height: 600 }]) {
    await page.setViewportSize(viewport);
    await expect(input).toHaveValue('cus_000005219613');
    await expect.poll(async () => {
      const heading = await modal.locator('.modal-head').boundingBox();
      const text = await label.boundingBox();
      return text!.y - (heading!.y + heading!.height);
    }).toBeGreaterThanOrEqual(12);
    const text = await label.boundingBox();
    const field = await input.boundingBox();
    expect(field!.y - (text!.y + text!.height)).toBeGreaterThanOrEqual(4);
    await expect(label).toBeInViewport({ ratio: 1 });
    await expect(modal.getByRole('button', { name: 'Salvar', exact: true })).toBeInViewport({ ratio: 1 });
    await expect(modal.getByRole('button', { name: 'Fechar' })).toBeInViewport({ ratio: 1 });
  }
  await input.fill('cus_updated');
  await expect(input).toHaveValue('cus_updated');
  await modal.getByRole('button', { name: 'Fechar' }).click();
  await expect(modal).toHaveCount(0);
});
