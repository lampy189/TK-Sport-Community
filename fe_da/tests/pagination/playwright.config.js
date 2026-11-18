import { fileURLToPath } from 'node:url';
import { defineConfig } from '@playwright/test';
export default defineConfig({
  testDir: '.', testMatch: '*.spec.js', reporter: 'list',
  use: { baseURL: 'http://127.0.0.1:5187', channel: 'chrome', headless: true },
  webServer: {
    command: 'npm run dev -- --host 127.0.0.1 --port 5187',
    url: 'http://127.0.0.1:5187', reuseExistingServer: false,
    cwd: fileURLToPath(new URL('../..', import.meta.url)),
  },
});
