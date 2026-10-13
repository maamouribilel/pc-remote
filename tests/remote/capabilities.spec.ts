import { test, expect } from '../../src/remote/node_modules/@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
const metadata = process.env.PC_CONTROLLER_MOUSE_FIXTURE;
const platform = process.env.PC_CONTROLLER_TEST_PLATFORM;
test.skip(!metadata || !platform, 'Run scripts/portable.py browser-capabilities');
for (const viewport of [{ width: 390, height: 700 }, { width: 1280, height: 800 }]) {
  test(`authenticated ${platform} capability controls ${viewport.width}`, async ({ page }) => {
    test.setTimeout(30000);
    await page.setViewportSize(viewport);
    const errors: string[] = []; page.on('pageerror', e => errors.push(e.message));
    page.on('console', m => { if (['error', 'warning'].includes(m.type())) errors.push(m.text()); });
    // Each fixture's pairing challenge is single-use; derive a fresh challenge for the
    // second viewport by using the first browser's remembered cookies instead.
    if (viewport.width === 1280) {
      const state = JSON.parse(fs.readFileSync(path.join(path.dirname(metadata!), 'browser-state.json'), 'utf8'));
      await page.context().addCookies(state.cookies);
      await page.goto('/');
    } else {
      const fixture = JSON.parse(fs.readFileSync(metadata!, 'utf8'));
      await page.goto('/#pair=' + fixture.challenge);
      await expect(page.locator('#state')).toHaveText('Secure transport · unpaired');
      await page.getByRole('button', { name: 'Request PC approval' }).click();
    }
    await expect(page.locator('#state')).toHaveText(platform === 'unavailable' ? 'Connected · controls unavailable' : 'Connected securely');
    await expect(page).toHaveTitle('PC Remote · secure pairing');
    expect(new URL(page.url()).pathname).toBe('/');
    await expect(page.locator('h1')).toHaveText('PC Remote');
    await expect(page.locator('vite-error-overlay')).toHaveCount(0);
    if (platform === 'macos') await expect(page.locator('#left-click')).toBeEnabled();
    else await expect(page.locator('#left-click')).toBeDisabled();
    await page.locator('#tab-keyboard').click();
    await expect(page.locator('[data-modifier="Windows"]')).toHaveText(platform === 'linux' ? 'Super' : 'Command');
    await expect(page.locator('[data-modifier="Alt"]')).toHaveText(platform === 'linux' ? 'Alt' : 'Option');
    await expect(page.locator('[data-shortcut="selectAll"]')).toHaveText(platform === 'linux' ? 'Ctrl + A' : 'Command + A');
    if (platform === 'unavailable') await expect(page.locator('[data-key="Enter"]').first()).toBeDisabled();
    else await expect(page.locator('[data-key="Enter"]').first()).toBeEnabled();
    if (platform === 'macos') {
      await page.locator('#remote-text').fill('fixture'); await expect(page.locator('#send-text')).toBeEnabled();
      await page.locator('#send-text').click(); await expect(page.locator('#text-feedback')).toHaveText('Sent to the Mac.');
    } else await expect(page.locator('#remote-text')).toBeDisabled();
    await page.screenshot({ path: path.join(os.tmpdir(), `pc-capabilities-${platform}-${viewport.width}.png`), fullPage: true });
    await page.locator('#tab-media').click();
    await expect(page.locator('#system-volume')).toBeDisabled();
    if (platform === 'macos') {
      await expect(page.locator('#media-controls [data-media="playPause"]')).toBeEnabled();
      await expect(page.locator('#media-controls [data-media="mute"]')).toBeHidden();
      await expect(page.locator('#media-note')).toContainText('Mac');
    } else await expect(page.locator('#media-profile')).toBeDisabled();
    await page.locator('#tab-pc').click();
    await expect(page.locator('[data-power="sleep"]')).toBeDisabled();
    await expect(page.locator('#release')).toBeEnabled();
    await page.locator('#release').click(); await expect(page.locator('#state')).toContainText('Connected');
    // UI is informational: direct HTTP mutations must fail at the actual backend too.
    const response = await page.request.post('/api/pc/power/prepare', { headers: { Origin: 'https://localhost:8443' }, data: { session: 'invalid', action: 'sleep' } });
    expect(response.ok()).toBe(false);
    expect(errors).toEqual([]);
    if (viewport.width === 390) await page.context().storageState({ path: path.join(path.dirname(metadata!), 'browser-state.json') });
    // Close the live session before the next browser context uses the same device.
    await page.goto('about:blank');
  });
}
