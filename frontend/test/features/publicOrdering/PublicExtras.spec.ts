import { test, expect, type Page } from '@playwright/test';
const token = '11111111-1111-4111-8111-111111111111';
async function menu(page: Page, flags = {}) {
    await page.route('**/api/publicordering/**/menu', route => route.fulfill({ json: {
        tableNumber: 7, branchName: 'Centro', isQrViewEnabled: true,
        isCameraInputEnabled: false, isBarcodeEnabled: false, isQrCodeEnabled: false, ...flags,
        items: [ { id: 1, name: 'Temaki', categoryName: 'Sushi', salePrice: 20,
            hasOptionalExtras: true, hasBoosts: true, complementGroups: [],
            optionalExtras: [{ id: 11, optionalExtraName: 'Sem cebolinha', displayOrder: 1 }],
            boosts: [{ id: 21, boostName: 'Couve frita', incrementalValue: 6, displayOrder: 1 }] },
            { id: 2, name: 'Água', categoryName: 'Bebidas', salePrice: 5, complementGroups: [] } ]
    } }));
    await page.goto('/pedido/' + token);
}
async function customize(page: Page) {
    await page.getByTestId('btn-qty-plus-1').click();
    await page.getByTestId('btn-add-item-1').click();
    await page.locator('.pw-option').filter({ hasText: 'Sem cebolinha' }).click();
    await page.getByRole('button', { name: /Avançar/ }).click();
    await page.locator('.pw-option').filter({ hasText: 'Couve frita' }).click();
    await expect(page.getByTestId('customization-subtotal')).toContainText('52,00');
    await page.getByRole('button', { name: /Adicionar ao pedido/ }).click();
}
for (const destination of ['mesa', 'comanda']) test('QR selections and quantity reach ' + destination, async ({ page }) => {
    let sent: any;
    let adminCalls = 0;
    await page.route('**/api/products/**', route => { adminCalls++; return route.abort(); });
    await page.route('**/api/publicordering/**/items', route => { sent = route.request().postDataJSON(); return route.fulfill({ json: { orderId: 100 } }); });
    await menu(page);
    await customize(page);
    expect(sent).toBeUndefined();
    if (destination === 'comanda') {
        await page.getByTestId('btn-select-comanda').click();
        await page.getByTestId('input-command-number').fill('001');
    }
    await page.getByTestId('btn-confirm-pending').click();
    await expect.poll(() => sent).toMatchObject({ productId: 1, quantity: 2, optionalExtraIds: [11], boostIds: [21], comandaCode: destination === 'comanda' ? '001' : null });
    expect(adminCalls).toBe(0);
    await expect(page.getByText('Pedido enviado com sucesso! Só aguardar.')).toBeVisible();
});
test('QR simple product sends directly and account displays saved selections', async ({ page }) => {
    let calls = 0;
    await page.route('**/api/publicordering/**/items', route => { calls++; return route.fulfill({ json: { orderId: 100 } }); });
    await page.route('**/api/publicordering/**/bill', route => route.fulfill({ json: { totalAmount: 52, items: [{ itemId: 1, productName: 'Temaki', quantity: 2, totalPrice: 52, statusId: 1, optionalExtras: [{ name: 'Sem cebolinha', unitPrice: 0 }], boosts: [{ name: 'Couve frita', unitPrice: 6 }] }] } }));
    await menu(page, { isQrViewEnabled: false });
    await page.getByTestId('btn-add-item-2').click();
    await expect.poll(() => calls).toBe(1);
    await expect(page.locator('dialog.product-wizard')).toHaveCount(0);
    await page.getByRole('button', { name: 'OK', exact: true }).click();
    await page.getByTestId('btn-my-orders').click();
    await expect(page.getByTestId('bill-items-list')).toContainText('Sem cebolinha');
    await expect(page.getByTestId('bill-items-list')).toContainText('Couve frita');
    await expect(page.getByTestId('bill-total-amount')).toContainText('52,00');
});
test('QR cancellation and scanner requirement do not submit an order', async ({ page }) => {
    let calls = 0;
    await page.route('**/api/publicordering/**/items', route => { calls++; return route.fulfill({ json: { orderId: 100 } }); });
    await menu(page, { isQrViewEnabled: false, isBarcodeEnabled: true });
    await customize(page);
    await expect(page.getByTestId('reading-validation-gate')).toBeVisible();
    await page.getByTestId('btn-cancel-validation').click();
    expect(calls).toBe(0);
    await page.getByTestId('btn-add-item-2').click();
    await expect(page.getByTestId('reading-validation-gate')).toBeVisible();
    expect(calls).toBe(0);
});
test('QR server rejection is visible and keeps the selection for a deliberate retry', async ({ page }) => {
    const sent: any[] = [];
    await page.route('**/api/publicordering/**/items', route => {
        sent.push(route.request().postDataJSON());
        return sent.length === 1 ? route.fulfill({ status: 400, json: { detail: 'Adicional indisponível. Tente novamente.' } }) : route.fulfill({ json: { orderId: 100 } });
    });
    await menu(page); await customize(page);
    await page.getByTestId('btn-confirm-pending').click();
    const alert = page.getByRole('dialog', { name: 'Ops!' });
    await expect(alert).toBeVisible();
    await expect(alert).toContainText('Adicional indisponível');
    await page.getByRole('button', { name: 'Voltar', exact: true }).click();
    await page.getByTestId('btn-confirm-pending').click();
    await expect.poll(() => sent.length).toBe(2);
    expect(sent[1]).toEqual(sent[0]);
});

for (const destination of ['mesa', 'comanda']) test('QR reading proof preserves customization for ' + destination, async ({ page }) => {
    let sent: any;
    await page.route('**/api/publicordering/**/items', route => { sent = route.request().postDataJSON(); return route.fulfill({ json: { orderId: 100 } }); });
    await page.route('**/api/publicordering/**/reading-validation', route => route.fulfill({ json: { proof: 'verified-' + destination } }));
    await menu(page, { isCameraInputEnabled: true });
    await customize(page);
    if (destination === 'comanda') {
        await page.getByTestId('btn-select-comanda').click();
        await page.getByTestId('input-command-number').fill('001');
    }
    await page.getByTestId('btn-confirm-pending').click();
    expect(sent).toBeUndefined();
    await page.getByTestId('input-camera-file').setInputFiles({ name: 'proof.png', mimeType: 'image/png', buffer: Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aS8cAAAAASUVORK5CYII=', 'base64') });
    await expect.poll(() => sent).toMatchObject({ quantity: 2, optionalExtraIds: [11], boostIds: [21], readingProof: 'verified-' + destination, comandaCode: destination === 'comanda' ? '001' : null });
});
test('QR invalid token shows menu error and cannot order', async ({ page }) => {
    await page.route('**/api/publicordering/**/menu', route => route.fulfill({ status: 400, json: { detail: 'QR Code inválido.' } }));
    await page.goto('/pedido/' + token);
    await expect(page.getByTestId('error-menu')).toBeVisible();
    await expect(page.getByTestId('btn-add-item-1')).toHaveCount(0);
});
