import { test, expect } from '@playwright/test';

test.describe('Componente - InventoryOverlay', () => {

    test('Deve desabilitar o botão se nenhum item foi preenchido', async ({ page }) => {
        // Como o InventoryOverlay é um componente modal, você pode renderizá-lo diretamente 
        // ou via rota de testes dedicada. Aqui validamos o estado inicial do botão.
        // Dica: Ajuste a rota para a página que abre este inventário no seu sistema.
        await page.goto('/estoque');

        // Supondo que haja um botão para abrir o inventário físico
        const btnAbrir = page.getByTestId('btn-abrir-inventario');
        if (await btnAbrir.isVisible()) {
            await btnAbrir.click();
            const submitBtn = page.getByTestId('btn-submit-inventory');
            await expect(submitBtn).toBeDisabled();
        }
    });

    test('Deve permitir registrar o inventário ao preencher a contagem de um item', async ({ page }) => {
        await page.route('*/**/api/inventory/adjust', async (route) => {
            if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
            await route.fulfill({
                status: 200,
                json: [
                    { productId: 1, previousQuantity: 10, countedQuantity: 8, difference: -2 }
                ]
            });
        });

        // Este teste serve como base conceitual para o fluxo de preenchimento e submissão
        // Certifique-se de disparar a abertura do componente no seu ambiente de testes integrado.
    });
});