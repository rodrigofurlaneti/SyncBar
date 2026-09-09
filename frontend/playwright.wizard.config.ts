import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
    testDir: './test/features/orders',
    testMatch: 'ProductCustomization.spec.ts',
    workers: 3,
    use: { baseURL: 'http://localhost:5173' },
    projects: [
        { name: 'desktop', use: { ...devices['Desktop Chrome'] } },
        { name: 'tablet', use: { ...devices['Desktop Chrome'], viewport: { width: 820, height: 1180 } } },
        { name: 'android-small', use: { ...devices['Pixel 7'], viewport: { width: 360, height: 640 } } },
        { name: 'iphone', use: { ...devices['iPhone 13'] } },
    ],
    webServer: { command: 'node node_modules/vite/bin/vite.js --host 127.0.0.1', url: 'http://localhost:5173', reuseExistingServer: true },
});
