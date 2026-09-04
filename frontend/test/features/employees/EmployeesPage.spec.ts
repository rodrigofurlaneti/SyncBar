import { test, expect } from '@playwright/test';

test.describe('Gerenciamento de Equipe - EmployeesPage', () => {

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

        // 3. Mocks das APIs de Funcionários e Cargos
        await page.route('*/**/api/employees/branch/*', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({
                status: 200,
                json: [
                    {
                        id: 1,
                        name: 'Ana Paula',
                        cpf: '12345678901',
                        jobTitleId: 10,
                        hasSystemAccess: true,
                        roleName: 'Garçom',
                        extraFeatureCount: 0,
                        email: 'ana@syncbar.com',
                        phone: '11999999999',
                        salary: 2500,
                        appUserId: 50
                    }
                ]
            });
        });

        await page.route('*/**/api/employees/jobtitles/company/*', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({
                status: 200,
                json: [
                    { id: 10, name: 'Garçom' },
                    { id: 20, name: 'Gerente' }
                ]
            });
        });

        await page.goto('/equipe');
    });

    test('Deve renderizar a lista de funcionários corretamente', async ({ page }) => {
        await expect(page.getByText('Equipe')).toBeVisible();
        await expect(page.getByText('Ana Paula')).toBeVisible();
        await expect(page.locator('.emp-role').filter({ hasText: 'Garçom' })).toBeVisible();
        await expect(page.getByTestId('btn-new-employee')).toBeVisible();
    });

    test('Deve abrir o modal de novo funcionário e validar os campos', async ({ page }) => {
        await page.getByTestId('btn-new-employee').click();

        await expect(page.getByTestId('input-emp-name')).toBeVisible();
        await expect(page.getByTestId('input-emp-cpf')).toBeVisible();
        await expect(page.getByTestId('select-emp-jobtitle')).toBeVisible();

        // O botão salvar deve estar desabilitado inicialmente
        await expect(page.getByTestId('btn-submit-employee')).toBeDisabled();
    });

    test('Deve permitir cadastrar um novo funcionário com sucesso (SweetAlert)', async ({ page }) => {
        // Mock da rota de criação
        await page.route('*/**/api/employees', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 201, json: { success: true } });
        });

        await page.getByTestId('btn-new-employee').click();

        await page.getByTestId('input-emp-name').fill('Carlos Eduardo');
        await page.getByTestId('input-emp-cpf').fill('98765432100');
        await page.getByTestId('select-emp-jobtitle').selectOption('10');
        await page.getByTestId('input-emp-email').fill('carlos@syncbar.com');

        await page.getByTestId('btn-submit-employee').click();

        // Valida se o Alerta de Sucesso do SweetAlert apareceu na tela
        const swalPopup = page.locator('.swal2-popup');
        await expect(swalPopup).toBeVisible();
        await expect(swalPopup.locator('.swal2-title')).toContainText('Funcionário cadastrado!');
    });

    test('Deve abrir o painel lateral (Drawer) de acessos extras do funcionário', async ({ page }) => {
        // Mock para buscar features e permissões do usuário no drawer
        await page.route('*/**/api/access/features', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({
                status: 200,
                json: [
                    { id: 1, name: 'Dashboard' },
                    { id: 2, name: 'Vendas' },
                    { id: 3, name: 'Estoque' }
                ]
            });
        });

        await page.route('*/**/api/access/jobtitles/*/features', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 200, json: [1] });
        });

        await page.route('*/**/api/access/users/*/features', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            if (route.request().method() === 'GET') {
                await route.fulfill({ status: 200, json: [2] });
            } else {
                await route.fulfill({ status: 200, json: { success: true } });
            }
        });

        // Clica no botão "Acessos" do card da Ana Paula
        await page.getByTestId('btn-access-employee-1').click();

        // Valida se o Drawer abriu
        await expect(page.getByText('Acessos — Ana Paula')).toBeVisible();
        await expect(page.getByTestId('btn-save-drawer-access')).toBeVisible();

        // Clica em salvar acessos
        await page.getByTestId('btn-save-drawer-access').click();

        // Valida o SweetAlert de sucesso
        const swalPopup = page.locator('.swal2-popup');
        await expect(swalPopup).toBeVisible();
        await expect(swalPopup.locator('.swal2-title')).toContainText('Acessos atualizados!');
    });
});