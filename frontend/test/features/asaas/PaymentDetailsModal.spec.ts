import { test, expect } from '@playwright/test';

test('payment details labels clear the header and the final action can be scrolled into view', async ({ page }) => {
  await page.route('**/api/**', async route => {
    const url = route.request().url();
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 200 });
    } else if (url.includes('/auth/login')) {
      await route.fulfill({ json: { accessToken: 'token', user: { id: 1, name: 'Admin', companyId: 1, branchId: 1 } } });
    } else if (url.includes('/access/my-features')) {
      await route.fulfill({ json: { canManageAccess: true, features: [] } });
    } else if (url.includes('/asaas/payments/branch/')) {
      await route.fulfill({ json: [{
        id: 1, asaasPaymentId: 'pay_12345678901234567890', customerOrderId: 1,
        customerId: null, branchId: 1, value: 120, netValue: 118,
        billingType: 'PIX', status: 'PENDING', dueDate: '2026-09-10T00:00:00Z',
        paymentDate: null, pixPayload: 'pix-code',
        pixQrCodeBase64: 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aZ1sAAAAASUVORK5CYII=',
        invoiceUrl: 'https://example.com/invoice', bankSlipUrl: 'https://example.com/boleto',
      }] });
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
  await page.getByRole('tab', { name: 'Pagamentos', exact: true }).click();
  await page.getByRole('button', { name: 'Detalhes', exact: true }).click();
  const modal = page.getByRole('dialog');
  for (const viewport of [{ width: 1868, height: 881 }, { width: 390, height: 600 }, { width: 800, height: 450 }]) {
    await page.setViewportSize(viewport);
    await modal.evaluate(el => { el.scrollTop = 0; });
    const header = modal.locator('.modal-head');
    for (const label of ['Valor', 'Forma', 'Status atual', 'Vencimento']) {
      const bounds = await modal.getByText(label, { exact: true }).boundingBox();
      const headBounds = await header.boundingBox();
      expect(bounds!.y - (headBounds!.y + headBounds!.height)).toBeGreaterThanOrEqual(12);
    }
    const bounds = await modal.boundingBox();
    expect(bounds!.y).toBeGreaterThanOrEqual(0);
    expect(bounds!.x).toBeGreaterThanOrEqual(0);
    expect(bounds!.x + bounds!.width).toBeLessThanOrEqual(viewport.width);
    expect(bounds!.y + bounds!.height).toBeLessThanOrEqual(viewport.height);
    expect(bounds!.height).toBeLessThanOrEqual(viewport.height * 0.9 + 1);
    const save = modal.getByRole('button', { name: 'Salvar status' });
    await save.scrollIntoViewIfNeeded();
    await expect(save).toBeInViewport({ ratio: 1 });
    await expect(modal.getByRole('button', { name: 'Fechar' })).toBeInViewport({ ratio: 1 });
    if (viewport.height <= 600) expect(await modal.evaluate(el => el.scrollTop)).toBeGreaterThan(0);
  }
  await page.keyboard.press('Escape');
  await expect(modal).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Detalhes', exact: true })).toBeFocused();
});
