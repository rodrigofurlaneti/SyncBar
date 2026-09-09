import { test, expect } from '@playwright/test';
import { mockMenu } from './helpers';

test('web menu extras flow preserves selections, quantities and prices through checkout without administrative endpoints', async ({ page }) => {
    await mockMenu(page);
    let protectedCalls = 0;
    await page.route('**/api/products/**', route => { protectedCalls++; return route.fulfill({ status: 403 }); });
    await page.route('**/api/branch-payment-method-settings/branch/7', route => route.fulfill({ json: {
        id: 1, branchId: 7, isActive: true, enableCashMachine: true,
    } }));
    await page.route('**/api/storefront/branches/7/menu', route => route.fulfill({ json: {
        companyId: 1, items: [
            { id: 1, name: 'Bebida', salePrice: 20, complementGroups: [], hasOptionalExtras: true, hasBoosts: true,
                optionalExtras: [{ id: 11, optionalExtraName: 'Gelo e limão', displayOrder: 0 }],
                boosts: [{ id: 21, boostName: 'Laranja extra', incrementalValue: 2, displayOrder: 0 }] },
            { id: 2, name: 'Água', salePrice: 5, complementGroups: [], hasOptionalExtras: false, hasBoosts: false },
        ],
    } }));
    await page.goto('/cardapio/7');
    await page.getByTestId('btn-add-item-2').click();
    await expect(page.locator('dialog.product-wizard')).toHaveCount(0);
    await page.getByTestId('btn-add-item-1').click();
    await page.locator('.pw-option').filter({ hasText: 'Gelo e limão' }).click();
    await page.getByRole('button', { name: /Avançar/ }).click();
    await page.locator('.pw-option').filter({ hasText: 'Laranja extra' }).click();
    await expect(page.getByTestId('customization-subtotal')).toContainText('22,00');
    await page.getByRole('button', { name: /Adicionar ao pedido/ }).click();
    await page.getByTestId('btn-open-cart').click();
    const custom = page.getByTestId('cart-item-1');
    await expect(custom).toContainText('Gelo e limão');
    await expect(custom).toContainText('Laranja extra');
    await custom.getByRole('button', { name: 'Aumentar quantidade' }).click();
    await expect(custom).toContainText('44,00');
    await expect(page.getByTestId('cart-total-amount')).toContainText('49,00');
    await page.getByRole('button', { name: 'Fechar carrinho' }).click();
    await page.getByTestId('btn-add-item-1').click();
    await page.getByRole('button', { name: /Pular/ }).click();
    await page.getByRole('button', { name: /Adicionar ao pedido/ }).click();
    await page.getByTestId('btn-open-cart').click();
    await expect(custom).toHaveCount(2);
    await custom.nth(1).getByRole('button', { name: /Remover/ }).click();
    await page.getByTestId('cart-item-2').getByRole('button', { name: /Remover/ }).click();
    let payload: unknown;
    await page.route('**/api/storefront/branches/7/orders', route => {
        payload = route.request().postDataJSON();
        return route.fulfill({ json: { orderId: 901 } });
    });
    await page.getByTestId('btn-submit-order').click();
    const auth = page.getByTestId('storefront-auth-modal');
    await auth.getByLabel('E-mail', { exact: true }).fill('cliente@example.test');
    await auth.getByLabel('Senha', { exact: true }).fill('password');
    await auth.getByRole('button', { name: 'Entrar e Finalizar Pedido' }).click();
    await page.getByTestId('btn-submit-order').click();
    await page.getByRole('button', { name: /Retirar no Balcão/ }).click();
    await page.getByRole('button', { name: 'Maquininha', exact: true }).click();
    await page.getByTestId('btn-submit-order').click();
    await expect.poll(() => payload).toMatchObject({ items: [{ productId: 1, quantity: 2, optionalExtraIds: [11], boostIds: [21] }] });
    expect(protectedCalls).toBe(0);
});
