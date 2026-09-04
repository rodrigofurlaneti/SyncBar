import { test, expect } from '@playwright/test';

test.describe('Configurações - AccessPage', () => {

    test.beforeEach(async ({ page }) => {
        // 1. MOCK DE AUTENTICAÇÃO: Simula o Login para criar a sessão no Zustand
        await page.route('*/**/api/auth/login', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({
                status: 200,
                json: { accessToken: 'token_valido', user: { id: 1, name: 'Admin', companyId: 1 } }
            });
        });

        // 2. MOCK DO FEATURE GATE: Simula as permissões do usuário logado para a tela não bloquear o acesso
        await page.route('*/**/api/access/my-features', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({
                status: 200,
                json: { canManageAccess: true, features: [] }
            });
        });

        // 3. EXECUTA O LOGIN NA INTERFACE
        await page.goto('/login');
        await page.getByTestId('username').fill('admin');
        await page.getByTestId('password').fill('123');
        await page.getByTestId('submit-login').click();
        await page.waitForURL('**/'); // Espera sair da tela de login

        // 4. MOCKS DA TELA DE ACESSOS (Usando coringas */**/ para garantir a interceptação exata)

        // Mock da busca de Telas/Funcionalidades
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

        // Mock da busca de Cargos (Rota da EmployeesController)
        await page.route('*/**/api/employees/jobtitles/company/*', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({
                status: 200,
                json: [{ id: 10, name: 'Garçom' }, { id: 20, name: 'Gerente' }]
            });
        });

        // Mock da busca de Usuários (Rota da UsersController)
        await page.route('*/**/api/users/company/*', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({
                status: 200,
                json: [
                    { id: 100, userName: 'joao.silva', email: 'joao@syncbar.com', isActive: true },
                    { id: 200, userName: 'maria.caixa', email: 'maria@syncbar.com', isActive: true }
                ]
            });
        });

        // Com o Login feito e os dados interceptados, acessamos a página alvo do teste!
        await page.goto('/acessos');
    });

    test('Deve renderizar inicialmente no modo "Por cargo" e popular o select', async ({ page }) => {
        // Valida se a página carregou em vez de redirecionar
        await expect(page.getByText('Acessos')).toBeVisible();
        await expect(page.getByTestId('mode-cargo')).toHaveClass(/btn-primary/);

        // Valida se os dados falsos de cargo apareceram
        const select = page.getByTestId('target-select');
        await expect(select).toContainText('Garçom');
    });

    test('Deve alternar para o modo "Por pessoa" e listar os usuários', async ({ page }) => {
        await page.getByTestId('mode-pessoa').click();
        await expect(page.getByTestId('mode-pessoa')).toHaveClass(/btn-primary/);

        // Valida se os dados falsos de pessoa apareceram
        const select = page.getByTestId('target-select');
        await expect(select).toContainText('joao.silva (joao@syncbar.com)');
    });

    test('Deve selecionar um cargo, carregar suas permissões atuais, alterar e salvar', async ({ page }) => {
        // Intercepta o GET (carregar) e o PUT (salvar) das permissões do Cargo 10
        await page.route('*/**/api/access/jobtitles/10/features', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });

            if (route.request().method() === 'GET') {
                await route.fulfill({ status: 200, json: [1] }); // API devolve que a feature 1 já está marcada
            } else {
                await route.fulfill({ status: 200, json: { success: true } }); // API responde sucesso no salvamento
            }
        });

        const select = page.getByTestId('target-select');
        await expect(select).toContainText('Garçom'); // Aguarda as options renderizarem
        await select.selectOption('10'); // Seleciona o Garçom

        const checkboxDash = page.getByTestId('feature-checkbox-1');
        const checkboxVendas = page.getByTestId('feature-checkbox-2');

        // Garante que o useEffect fez o trabalho dele marcando a tela correta
        await expect(checkboxDash).toBeChecked();
        await expect(checkboxVendas).not.toBeChecked();

        // O usuário interage (Marca Vendas e Salva)
        await checkboxVendas.check();
        await page.getByTestId('save-button').click();

        // Valida se a mutação rodou e exibiu a mensagem
        const successMsg = page.getByTestId('success-message');
        await expect(successMsg).toBeVisible();
        await expect(successMsg).toContainText('Acessos salvos.');
    });

    test('Deve exibir erro ao falhar na gravação dos acessos', async ({ page }) => {
        // Intercepta o GET (carregar) e o PUT (salvar falhando) das permissões do Usuário 100
        await page.route('*/**/api/access/users/100/features', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });

            if (route.request().method() === 'GET') {
                await route.fulfill({ status: 200, json: [] });
            } else {
                await route.fulfill({
                    status: 403,
                    json: {
                        message: 'Acesso negado.',
                        detail: 'Você não tem permissão para alterar acessos.' // Propriedade que o apiClient exige
                    }
                });
            }
        });

        await page.getByTestId('mode-pessoa').click();

        const select = page.getByTestId('target-select');
        await expect(select).toContainText('joao.silva');
        await select.selectOption('100');

        await page.getByTestId('feature-checkbox-3').check();
        await page.getByTestId('save-button').click();

        // Valida se o cliente tratou o erro HTTP 403 e exibiu na tela
        const errorMessage = page.getByTestId('error-message');
        await expect(errorMessage).toBeVisible();
        await expect(errorMessage).toContainText('Você não tem permissão para alterar acessos.');
    });
});