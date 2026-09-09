import { randomInt, randomUUID } from 'node:crypto';
import { expect, type Page } from '@playwright/test';

// Synthetic test data: valid check digits do not establish a real person's identity.
export function generateCpf(): string {
  const digits = Array.from({ length: 9 }, () => randomInt(10));
  for (let weight = 10; weight <= 11; weight++) {
    const remainder = digits.reduce((sum, digit, index) => sum + digit * (weight - index), 0) % 11;
    digits.push(remainder < 2 ? 0 : 11 - remainder);
  }
  const cpf = digits.join('');
  return /^(\d)\1{10}$/.test(cpf) ? generateCpf() : cpf;
}

export function newCustomer() {
  const suffix = `${Date.now()}-${randomUUID().slice(0, 8)}`;
  return {
    name: `João Furlaneti Teste ${suffix}`,
    cpf: generateCpf(),
    email: `joao.furlaneti.teste.${suffix}@example.com`,
    password: `E2e!${randomUUID()}`,
    zipCode: '01001000',
    number: '100',
  };
}

export async function fillNewCustomer(page: Page, customer: ReturnType<typeof newCustomer>) {
  const auth = page.getByTestId('storefront-auth-modal');
  await auth.getByRole('tab', { name: 'Novo Cliente' }).click();
  await auth.getByLabel('Nome Completo').fill(customer.name);
  await auth.getByLabel('CPF', { exact: true }).fill(customer.cpf);
  await auth.getByLabel('E-mail', { exact: true }).fill(customer.email);
  await auth.getByLabel('Senha', { exact: true }).fill(customer.password);
  await auth.getByLabel('Confirmar Senha', { exact: true }).fill(customer.password);
  await auth.getByLabel('CEP', { exact: true }).fill(customer.zipCode);
  await expect(auth.getByLabel('Rua / Avenida')).not.toHaveValue('');
  await auth.getByLabel('Número', { exact: true }).fill(customer.number);
}
