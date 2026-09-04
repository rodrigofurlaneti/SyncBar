# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: features\comandas\OpenComandaDialog.spec.ts >> OpenComandaDialog >> deve exibir erro do SweetAlert caso a abertura falhe na API
- Location: test\features\comandas\OpenComandaDialog.spec.ts:84:5

# Error details

```
Error: expect(locator).toBeVisible() failed

Locator: getByTestId('customer-name-input')
Expected: visible
Timeout: 10000ms
Error: element(s) not found

Call log:
  - Expect "toBeVisible" with timeout 10000ms
  - waiting for getByTestId('customer-name-input')

```

```yaml
- link "Pular para o conteúdo":
  - /url: "#main-content"
- banner:
  - img "Logo do Sistema"
  - navigation:
    - link "Salão":
      - /url: /
    - link "Modo Garçom":
      - /url: /garcom
    - link "Delivery":
      - /url: /delivery
    - link "Preparo":
      - /url: /preparo
    - link "🍔 iFood":
      - /url: /integracoes/ifood
    - link "Config.":
      - /url: /configuracoes
  - text: Filial 1
  - button "Caixa"
  - button "Ativar tema claro": ☀
  - button "Sair"
- main:
  - heading "Mesas" [level=2]
  - text: toque numa mesa livre para abrir um pedido
  - button "+ Retirada / Delivery"
  - button "Gerar QR de autoatendimento"
  - button "1 Livre 4 lugares"
  - button "2 Livre 4 lugares"
  - button "3 Livre 4 lugares"
  - button "4 Livre 4 lugares"
  - button "5 Livre 4 lugares"
  - heading "Comandas" [level=2]
  - text: toque numa comanda livre para abrir uma conta individual limite R$ 500,00
  - textbox "nº…"
  - button "100"
```

# Test source

```ts
  1   | ﻿import { test, expect } from '@playwright/test';
  2   | 
  3   | test.describe('OpenComandaDialog', () => {
  4   | 
  5   |     test.beforeEach(async ({ page }) => {
  6   |         // 1. Simula Login
  7   |         await page.route('*/**/api/auth/login', async (route) => {
  8   |             if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
  9   |             await route.fulfill({ status: 200, json: { accessToken: 'token', user: { id: 1, name: 'Admin', companyId: 1, branchId: 1 } } });
  10  |         });
  11  | 
  12  |         // 2. Simula o FeatureGate
  13  |         await page.route('*/**/api/access/my-features', async (route) => {
  14  |             if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
  15  |             await route.fulfill({ status: 200, json: { canManageAccess: true, features: [] } });
  16  |         });
  17  | 
  18  |         // 3. Login real na UI
  19  |         await page.goto('/login');
  20  |         await page.getByTestId('username').fill('admin');
  21  |         await page.getByTestId('password').fill('123');
  22  |         await page.getByTestId('submit-login').click();
  23  |         await page.waitForURL('**/');
  24  | 
  25  |         // 4. Mocks EXATOS baseados no seu api.ts
  26  |         // Rota de listagem de comandas
  27  |         await page.route('*/**/api/comandas/branch/*', async (route) => {
  28  |             if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
  29  |             await route.fulfill({
  30  |                 status: 200,
  31  |                 json: [
  32  |                     { id: 1, code: '100', status: 'Livre', statusId: 1 }
  33  |                 ]
  34  |             });
  35  |         });
  36  | 
  37  |         // Rota de configurações de comanda (se não mockar essa, a tela pode ficar travada no loading)
  38  |         await page.route('*/**/api/comandas/settings/branch/*', async (route) => {
  39  |             if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
  40  |             await route.fulfill({ status: 200, json: { branchId: 1, defaultLimitAmount: 500 } });
  41  |         });
  42  | 
  43  |         // Rota de listagem de pedidos (para a tela inicial não quebrar)
  44  |         await page.route('*/**/api/orders/branch/*', async (route) => {
  45  |             if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
  46  |             await route.fulfill({ status: 200, json: [] });
  47  |         });
  48  | 
  49  |         // Acessa a página inicial de Pedidos/Comandas
  50  |         await page.goto('/');
  51  |     });
  52  | 
  53  |     const openDialogInApp = async (page) => {
  54  |         // Tenta achar a comanda livre pelo texto "100" injetado pelo mock
  55  |         const fallbackCard = page.getByText('100').first();
  56  |         await expect(fallbackCard).toBeVisible({ timeout: 15000 });
  57  |         await fallbackCard.click();
  58  |     };
  59  | 
  60  |     test('deve abrir a comanda com sucesso e exibir SweetAlert', async ({ page }) => {
  61  |         // Mock da rota POST que abre o pedido
  62  |         await page.route('*/**/api/orders', async (route) => {
  63  |             if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
  64  |             if (route.request().method() === 'POST') {
  65  |                 await route.fulfill({ status: 200, json: 999 });
  66  |             } else {
  67  |                 await route.continue();
  68  |             }
  69  |         });
  70  | 
  71  |         await openDialogInApp(page);
  72  | 
  73  |         const inputName = page.getByTestId('customer-name-input');
  74  |         await expect(inputName).toBeVisible({ timeout: 10000 });
  75  | 
  76  |         await inputName.fill('João Silva');
  77  |         await page.getByTestId('submit-comanda-btn').click();
  78  | 
  79  |         const swalPopup = page.locator('.swal2-popup');
  80  |         await expect(swalPopup).toBeVisible({ timeout: 10000 });
  81  |         await expect(swalPopup.locator('.swal2-title')).toContainText('Sucesso!');
  82  |     });
  83  | 
  84  |     test('deve exibir erro do SweetAlert caso a abertura falhe na API', async ({ page }) => {
  85  |         // Mock da rota POST simulando erro do backend
  86  |         await page.route('*/**/api/orders', async (route) => {
  87  |             if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 200, headers: { 'Access-Control-Allow-Origin': '*' } });
  88  |             if (route.request().method() === 'POST') {
  89  |                 await route.fulfill({ status: 400, json: { message: "Esta comanda já está em uso." } });
  90  |             } else {
  91  |                 await route.continue();
  92  |             }
  93  |         });
  94  | 
  95  |         await openDialogInApp(page);
  96  | 
  97  |         const inputName = page.getByTestId('customer-name-input');
> 98  |         await expect(inputName).toBeVisible({ timeout: 10000 });
      |                                 ^ Error: expect(locator).toBeVisible() failed
  99  | 
  100 |         await inputName.fill('Maria');
  101 |         await page.getByTestId('submit-comanda-btn').click();
  102 | 
  103 |         const swalPopup = page.locator('.swal2-popup');
  104 |         await expect(swalPopup).toBeVisible({ timeout: 10000 });
  105 |         await expect(swalPopup.locator('.swal2-title')).toContainText('Erro');
  106 |         await expect(swalPopup.locator('.swal2-html-container')).toContainText('Esta comanda já está em uso.');
  107 |     });
  108 | });
```