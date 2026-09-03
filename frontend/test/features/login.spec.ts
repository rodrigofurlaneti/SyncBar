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

    test('Deve exibir erro ao tentar logar com credenciais inválidas', async ({ page }) => {
        await page.route('**/api/auth/login', async (route) => {
            await route.fulfill({
                status: 401,
                contentType: 'application/json',
                body: JSON.stringify({ message: 'Credenciais inválidas' })
            });
        });

        // Seletores limpos e diretos graças ao data-testid
        await page.getByTestId('username').fill('usuario_errado');
        await page.getByTestId('password').fill('senha_errada');
        await page.getByTestId('submit-login').click();

        await expect(page.getByTestId('error-message')).toBeVisible();
    });

    test('Deve realizar o login com sucesso e navegar para a home', async ({ page }) => {
        await page.route('**/api/auth/login', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    accessToken: 'jwt_valido_simulado',
                    user: { id: '1', name: 'Admin' }
                })
            });
        });

        await page.getByTestId('username').fill('admin');
        await page.getByTestId('password').fill('senha_correta');
        await page.getByTestId('submit-login').click();

        await expect(page).toHaveURL('http://localhost:5173/');
    });
});