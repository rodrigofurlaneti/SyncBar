import { test, expect } from '@playwright/test';

test('credentials scroll inside the modal while header and actions remain visible', async ({ page }) => {
  await page.route('**/api/**', async route => {
    const url = route.request().url();
    if (route.request().method() === 'OPTIONS') await route.fulfill({ status: 200 });
    else if (url.includes('/auth/login')) await route.fulfill({ json: { accessToken: 'token', user: { id: 1, name: 'Admin', companyId: 1, branchId: 1 } } });
    else if (url.includes('/access/my-features')) await route.fulfill({ json: { canManageAccess: true, features: [] } });
    else if (url.includes('/asaas/settings/company/')) await route.fulfill({ json: [{ id: 1, companyId: 1, branchId: 1, environment: 'Sandbox', isActive: true }] });
    else await route.fulfill({ json: [] });
  });
  await page.goto('/login');
  await page.getByTestId('username').fill('admin');
  await page.getByTestId('password').fill('123');
  await page.getByTestId('submit-login').click();
  await page.waitForURL('**/');
  await page.goto('/integracoes/asaas');
  await page.getByRole('button', { name: 'Editar', exact: true }).click();
  const modal = page.getByRole('dialog');
  for (const viewport of [{ width: 1902, height: 880 }, { width: 390, height: 600 }, { width: 800, height: 450 }]) {
    await page.setViewportSize(viewport);
    await modal.locator('.modal-body').evaluate(el => { el.scrollTop = 0; });
    await expect(modal.getByText('Nova chave de API (opcional)', { exact: true })).toBeInViewport({ ratio: 1 });
    await expect(modal.getByRole('button', { name: 'Salvar alterações' })).toBeInViewport({ ratio: 1 });
    await expect(modal.getByRole('button', { name: 'Cancelar' })).toBeInViewport({ ratio: 1 });
    const bounds = await modal.boundingBox();
    expect(bounds!.height).toBeLessThanOrEqual(viewport.height * 0.9 + 1);
    await modal.locator('.modal-body').evaluate(el => { el.scrollTop = el.scrollHeight; });
    await expect(modal.getByRole('switch')).toBeInViewport({ ratio: 1 });
    await expect(modal.getByRole('button', { name: 'Fechar' })).toBeInViewport({ ratio: 1 });
    await expect(modal.getByRole('button', { name: 'Salvar alterações' })).toBeInViewport({ ratio: 1 });
  }
  await modal.getByRole('button', { name: 'Cancelar' }).click();
  await expect(modal).toHaveCount(0);
});
