import { test, expect } from '@playwright/test';

const categoriesManagement = [
    { id: 1, name: 'Bebidas', displayOrder: 1, isActive: true, productCount: 2 },
    { id: 2, name: 'Sobremesas', displayOrder: 2, isActive: false, productCount: 1 },
    { id: 3, name: 'Petiscos', displayOrder: 3, isActive: true, productCount: 0 },
];

const categories = [
    { id: 1, name: 'Bebidas' },
];

const productsManagement = [
    { id: 10, categoryId: 1, categoryName: 'Bebidas', unitOfMeasureId: 1, name: 'Refrigerante', description: null, barcode: null, salePrice: 8, costPrice: null, isStockControlled: false, preparationTimeMinutes: null, imageUrl: null, isActive: true },
    { id: 11, categoryId: 1, categoryName: 'Bebidas', unitOfMeasureId: 1, name: 'Suco', description: null, barcode: null, salePrice: 6, costPrice: null, isStockControlled: false, preparationTimeMinutes: null, imageUrl: null, isActive: true },
    // Produto vinculado à categoria "Sobremesas", que está desativada — precisa continuar
    // aparecendo normalmente na tela de gerenciamento (desativar categoria não afeta produtos).
    { id: 12, categoryId: 2, categoryName: 'Sobremesas', unitOfMeasureId: 1, name: 'Pudim', description: null, barcode: null, salePrice: 12, costPrice: null, isStockControlled: false, preparationTimeMinutes: null, imageUrl: null, isActive: true },
];

test.describe('Gerenciamento de Cardápio - Categorias (ProductsPage)', () => {

    test.beforeEach(async ({ page }) => {
        await page.route('*/**/api/auth/login', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 200, json: { accessToken: 'token', user: { id: 1, name: 'Admin', companyId: 1, branchId: 1 } } });
        });

        await page.route('*/**/api/access/my-features', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 200, json: { canManageAccess: true, features: [] } });
        });

        await page.goto('/login');
        await page.getByTestId('username').fill('admin');
        await page.getByTestId('password').fill('123');
        await page.getByTestId('submit-login').click();
        await page.waitForURL('**/');

        await page.route('*/**/api/categories/company/*/management', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 200, json: categoriesManagement });
        });
        await page.route('*/**/api/categories/company/*', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 200, json: categories });
        });
        await page.route('*/**/api/products/company/*/management', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 200, json: productsManagement });
        });
        await page.route('*/**/api/stock/branch/*', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 200, json: [] });
        });

        await page.goto('/produtos');
    });

    test('Deve renderizar a lista de categorias com contagem de produtos', async ({ page }) => {
        await expect(page.getByTestId('category-row-all')).toBeVisible();
        await expect(page.getByTestId('category-row-1')).toContainText('Bebidas');
        await expect(page.getByTestId('category-row-1')).toContainText('2');
        await expect(page.getByTestId('category-row-2')).toContainText('Sobremesas');
        await expect(page.getByTestId('category-row-2')).toContainText('Inativa');
    });

    test('Categoria desativada não deve esconder os produtos já vinculados a ela', async ({ page }) => {
        // Critério de aceite do cartão: "Desativação operando sem quebrar produtos vinculados".
        // "Sobremesas" (categoria 2) está desativada, mas o produto "Pudim" continua vinculado a
        // ela e precisa continuar visível/gerenciável na tela de produtos.
        await expect(page.getByTestId('product-row-12')).toBeVisible();
        await expect(page.getByTestId('product-row-12')).toContainText('Pudim');
    });

    test('Deve criar uma nova categoria com sucesso', async ({ page }) => {
        await page.route('*/**/api/categories', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            expect(route.request().method()).toBe('POST');
            const body = route.request().postDataJSON();
            expect(body.name).toBe('Sobremesas Geladas');
            await route.fulfill({ status: 200, json: 3 });
        });

        await page.getByTestId('input-new-category').fill('Sobremesas Geladas');
        await page.getByTestId('btn-add-category').click();

        const swalPopup = page.locator('.swal2-popup');
        await expect(swalPopup).toBeVisible();
        await expect(swalPopup).toContainText('Categoria criada.');
    });

    test('O botão de criar categoria deve ficar desabilitado com o nome vazio', async ({ page }) => {
        await expect(page.getByTestId('btn-add-category')).toBeDisabled();
        await page.getByTestId('input-new-category').fill('   ');
        await expect(page.getByTestId('btn-add-category')).toBeDisabled();
    });

    test('Deve editar o nome e a ordem de exibição de uma categoria', async ({ page }) => {
        await page.route('*/**/api/categories/1', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            expect(route.request().method()).toBe('PUT');
            const body = route.request().postDataJSON();
            expect(body.name).toBe('Bebidas Geladas');
            expect(body.displayOrder).toBe(5);
            await route.fulfill({ status: 204 });
        });

        await page.getByTestId('btn-edit-category-1').click();
        await page.getByTestId('input-edit-cat-name-1').fill('Bebidas Geladas');
        await page.getByTestId('input-edit-cat-order-1').fill('5');
        await page.getByTestId('btn-save-cat-1').click();

        const swalPopup = page.locator('.swal2-popup');
        await expect(swalPopup).toBeVisible();
        await expect(swalPopup).toContainText('Categoria atualizada.');
    });

    test('Deve cancelar a edição de uma categoria sem chamar a API', async ({ page }) => {
        let updateCalled = false;
        await page.route('*/**/api/categories/1', async (route) => {
            updateCalled = true;
            await route.fulfill({ status: 204 });
        });

        await page.getByTestId('btn-edit-category-1').click();
        await page.getByTestId('input-edit-cat-name-1').fill('Nome que não deve ser salvo');
        await page.getByTestId('btn-cancel-cat-1').click();

        await expect(page.getByTestId('input-edit-cat-name-1')).toHaveCount(0);
        expect(updateCalled).toBe(false);
    });

    test('Deve pedir confirmação e desativar uma categoria ativa sem produtos vinculados', async ({ page }) => {
        // Categoria 3 ("Petiscos") tem productCount 0 — é a única elegível pela nova regra de
        // negócio (só pode desativar categoria sem produto ativo vinculado).
        await page.route('*/**/api/categories/3/deactivate', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            expect(route.request().method()).toBe('PUT');
            await route.fulfill({ status: 204 });
        });

        await page.getByTestId('switch-category-3').click();

        const confirmPopup = page.locator('.swal2-popup');
        await expect(confirmPopup).toBeVisible();
        await expect(confirmPopup).toContainText('Só é possível desativar categorias sem nenhum produto ativo vinculado');
        await confirmPopup.getByRole('button', { name: 'Desativar' }).click();

        const toast = page.locator('.swal2-popup');
        await expect(toast).toContainText('Categoria desativada.');
    });

    test('Deve cancelar a desativação de uma categoria ativa sem chamar a API', async ({ page }) => {
        let deactivateCalled = false;
        await page.route('*/**/api/categories/3/deactivate', async (route) => {
            deactivateCalled = true;
            await route.fulfill({ status: 204 });
        });

        await page.getByTestId('switch-category-3').click();

        const confirmPopup = page.locator('.swal2-popup');
        await expect(confirmPopup).toBeVisible();
        await confirmPopup.getByRole('button', { name: 'Cancelar' }).click();

        await expect(confirmPopup).toBeHidden();
        expect(deactivateCalled).toBe(false);
    });

    test('Não deve permitir desativar categoria com produto ativo vinculado', async ({ page }) => {
        // Regra de negócio nova: categoria 1 ("Bebidas") tem 2 produtos ativos vinculados — a API
        // deve rejeitar a desativação, e a tela deve mostrar o erro em vez do toast de sucesso.
        await page.route('*/**/api/categories/1/deactivate', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({
                status: 400,
                json: {
                    title: 'Category.HasLinkedProducts',
                    detail: 'Não é possível desativar esta categoria enquanto houver produtos ativos vinculados a ela. Desative os produtos primeiro.',
                },
            });
        });

        await page.getByTestId('switch-category-1').click();

        const confirmPopup = page.locator('.swal2-popup');
        await expect(confirmPopup).toBeVisible();
        await confirmPopup.getByRole('button', { name: 'Desativar' }).click();

        const errorPopup = page.locator('.swal2-popup');
        await expect(errorPopup).toContainText('Não é possível desativar esta categoria enquanto houver produtos ativos vinculados');

        // A categoria continua ativa na lista (nenhuma mudança otimista foi aplicada).
        await expect(page.getByTestId('category-row-1')).not.toContainText('Inativa');
    });

    test('Deve ativar uma categoria inativa sem pedir confirmação', async ({ page }) => {
        await page.route('*/**/api/categories/2/activate', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            expect(route.request().method()).toBe('PUT');
            await route.fulfill({ status: 204 });
        });

        await page.getByTestId('switch-category-2').click();

        const toast = page.locator('.swal2-popup');
        await expect(toast).toBeVisible();
        await expect(toast).toContainText('Categoria ativada.');
    });
});
