import { test, expect } from '../../src/remote/node_modules/@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
const metadata = process.env.PC_CONTROLLER_MOUSE_FIXTURE;
test.skip(!metadata, 'Run scripts/portable.py browser with the companion closed.');
test.use({ hasTouch: true, viewport: { width: 390, height: 700 } });
// Synthetic devicemotion events dispatched by the page stand in for a phone's gyroscope. They prove the
// clutch, gating, filtering, release paths and the recorded wire output over real HTTPS/WSS; they do not
// prove iOS/Android sensor axes, permission dialogs or hand feel (see AIR_POINTER_PLAN.md, Phase 4).
test('air pointer: permission states, hold-to-point, taps, release paths and drag over real WSS', async ({ page, context }) => {
  test.setTimeout(90000);
  const fixture = JSON.parse(fs.readFileSync(metadata!, 'utf8'));
  const eventsPath = path.join(path.dirname(metadata!), 'events.json');
  const events = (): string[] => JSON.parse(fs.readFileSync(eventsPath, 'utf8'));
  const moves = () => events().filter(e => e.startsWith('move:')).map(e => { const [x, y] = e.slice(5).split(',').map(Number); return { x, y }; });
  const clicks = () => events().filter(e => e === 'click:left:False').length;
  const releases = () => events().filter(e => e === 'release').length;
  const errors: string[] = [];
  page.on('pageerror', error => errors.push(error.message));
  page.on('console', message => { if (message.type() === 'error' || message.type() === 'warning') errors.push(message.text()); });
  await page.goto('/#pair=' + fixture.challenge);
  await expect(page.locator('#state')).toHaveText('Secure transport · unpaired');
  await page.getByRole('button', { name: 'Request PC approval' }).click();
  await expect(page.locator('#state')).toHaveText('Connected securely', { timeout: 10000 });
  // The companion sends no Permissions-Policy today; one added later must keep motion sensors for self.
  const policy = (await page.request.get('/')).headers()['permissions-policy'];
  if (policy) { expect(policy).toMatch(/gyroscope=\(?self/); expect(policy).toMatch(/accelerometer=\(?self/); }
  await expect(page.locator('#mode-touch')).toHaveAttribute('aria-checked', 'true');
  await expect(page.locator('#air-pad')).toBeHidden();
  await expect(page.locator('#air-settings')).toBeHidden();
  // Headless Chromium without the stub: no samples ever arrive. Chromium exposes requestPermission (answering
  // "prompt" without a dialog), so Enable motion may appear first; either way the pad ends in the no-sensor state.
  await page.locator('#mode-air').click();
  await expect(page.locator('#air-pad')).toBeVisible();
  await expect(page.locator('#touchpad')).toBeHidden();
  await expect(page.locator('#air-settings')).toBeVisible();
  await expect(page.locator('#air-pad')).toHaveAttribute('data-state', /needs-permission|none/, { timeout: 3000 });
  if (await page.locator('#air-enable').isVisible()) await page.locator('#air-enable').click();
  await expect(page.locator('#air-status')).toHaveText('No motion sensor on this device.', { timeout: 3000 });
  await expect(page.locator('#air-pad')).toHaveAttribute('data-state', 'none');
  await expect(page.locator('#air-use-touch')).toBeVisible();
  await page.screenshot({ path: path.join(os.tmpdir(), 'pc-air-none-390x700.png') });
  await page.locator('#air-use-touch').click();
  await expect(page.locator('#touchpad')).toBeVisible();
  await expect(page.locator('#mode-touch')).toHaveAttribute('aria-checked', 'true');
  // iOS-style permission stub plus a synthetic sensor driven from the test.
  await page.addInitScript(() => {
    const w = window as unknown as Record<string, any>;
    w.__motionPermission = { calls: 0, active: [] as boolean[], result: 'granted' };
    (DeviceMotionEvent as unknown as Record<string, unknown>).requestPermission = () => {
      w.__motionPermission.calls++; w.__motionPermission.active.push(navigator.userActivation.isActive);
      return Promise.resolve(w.__motionPermission.result);
    };
    w.__air = { rate: { alpha: 0, beta: 0, gamma: 0 }, gravity: { x: 0, y: 0, z: 9.81 }, timer: 0 };
    w.__airStart = (interval = 16) => { clearInterval(w.__air.timer); w.__air.timer = setInterval(() => {
      window.dispatchEvent(new DeviceMotionEvent('devicemotion', { rotationRate: { ...w.__air.rate }, accelerationIncludingGravity: { ...w.__air.gravity }, interval }));
    }, interval); };
    w.__airStop = () => { clearInterval(w.__air.timer); w.__air.timer = 0; };
    // rotationRate per the specification: alpha about x (tilt), beta about y (roll), gamma about z (turn).
    w.__airRate = (rate: Record<string, number>) => { w.__air.rate = { alpha: 0, beta: 0, gamma: 0, ...rate }; };
  });
  const rate = (value: Record<string, number>) => page.evaluate(r => (window as unknown as Record<string, any>).__airRate(r), value);
  const inject = (interval = 16) => page.evaluate(i => (window as unknown as Record<string, any>).__airStart(i), interval);
  const stopInjecting = () => page.evaluate(() => (window as unknown as Record<string, any>).__airStop());
  await page.reload();
  await expect(page.locator('#state')).toHaveText('Connected securely');
  await page.locator('#mode-air').click();
  await expect(page.locator('#air-enable')).toBeVisible({ timeout: 3000 });
  await expect(page.locator('#air-pad')).toHaveAttribute('data-state', 'needs-permission');
  await page.screenshot({ path: path.join(os.tmpdir(), 'pc-air-permission-390x700.png') });
  await page.evaluate(() => { (window as unknown as Record<string, any>).__motionPermission.result = 'denied'; });
  await page.locator('#air-enable').click();
  await expect(page.locator('#air-pad')).toHaveAttribute('data-state', 'denied');
  await expect(page.locator('#air-status')).toContainText('declined');
  await page.screenshot({ path: path.join(os.tmpdir(), 'pc-air-denied-390x700.png') });
  await page.locator('#air-use-touch').click();
  await expect(page.locator('#touchpad')).toBeVisible();
  await page.locator('#mode-air').click();
  await expect(page.locator('#air-enable')).toBeVisible({ timeout: 3000 });
  await page.evaluate(() => { (window as unknown as Record<string, any>).__motionPermission.result = 'granted'; });
  await page.locator('#air-enable').click();
  // iOS only grants inside a user gesture; the stub records the activation state at call time.
  expect(await page.evaluate(() => (window as unknown as Record<string, any>).__motionPermission)).toMatchObject({ calls: 2, active: [true, true] });
  await inject();
  await expect(page.locator('#air-pad')).toHaveAttribute('data-state', 'ready');
  await expect(page.locator('#air-hint')).toHaveText('Hold to point · tap to click');
  expect(moves().length).toBe(0);
  await page.screenshot({ path: path.join(os.tmpdir(), 'pc-air-ready-390x700.png') });
  const session = await context.newCDPSession(page);
  const touch = async (type: string, points: { id: number; x: number; y: number }[]) => {
    await session.send('Input.dispatchTouchEvent', { type, touchPoints: points });
    await page.evaluate(() => new Promise<void>(resolve => requestAnimationFrame(() => resolve())));
  };
  const center = async () => { await page.locator('#air-pad').scrollIntoViewIfNeeded(); const box = (await page.locator('#air-pad').boundingBox())!; return { x: Math.round(box.x + box.width / 2), y: Math.round(box.y + box.height / 2) }; };
  const settle = () => page.waitForTimeout(450); // > the 100 ms settle window, several frames, fixture snapshots every 50 ms
  const quiet = async () => { await page.waitForTimeout(250); return moves().length; }; // let trailing frames reach events.json
  // Turning left while holding moves the cursor left and never vertically.
  let before = moves().length;
  await touch('touchStart', [{ id: 1, ...(await center()) }]);
  await expect(page.locator('#air-pad')).toHaveClass(/holding/);
  await expect(page.locator('#air-status')).toHaveText('Pointing');
  await rate({ gamma: 60 });
  await page.waitForTimeout(400);
  await page.screenshot({ path: path.join(os.tmpdir(), 'pc-air-holding-390x700.png') });
  await rate({});
  await touch('touchEnd', []);
  await expect(page.locator('#air-pad')).not.toHaveClass(/holding/);
  await expect.poll(() => moves().length).toBeGreaterThan(before + 5);
  let sent = moves().slice(before);
  expect(sent.every(m => m.x < 0 && m.y === 0)).toBe(true);
  expect(clicks()).toBe(0);
  // Motion without a hold sends nothing.
  before = await quiet();
  await rate({ gamma: 90, alpha: 90 }); await settle(); await rate({});
  expect(moves().length).toBe(before);
  // A tap clicks once and adds no movement.
  before = moves().length;
  await touch('touchStart', [{ id: 1, ...(await center()) }]);
  await touch('touchEnd', []);
  await expect.poll(clicks).toBe(1);
  expect(moves().length).toBe(before);
  // Aiming up moves up; Invert vertical flips it and the setting persists on the phone.
  before = moves().length;
  await touch('touchStart', [{ id: 1, ...(await center()) }]);
  await rate({ alpha: 60 }); await settle(); await rate({});
  await touch('touchEnd', []);
  await expect.poll(() => moves().length).toBeGreaterThan(before + 5);
  sent = moves().slice(before);
  expect(sent.every(m => m.y < 0 && m.x === 0)).toBe(true);
  await page.locator('#air-settings').click();
  await expect(page.locator('#air-dialog')).toBeVisible();
  await page.locator('#air-invert-y').check();
  await page.locator('#air-speed').fill('2'); await page.locator('#air-speed').dispatchEvent('input');
  await expect(page.locator('#air-speed-value')).toHaveText('2.0×');
  await page.screenshot({ path: path.join(os.tmpdir(), 'pc-air-settings-390x700.png') });
  await page.locator('#close-air').click();
  await expect(page.locator('#air-dialog')).toBeHidden();
  before = moves().length;
  await touch('touchStart', [{ id: 1, ...(await center()) }]);
  await rate({ alpha: 60 }); await settle(); await rate({});
  await touch('touchEnd', []);
  await expect.poll(() => moves().length).toBeGreaterThan(before + 5);
  expect(moves().slice(before).every(m => m.y > 0)).toBe(true);
  expect(await page.evaluate(() => localStorage.getItem('pc-remote.air'))).toBe('{"speed":2,"invertX":false,"invertY":true}');
  await page.reload();
  await expect(page.locator('#state')).toHaveText('Connected securely');
  await page.locator('#mode-air').click();
  await page.locator('#air-enable').click();
  await inject();
  await expect(page.locator('#air-pad')).toHaveAttribute('data-state', 'ready');
  await page.locator('#air-settings').click();
  await expect(page.locator('#air-speed-value')).toHaveText('2.0×');
  await expect(page.locator('#air-invert-y')).toBeChecked();
  await page.locator('#air-invert-y').uncheck();
  await page.locator('#close-air').click();
  // A sensor stall pauses pointing; resumed samples move nothing until a new hold.
  await touch('touchStart', [{ id: 1, ...(await center()) }]);
  await expect(page.locator('#air-pad')).toHaveClass(/holding/);
  await stopInjecting();
  await expect(page.locator('#air-pad')).not.toHaveClass(/holding/, { timeout: 2000 });
  await expect(page.locator('#air-hint')).toHaveText('Motion paused · hold again');
  before = await quiet();
  await rate({ gamma: 60 }); await inject(); await settle();
  expect(moves().length).toBe(before);
  await touch('touchEnd', []);
  await rate({});
  await page.waitForTimeout(150);
  expect(clicks()).toBe(1);
  // A tab switch mid-hold stops pointing, releases input and never re-engages by itself.
  await touch('touchStart', [{ id: 1, ...(await center()) }]);
  await expect(page.locator('#air-pad')).toHaveClass(/holding/);
  let released = releases();
  await page.evaluate(() => document.getElementById('tab-keyboard')!.click());
  await expect(page.locator('#keyboard-controls')).toBeVisible();
  await expect.poll(releases).toBeGreaterThan(released);
  before = await quiet();
  await rate({ gamma: 60 }); await settle();
  expect(moves().length).toBe(before);
  await touch('touchEnd', []);
  await page.evaluate(() => document.getElementById('tab-touchpad')!.click());
  await expect(page.locator('#air-pad')).toBeVisible();
  await expect(page.locator('#air-pad')).toHaveAttribute('data-state', 'ready');
  await expect(page.locator('#air-pad')).not.toHaveClass(/holding/);
  await settle();
  expect(moves().length).toBe(before);
  await rate({});
  // Connection settings mid-hold.
  released = releases();
  await touch('touchStart', [{ id: 1, ...(await center()) }]);
  await expect(page.locator('#air-pad')).toHaveClass(/holding/);
  await page.evaluate(() => document.getElementById('open-settings')!.click());
  await expect(page.getByRole('dialog', { name: 'Connection settings' })).toBeVisible();
  await expect(page.locator('#air-pad')).not.toHaveClass(/holding/);
  await expect.poll(releases).toBeGreaterThan(released);
  await touch('touchEnd', []);
  await page.getByRole('button', { name: 'Close connection settings', exact: true }).click();
  await expect(page.getByRole('dialog', { name: 'Connection settings' })).toBeHidden();
  // Hidden page mid-hold: suspended, sensors released; reconnecting restores Ready without pointing.
  await touch('touchStart', [{ id: 1, ...(await center()) }]);
  await expect(page.locator('#air-pad')).toHaveClass(/holding/);
  await page.evaluate(() => { Object.defineProperty(document, 'hidden', { configurable: true, get: () => true }); document.dispatchEvent(new Event('visibilitychange')); });
  await expect(page.locator('#state')).toHaveText('Suspended');
  await expect(page.locator('#air-pad')).not.toHaveClass(/holding/);
  await expect(page.locator('#air-pad')).toHaveAttribute('data-state', 'idle');
  await touch('touchEnd', []);
  await page.evaluate(() => { delete (document as unknown as Record<string, unknown>).hidden; document.dispatchEvent(new Event('visibilitychange')); });
  await expect(page.locator('#state')).toHaveText('Connected securely');
  await expect(page.locator('#air-pad')).toHaveAttribute('data-state', 'ready');
  before = await quiet();
  await rate({ gamma: 60 }); await settle(); await rate({});
  expect(moves().length).toBe(before);
  // Drag: Start drag, hold and turn, End drag keeps the button order around the motion.
  await page.locator('#drag').click();
  await expect(page.locator('#drag')).toHaveAttribute('aria-pressed', 'true');
  await expect(page.locator('#air-pad')).toHaveClass(/dragging/);
  await expect(page.locator('#air-hint')).toHaveText('Dragging · hold to move, then End drag');
  await expect.poll(() => events().includes('left:True')).toBe(true);
  await touch('touchStart', [{ id: 1, ...(await center()) }]);
  await rate({ gamma: -40 }); await settle(); await rate({});
  await touch('touchEnd', []);
  await page.locator('#drag').click();
  await expect.poll(() => events().includes('left:False')).toBe(true);
  const ordered = events();
  const down = ordered.indexOf('left:True'), up = ordered.indexOf('left:False');
  expect(up).toBeGreaterThan(down);
  expect(ordered.slice(down + 1, up).some(e => e.startsWith('move:') && Number(e.slice(5).split(',')[0]) > 0)).toBe(true);
  await expect(page.locator('#air-hint')).toHaveText('Hold to point · tap to click');
  // Sustained fast samples stay at one aggregate per frame, well under the PC's 180 messages/second.
  await rate({ gamma: 30, alpha: 10 }); await inject(4);
  await touch('touchStart', [{ id: 1, ...(await center()) }]);
  await page.waitForTimeout(200);
  before = moves().length; const started = Date.now();
  await page.waitForTimeout(1000);
  const perSecond = (moves().length - before) / ((Date.now() - started) / 1000);
  await touch('touchEnd', []);
  await rate({}); await inject(16);
  expect(perSecond).toBeGreaterThan(20); expect(perSecond).toBeLessThanOrEqual(130);
  // Keyboard access to the mode switch, target sizes and layouts while Ready.
  await page.locator('#mode-air').focus(); await page.keyboard.press('ArrowRight');
  await expect(page.locator('#mode-touch')).toHaveAttribute('aria-checked', 'true');
  await expect(page.locator('#touchpad')).toBeVisible();
  await page.keyboard.press('ArrowLeft');
  await expect(page.locator('#mode-air')).toHaveAttribute('aria-checked', 'true');
  await expect(page.locator('#air-pad')).toHaveAttribute('data-state', 'ready');
  for (const viewport of [{ width: 363, height: 874 }, { width: 390, height: 700 }, { width: 844, height: 390 }, { width: 1280, height: 800 }]) {
    await page.setViewportSize(viewport);
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    const targets = await page.locator('button:visible, select:visible').evaluateAll(elements => elements.map(element => { const r = element.getBoundingClientRect(); return { width: r.width, height: r.height }; }));
    expect(targets.every(r => r.width >= 44 && r.height >= 44)).toBe(true);
    await page.screenshot({ path: path.join(os.tmpdir(), `pc-air-ready-${viewport.width}x${viewport.height}.png`), animations: 'disabled' });
  }
  await stopInjecting();
  expect(errors).toEqual([]);
  await page.goto('about:blank');
});
