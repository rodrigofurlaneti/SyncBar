import { test, expect } from "@playwright/test";

test("Gerencia opcionais e boosts sem recarregar a página", async ({ page }) => {
    const product = { id: 10, categoryId: 1, categoryName: "Bebidas", unitOfMeasureId: 1, name: "Água", description: null, barcode: null, salePrice: 5, costPrice: null, isStockControlled: false, preparationTimeMinutes: null, imageUrl: null, isActive: true, hasOptionalExtras: false, hasBoosts: false };
    const lists: Record<string, Record<string, unknown>[]> = { "optional-extras": [], boosts: [] };
    let nextId = 1;
    await page.route("**/api/**", async route => {
        const request = route.request();
        const path = new URL(request.url()).pathname;
        const method = request.method();
        if (method === "OPTIONS") return route.fulfill({ status: 200 });
        if (path === "/api/auth/login") return route.fulfill({ json: { accessToken: "token", user: { id: 1, name: "Admin", companyId: 1, branchId: 1 } } });
        if (path === "/api/access/my-features") return route.fulfill({ json: { canManageAccess: true, features: [] } });
        if (path.startsWith("/api/categories/company/")) return route.fulfill({ json: [{ id: 1, name: "Bebidas", displayOrder: 1, isActive: true, productCount: 1 }] });
        if (path === "/api/products/company/1/management") return route.fulfill({ json: [product] });
        if (path === "/api/products/10") return route.fulfill({ json: product });
        if (path === "/api/products/10/extras-and-boosts") {
            Object.assign(product, request.postDataJSON());
            return route.fulfill({ status: 204 });
        }
        const match = path.match(/\/api\/products\/10\/(optional-extras|boosts)(?:\/(\d+))?$/);
        if (match) {
            const kind = match[1];
            const id = Number(match[2]);
            if (method === "GET") return route.fulfill({ json: [...lists[kind]].sort((a, b) => Number(a.displayOrder) - Number(b.displayOrder)) });
            if (method === "POST") lists[kind].push({ id: nextId++, productId: 10, ...request.postDataJSON() });
            if (method === "PUT") Object.assign(lists[kind].find(item => item.id === id)!, request.postDataJSON());
            if (method === "DELETE") lists[kind] = lists[kind].filter(item => item.id !== id);
            return route.fulfill({ status: 204 });
        }
        return route.fulfill({ json: [] });
    });
    await page.goto("/login");
    await page.getByTestId("username").fill("admin");
    await page.getByTestId("password").fill("123");
    await page.getByTestId("submit-login").click();
    await page.waitForURL("**/");
    await page.goto("/produtos");
    await page.getByTestId("btn-edit-product-10").click();
    const panel = page.getByRole("region", { name: "Opcionais e adicionais do produto" });
    await panel.getByRole("switch", { name: "Possui Opcionais Gratuitos?" }).click();
    await panel.getByLabel("Nome do opcional").fill("Gelo");
    await panel.getByLabel("Ordem de exibição").fill("2");
    await panel.getByRole("button", { name: "Adicionar item" }).click();
    await expect(panel.getByRole("listitem")).toContainText("Gelo");
    await panel.getByRole("button", { name: "Editar", exact: true }).click();
    await panel.getByLabel("Nome do opcional").fill("Limão");
    await panel.getByRole("button", { name: "Salvar item" }).click();
    await expect(panel.getByRole("listitem")).toContainText("Limão");
    await panel.getByRole("switch", { name: "Possui Opcionais Gratuitos?" }).click();
    await expect(panel.getByLabel("Nome do opcional")).toHaveCount(0);
    await panel.getByRole("switch", { name: "Possui Opcionais Gratuitos?" }).click();
    await expect(panel.getByRole("listitem")).toContainText("Limão");
    await panel.getByRole("button", { name: "Excluir", exact: true }).click();
    await expect(panel.getByRole("listitem")).toHaveCount(0);
    await panel.getByRole("switch", { name: "Possui Opcionais Gratuitos?" }).click();
    await panel.getByRole("switch", { name: "Possui Adicionais Pagos?" }).click();
    await panel.getByLabel("Nome do adicional").fill("Dose extra");
    await panel.getByLabel("Valor adicional (R$)").fill("-1");
    await expect(panel.getByRole("button", { name: "Adicionar item" })).toBeDisabled();
    await panel.getByLabel("Valor adicional (R$)").fill("2,50");
    await panel.getByRole("button", { name: "Adicionar item" }).click();
    await expect(panel.getByRole("listitem")).toContainText("Dose extra");
    expect(lists.boosts[0].incrementalValue).toBe(2.5);
    await panel.getByRole("button", { name: "Editar", exact: true }).click();
    await panel.getByLabel("Valor adicional (R$)").fill("3,75");
    await panel.getByRole("button", { name: "Salvar item" }).click();
    await expect.poll(() => lists.boosts[0].incrementalValue).toBe(3.75);
    await panel.getByRole("button", { name: "Excluir", exact: true }).click();
    await expect(panel.getByRole("listitem")).toHaveCount(0);
});

