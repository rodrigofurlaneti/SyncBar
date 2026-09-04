import { test, expect } from '@playwright/test';

test.describe('Cadastro - SignupPage', () => {

    test.beforeEach(async ({ page }) => {
        await page.goto('/cadastro');
    });

    test('Deve renderizar os elementos iniciais da tela de cadastro', async ({ page }) => {
        await expect(page.getByText('SYNCBAR')).toBeVisible();
        await expect(page.getByText('cadastre seu bar')).toBeVisible();

        await expect(page.getByTestId('legalName')).toBeVisible();
        await expect(page.getByTestId('cnpj')).toBeVisible();
        await expect(page.getByTestId('adminEmail')).toBeVisible();

        await expect(page.getByTestId('submit-signup')).toBeVisible();
    });

    test('Deve bloquear o envio se os campos obrigatórios estiverem vazios', async ({ page }) => {
        await page.getByTestId('submit-signup').click();

        // O navegador deve impedir o envio (validação HTML5) e continuar na mesma rota
        await expect(page).toHaveURL(/.*\/cadastro/);
    });

    test('Deve exibir erro (SweetAlert) ao falhar na criação da conta (Ex: CNPJ duplicado)', async ({ page }) => {

        // Coringa agressivo para interceptar independente da base URL
        await page.route('*/**/api/companies/register', async (route) => {
            if (route.request().method() === 'OPTIONS') {
                return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            }

            await route.fulfill({
                status: 400, // Status 400 para evitar cair na regra global de sessão expirada
                json: {
                    message: 'CNPJ já cadastrado no sistema.',
                    errors: { cnpj: ['CNPJ já cadastrado no sistema.'] },
                    detail: 'CNPJ já cadastrado no sistema.'
                }
            });
        });

        // Preenche o formulário
        await page.getByTestId('legalName').fill('Bar do João LTDA');
        await page.getByTestId('tradeName').fill('Bar do João');
        await page.getByTestId('cnpj').fill('12345678000199');
        await page.getByTestId('adminName').fill('João Silva');
        await page.getByTestId('adminCpf').fill('12345678900');
        await page.getByTestId('branchName').fill('Matriz');
        await page.getByTestId('adminUserName').fill('joao.admin');
        await page.getByTestId('adminEmail').fill('joao@email.com');
        await page.getByTestId('adminPassword').fill('senha123');

        await page.getByTestId('submit-signup').click();

        // Valida se o modal do SweetAlert2 apareceu na tela
        const swalPopup = page.locator('.swal2-popup');
        await expect(swalPopup).toBeVisible();

        // Valida se o título e a mensagem de erro estão corretas
        await expect(swalPopup.locator('.swal2-title')).toContainText('Erro no Cadastro');
        await expect(swalPopup.locator('.swal2-html-container')).toContainText('CNPJ já cadastrado no sistema.');

        // Clica no botão para fechar o alerta
        await swalPopup.getByRole('button', { name: 'Revisar dados' }).click();
        await expect(swalPopup).not.toBeVisible();
    });

    test('Deve exibir sucesso (SweetAlert) e navegar para a tela de login', async ({ page }) => {
        // Intercepta a chamada simulando o retorno de sucesso
        await page.route('*/**/api/companies/register', async (route) => {
            if (route.request().method() === 'OPTIONS') {
                return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            }

            await route.fulfill({
                status: 201, // Created
                json: {
                    companyId: 1,
                    branchId: 1,
                    adminUserId: 100
                }
            });
        });

        // Preenche o formulário completo
        await page.getByTestId('legalName').fill('Bar do João LTDA');
        await page.getByTestId('tradeName').fill('Bar do João');
        await page.getByTestId('cnpj').fill('12345678000199');
        await page.getByTestId('adminName').fill('João Silva');
        await page.getByTestId('adminCpf').fill('12345678900');
        await page.getByTestId('branchName').fill('Matriz');
        await page.getByTestId('adminUserName').fill('joao.admin');
        await page.getByTestId('adminEmail').fill('joao@email.com');
        await page.getByTestId('adminPassword').fill('senhaSuperSegura123');

        await page.getByTestId('submit-signup').click();

        // Valida o Alerta de Sucesso
        const swalPopup = page.locator('.swal2-popup');
        await expect(swalPopup).toBeVisible();
        await expect(swalPopup.locator('.swal2-title')).toContainText('Conta criada!');
        await expect(swalPopup.locator('.swal2-html-container')).toContainText('Seu bar foi cadastrado com sucesso.');

        // Verifica se houve o redirecionamento automático após 1.5s
        await expect(page).toHaveURL(/.*\/login/);
    });
});