import { test, expect } from '@playwright/test';

test.describe('Controle de Acesso - FeatureGate & NoAccessPage', () => {

    test.beforeEach(async ({ page }) => {
        // 1. Simula o login para preencher a store e não ser bloqueado pela falta de Token
        await page.route('*/**/api/auth/login', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({
                status: 200,
                json: { accessToken: 'token_valido', user: { id: 1, name: 'Usuário', userName: 'user' } }
            });
        });

        // 2. Faz login rapidamente em todos os testes
        await page.goto('/login');
        await page.getByTestId('username').fill('user');
        await page.getByTestId('password').fill('123');
        await page.getByTestId('submit-login').click();
        await page.waitForURL('**/'); // Espera sair da tela de login
    });

    test('Deve renderizar a NoAccessPage corretamente', async ({ page }) => {
        // Navega direto para a página de erro
        await page.goto('/sem-acesso');

        await expect(page.getByTestId('no-access-title')).toBeVisible();
        await expect(page.getByTestId('no-access-title')).toContainText('Sem telas liberadas');
        await expect(page.getByTestId('no-access-message')).toContainText('Peça ao gerente para conceder acesso');
    });

    test('Cenário 1: Deve PERMITIR acesso caso o usuário seja gerente (canManageAccess: true)', async ({ page }) => {
        await page.route('*/**/api/access/my-features', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            // Retorna TRUE para gerenciar, mesmo sem features específicas
            await route.fulfill({
                status: 200,
                json: { canManageAccess: true, features: [] }
            });
        });

        // Tenta acessar a tela de configurações de acessos (ou qualquer outra rota protegida)
        await page.goto('/acessos');

        // Verifica se a URL não foi redirecionada
        await expect(page).toHaveURL(/.*\/acessos/);
    });

    test('Cenário 2: Deve PERMITIR acesso caso o usuário tenha a feature específica liberada', async ({ page }) => {
        await page.route('*/**/api/access/my-features', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            // Não é gerente, mas tem a feature "Estoque"
            await route.fulfill({
                status: 200,
                json: { canManageAccess: false, features: ['Estoque'] }
            });
        });

        // Tenta acessar a rota de estoque
        await page.goto('/estoque');

        // Verifica se continuou na rota
        await expect(page).toHaveURL(/.*\/estoque/);
    });

    test('Cenário 3: Deve REDIRECIONAR para a primeira tela permitida caso tente acessar rota bloqueada', async ({ page }) => {
        await page.route('*/**/api/access/my-features', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            // Não tem Estoque, mas tem Cardápio
            await route.fulfill({
                status: 200,
                json: { canManageAccess: false, features: ['Cardapio'] }
            });
        });

        // Tenta acessar a rota de estoque
        await page.goto('/estoque');

        // Como não tem Estoque, o "firstAllowed" vai achar o "Cardapio", que aponta para "/produtos"
        await expect(page).toHaveURL(/.*\/produtos/);
    });

    test('Cenário 4: Deve REDIRECIONAR para /sem-acesso caso o usuário não tenha NENHUMA permissão', async ({ page }) => {
        await page.route('*/**/api/access/my-features', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            // Lista de acessos vazia
            await route.fulfill({
                status: 200,
                json: { canManageAccess: false, features: [] }
            });
        });

        // Tenta acessar a rota de estoque
        await page.goto('/estoque');

        // Nenhum acesso encontrado, joga para a página de bloqueio absoluto
        await expect(page).toHaveURL(/.*\/sem-acesso/);
        await expect(page.getByTestId('no-access-title')).toBeVisible();
    });
});