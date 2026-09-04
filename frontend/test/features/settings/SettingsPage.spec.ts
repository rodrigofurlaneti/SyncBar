import { test, expect } from '@playwright/test';

test.describe('Configurações da Filial - SettingsPage', () => {

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

        // Login na aplicação
        await page.goto('/login');
        await page.getByTestId('username').fill('admin');
        await page.getByTestId('password').fill('123');
        await page.getByTestId('submit-login').click();
        await page.waitForURL('**/');

        // Mocks das configurações da filial
        await page.route('*/**/api/settings/service-fee*', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 200, json: { enabled: true } });
        });

        await page.route('*/**/api/settings/qr-view*', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 200, json: { enabled: true } });
        });

        await page.route('*/**/api/settings/table-reading*', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 200, json: { isCameraInputEnabled: false, isBarcodeEnabled: false, isQrCodeEnabled: false } });
        });

        await page.route('*/**/api/comandas/setting*', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 200, json: { defaultLimitAmount: 500 } });
        });

        await page.route('*/**/api/employees/branch/*', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 200, json: [{ id: 1, name: 'Garçom Teste', isActive: true }] });
        });

        await page.goto('/configuracoes'); // Ajuste se a rota exata for diferente
    });

    test('Deve renderizar os títulos de configuração corretamente', async ({ page }) => {
        await expect(page.getByText('Configurações')).toBeVisible();
        await expect(page.getByText('Taxa de serviço (10%)')).toBeVisible();
        await expect(page.getByText('Visualização do cliente (QR Code)')).toBeVisible();
        await expect(page.getByText('Limite de comanda')).toBeVisible();
    });

    test('Deve permitir alterar o limite padrão de comanda', async ({ page }) => {
        await page.route('*/**/api/comandas/setting*', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 200, json: { success: true } });
        });

        const input = page.getByTestId('input-comanda-limit');
        await input.fill('750');
        await page.getByTestId('btn-save-comanda-limit').click();
    });

    test('Deve permitir selecionar e salvar o funcionário de autoatendimento', async ({ page }) => {
        await page.route('*/**/api/settings/self-service-employee*', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 200, json: { success: true } });
        });

        const select = page.getByTestId('select-self-service-employee');
        await select.selectOption('1');
        await page.getByTestId('btn-save-self-service').click();
    });
});