import { test, expect } from '@playwright/test';

// Aba "Métodos de pagamentos" da Integração Asaas — nunca teve cobertura de testes de UI.
// Cobre a resolução com fallback (própria da filial / herdada da matriz / nenhuma cadastrada),
// os toggles, e o roteamento create-vs-update (POST quando não existe linha própria da filial,
// PUT quando existe).

async function opts(route: import('@playwright/test').Route) {
    if (route.request().method() === 'OPTIONS') {
        await route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
        return true;
    }
    return false;
}

async function openPaymentMethodsTab(page: import('@playwright/test').Page) {
    await page.goto('/integracoes/asaas');
    await page.getByRole('tab', { name: 'Métodos de pagamentos' }).click();
    await expect(page.getByTestId('payment-methods-list')).toBeVisible();
}

test.describe('Integração Asaas - Métodos de pagamentos', () => {
    test.beforeEach(async ({ page }) => {
        await page.route('*/**/api/auth/login', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: { accessToken: 'token', user: { id: 1, name: 'Admin', companyId: 1, branchId: 1 } } });
        });
        await page.route('*/**/api/access/my-features', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: { canManageAccess: true, features: [] } });
        });
        await page.route('*/**/api/asaas/settings/company/*/active', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: [] });
        });

        await page.goto('/login');
        await page.getByTestId('username').fill('admin');
        await page.getByTestId('password').fill('123');
        await page.getByTestId('submit-login').click();
        await page.waitForURL('**/');
    });

    test('Sem configuração cadastrada, todos os métodos vêm ligados por padrão', async ({ page }) => {
        await page.route('*/**/api/branch-payment-method-settings/resolve*', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: null });
        });

        await openPaymentMethodsTab(page);

        await expect(page.getByTestId('payment-methods-none-chip')).toBeVisible();
        for (const key of ['enablePix', 'enableBoleto', 'enableCreditCard', 'enableDebitCard', 'enableCashMachine']) {
            await expect(page.getByTestId(`payment-method-switch-${key}`)).toHaveAttribute('aria-checked', 'true');
            await expect(page.getByTestId(`payment-method-status-${key}`)).toContainText('Ligada');
        }
        await expect(page.getByTestId('payment-methods-save-btn')).toBeDisabled();
        await expect(page.getByTestId('payment-methods-cancel-btn')).toBeDisabled();
    });

    test('Com configuração própria da filial, reflete os flags retornados pela API', async ({ page }) => {
        await page.route('*/**/api/branch-payment-method-settings/resolve*', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({
                status: 200,
                json: { id: 77, companyId: 1, branchId: 1, enablePix: true, enableBoleto: true, enableCreditCard: true, enableDebitCard: true, enableCashMachine: false, isActive: true },
            });
        });

        await openPaymentMethodsTab(page);

        await expect(page.getByTestId('payment-methods-own-chip')).toBeVisible();
        await expect(page.getByTestId('payment-method-switch-enableCashMachine')).toHaveAttribute('aria-checked', 'false');
        await expect(page.getByTestId('payment-method-status-enableCashMachine')).toContainText('Desligada');
        await expect(page.getByTestId('payment-method-switch-enablePix')).toHaveAttribute('aria-checked', 'true');
    });

    test('Configuração herdada da matriz exibe o aviso de herança', async ({ page }) => {
        await page.route('*/**/api/branch-payment-method-settings/resolve*', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({
                status: 200,
                json: { id: 5, companyId: 1, branchId: null, enablePix: true, enableBoleto: true, enableCreditCard: true, enableDebitCard: true, enableCashMachine: true, isActive: true },
            });
        });

        await openPaymentMethodsTab(page);

        await expect(page.getByTestId('payment-methods-inherited-chip')).toBeVisible();
    });

    test('Alternar um método habilita Cancelar e Salvar alterações', async ({ page }) => {
        await page.route('*/**/api/branch-payment-method-settings/resolve*', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: null });
        });

        await openPaymentMethodsTab(page);
        await page.getByTestId('payment-method-switch-enableCashMachine').click();

        await expect(page.getByTestId('payment-method-switch-enableCashMachine')).toHaveAttribute('aria-checked', 'false');
        await expect(page.getByTestId('payment-method-status-enableCashMachine')).toContainText('Desligada');
        await expect(page.getByTestId('payment-methods-save-btn')).toBeEnabled();
        await expect(page.getByTestId('payment-methods-cancel-btn')).toBeEnabled();
    });

    test('Cancelar reverte a alteração local sem chamar a API', async ({ page }) => {
        let saveCalled = false;
        await page.route('*/**/api/branch-payment-method-settings/resolve*', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: null });
        });
        await page.route('*/**/api/branch-payment-method-settings', async (route) => {
            saveCalled = true;
            await route.fulfill({ status: 201, json: {} });
        });

        await openPaymentMethodsTab(page);
        await page.getByTestId('payment-method-switch-enableCashMachine').click();
        await page.getByTestId('payment-methods-cancel-btn').click();

        await expect(page.getByTestId('payment-method-switch-enableCashMachine')).toHaveAttribute('aria-checked', 'true');
        await expect(page.getByTestId('payment-methods-save-btn')).toBeDisabled();
        expect(saveCalled).toBe(false);
    });

    test('Salvar quando já existe configuração própria da filial dispara PUT com os flags atualizados', async ({ page }) => {
        let putBody: unknown = null;

        await page.route('*/**/api/branch-payment-method-settings/resolve*', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({
                status: 200,
                json: { id: 77, companyId: 1, branchId: 1, enablePix: true, enableBoleto: true, enableCreditCard: true, enableDebitCard: true, enableCashMachine: false, isActive: true },
            });
        });
        await page.route('*/**/api/branch-payment-method-settings/77', async (route) => {
            if (await opts(route)) return;
            expect(route.request().method()).toBe('PUT');
            putBody = route.request().postDataJSON();
            await route.fulfill({ status: 204 });
        });

        await openPaymentMethodsTab(page);
        await page.getByTestId('payment-method-switch-enableCashMachine').click();
        await page.getByTestId('payment-methods-save-btn').click();

        const toast = page.locator('.swal2-popup');
        await expect(toast).toBeVisible();
        await expect(toast).toContainText('Métodos de pagamento salvos.');

        expect(putBody).toEqual({
            companyId: 1,
            enablePix: true,
            enableBoleto: true,
            enableCreditCard: true,
            enableDebitCard: true,
            enableCashMachine: true,
        });
        await expect(page.getByTestId('payment-methods-save-btn')).toBeDisabled();
    });

    test('Salvar quando não existe configuração cria uma nova (POST) com companyId e branchId', async ({ page }) => {
        let postBody: unknown = null;

        await page.route('*/**/api/branch-payment-method-settings/resolve*', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: null });
        });
        await page.route('*/**/api/branch-payment-method-settings', async (route) => {
            if (await opts(route)) return;
            expect(route.request().method()).toBe('POST');
            postBody = route.request().postDataJSON();
            await route.fulfill({ status: 201, json: { id: 99, ...(postBody as object) } });
        });

        await openPaymentMethodsTab(page);
        await page.getByTestId('payment-method-switch-enableCashMachine').click();
        await page.getByTestId('payment-methods-save-btn').click();

        const toast = page.locator('.swal2-popup');
        await expect(toast).toContainText('Métodos de pagamento salvos.');

        expect(postBody).toEqual({
            companyId: 1,
            branchId: 1,
            enablePix: true,
            enableBoleto: true,
            enableCreditCard: true,
            enableDebitCard: true,
            enableCashMachine: false,
        });
    });

    test('Salvar quando a configuração é herdada da matriz cria uma configuração própria da filial (POST), não atualiza a da empresa', async ({ page }) => {
        let postCalled = false;
        let putCalled = false;

        await page.route('*/**/api/branch-payment-method-settings/resolve*', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({
                status: 200,
                json: { id: 5, companyId: 1, branchId: null, enablePix: true, enableBoleto: true, enableCreditCard: true, enableDebitCard: true, enableCashMachine: true, isActive: true },
            });
        });
        await page.route('*/**/api/branch-payment-method-settings/5', async (route) => {
            putCalled = true;
            await route.fulfill({ status: 204 });
        });
        await page.route('*/**/api/branch-payment-method-settings', async (route) => {
            if (await opts(route)) return;
            expect(route.request().method()).toBe('POST');
            postCalled = true;
            const body = route.request().postDataJSON();
            expect(body.branchId).toBe(1);
            await route.fulfill({ status: 201, json: { id: 88, ...body } });
        });

        await openPaymentMethodsTab(page);
        await page.getByTestId('payment-method-switch-enableCashMachine').click();
        await page.getByTestId('payment-methods-save-btn').click();

        await expect.poll(() => postCalled).toBe(true);
        expect(putCalled).toBe(false);
    });

    test('Deve exibir a mensagem de erro da API ao falhar o salvamento', async ({ page }) => {
        await page.route('*/**/api/branch-payment-method-settings/resolve*', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: null });
        });
        await page.route('*/**/api/branch-payment-method-settings', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({
                status: 400,
                json: { title: 'BranchPaymentMethodSetting.AlreadyExists', detail: 'Já existe uma configuração cadastrada para esta filial.' },
            });
        });

        await openPaymentMethodsTab(page);
        await page.getByTestId('payment-method-switch-enableCashMachine').click();
        await page.getByTestId('payment-methods-save-btn').click();

        await expect(page.getByTestId('payment-methods-error')).toContainText('Já existe uma configuração cadastrada para esta filial.');
    });
});
