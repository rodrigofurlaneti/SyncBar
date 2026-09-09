import { test, expect } from '@playwright/test';
import { mockMenu } from './helpers';

test('customer customizes a product in steps before adding it to the cart', async ({ page }) => {
    await mockMenu(page);
    await page.route('**/api/branch-payment-method-settings/branch/7', route => route.fulfill({ json: {
        id: 1, branchId: 7, isActive: true, enablePix: false, enableBoleto: false,
        enableCreditCard: false, enableDebitCard: false, enableCashMachine: true,
    } }));

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
    await expect(page.getByTestId('cart-item-1')).toContainText('26,00');
    await page.getByRole('button', { name: 'Fechar carrinho' }).click();
    await page.getByTestId('btn-add-item-1').click();
    await page.getByRole('button', { name: /Pular/ }).click();
    await page.getByRole('button', { name: /Adicionar ao pedido/ }).click();
    await page.getByTestId('btn-open-cart').click();
    await expect(page.getByTestId('cart-item-1')).toHaveCount(2);
    await page.getByTestId('cart-item-1').nth(1).getByRole('button', { name: /Remover/ }).click();
    await expect(page.getByTestId('cart-item-1')).toHaveCount(1);
    await expect(page.getByTestId('cart-item-1')).toContainText('Couve frita');
    let sent: unknown;
    await page.route('**/api/storefront/branches/7/orders', route => {
        sent = route.request().postDataJSON();
        return route.fulfill({ json: { orderId: 903 } });
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
    await expect.poll(() => sent).toMatchObject({ items: [{ productId: 1, quantity: 1, complements: [
        { complementGroupId: 10, complementId: 11 }, { complementGroupId: 20, complementId: 21 },
    ] }] });
});
