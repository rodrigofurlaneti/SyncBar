import { test, expect } from '@playwright/test';

test.describe('Gerenciamento - UsersPage', () => {

    test.beforeEach(async ({ page }) => {
        // 1. Simula Login
        await page.route('*/**/api/auth/login', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 200, json: { accessToken: 'token', user: { id: 1, name: 'Admin', companyId: 1 } } });
        });

        // 2. Simula permissões do FeatureGate
        await page.route('*/**/api/access/my-features', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 200, json: { canManageAccess: true, features: [] } });
        });

        // 3. Faz login
        await page.goto('/login');
        await page.getByTestId('username').fill('admin');
        await page.getByTestId('password').fill('123');
        await page.getByTestId('submit-login').click();
        await page.waitForURL('**/');

        // 4. Mocks da API de Usuários e Perfis
        await page.route('*/**/api/users/company/*', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({
                status: 200,
                json: [
                    { id: 1, userName: 'joao.silva', email: 'joao@syncbar.com', roleIds: [10], isActive: true }
                ]
            });
        });

        await page.route('*/**/api/users/roles/company/*', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({
                status: 200,
                json: [
                    { id: 10, name: 'Garçom', description: 'Atendimento nas mesas' }
                ]
            });
        });

        await page.goto('/usuarios'); // Ajuste a rota real se necessário
    });

    test('Deve listar os usuários cadastrados corretamente', async ({ page }) => {
        await expect(page.getByText('Usuários e perfis')).toBeVisible();
        await expect(page.getByText('joao.silva')).toBeVisible();
        await expect(page.getByText('joao@syncbar.com')).toBeVisible();
    });

    test('Deve abrir o overlay de criação de usuário', async ({ page }) => {
        await page.getByTestId('btn-new-user').click();
        await expect(page.getByTestId('input-username')).toBeVisible();
        await expect(page.getByTestId('input-email')).toBeVisible();
        await expect(page.getByTestId('input-password')).toBeVisible();
    });

    test('Deve criar um novo perfil (Role) direto pelo modal', async ({ page }) => {
        await page.getByTestId('btn-new-user').click();

        // Mock para criação de perfil
        await page.route('*/**/api/users/roles', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 201, json: 20 }); // Retorna o ID do novo perfil
        });

        await page.getByTestId('new-role-name').fill('Cozinha');
        await page.getByTestId('new-role-description').fill('Preparo de pratos');
        await page.getByTestId('btn-create-role').click();

        // O novo perfil deve aparecer selecionado no checklist
        await expect(page.getByTestId('role-checkbox-10')).toBeVisible();
    });
});