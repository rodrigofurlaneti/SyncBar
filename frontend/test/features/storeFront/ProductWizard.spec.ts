import { test, expect } from '@playwright/test';
import { mockMenu } from './helpers';

test('customer customizes a product in steps before adding it to the cart', async ({ page }) => {
    await mockMenu(page);
    await page.route('**/api/storefront/branches/7/menu', route => route.fulfill({ json: {
        companyId: 1, items: [{ id: 1, name: 'Temaki', description: 'Salmão e cream cheese', salePrice: 20,
            complementGroups: [
                { id: 10, name: 'Opcionais', minSelection: 0, maxSelection: 1, complements: [
                    { id: 11, complementItemName: 'Sem cebolinha', extraPrice: 0, isActive: true },
                ] },
                { id: 20, name: 'Incremente seu temaki', minSelection: 0, maxSelection: 1, complements: [
                    { id: 21, complementItemName: 'Couve frita', extraPrice: 6, isActive: true },
                ] },
            ],
        }],
    } }));
    await page.goto('/cardapio/7');
    await page.getByTestId('btn-add-item-1').click();
    const dialog = page.locator('dialog.product-wizard');
    await expect(dialog).toBeVisible();
    await expect(dialog).toContainText('Salmão e cream cheese');
    await page.getByTestId('complement-label-11').click();
    await page.getByRole('button', { name: /Avançar/ }).click();
    await page.getByTestId('complement-label-21').click();
    await expect(page.getByTestId('customization-subtotal')).toContainText('26,00');
    await page.getByRole('button', { name: /Adicionar ao pedido/ }).click();
    await expect(dialog).toHaveCount(0);
    await page.getByTestId('btn-open-cart').click();
    await expect(page.getByText('Couve frita', { exact: false }).first()).toBeVisible();
});
