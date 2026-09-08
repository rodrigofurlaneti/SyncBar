import type { Page } from '@playwright/test';

export async function adminLogin(page: Page) {
  await page.route('**/api/**', route => {
    const url = route.request().url();
    if (url.includes('/auth/login')) return route.fulfill({ json: { accessToken: 'token', user: { id: 1, name: 'Admin', companyId: 1, branchId: 1, employeeId: 1 } } });
    if (url.includes('/access/my-features')) return route.fulfill({ json: { canManageAccess: true, features: [] } });
    if (url.includes('/comandas/settings/')) return route.fulfill({ json: { defaultLimitAmount: 100 } });
    return route.fulfill({ json: [] });
  });
  await page.goto('/login');
  await page.getByTestId('username').fill('admin');
  await page.getByTestId('password').fill('123');
  await page.getByTestId('submit-login').click();
  await page.waitForURL('**/');
}

export const orderFixture = {
  id: 14, branchId: 1, employeeId: 1, diningTableId: null, comandaId: null,
  orderTypeId: 3, orderOriginId: 1, customerName: 'Cliente', orderStatusId: 2,
  subtotalAmount: 0.03, totalAmount: 0.03, partialPaidAmount: 0, discountAmount: 0, serviceFeeAmount: 0,
  openedAt: '2026-09-08T12:00:00Z', isActive: true,
  items: [{ id: 1, productId: 1, orderItemStatusId: 3, quantity: 1, unitPrice: 0.03, totalAmount: 0.03, complements: [] }],
};
