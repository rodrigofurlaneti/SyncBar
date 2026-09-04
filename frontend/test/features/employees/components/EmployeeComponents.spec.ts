import { test, expect } from '@playwright/test';

test.describe('Componentes Isolados - Equipe (EmployeeCard, Modal e Drawer)', () => {

    test.beforeEach(async ({ page }) => {
        // Simulação básica de login para habilitar o contexto da aplicação
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
    });

    test('EmployeeCard: Deve exibir corretamente as iniciais, cargo e status de acesso', async ({ page }) => {
        // Mock de dados para exibir apenas um cartão de funcionário isolado
        await page.route('*/**/api/employees/branch/*', async (route) => {
            await route.fulfill({
                status: 200,
                json: [
                    {
                        id: 99,
                        name: 'Roberto Carlos',
                        cpf: '11122233344',
                        jobTitleId: 10,
                        hasSystemAccess: true,
                        roleName: 'Caixa',
                        extraFeatureCount: 1,
                        email: 'roberto@syncbar.com',
                        phone: '11988887777',
                        salary: 3000,
                        appUserId: 88
                    }
                ]
            });
        });

        await page.route('*/**/api/employees/jobtitles/company/*', async (route) => {
            await route.fulfill({ status: 200, json: [{ id: 10, name: 'Caixa' }] });
        });

        await page.goto('/equipe');

        // Valida o card do funcionário (EmployeeCard)
        const card = page.getByTestId('employee-card-99');
        await expect(card).toBeVisible();
        await expect(card.locator('.emp-avatar')).toHaveText('RC'); // Iniciais geradas pelo initialsOf
        await expect(card.getByText('Roberto Carlos')).toBeVisible();
        await expect(card.locator('.emp-role')).toHaveText('Caixa');
        await expect(card.getByTestId('btn-edit-employee-99')).toBeVisible();
        await expect(card.getByTestId('btn-access-employee-99')).toBeVisible();
    });

    test('EmployeeModal: Deve abrir o modal de novo funcionário ao clicar no botão principal', async ({ page }) => {
        await page.route('*/**/api/employees/branch/*', async (route) => {
            await route.fulfill({ status: 200, json: [] });
        });
        await page.route('*/**/api/employees/jobtitles/company/*', async (route) => {
            await route.fulfill({ status: 200, json: [{ id: 10, name: 'Cozinheiro' }] });
        });

        await page.goto('/equipe');

        // Dispara a abertura do EmployeeModal
        await page.getByTestId('btn-new-employee').click();

        // Valida elementos estruturais do modal de cadastro/edição
        await expect(page.getByTestId('input-emp-name')).toBeVisible();
        await expect(page.getByTestId('input-emp-cpf')).toBeVisible();
        await expect(page.getByTestId('select-emp-jobtitle')).toBeVisible();
        await expect(page.getByTestId('btn-submit-employee')).toBeDisabled(); // Desabilitado por padrão se vazio
    });
});