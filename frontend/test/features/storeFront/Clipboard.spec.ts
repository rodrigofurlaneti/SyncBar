import { test, expect } from '@playwright/test';
import { adminLogin } from '../orders/helpers';

test('menu link copies with Clipboard API, HTTP fallback and manual selection when both are blocked', async ({ page, context }) => {
  await context.grantPermissions(['clipboard-read', 'clipboard-write']);
  await adminLogin(page);
  await page.getByTestId('btn-open-storefront-modal').click();
  const modal = page.getByTestId('storefront-hub-modal');
  const link = await modal.getByRole('textbox', { name: 'URL do cardápio' }).inputValue();
  await modal.getByRole('button', { name: 'Copiar Link do Cardápio' }).click();
  await expect.poll(() => page.evaluate(() => navigator.clipboard.readText())).toBe(link);
  await page.evaluate(() => {
    (window as any).readClipboard = navigator.clipboard.readText.bind(navigator.clipboard);
    Object.defineProperty(navigator, 'clipboard', { value: undefined, configurable: true });
  });
  await modal.getByRole('button', { name: 'Copiar Link do Cardápio' }).click();
  await expect.poll(() => page.evaluate(() => (window as any).readClipboard())).toBe(link);
  await expect(modal.getByRole('status')).toHaveCount(0);
  await page.evaluate(() => {
    Object.defineProperty(navigator, 'clipboard', { value: { writeText: () => Promise.reject(new Error('Denied')) }, configurable: true });
  });
  await modal.getByRole('button', { name: 'Copiar Link do Cardápio' }).click();
  await expect(modal.getByRole('status')).toHaveCount(0);
  await page.evaluate(() => { document.execCommand = () => false; });
  await modal.getByRole('button', { name: 'Copiar Link do Cardápio' }).click();
  await expect(modal.getByRole('status')).toContainText('O link está selecionado');
  const selected = await modal.getByRole('textbox').evaluate((input: HTMLInputElement) => input.value.slice(input.selectionStart!, input.selectionEnd!));
  expect(selected).toBe(link);
  await expect(page.getByRole('dialog', { name: 'Erro', exact: true })).toHaveCount(0);
});
