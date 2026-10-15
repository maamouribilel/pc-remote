// Page-less unit tests for the Air pointer math. No server, browser or fixture is involved; these
// prove the pipeline, not phone sensors (see AIR_POINTER_PLAN.md for the physical gates).
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { test, expect } from '../../src/remote/node_modules/@playwright/test';
import { AirPipeline, GravityEstimator, OneEuro, accel, clampDt, clampOutput, constants, detectGravitySign, gain, isTap, normalizeSettings,
  project, quantize, resolveGravitySign, sanitize, screenAxes, softDeadzone, type AirSettings, type GravitySign, type Vec3 } from '../../src/remote/src/air-math';
// Specification gravity (+9.8 face-up) and the WebKit/iOS reading (gravity toward the ground).
const flat: Vec3 = { x: 0, y: 0, z: 9.81 }, upright: Vec3 = { x: 0, y: 9.81, z: 0 };
const flatIos: Vec3 = { x: 0, y: 0, z: -9.81 }, uprightIos: Vec3 = { x: 0, y: -9.81, z: 0 };
const settings: AirSettings = { speed: 1, invertX: false, invertY: false };
// rotationRate: alpha about x (tilt), beta about y (roll), gamma about z (turn).
function run(rate: { alpha?: number; beta?: number; gamma?: number }, gravity: Vec3 | null, options: { angle?: number; settings?: AirSettings; samples?: number; warm?: number; gravitySign?: GravitySign } = {}) {
  const pipeline = new AirPipeline(options.gravitySign);
  let t = 1000;
  for (let i = 0; i < (options.warm ?? 20); i++, t += 16) pipeline.feed({ rotationRate: { alpha: 0, beta: 0, gamma: 0 }, gravity, timeStamp: t }, t, settings, options.angle ?? 0, false);
  pipeline.engage(t, options.angle ?? 0);
  let dx = 0, dy = 0, emitted = 0;
  for (let i = 0; i < (options.samples ?? 40); i++, t += 16) {
    const out = pipeline.feed({ rotationRate: { alpha: rate.alpha ?? 0, beta: rate.beta ?? 0, gamma: rate.gamma ?? 0 }, gravity, timeStamp: t }, t, options.settings ?? settings, options.angle ?? 0, true);
    if (out) { dx += out.dx; dy += out.dy; emitted++; }
  }
  return { dx, dy, emitted, pipeline };
}
test('flat remote grip: turning left (gamma, about z) moves left, tilting the top edge up (alpha, about x) moves up', () => {
  const left = run({ gamma: 60 }, flat); expect(left.dx).toBeLessThan(-100); expect(Math.abs(left.dy)).toBeLessThan(1);
  const up = run({ alpha: 60 }, flat); expect(up.dy).toBeLessThan(-100); expect(Math.abs(up.dx)).toBeLessThan(1);
  const right = run({ gamma: -60 }, flat); expect(right.dx).toBeGreaterThan(100);
  const roll = run({ beta: 60 }, flat); expect(Math.abs(roll.dx)).toBeLessThan(1); expect(Math.abs(roll.dy)).toBeLessThan(1);
});
test('upright grip gives the same screen directions (turning is then beta, about y)', () => {
  const left = run({ beta: 60 }, upright); expect(left.dx).toBeLessThan(-100); expect(Math.abs(left.dy)).toBeLessThan(1);
  const up = run({ alpha: 60 }, upright); expect(up.dy).toBeLessThan(-100); expect(Math.abs(up.dx)).toBeLessThan(1);
});
test('30° roll: a pure turn about the vertical produces no vertical motion', () => {
  const rolled: Vec3 = { x: 9.81 * Math.sin(Math.PI / 6), y: 0, z: 9.81 * Math.cos(Math.PI / 6) };
  const u = { x: rolled.x / 9.81, y: 0, z: rolled.z / 9.81 };
  const result = run({ alpha: 60 * u.x, beta: 0, gamma: 60 * u.z }, rolled);
  expect(result.dx).toBeLessThan(-100); expect(Math.abs(result.dy)).toBeLessThan(1);
});
test('landscape remaps the screen axes', () => {
  const up = run({ beta: -60 }, flat, { angle: 90 }); expect(up.dy).toBeLessThan(-100); expect(Math.abs(up.dx)).toBeLessThan(1);
  const left = run({ gamma: 60 }, flat, { angle: 90 }); expect(left.dx).toBeLessThan(-100);
  const upOther = run({ beta: 60 }, flat, { angle: 270 }); expect(upOther.dy).toBeLessThan(-100);
});
test('screen axes follow the clockwise orientation angle, including legacy negative angles', () => {
  expect(screenAxes(0)).toEqual({ right: { x: 1, y: 0, z: 0 }, up: { x: 0, y: 1, z: 0 } });
  expect(screenAxes(90)).toEqual({ right: { x: 0, y: -1, z: 0 }, up: { x: 1, y: 0, z: 0 } });
  expect(screenAxes(270)).toEqual({ right: { x: 0, y: 1, z: 0 }, up: { x: -1, y: 0, z: 0 } });
  expect(screenAxes(-90)).toEqual(screenAxes(270));
  expect(screenAxes(180).right).toEqual({ x: -1, y: 0, z: 0 });
  expect(screenAxes(Number.NaN)).toEqual(screenAxes(0));
});
test('rolled about 90°: the screen-right axis is kept or vertical motion is suppressed', () => {
  const omega = { x: 60, y: 0, z: 0 }, vertical = { x: 1, y: 0, z: 0 }, screenRight = { x: 1, y: 0, z: 0 };
  const kept = project(omega, vertical, screenRight, { x: 0, y: 0, z: 1 });
  expect(kept.right).toEqual({ x: 0, y: 0, z: 1 }); expect(kept.pitch).toBe(0);
  const none = project(omega, vertical, screenRight, null);
  expect(none.right).toBeNull(); expect(none.pitch).toBeNull();
  const degenerate = run({ alpha: 60 }, { x: 9.81, y: 0, z: 0 });
  expect(degenerate.dy).toBe(0);
});
test('missing gravity falls back to the fixed remote grip', () => {
  const left = run({ gamma: 60 }, null); expect(left.dx).toBeLessThan(-100);
  const up = run({ alpha: 60 }, null); expect(up.dy).toBeLessThan(-100);
});
test('gravity estimate ignores shaking and seeds without warm-up', () => {
  const estimator = new GravityEstimator();
  estimator.update({ x: 0, y: 0, z: 25 }, 16); expect(estimator.vector).toBeNull();
  estimator.update(flat, 16); expect(estimator.vector).toEqual(flat);
  estimator.update({ x: 20, y: 0, z: 0 }, 16); expect(estimator.vector).toEqual(flat);
  estimator.update({ x: 1, y: 0, z: 0 }, 16); expect(estimator.vector).toEqual(flat);
  estimator.update(upright, 16); expect(estimator.vector!.y).toBeGreaterThan(0); expect(estimator.vector!.z).toBeLessThan(9.81);
});
test('gravity sign is seeded per platform and confirmed from the grip', () => {
  const portrait = screenAxes(0);
  expect(resolveGravitySign(flat, portrait)).toBe(1);
  expect(resolveGravitySign(flatIos, portrait)).toBe(-1);
  expect(resolveGravitySign(upright, portrait)).toBe(1);
  expect(resolveGravitySign({ x: 9.81, y: 0, z: 0 }, portrait)).toBeNull();
  expect(resolveGravitySign(null, portrait)).toBeNull();
  // WebKit's ground-pointing gravity gives the same screen directions, with or without the seeded sign.
  for (const gravitySign of [1, -1] as GravitySign[]) {
    const left = run({ gamma: 60 }, flatIos, { gravitySign }); expect(left.dx).toBeLessThan(-100); expect(Math.abs(left.dy)).toBeLessThan(1);
    const up = run({ alpha: 60 }, flatIos, { gravitySign }); expect(up.dy).toBeLessThan(-100); expect(Math.abs(up.dx)).toBeLessThan(1);
    const uprightLeft = run({ beta: 60 }, uprightIos, { gravitySign }); expect(uprightLeft.dx).toBeLessThan(-100); expect(Math.abs(uprightLeft.dy)).toBeLessThan(1);
    const uprightUp = run({ alpha: 60 }, uprightIos, { gravitySign }); expect(uprightUp.dy).toBeLessThan(-100); expect(Math.abs(uprightUp.dx)).toBeLessThan(1);
  }
  const iphone = 'Mozilla/5.0 (iPhone; CPU iPhone OS 18_7 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.7 Mobile/15E148 Safari/604.1';
  expect(detectGravitySign(iphone, 'iPhone', 5)).toBe(-1);
  expect(detectGravitySign(iphone.replace('Safari/604.1', 'Safari/604.1 Brave'), 'iPhone', 5)).toBe(-1);
  expect(detectGravitySign('Mozilla/5.0 (iPad; CPU OS 18_7 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.7 Mobile/15E148 Safari/604.1', 'iPad', 5)).toBe(-1);
  expect(detectGravitySign('Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.7 Safari/605.1.15', 'MacIntel', 5)).toBe(-1);
  expect(detectGravitySign('Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.7 Safari/605.1.15', 'MacIntel', 0)).toBe(1);
  expect(detectGravitySign('Mozilla/5.0 (Linux; Android 15; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Mobile Safari/537.36', 'Linux armv81', 5)).toBe(1);
  expect(detectGravitySign('', '', 0)).toBe(1);
});
test('first sample after engaging is dropped and the settle window emits nothing', () => {
  const pipeline = new AirPipeline();
  pipeline.engage(0, 0);
  expect(pipeline.feed({ rotationRate: { alpha: 0, beta: 0, gamma: 60 }, gravity: flat, timeStamp: 500 }, 500, settings, 0, true)).toBeNull();
  const second = pipeline.feed({ rotationRate: { alpha: 0, beta: 0, gamma: 60 }, gravity: flat, timeStamp: 516 }, 516, settings, 0, true);
  expect(second).not.toBeNull(); expect(second!.dx).toBeLessThan(0);
  const settled = new AirPipeline(); settled.engage(0, 0);
  for (let t = 0; t < 96; t += 16) expect(settled.feed({ rotationRate: { alpha: 0, beta: 0, gamma: 60 }, gravity: flat, timeStamp: t }, t, settings, 0, true)).toBeNull();
  expect(settled.rotation).toBeGreaterThan(0);
  expect(settled.feed({ rotationRate: { alpha: 0, beta: 0, gamma: 60 }, gravity: flat, timeStamp: 112 }, 112, settings, 0, true)).not.toBeNull();
});
test('dt clamps and falls back to the interval, which iOS reports in seconds', () => {
  expect(clampDt(1)).toBe(constants.minDt); expect(clampDt(100)).toBe(constants.maxDt); expect(clampDt(16)).toBe(16);
  expect(clampDt(null, 8)).toBe(8); expect(clampDt(Number.NaN, null)).toBe(16); expect(clampDt(-5, 0)).toBe(16);
  expect(clampDt(null, 0.01666667)).toBeCloseTo(16.67, 2); expect(clampDt(null, 0.5)).toBe(constants.maxDt);
});
type Window = { t: number; a: number; b: number; g: number; x: number; y: number; z: number }[];
function replay(window: Window, gravitySign: GravitySign = -1) {
  const pipeline = new AirPipeline(gravitySign);
  const sample = (w: Window[number]) => ({ rotationRate: { alpha: w.a, beta: w.b, gamma: w.g }, gravity: { x: w.x, y: w.y, z: w.z }, timeStamp: w.t });
  for (let i = 0; i < 10; i++) pipeline.feed(sample(window[i]), window[i].t, settings, 0, false);
  pipeline.engage(window[10].t, 0);
  let dx = 0, dy = 0;
  for (let i = 10; i < window.length; i++) { const out = pipeline.feed(sample(window[i]), window[i].t, settings, 0, true); if (out) { dx += out.dx; dy += out.dy; } }
  return { dx, dy };
}
test('recorded iPhone motions move the expected way (seeded or resolved gravity sign) and rest stays still', () => {
  const fixture = JSON.parse(readFileSync(join(__dirname, 'fixtures', 'ios-flat-motions.json'), 'utf8')) as { windows: Record<string, Window> };
  for (const gravitySign of [-1, 1] as GravitySign[]) {
    const left = replay(fixture.windows.turnLeft, gravitySign); expect(left.dx).toBeLessThan(-50); expect(Math.abs(left.dx)).toBeGreaterThan(3 * Math.abs(left.dy));
    const right = replay(fixture.windows.turnRight, gravitySign); expect(right.dx).toBeGreaterThan(50); expect(Math.abs(right.dx)).toBeGreaterThan(3 * Math.abs(right.dy));
    const up = replay(fixture.windows.tiltUp, gravitySign); expect(up.dy).toBeLessThan(-50); expect(Math.abs(up.dy)).toBeGreaterThan(3 * Math.abs(up.dx));
    const down = replay(fixture.windows.tiltDown, gravitySign); expect(down.dy).toBeGreaterThan(50); expect(Math.abs(down.dy)).toBeGreaterThan(3 * Math.abs(down.dx));
    const still = replay(fixture.windows.still, gravitySign); expect(Math.hypot(still.dx, still.dy)).toBeLessThan(2);
  }
  // The stale "alpha about z" reading that 0.4.0 used puts a tilt on the horizontal axis.
  const stale = fixture.windows.tiltUp.map(w => ({ ...w, a: w.g, b: w.a, g: w.b }));
  const wrong = replay(stale); expect(Math.abs(wrong.dx)).toBeGreaterThan(Math.abs(wrong.dy));
});
test('spikes, non-finite values and missing gyroscopes are rejected', () => {
  expect(sanitize({ rotationRate: null, gravity: flat, timeStamp: 0 })).toBeNull();
  expect(sanitize({ rotationRate: { alpha: null, beta: 0, gamma: 0 }, gravity: flat, timeStamp: 0 })).toBeNull();
  expect(sanitize({ rotationRate: { alpha: Number.NaN, beta: 0, gamma: 0 }, gravity: flat, timeStamp: 0 })).toBeNull();
  expect(sanitize({ rotationRate: { alpha: 3000, beta: 0, gamma: 0 }, gravity: flat, timeStamp: 0 })).toBeNull();
  const ok = sanitize({ rotationRate: { alpha: 1, beta: 2, gamma: 3 }, gravity: { x: 0, y: null, z: 9 }, timeStamp: 0 });
  expect(ok).toEqual({ omega: { x: 1, y: 2, z: 3 }, gravity: null });
  const pipeline = new AirPipeline(); pipeline.engage(0, 0);
  expect(pipeline.feed({ rotationRate: null, gravity: flat, timeStamp: 10 }, 10, settings, 0, true)).toBeUndefined();
});
test('soft deadzone is continuous and applied after the filter', () => {
  expect(softDeadzone(constants.deadzone)).toBe(0); expect(softDeadzone(-1)).toBe(0);
  expect(softDeadzone(constants.deadzone + 0.1)).toBeCloseTo(0.1, 6); expect(softDeadzone(-3)).toBe(-(3 - constants.deadzone));
  expect(softDeadzone(10, 2)).toBe(8);
  // Raw noise peaks above the deadzone must not leak: a zero-mean ±4 °/s jitter yields (almost) no movement.
  const pipeline = new AirPipeline(-1); let seed = 3; const noise = () => { seed = (seed * 16807) % 2147483647; return (seed / 2147483647 - 0.5) * 8; };
  pipeline.engage(0, 0); let dx = 0, dy = 0;
  for (let t = 0; t < 10000; t += 16.67) { const out = pipeline.feed({ rotationRate: { alpha: noise(), beta: noise(), gamma: noise() }, gravity: { x: 0, y: 0, z: -9.81 }, timeStamp: t }, t, settings, 0, true); if (out) { dx += out.dx; dy += out.dy; } }
  expect(Math.hypot(dx, dy)).toBeLessThan(5);
});
test('One-Euro filter settles, attenuates jitter and has bounded lag on a step', () => {
  const steady = new OneEuro();
  let value = 0; for (let i = 0; i < 120; i++) value = steady.filter(10, 1 / 60);
  expect(value).toBeCloseTo(10, 3);
  const noisy = new OneEuro(); let seed = 7; const noise = () => { seed = (seed * 16807) % 2147483647; return (seed / 2147483647 - 0.5) * 4; };
  let inputVariance = 0, outputVariance = 0;
  for (let i = 0; i < 600; i++) { const n = noise(); const f = noisy.filter(5 + n, 1 / 60); if (i >= 100) { inputVariance += n * n; outputVariance += (f - 5) ** 2; } }
  expect(outputVariance).toBeLessThan(inputVariance / 4);
  const step = new OneEuro(); step.filter(0, 1 / 60); let frames = 0, out = 0;
  while (out < 90 && frames < 120) { out = step.filter(100, 1 / 60); frames++; }
  expect(frames).toBeLessThan(60);
});
test('gain is monotonic with a mild acceleration curve', () => {
  expect(accel(10)).toBe(1); expect(accel(20)).toBe(1); expect(accel(200)).toBe(2); expect(accel(500)).toBe(2);
  expect(accel(110)).toBeCloseTo(1.5, 6);
  const px = [5, 20, 50, 100, 200, 400].map(r => gain(r, 16, 1));
  for (let i = 1; i < px.length; i++) expect(px[i]).toBeGreaterThan(px[i - 1]);
  expect(gain(-50, 16, 1)).toBeCloseTo(-gain(50, 16, 1), 9);
  expect(gain(50, 16, 2)).toBeCloseTo(2 * gain(50, 16, 1), 9);
  expect(gain(10, 16, 1)).toBeCloseTo(10 * 0.016 * 40, 9);
});
test('speed and invert settings apply and are normalized', () => {
  expect(normalizeSettings(null)).toEqual({ speed: 1, invertX: false, invertY: false });
  expect(normalizeSettings({ speed: 9, invertX: 'yes', invertY: true })).toEqual({ speed: 2.5, invertX: false, invertY: true });
  expect(normalizeSettings({ speed: 0.01 })).toEqual({ speed: 0.5, invertX: false, invertY: false });
  expect(normalizeSettings({ speed: 'fast' }).speed).toBe(1); expect(normalizeSettings({ speed: 1.26 }).speed).toBe(1.3);
  const base = run({ alpha: 60, gamma: 60 }, flat);
  const fast = run({ alpha: 60, gamma: 60 }, flat, { settings: { speed: 2, invertX: false, invertY: false } });
  expect(fast.dx).toBeCloseTo(2 * base.dx, 6); expect(fast.dy).toBeCloseTo(2 * base.dy, 6);
  const flipped = run({ alpha: 60, gamma: 60 }, flat, { settings: { speed: 1, invertX: true, invertY: true } });
  expect(flipped.dx).toBeCloseTo(-base.dx, 6); expect(flipped.dy).toBeCloseTo(-base.dy, 6);
});
test('output is rounded to two decimals and bounded to the protocol limit', () => {
  expect(quantize(0.126)).toBe(0.13); expect(quantize(-0.126)).toBe(-0.13); expect(quantize(3)).toBe(3);
  expect(clampOutput(700)).toBe(500); expect(clampOutput(-700)).toBe(-500); expect(clampOutput(12.5)).toBe(12.5);
  const fast = run({ gamma: 1900 }, flat, { samples: 20 });
  expect(fast.dx).toBe(-500 * (fast.emitted)); // every emitted frame is clamped
  expect(fast.emitted).toBeGreaterThan(0);
});
test('tap classification', () => {
  expect(isTap(100, 2, 0.5)).toBe(true);
  expect(isTap(300, 2, 0.5)).toBe(false); expect(isTap(100, 10, 0.5)).toBe(false); expect(isTap(100, 2, 2)).toBe(false);
  const held = run({ gamma: 60 }, flat, { samples: 20 });
  expect(held.pipeline.rotation).toBeGreaterThan(constants.tapRotationDeg);
  const still = run({}, flat, { samples: 20 });
  expect(still.pipeline.rotation).toBe(0); expect(still.dx).toBe(0); expect(still.dy).toBe(0);
});
