import { test, expect } from '@playwright/test';

test('webhook status contract, successful updates and bounded payload modal', async ({ page }) => {
  const logs = ['Pending', 'Processed', 'Failed'].map((status, index) => ({
    id: index + 1, companyId: 1, branchId: null, event: `EVENT_${index}`,
    asaasEventId: null, paymentId: 'pay_test', status, errorMessage: null,
    payload: JSON.stringify({ entries: Array.from({ length: 150 }, () => 'x'.repeat(400)) }),
    requestHeaders: null, ipAddress: null, createdAt: '2026-09-08T12:00:00Z', processedAt: null,
  }));
  let holdRefresh = false;
  await page.route('**/api/**', async route => {
    const url = route.request().url();
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 200 });
    } else if (url.includes('/auth/login')) {
      await route.fulfill({ json: { accessToken: 'token', user: { id: 1, name: 'Admin', companyId: 1, branchId: 1 } } });
    } else if (url.includes('/access/my-features')) {
      await route.fulfill({ json: { canManageAccess: true, features: [] } });
    } else if (url.includes('/status')) {
      expect(route.request().postDataJSON().status).toBe(2);
      holdRefresh = true;
      await route.fulfill({ status: 204 });
    } else if (url.includes('/webhook-logs/')) {
      // A failed refetch must not undo the confirmed update in the cache.
      if (holdRefresh) await route.abort();
      else await route.fulfill({ json: logs });
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
  await page.getByRole('tab', { name: 'Webhooks' }).click();
  const pending = page.locator('.ticket-row').filter({ hasText: 'EVENT_0' });
  const processed = page.locator('.ticket-row').filter({ hasText: 'EVENT_1' });
  const failed = page.locator('.ticket-row').filter({ hasText: 'EVENT_2' });
  await expect(pending.locator('.chip')).toHaveText('Pendente');
  await expect(processed.locator('.chip')).toHaveText('Processado');
  await expect(processed.getByRole('button', { name: 'Marcar processado' })).toHaveCount(0);
  await expect(failed.locator('.chip')).toHaveText('Falha');
  await pending.getByRole('button', { name: 'Ver payload' }).click();
  const modal = page.getByRole('dialog');
  for (const viewport of [{ width: 1874, height: 884 }, { width: 390, height: 600 }]) {
    await page.setViewportSize(viewport);
    await expect(modal).toBeVisible();
    const bounds = await modal.boundingBox();
    expect(bounds!.x).toBeGreaterThanOrEqual(0);
    expect(bounds!.y).toBeGreaterThanOrEqual(0);
    expect(bounds!.x + bounds!.width).toBeLessThanOrEqual(viewport.width);
    expect(bounds!.height).toBeLessThanOrEqual(viewport.height * 0.9 + 1);
    expect(await modal.evaluate(el => el.parentElement?.parentElement === document.body)).toBe(true);
    expect(await modal.locator('pre').evaluate(el => {
      el.scrollTop = el.scrollHeight;
      return el.scrollTop > 0;
    })).toBe(true);
  }
  await modal.getByRole('button', { name: 'Fechar' }).click();
  await page.setViewportSize({ width: 1280, height: 800 });
  await pending.getByRole('button', { name: 'Marcar falha' }).click();
  await page.locator('.swal2-input').fill('Falha de teste');
  await page.getByRole('button', { name: 'Confirmar', exact: true }).click();
  await expect(pending.locator('.chip')).toHaveText('Falha');
  await expect(pending).toContainText('Falha de teste');
  await expect(pending.getByRole('button', { name: 'Marcar falha' })).toHaveCount(0);
});
