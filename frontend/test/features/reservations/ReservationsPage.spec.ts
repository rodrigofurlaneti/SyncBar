import { test, expect } from '@playwright/test';

test.describe('Gerenciamento - ReservationsPage', () => {

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

        // Login rápido na interface
        await page.goto('/login');
        await page.getByTestId('username').fill('admin');
        await page.getByTestId('password').fill('123');
        await page.getByTestId('submit-login').click();
        await page.waitForURL('**/');

        // 3. Mocks das APIs de Reservas e Mesas
        await page.route('*/**/api/reservations/branch/*', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({
                status: 200,
                json: [
                    {
                        id: 1,
                        customerName: 'Carlos Souza',
                        customerPhone: '11999999999',
                        partySize: 4,
                        reservedFor: '2026-09-05T20:00:00Z',
                        reservationStatusId: 1, // Pending
                        notes: 'Aniversário'
                    }
                ]
            });
        });

        await page.route('*/**/api/tables/branch/*', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({
                status: 200,
                json: [
                    { id: 10, number: 5, tableStatusId: 1 } // Livre
                ]
            });
        });

        await page.goto('/reservas'); // Ajuste para a rota real de reservas, se necessário
    });

    test('Deve renderizar a lista de reservas corretamente', async ({ page }) => {
        await expect(page.getByText('Reservas de mesa')).toBeVisible();
        await expect(page.getByText('Carlos Souza')).toBeVisible();
        await expect(page.getByTestId('btn-confirm-1')).toBeVisible();
        await expect(page.getByTestId('btn-cancel-1')).toBeVisible();
    });

    test('Deve abrir e preencher o modal de nova reserva com sucesso (SweetAlert)', async ({ page }) => {
        await page.route('*/**/api/reservations', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 201, json: { success: true } });
        });

        await page.getByTestId('btn-new-reservation').click();

        await expect(page.getByTestId('input-customer-name')).toBeVisible();

        await page.getByTestId('input-customer-name').fill('Mariana Lima');
        await page.getByTestId('input-customer-phone').fill('11988888888');
        await page.getByTestId('input-party-size').fill('3');
        await page.getByTestId('input-reserved-for').fill('2026-09-06T21:00');
        await page.getByTestId('input-notes').fill('Área externa');

        await page.getByTestId('btn-submit-reservation').click();

        // Valida se o Alerta de Sucesso do SweetAlert apareceu
        const swalPopup = page.locator('.swal2-popup');
        await expect(swalPopup).toBeVisible();
        await expect(swalPopup.locator('.swal2-title')).toContainText('Reserva criada!');
    });

    test('Deve abrir o modal de confirmação e escolher uma mesa livre', async ({ page }) => {
        await page.route('*/**/api/reservations/*/confirm', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({ status: 200, json: { success: true } });
        });

        await page.getByTestId('btn-confirm-1').click();

        const selectMesa = page.getByTestId('select-free-table');
        await expect(selectMesa).toBeVisible();

        await selectMesa.selectOption('10');
        await page.getByTestId('btn-submit-confirm-reservation').click();

        // Valida o SweetAlert de confirmação
        const swalPopup = page.locator('.swal2-popup');
        await expect(swalPopup).toBeVisible();
        await expect(swalPopup.locator('.swal2-title')).toContainText('Reserva confirmada!');
    });
});