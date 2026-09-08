import { test, expect, type Page } from '@playwright/test';

// CashDrawer nunca teve cobertura de testes de UI — cobre o fluxo completo do caixa:
// abrir sem sessão, resumo de sessão aberta, sangria/suprimento, estorno de venda e o fechamento
// com a conferência por forma de pagamento (Cartão de Crédito/Débito/Pix) recém-adicionada.

const openSession = {
    id: 500,
    cashRegisterId: 1,
    cashSessionStatusId: 1,
    openedByEmployeeId: 1,
    openingAmount: 100,
    openedAt: '2026-09-08T10:00:00Z',
};

const baseSummary = {
    cashSessionId: 500,
    openingAmount: 100,
    salesCount: 2,
    salesTotal: 180,
    paymentTotals: [
        { paymentMethodId: 2, totalAmount: 200 }, // Cartão de crédito
        { paymentMethodId: 4, totalAmount: 50 },  // Pix
    ],
    suprimentoTotal: 0,
    sangriaTotal: 0,
    despesaTotal: 0,
    partialPaymentsTotal: 0,
    expectedCashAmount: 100,
};

const sales = [
    { id: 900, saleNumber: 1001, customerOrderId: 42, totalAmount: 90, soldAt: '2026-09-08T10:30:00Z', paymentSummary: ['Dinheiro'] },
];

async function opts(route: import('@playwright/test').Route) {
    if (route.request().method() === 'OPTIONS') {
        await route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
        return true;
    }
    return false;
}

async function openCashDrawer(page: Page) {
    await page.getByRole('button', { name: 'Caixa', exact: true }).click();
    await expect(page.getByTestId('cash-drawer-overlay')).toBeVisible();
}

test.describe('Caixa - CashDrawer', () => {
    // O drawer da Caixa empilha resumo + vendas + movimentação + fechamento numa única coluna
    // rolável com um header sticky (.modal-head) no topo — no viewport padrão (720px de altura)
    // o scroll automático do Playwright pode deixar o alvo do clique parcialmente atrás do
    // header sticky. Um viewport mais alto evita depender de scroll para os testes abaixo.
    test.use({ viewport: { width: 1280, height: 1600 } });

    test.beforeEach(async ({ page }) => {
        await page.route('**/api/cash/registers/branch/*', route => route.fulfill({json:[{id:1,name:'Caixa 1'}]}));
        await page.route('*/**/api/auth/login', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: { accessToken: 'token', user: { id: 1, name: 'Admin', companyId: 1, branchId: 1 } } });
        });
        await page.route('*/**/api/access/my-features', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: { canManageAccess: true, features: [] } });
        });
        await page.route('*/**/api/printing/settings/branch/*', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: { printOrdersEnabled: true, printBillsEnabled: true } });
        });

        await page.goto('/login');
        await page.getByTestId('username').fill('admin');
        await page.getByTestId('password').fill('123');
        await page.getByTestId('submit-login').click();
        await page.waitForURL('**/');
    });

    test('Sem sessão aberta, exibe o formulário de abertura de caixa', async ({ page }) => {
        await page.route('*/**/api/cash/registers/1/open-session', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 404, json: {} });
        });

        await openCashDrawer(page);

        await expect(page.getByTestId('open-session-view')).toBeVisible();
        await expect(page.getByTestId('open-session-view')).toContainText('Nenhuma sessão aberta neste caixa.');
        await expect(page.getByTestId('opening-amount-input')).toHaveValue('');
        await expect(page.getByTestId('open-cash-btn')).toBeDisabled();
    });

    test('Deve abrir o caixa com o fundo de troco informado', async ({ page }) => {
        let sessionOpened = false;

        await page.route('*/**/api/cash/registers/1/open-session', async (route) => {
            if (await opts(route)) return;
            if (sessionOpened) {
                await route.fulfill({ status: 200, json: openSession });
            } else {
                await route.fulfill({ status: 404, json: {} });
            }
        });
        await page.route('*/**/api/cash/sessions', async (route) => {
            if (await opts(route)) return;
            expect(route.request().method()).toBe('POST');
            const body = route.request().postDataJSON();
            expect(body).toEqual({ cashRegisterId: 1, openedByEmployeeId: 1, openingAmount: 150 });
            sessionOpened = true;
            await route.fulfill({ status: 201, json: 500 });
        });
        await page.route('*/**/api/cash/sessions/500/summary', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: { ...baseSummary, openingAmount: 150 } });
        });
        await page.route('*/**/api/sales/session/500', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: [] });
        });

        await openCashDrawer(page);
        await expect(page.getByTestId('open-session-view')).toBeVisible();

        await page.getByTestId('opening-amount-input').fill('150');
        await page.getByTestId('open-cash-btn').click();

        await expect(page.getByTestId('session-summary-view')).toBeVisible();
        await expect(page.getByTestId('session-summary-view')).toContainText('#500');
    });

    test('Com sessão aberta, exibe o resumo consolidado', async ({ page }) => {
        await page.route('*/**/api/cash/registers/1/open-session', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: openSession });
        });
        await page.route('*/**/api/cash/sessions/500/summary', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: baseSummary });
        });
        await page.route('*/**/api/sales/session/500', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: [] });
        });

        await openCashDrawer(page);

        const summary = page.getByTestId('session-summary-view');
        await expect(summary).toContainText('Fundo de troco');
        await expect(summary).toContainText('100,00');
        await expect(summary).toContainText('Vendas (2)');
        await expect(summary).toContainText('180,00');
        await expect(summary).toContainText('Cartão de crédito');
        await expect(summary).toContainText('Pix');
        await expect(summary).toContainText('Esperado em dinheiro');
    });

    test('Lista as vendas da sessão e permite estornar com confirmação', async ({ page }) => {
        let refundCalled = false;

        await page.route('*/**/api/cash/registers/1/open-session', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: openSession });
        });
        await page.route('*/**/api/cash/sessions/500/summary', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: baseSummary });
        });
        await page.route('*/**/api/sales/session/500', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: sales });
        });
        await page.route('*/**/api/sales/900/refund', async (route) => {
            if (await opts(route)) return;
            refundCalled = true;
            expect(route.request().method()).toBe('PUT');
            const body = route.request().postDataJSON();
            expect(body.reason).toBe('Cliente desistiu');
            await route.fulfill({ status: 204 });
        });

        await openCashDrawer(page);

        await expect(page.getByTestId('sale-row-900')).toContainText('Venda #1001');
        await page.getByTestId('refund-btn-900').click();

        const confirmPopup = page.locator('.swal2-popup');
        await expect(confirmPopup).toBeVisible();
        await confirmPopup.locator('.swal2-input').fill('Cliente desistiu');
        await confirmPopup.getByRole('button', { name: 'Estornar' }).click();

        await expect.poll(() => refundCalled).toBe(true);
    });

    test('Cancelar o estorno não deve chamar a API', async ({ page }) => {
        let refundCalled = false;

        await page.route('*/**/api/cash/registers/1/open-session', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: openSession });
        });
        await page.route('*/**/api/cash/sessions/500/summary', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: baseSummary });
        });
        await page.route('*/**/api/sales/session/500', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: sales });
        });
        await page.route('*/**/api/sales/900/refund', async (route) => {
            refundCalled = true;
            await route.fulfill({ status: 204 });
        });

        await openCashDrawer(page);
        await page.getByTestId('refund-btn-900').click();

        const confirmPopup = page.locator('.swal2-popup');
        await expect(confirmPopup).toBeVisible();
        await confirmPopup.getByRole('button', { name: 'Cancelar' }).click();

        await expect(confirmPopup).toBeHidden();
        expect(refundCalled).toBe(false);
    });

    test('Deve registrar um suprimento e limpar o formulário', async ({ page }) => {
        await page.route('*/**/api/cash/registers/1/open-session', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: openSession });
        });
        await page.route('*/**/api/cash/sessions/500/summary', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: baseSummary });
        });
        await page.route('*/**/api/sales/session/500', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: [] });
        });
        await page.route('*/**/api/cash/sessions/500/movements', async (route) => {
            if (await opts(route)) return;
            expect(route.request().method()).toBe('POST');
            const body = route.request().postDataJSON();
            expect(body).toEqual({ cashMovementTypeId: 1, employeeId: 1, amount: 50, description: 'Reforço de troco' });
            await route.fulfill({ status: 204 });
        });

        await openCashDrawer(page);

        await page.getByTestId('movement-amount-input').fill('50');
        await page.getByTestId('movement-description-input').fill('Reforço de troco');
        await page.getByTestId('register-movement-btn').click();

        await expect(page.getByTestId('movement-amount-input')).toHaveValue('');
        await expect(page.getByTestId('movement-description-input')).toHaveValue('');
    });

    test('O botão de registrar movimento fica desabilitado com valor zero ou vazio', async ({ page }) => {
        await page.route('*/**/api/cash/registers/1/open-session', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: openSession });
        });
        await page.route('*/**/api/cash/sessions/500/summary', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: baseSummary });
        });
        await page.route('*/**/api/sales/session/500', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: [] });
        });

        await openCashDrawer(page);

        await expect(page.getByTestId('register-movement-btn')).toBeDisabled();
        await page.getByTestId('movement-amount-input').fill('0');
        await expect(page.getByTestId('register-movement-btn')).toBeDisabled();
    });

    test('A conferência por forma de pagamento calcula a diferença no cliente sem chamar a API', async ({ page }) => {
        await page.route('*/**/api/cash/registers/1/open-session', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: openSession });
        });
        await page.route('*/**/api/cash/sessions/500/summary', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: baseSummary });
        });
        await page.route('*/**/api/sales/session/500', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: [] });
        });

        await openCashDrawer(page);

        // Cartão de crédito (methodId 2): esperado 200, conferido 190 => Falta 10,00.
        await page.getByTestId('conference-input-2').fill('190');
        await expect(page.getByTestId('conference-diff-2')).toContainText('10,00');

        // Pix (methodId 4): esperado 50, conferido 50 => Confere.
        await page.getByTestId('conference-input-4').fill('50');
        await expect(page.getByTestId('conference-diff-4')).toContainText('Confere');
    });

    test('O botão de fechar caixa fica desabilitado sem o valor de dinheiro contado', async ({ page }) => {
        await page.route('*/**/api/cash/registers/1/open-session', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: openSession });
        });
        await page.route('*/**/api/cash/sessions/500/summary', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: baseSummary });
        });
        await page.route('*/**/api/sales/session/500', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: [] });
        });

        await openCashDrawer(page);

        await expect(page.getByTestId('close-cash-btn')).toBeDisabled();
    });

    test('Deve fechar o caixa enviando somente as modalidades conferidas e mostrar a quebra total', async ({ page }) => {
        let closeRequestBody: unknown = null;

        await page.route('*/**/api/cash/registers/1/open-session', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: openSession });
        });
        await page.route('*/**/api/cash/sessions/500/summary', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: baseSummary });
        });
        await page.route('*/**/api/sales/session/500', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: [] });
        });
        await page.route('*/**/api/cash/sessions/500/close', async (route) => {
            if (await opts(route)) return;
            expect(route.request().method()).toBe('PUT');
            closeRequestBody = route.request().postDataJSON();
            await route.fulfill({
                status: 200,
                json: {
                    cashSessionId: 500,
                    expectedAmount: 100,
                    closingAmount: 95,
                    differenceAmount: -5,
                    totalDifferenceAmount: -15,
                    paymentReconciliations: [
                        { paymentMethodId: 2, expectedAmount: 200, countedAmount: 190, differenceAmount: -10 },
                    ],
                },
            });
        });

        await openCashDrawer(page);

        // Só preenche a conferência do Cartão de Crédito — Débito e Pix ficam em branco e não
        // devem entrar no payload enviado ao backend.
        await page.getByTestId('conference-input-2').fill('190');
        await page.getByTestId('conference-input-4').fill('50');
        await page.getByTestId('counted-amount-input').fill('95');
        await page.getByTestId('close-cash-btn').click();

        const confirmPopup = page.locator('.swal2-popup');
        await expect(confirmPopup).toBeVisible();
        await confirmPopup.getByRole('button', { name: 'Fechar caixa' }).click();

        await expect(page.getByTestId('close-result-view')).toBeVisible();
        await expect.poll(() => closeRequestBody).not.toBeNull();
        expect(closeRequestBody).toEqual({
            closedByEmployeeId: 1,
            closingAmount: 95,
            paymentMethodCounts: [{ paymentMethodId: 2, countedAmount: 190 }, {paymentMethodId:4,countedAmount:50}],
        });

        await expect(page.getByTestId('close-result-view')).toContainText('Falta (dinheiro)');
        await expect(page.getByTestId('close-reconciliation-2')).toContainText('Cartão de crédito');
        await expect(page.getByTestId('close-reconciliation-2')).toContainText('Falta');
        await expect(page.getByTestId('close-total-difference')).toContainText('15,00');
    });

    test('Cancelar o fechamento não deve chamar a API', async ({ page }) => {
        let closeCalled = false;

        await page.route('*/**/api/cash/registers/1/open-session', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: openSession });
        });
        await page.route('*/**/api/cash/sessions/500/summary', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: baseSummary });
        });
        await page.route('*/**/api/sales/session/500', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: [] });
        });
        await page.route('*/**/api/cash/sessions/500/close', async (route) => {
            closeCalled = true;
            await route.fulfill({ status: 200, json: {} });
        });

        await openCashDrawer(page);

        await page.getByTestId('conference-input-2').fill('200');
        await page.getByTestId('conference-input-4').fill('50');
        await page.getByTestId('counted-amount-input').fill('100');
        await page.getByTestId('close-cash-btn').click();

        const confirmPopup = page.locator('.swal2-popup');
        await expect(confirmPopup).toBeVisible();
        await confirmPopup.getByRole('button', { name: 'Cancelar' }).click();

        await expect(confirmPopup).toBeHidden();
        expect(closeCalled).toBe(false);
        await expect(page.getByTestId('close-session-view')).toBeVisible();
    });

    test('Deve exibir a mensagem de erro da API ao falhar o fechamento', async ({ page }) => {
        await page.route('*/**/api/cash/registers/1/open-session', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: openSession });
        });
        await page.route('*/**/api/cash/sessions/500/summary', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: baseSummary });
        });
        await page.route('*/**/api/sales/session/500', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({ status: 200, json: [] });
        });
        await page.route('*/**/api/cash/sessions/500/close', async (route) => {
            if (await opts(route)) return;
            await route.fulfill({
                status: 400,
                json: { title: 'CashSession.NotOpen', detail: 'Apenas uma sessão aberta pode ser fechada.' },
            });
        });

        await openCashDrawer(page);

        await page.getByTestId('conference-input-2').fill('200');
        await page.getByTestId('conference-input-4').fill('50');
        await page.getByTestId('counted-amount-input').fill('100');
        await page.getByTestId('close-cash-btn').click();

        const confirmPopup = page.locator('.swal2-popup');
        await expect(confirmPopup).toBeVisible();
        await confirmPopup.getByRole('button', { name: 'Fechar caixa' }).click();

        await expect(page.getByTestId('error-message')).toContainText('Apenas uma sessão aberta pode ser fechada.');
        await expect(page.getByTestId('close-result-view')).toHaveCount(0);
    });

    test('Bloqueia texto inválido e modalidades não conferidas antes de encerrar', async ({ page }) => {
        let calls = 0;
        await page.route('**/api/cash/registers/1/open-session', route => route.fulfill({json:openSession}));
        await page.route('**/api/cash/sessions/500/summary', route => route.fulfill({json:baseSummary}));
        await page.route('**/api/sales/session/500', route => route.fulfill({json:[]}));
        await page.route('**/api/cash/sessions/500/close', route => { calls++; return route.fulfill({status:204}); });
        await openCashDrawer(page);
        await page.getByTestId('counted-amount-input').fill('abc');
        await expect(page.getByTestId('close-cash-btn')).toBeDisabled();
        await page.getByTestId('counted-amount-input').fill('100');
        await expect(page.getByTestId('close-cash-btn')).toBeDisabled();
        await page.getByTestId('conference-input-2').fill('200');
        await expect(page.getByTestId('close-cash-btn')).toBeDisabled();
        await page.getByTestId('conference-input-4').fill('50');
        await expect(page.getByTestId('close-cash-btn')).toBeEnabled();
        await expect(page.getByTestId('conference-total')).toContainText('350,00');
        expect(calls).toBe(0);
    });
});

