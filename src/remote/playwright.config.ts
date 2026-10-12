import { defineConfig } from '@playwright/test';
import os from 'node:os';
import path from 'node:path';

export default defineConfig({
  testDir: '../../tests/remote',
  outputDir: path.join(os.tmpdir(), 'pc-controller-playwright'),
  workers: 1,
  retries: 0,
  reporter: 'list',
  // Explicit loopback-only UI QA fallback. It cannot validate certificate onboarding.
  // Production server and remote have no insecure mode; .NET integration still verifies TLS.
  use: { baseURL: 'https://localhost:8443', channel: 'msedge', browserName: 'chromium',
    ignoreHTTPSErrors: process.env.PC_CONTROLLER_BROWSER_TLS_BYPASS === '1' },
});
