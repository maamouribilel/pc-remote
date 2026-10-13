import { test, expect } from '../../src/remote/node_modules/@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
const metadata = process.env.PC_CONTROLLER_MOUSE_FIXTURE;
test.skip(!metadata, 'Run scripts/portable.py browser-capabilities');
test('older Windows v1 fallback and malformed extension fail-closed', async ({ page }) => {
  let malformed = false, unsupported = false;
  let claims = 0; page.on("request", r => { if (r.url().endsWith("/api/session/claim")) claims++; });
  const errors: string[] = []; page.on('pageerror', e => errors.push(e.message));
  await page.route('**/api/status', async route => {
    const response = await route.fetch(); const body = await response.json();
    if (unsupported) body.protocol = 2;
    if (malformed) body.capabilities = { platform: 'macos', pointer: 'invalid' };
    else { delete body.capabilities; delete body.readiness; }
    await route.fulfill({ response, json: body });
  });
  // Relay actual authenticated WSS, removing only additive fields to model an old
  // companion. Client v1 commands and backend input checks remain real.
  await page.routeWebSocket('**/control/ws', ws => {
    const server = ws.connectToServer();
    server.onMessage(message => {
      const body = JSON.parse(String(message));
      if (body.type === 'pong') {
        if (malformed) body.payload.capabilities = { platform: 'macos', pointer: 'invalid' };
        else { delete body.payload.capabilities; delete body.payload.readiness; }
      }
      ws.send(JSON.stringify(body));
    });
  });
  const fixture = JSON.parse(fs.readFileSync(metadata!, 'utf8'));
  await page.goto('/#pair=' + fixture.challenge);
  await expect(page.locator('#state')).toHaveText('Secure transport · unpaired');
  await page.getByRole('button', { name: 'Request PC approval' }).click();
  await expect(page.locator('#state')).toHaveText('Connected securely');
  await expect(page.locator('#left-click')).toBeEnabled();
  await page.locator('#tab-keyboard').click();
  await expect(page.locator('[data-modifier="Windows"]')).toContainText('Win');
  await expect(page.locator('[data-shortcut="selectAll"]')).toContainText('Ctrl + A');
  await page.locator('#remote-text').fill('Legacy fixture'); await page.locator('#send-text').click();
  await expect(page.locator('#text-feedback')).toHaveText('Sent to the PC.');
  malformed = true; await page.reload();
  await expect(page.locator('#state')).toHaveText('Connected · controls unavailable');
  await expect(page.locator('#left-click')).toBeDisabled();
  await page.locator('#tab-keyboard').click();
  await expect(page.locator('#remote-text')).toBeDisabled();
  await expect(page.locator('[data-modifier="Windows"]')).toBeDisabled();
  await expect(page.locator('[data-shortcut="selectAll"]')).toBeDisabled();
  await expect(page.locator('#release')).toBeEnabled();
  await page.screenshot({ path: path.join(os.tmpdir(), 'pc-capabilities-malformed.png') });
  const before = claims; unsupported = true; await page.reload();
  await expect(page.locator('#state')).toHaveText('Unsupported companion protocol');
  await expect(page.locator('#release')).toBeDisabled();
  expect(claims).toBe(before);
  expect(errors).toEqual([]);
});
