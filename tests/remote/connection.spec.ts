// Resolve Playwright from the mobile project's pinned dependency tree.
import { test, expect } from '../../src/remote/node_modules/@playwright/test';
import os from 'node:os';
import path from 'node:path';

for (const viewport of [{ width: 390, height: 700 }, { width: 844, height: 390 }, { width: 1280, height: 800 }]) {
  test(`real secure connection and layout ${viewport.width}x${viewport.height}`, async ({ page }) => {
    await page.setViewportSize(viewport);
    const errors: string[] = [];
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => { if (message.type() === 'error' || message.type() === 'warning') errors.push(message.text()); });
    await page.goto('/');
    await expect(page).toHaveTitle('PC Remote · secure pairing');
    await expect(page.getByRole('heading', { name: 'PC Remote', exact: true, includeHidden: true })).toHaveText('PC Remote');
    await expect(page.locator('#state')).toHaveText('Secure transport · unpaired');
    await expect(page.locator('#transport')).toHaveText('HTTPS + WSS');
    await expect(page.locator('#latency')).toHaveText(/^\d+ ms$/);
    const testButton = page.getByRole('button', { name: 'Test connection' });
    await expect(testButton).toBeEnabled();
    await testButton.click();
    await expect(testButton).toBeEnabled();
    await expect(page.locator('#state')).toHaveText('Secure transport · unpaired');
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    const target = await testButton.boundingBox();
    expect(target?.height).toBeGreaterThanOrEqual(44);
    await page.screenshot({ path: path.join(os.tmpdir(), `pc-controller-${viewport.width}.png`), fullPage: true });
    expect(errors).toEqual([]);
  });
}

test('network loss is visible and retry restores real connection', async ({ page, context }) => {
  await page.goto('/');
  await expect(page.locator('#state')).toHaveText('Secure transport · unpaired');
  await context.setOffline(true);
  await expect(page.locator('#state')).not.toHaveText('Secure transport · unpaired', { timeout: 12000 });
  await expect(page.getByRole('button', { name: 'Test connection' })).toBeDisabled();
  await context.setOffline(false);
  await expect(page.locator('#state')).toHaveText('Secure transport · unpaired', { timeout: 22000 });
});


test('pair form reports an expired challenge without granting control', async ({ page }) => {
  await page.goto('/#pair=' + '0'.repeat(64));
  await expect(page.locator('#state')).toHaveText('Secure transport · unpaired');
  expect(new URL(page.url()).hash).toBe('');
  await expect(page.locator('#pair-link')).toHaveValue('0'.repeat(64));
  await page.getByRole('button', { name: 'Request PC approval' }).click();
  await expect(page.locator('#error')).toContainText('pair_expired_or_invalid');
  await expect(page.locator('#pair-link')).toHaveValue('');
  await expect(page.locator('#forget')).toBeHidden();
  const status = await page.request.get('/api/status');
  expect((await status.json()).authenticated).toBe(false);
});
