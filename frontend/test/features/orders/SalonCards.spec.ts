import { test, expect } from '@playwright/test';
import { adminLogin } from './helpers';

test('salon cards adapt to mobile, tablet and desktop and support keyboard and unavailable states', async ({ page }) => {
  await adminLogin(page);
  await page.route('**/api/tables/branch/1', route => route.fulfill({ json: Array.from({ length: 8 }, (_, i) => ({ id: i + 1, number: i + 1, tableStatusId: i === 1 ? 5 : 1, capacity: 4 })) }));
  await page.route('**/api/comandas/branch/1', route => route.fulfill({ json: [{ id: 1, code: '001', comandaStatusId: 1 }, { id: 2, code: '002', comandaStatusId: 4 }] }));
  await page.goto('/');
  for (const viewport of [{ width: 390, height: 736, columns: 1 }, { width: 820, height: 1000, columns: 2 }, { width: 1600, height: 1000, columns: 4 }]) {
    await page.setViewportSize(viewport);
    const grid = page.getByTestId('tables-grid');
    await expect(page.getByTestId('table-tile-1')).toBeVisible();
    const count = await grid.evaluate(el => getComputedStyle(el).gridTemplateColumns.split(' ').length);
    expect(count).toBeGreaterThanOrEqual(viewport.columns);
    const bounds = await grid.boundingBox();
    expect(bounds!.x + bounds!.width).toBeLessThanOrEqual(viewport.width);
    await expect(page.getByTestId('table-tile-2')).toBeDisabled();
    await expect(page.getByTestId('comanda-tile-2')).toBeDisabled();
    await page.getByTestId('table-tile-1').focus();
    expect(await page.getByTestId('table-tile-1').evaluate(el => getComputedStyle(el).outlineStyle)).not.toBe('none');
  }
  await page.getByLabel('Buscar comanda pelo número').fill('999');
  await expect(page.getByText('Nenhuma comanda encontrada', { exact: true })).toBeVisible();
  await page.getByTestId('table-tile-1').focus();
  await page.keyboard.press('Enter');
  await expect(page.getByRole('dialog')).toBeVisible();
});
