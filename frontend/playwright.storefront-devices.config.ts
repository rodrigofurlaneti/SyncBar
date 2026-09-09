import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './test/features/storeFront',
  testMatch: ['CheckoutFlow.spec.ts', 'Registration.spec.ts', 'PaymentMethods.spec.ts'],
  workers: 3,
  retries: 0,
  use: { baseURL: 'http://localhost:5173' },
  projects: [
    { name: 'desktop', use: { ...devices['Desktop Chrome'] } },
    { name: 'android', use: { ...devices['Pixel 7'] } },
    { name: 'iphone', use: { ...devices['iPhone 13'] } },
  ],
  webServer: { command: 'npm run dev', url: 'http://localhost:5173', reuseExistingServer: true },
});
