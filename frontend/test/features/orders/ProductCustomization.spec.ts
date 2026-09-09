import { test, expect, type Page } from '@playwright/test';
import { adminLogin, orderFixture } from './helpers';

async function setup(page: Page, kind = 'table', empty = false, fail = false, groups = false) {
    await adminLogin(page);
    let current = { ...orderFixture, diningTableId: kind === 'table' ? 1 : null, comandaId: kind === 'comanda' ? 1 : null,
        subtotalAmount: 0, totalAmount: 0, items: [] as any[] };
    const sent: any[] = [];
    let failDetails = fail;
    const group = { id: 50, name: 'Copo', minSelection: 1, maxSelection: 1, isActive: true, complementGroupTypeId: 1,
        complements: [{ id: 51, complementItemId: 52, complementItemName: 'Taça', extraPrice: 1, isActive: true }] };
    const menu = [
        { id: 1, name: 'Água', salePrice: 5, complementGroups: [], hasOptionalExtras: false, hasBoosts: false },
        { id: 2, name: 'Coca cola', salePrice: 10, complementGroups: groups ? [group] : [], hasOptionalExtras: true, hasBoosts: true },
    ];
    await page.route('**/api/tables/branch/1', route => route.fulfill({ json: [{ id: 1, number: 1, tableStatusId: 2 }] }));
    await page.route('**/api/comandas/branch/1', route => route.fulfill({ json: [{ id: 1, code: '001', comandaStatusId: 2 }] }));
    await page.route('**/api/orders/open/branch/1', route => route.fulfill({ json: [current] }));
    await page.route('**/api/orders/14', route => route.fulfill({ json: current }));
    await page.route('**/api/catalog/menu/company/1', route => route.fulfill({ json: menu }));
    await page.route('**/api/products/2', route => failDetails
        ? route.fulfill({ status: 503, json: { detail: 'Indisponível' } })
        : route.fulfill({ json: {
            hasOptionalExtras: true, hasBoosts: true,
            optionalExtras: empty ? [] : [{ id: 11, optionalExtraName: 'Gelo e Limão', displayOrder: 0 }],
            boosts: empty ? [] : [{ id: 12, boostName: 'Laranja', incrementalValue: 2, displayOrder: 0 }],
        } }));
    await page.route('**/api/orders/14/items', route => {
        const body = route.request().postDataJSON();
        sent.push(body);
        const price = (body.productId === 1 ? 5 : 10) + (body.boostIds?.includes(12) ? 2 : 0) + (body.complements?.length ? 1 : 0);
        current.items.push({ id: sent.length, productId: body.productId, quantity: 1, unitPrice: price, totalAmount: price,
            discountAmount: 0, orderItemStatusId: 1, notes: null, complements: [],
            optionalExtras: body.optionalExtraIds?.includes(11) ? [{ productOptionalExtraId: 11, name: 'Gelo e Limão' }] : [],
            boosts: body.boostIds?.includes(12) ? [{ productBoostId: 12, name: 'Laranja', unitPriceCharged: 2 }] : [] });
        current.totalAmount += price;
        current.subtotalAmount = current.totalAmount;
        return route.fulfill({ status: 204 });
    });
    await page.goto('/');
    await page.getByTestId(kind === 'table' ? 'table-tile-1' : 'comanda-tile-1').click();
    await page.getByTestId('btn-toggle-menu').click();
    return { sent, recover: () => { failDetails = false; } };
}

test('simple product is added directly', async ({ page }) => {
    const { sent } = await setup(page);
    await page.getByTestId('btn-add-menu-item-1').click();
    await expect(page.getByTestId('order-item-row-1')).toContainText('Água');
    expect(sent).toHaveLength(1);
    await expect(page.getByTestId('complement-selector-view')).toHaveCount(0);
    await expect(page.getByTestId('order-total-amount')).toContainText('5,00');
});

for (const kind of ['table', 'comanda']) test(`configurable product waits for selection in ${kind}`, async ({ page }) => {
    const { sent } = await setup(page, kind);
    await page.getByTestId('btn-add-menu-item-2').click();
    await expect(page.getByRole('group', { name: 'Opcionais gratuitos' })).toBeVisible();
    expect(sent).toHaveLength(0);
    await page.locator('.pw-option').filter({ hasText: 'Gelo e Limão' }).click();
    await page.getByRole('button', { name: /2 Adicionais pagos/ }).click();
    await page.locator('.pw-option').filter({ hasText: 'Laranja' }).click();
    await expect(page.getByTestId('customization-subtotal')).toContainText('12,00');
    await page.getByTestId('btn-confirm-complements').click();
    await expect(page.getByTestId('order-item-row-1')).toContainText('Gelo e Limão');
    await expect(page.getByTestId('order-item-row-1')).toContainText('Laranja');
    await expect(page.getByTestId('order-total-amount')).toContainText('12,00');
    expect(sent).toEqual([expect.objectContaining({ productId: 2, optionalExtraIds: [11], boostIds: [12] })]);
    expect(sent[0]).not.toHaveProperty('unitPrice');
    await page.reload();
    await page.getByTestId(kind === 'table' ? 'table-tile-1' : 'comanda-tile-1').click();
    await expect(page.getByTestId('order-item-row-1')).toContainText('Gelo e Limão');
});

test('cancel does not launch; reopening clears selections', async ({ page }) => {
    const { sent } = await setup(page);
    await page.getByTestId('btn-add-menu-item-2').click();
    await page.getByRole('button', { name: /2 Adicionais pagos/ }).click();
    await page.locator('.pw-option').filter({ hasText: 'Laranja' }).click();
    await page.keyboard.press('Escape');
    expect(sent).toHaveLength(0);
    await page.getByTestId('btn-add-menu-item-2').click();
    await page.getByRole('button', { name: /2 Adicionais pagos/ }).click();
    await expect(page.getByRole('checkbox', { name: /Laranja/ })).not.toBeChecked();
    await page.getByTestId('btn-confirm-complements').click();
    await expect.poll(() => sent.length).toBe(1);
    expect(sent[0].boostIds).toEqual([]);
});

test('empty configuration hides both sections but permits confirmation', async ({ page }) => {
    const { sent } = await setup(page, 'table', true);
    await page.getByTestId('btn-add-menu-item-2').click();
    await expect(page.getByText('Nenhum opcional ou adicional disponível.', { exact: false })).toBeVisible();
    await expect(page.getByRole('group', { name: 'Opcionais gratuitos' })).toHaveCount(0);
    await expect(page.getByRole('group', { name: 'Adicionais pagos' })).toHaveCount(0);
    expect(sent).toHaveLength(0);
    await page.getByTestId('btn-confirm-complements').click();
    await expect.poll(() => sent.length).toBe(1);
});

test('failed options lookup blocks launch and can be retried', async ({ page }) => {
    const state = await setup(page, 'table', false, true);
    await page.getByTestId('btn-add-menu-item-2').click();
    await expect(page.getByRole('alert')).toContainText('Não foi possível carregar');
    expect(state.sent).toHaveLength(0);
    await expect(page.getByTestId('btn-confirm-complements')).toHaveCount(0);
    state.recover();
    await page.getByRole('button', { name: 'Tentar novamente' }).click();
    await expect(page.getByRole('group', { name: 'Opcionais gratuitos' })).toBeVisible();
});

test('existing mandatory complements remain selectable alongside boosts', async ({ page }) => {
    const { sent } = await setup(page, 'table', false, false, true);
    await page.getByTestId('btn-add-menu-item-2').click();
    await page.getByRole('button', { name: /3 Copo/ }).click();
    await expect(page.getByTestId('btn-confirm-complements')).toBeDisabled();
    await page.getByTestId('complement-label-51').click();
    await page.getByRole('button', { name: /2 Adicionais pagos/ }).click();
    await page.locator('.pw-option').filter({ hasText: 'Laranja' }).click();
    await expect(page.getByTestId('customization-subtotal')).toContainText('13,00');
    await page.getByRole('button', { name: /3 Copo/ }).click();
    await page.getByTestId('btn-confirm-complements').click();
    await expect.poll(() => sent.length).toBe(1);
    expect(sent[0]).toMatchObject({ complements: [{ complementGroupId: 50, complementId: 51 }], boostIds: [12] });
});

test('disabled optional flag hides its section while boosts stay available', async ({ page }) => {
    await setup(page);
    await page.route('**/api/products/2', route => route.fulfill({ json: {
        salePrice: 10, hasOptionalExtras: false, hasBoosts: true,
        optionalExtras: [], boosts: [{ id: 12, boostName: 'Laranja', incrementalValue: 2, displayOrder: 0 }],
    } }));
    await page.getByTestId('btn-add-menu-item-2').click();
    await expect(page.getByRole('group', { name: 'Adicionais pagos' })).toBeVisible();
    await expect(page.getByRole('group', { name: 'Opcionais gratuitos' })).toHaveCount(0);
});

test('launch failure keeps choices for an explicit retry', async ({ page }) => {
    const { sent } = await setup(page);
    let fail = true;
    await page.route('**/api/orders/14/items', route => fail
        ? route.fulfill({ status: 400, json: { detail: 'Limite da comanda atingido.' } })
        : route.fallback());
    await page.getByTestId('btn-add-menu-item-2').click();
    await page.getByRole('button', { name: /2 Adicionais pagos/ }).click();
    await page.locator('.pw-option').filter({ hasText: 'Laranja' }).click();
    await page.getByTestId('btn-confirm-complements').click();
    await expect(page.getByTestId('complement-selector-view').getByRole('alert')).toContainText('Limite');
    expect(sent).toHaveLength(0);
    await expect(page.getByRole('checkbox', { name: /Laranja/ })).toBeChecked();
    fail = false;
    await page.getByTestId('btn-confirm-complements').click();
    await expect.poll(() => sent.length).toBe(1);
});

test('preview discounts the base product without discounting boosts', async ({ page }) => {
    await setup(page);
    await page.route('**/api/catalog/promotions/active/branch/1', route => route.fulfill({ json: [
        { productId: 2, name: 'Happy hour', promotionTypeId: 2, discountRate: 0.25 },
    ] }));
    // Reopen to load the promotion together with the menu.
    await page.reload();
    await page.getByTestId('table-tile-1').click();
    await page.getByTestId('btn-toggle-menu').click();
    await page.getByTestId('btn-add-menu-item-2').click();
    await page.getByRole('button', { name: /2 Adicionais pagos/ }).click();
    await page.locator('.pw-option').filter({ hasText: 'Laranja' }).click();
    await expect(page.getByTestId('customization-subtotal')).toContainText('9,50');
});

test('wizard advances, preserves choices on back and keeps its footer inside the viewport', async ({ page }, testInfo) => {
    await setup(page);
    await page.getByTestId('btn-add-menu-item-2').click();
    const dialog = page.locator('dialog.product-wizard');
    await expect(dialog).toBeVisible();
    await expect(page.getByTestId('btn-confirm-complements')).toHaveText(/Pular/);
    await page.locator('.pw-option').filter({ hasText: 'Gelo e Limão' }).click();
    await expect(page.getByTestId('btn-confirm-complements')).toHaveText(/Avançar/);
    await page.getByTestId('btn-confirm-complements').click();
    await expect(page.getByTestId('btn-confirm-complements')).toHaveText(/Adicionar ao pedido/);
    await page.locator('.pw-option').filter({ hasText: 'Laranja' }).click();
    await page.getByRole('button', { name: 'Voltar à etapa anterior' }).click();
    await expect(page.getByRole('checkbox', { name: /Gelo e Limão/ })).toBeChecked();
    await expect(page.getByTestId('customization-subtotal')).toContainText('12,00');
    const overflow = await dialog.evaluate(el => el.scrollWidth > el.clientWidth);
    expect(overflow).toBe(false);
    const footer = await page.getByTestId('btn-confirm-complements').boundingBox();
    expect(footer!.y + footer!.height).toBeLessThanOrEqual(page.viewportSize()!.height);
    await page.screenshot({ path: testInfo.outputPath('wizard.png'), animations: 'disabled' });
});

test('keyboard focus stays in dialog, Space selects and Escape restores the menu', async ({ page }) => {
    await setup(page);
    const trigger = page.getByTestId('btn-add-menu-item-2');
    await trigger.focus();
    await page.keyboard.press('Enter');
    const checkbox = page.getByRole('checkbox', { name: /Gelo e Limão/ });
    await checkbox.focus();
    await page.keyboard.press('Space');
    await expect(checkbox).toBeChecked();
    await page.getByTestId('btn-confirm-complements').focus();
    await page.keyboard.press('Tab');
    await expect(page.getByRole('button', { name: 'Fechar personalização' })).toBeFocused();
    await page.keyboard.press('Shift+Tab');
    await expect(page.getByTestId('btn-confirm-complements')).toBeFocused();
    await page.keyboard.press('Escape');
    await expect(page.locator('dialog.product-wizard')).toHaveCount(0);
    await expect(trigger).toBeFocused();
});

test('mandatory limits disable unselected options and unblock after deselection', async ({ page }) => {
    await setup(page);
    await page.route('**/api/catalog/menu/company/1', route => route.fulfill({ json: [{
        id: 2, name: 'Temaki', salePrice: 20, complementGroups: [{ id: 50, name: 'Escolhas', minSelection: 1, maxSelection: 2,
            complements: [1, 2, 3].map(id => ({ id, complementItemName: `Opção ${id}`, extraPrice: id, isActive: true })) }],
    }] }));
    await page.reload();
    await page.getByTestId('table-tile-1').click();
    await page.getByTestId('btn-toggle-menu').click();
    await page.getByTestId('btn-add-menu-item-2').click();
    await expect(page.getByTestId('btn-confirm-complements')).toBeDisabled();
    await page.getByTestId('complement-label-1').click();
    await page.getByTestId('complement-label-2').click();
    await expect(page.getByTestId('input-complement-3')).toBeDisabled();
    await expect(page.getByTestId('customization-subtotal')).toContainText('23,00');
    await page.getByTestId('complement-label-1').click();
    await expect(page.getByTestId('input-complement-3')).toBeEnabled();
    await expect(page.getByTestId('customization-subtotal')).toContainText('22,00');
});

test('slow detail request displays skeleton without permitting submission', async ({ page }) => {
    await setup(page);
    let release: () => void = () => {};
    const pending = new Promise<void>(resolve => { release = resolve; });
    await page.route('**/api/products/2', async route => {
        await pending;
        await route.fulfill({ json: { salePrice: 10, hasOptionalExtras: false, hasBoosts: false, optionalExtras: [], boosts: [] } });
    });
    await page.getByTestId('btn-add-menu-item-2').click();
    await expect(page.getByRole('status', { name: 'Carregando opções' })).toBeVisible();
    await expect(page.getByTestId('btn-confirm-complements')).toHaveCount(0);
    release();
    await expect(page.getByTestId('btn-confirm-complements')).toBeEnabled();
});

test('long option lists scroll without moving the subtotal or action off screen', async ({ page }) => {
    await setup(page);
    await page.route('**/api/products/2', route => route.fulfill({ json: {
        salePrice: 10, hasOptionalExtras: true, hasBoosts: false, boosts: [],
        optionalExtras: Array.from({ length: 30 }, (_, index) => ({ id: index + 100,
            optionalExtraName: `Opção ${index + 1} com descrição longa para personalizar o preparo do produto`, displayOrder: index })),
    } }));
    await page.getByTestId('btn-add-menu-item-2').click();
    await page.locator('.pw-option').last().scrollIntoViewIfNeeded();
    await page.locator('.pw-option').last().click();
    const action = page.getByTestId('btn-confirm-complements');
    const rect = await action.boundingBox();
    expect(rect!.y + rect!.height).toBeLessThanOrEqual(page.viewportSize()!.height);
    expect(await action.evaluate(el => {
        const box = el.getBoundingClientRect();
        return el.contains(document.elementFromPoint(box.x + box.width / 2, box.y + box.height / 2));
    })).toBe(true);
    await expect(page.getByTestId('customization-subtotal')).toBeInViewport();
});

