import { test, expect } from '@playwright/test';

test.describe('Gerenciamento - StockPage', () => {

    test.beforeEach(async ({ page }) => {
        // 1. Simula Login
        await page.route('*/**/api/auth/login', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 200, json: { accessToken: 'token', user: { id: 1, name: 'Admin', companyId: 1, branchId: 1 } } });
        });

        // 2. Simula o FeatureGate
        await page.route('*/**/api/access/my-features', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 200, json: { canManageAccess: true, features: [] } });
        });

        // 3. Login rápido na interface
        await page.goto('/login');
        await page.getByTestId('username').fill('admin');
        await page.getByTestId('password').fill('123');
        await page.getByTestId('submit-login').click();
        await page.waitForURL('**/');

        // 4. Mocks das APIs do Estoque e Catálogo com coringas agressivos (*/**/)
        await page.route('*/**/api/stock/branch/*', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({
                status: 200,
                json: [
                    { id: 1, productId: 10, currentQuantity: 15, minimumQuantity: 5, maximumQuantity: 50, isBelowMinimum: false }
                ]
            });
        });

        await page.route('*/**/api/catalog/menu/*', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({
                status: 200,
                json: [
                    { id: 10, name: 'Refrigerante Cola' }
                ]
            });
        });

        await page.goto('/estoque');
    });

    test('Deve abrir e fechar o modal de lançamento de movimento', async ({ page }) => {
        await page.getByTestId('btn-open-movement').click();
        await expect(page.getByTestId('select-movement-product')).toBeVisible();
        await expect(page.getByTestId('input-movement-quantity')).toBeVisible();
    });

    test('Deve permitir alterar os limites de estoque de um item', async ({ page }) => {
        await page.getByTestId('btn-limits-10').click();
        await expect(page.getByTestId('input-limit-min')).toBeVisible();
        await expect(page.getByTestId('input-limit-max')).toBeVisible();

        await page.route('*/**/api/stock/limits/*', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 200, json: { success: true } });
        });

        await page.getByTestId('input-limit-min').fill('3');
        await page.getByTestId('btn-submit-limits').click();
    });
});