import { test, expect } from '@playwright/test';

test.describe('Autenticação - LoginPage', () => {

    test.beforeEach(async ({ page }) => {
        await page.goto('/login');
    });

    test('Deve renderizar os elementos iniciais da tela de login', async ({ page }) => {
        await expect(page.getByTestId('system-logo')).toBeVisible();
        await expect(page.getByTestId('username')).toBeVisible();
        await expect(page.getByTestId('password')).toBeVisible();
        await expect(page.getByTestId('submit-login')).toBeVisible();
    });

    test('Deve bloquear o envio se os campos estiverem vazios', async ({ page }) => {
        await page.getByTestId('submit-login').click();
        await expect(page).toHaveURL(/.*\/login/);
    });

    test('Deve alternar o tema do sistema', async ({ page }) => {
        const themeToggle = page.getByTestId('theme-toggle');
        const logo = page.getByTestId('system-logo');

        const initialSrc = await logo.getAttribute('src');
        await themeToggle.click();
        const newSrc = await logo.getAttribute('src');

        expect(newSrc).not.toBe(initialSrc);
    });

    test('Deve exibir erro (SweetAlert) ao tentar logar com credenciais inválidas', async ({ page }) => {
        await page.route('*/**/api/auth/login', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });

            // CORREÇÃO AQUI: Mudamos de 401 para 400 para evitar a regra global de "Sessão Expirada"
            await route.fulfill({
                status: 400,
                json: {
                    message: 'Credenciais inválidas',
                    detail: 'Credenciais inválidas'
                }
            });
        });

        await page.getByTestId('username').fill('usuario_errado');
        await page.getByTestId('password').fill('senha_errada');
        await page.getByTestId('submit-login').click();

        // Valida se o modal do SweetAlert2 apareceu na tela
        const swalPopup = page.locator('.swal2-popup');
        await expect(swalPopup).toBeVisible();

        // Valida se o título e a mensagem de erro estão corretas
        await expect(swalPopup.locator('.swal2-title')).toContainText('Falha na Autenticação');
        await expect(swalPopup.locator('.swal2-html-container')).toContainText('Credenciais inválidas');

        // Clica no botão para fechar o alerta
        await swalPopup.getByRole('button', { name: 'Tentar novamente' }).click();
        await expect(swalPopup).not.toBeVisible();
    });

    test('Deve exibir sucesso (SweetAlert) e navegar para a home', async ({ page }) => {
        await page.route('*/**/api/auth/login', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({
                status: 200,
                json: {
                    accessToken: 'jwt_valido_simulado',
                    user: { id: '1', name: 'Admin', companyId: 1 }
                }
            });
        });

        await page.getByTestId('username').fill('admin');
        await page.getByTestId('password').fill('senha_correta');
        await page.getByTestId('submit-login').click();

        // Valida se o alerta de sucesso apareceu antes de navegar
        const swalPopup = page.locator('.swal2-popup');
        await expect(swalPopup).toBeVisible();
        await expect(swalPopup.locator('.swal2-title')).toContainText('Bem-vindo(a)!');

        // Aguarda a resolução da Promise do SweetAlert (timer de 1.5s) e o redirecionamento
        await expect(page).toHaveURL('http://localhost:5173/');
    });
});