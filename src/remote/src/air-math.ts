// Air pointer motion pipeline. Pure functions with no DOM access, so a page-less spec can test them.
// Device frame: x = right, y = top of the screen, z = out of the screen toward the user; rotation rates
// follow the right-hand rule in °/s. rotationRate.alpha is the rotation rate about x, beta about y and
// gamma about z (W3C Candidate Recommendation Draft 2025-02-12; WebKit, Chromium and Gecko all pass the
// gyroscope's x/y/z in that order; older MDN text describing alpha as "about z" is stale and misled
// version 0.4.0). accelerationIncludingGravity differs per platform: the specification and Android
// report (0, 0, +9.8) m/s² for a phone lying face-up, while WebKit on iOS reports gravity pointing toward
// the ground (face-up z ≈ -9.8, measured 2026-10-15). The "up" sign is seeded per platform
// (detectGravitySign) and confirmed per hold from the grip (resolveGravitySign). Both output axes can
// still be inverted in the Air settings.
export type Vec3 = { x: number; y: number; z: number };
export type MotionSample = {
  rotationRate: { alpha: number | null; beta: number | null; gamma: number | null } | null;
  gravity: { x: number | null; y: number | null; z: number | null } | null;
  timeStamp: number;
  interval?: number | null;
};
export type AirSettings = { speed: number; invertX: boolean; invertY: boolean };
export type GravitySign = 1 | -1;
// Every iOS browser uses WebKit's sensor implementation (gravity toward the ground); iPadOS 13+ presents a
// Mac user agent with touch points. Everything else is assumed to follow the specification (+9.8 face-up).
export function detectGravitySign(userAgent: string, platform: string, maxTouchPoints: number): GravitySign {
  const ios = /\b(iPhone|iPad|iPod)\b/.test(userAgent) || (platform === 'MacIntel' && maxTouchPoints > 1);
  return ios ? -1 : 1;
}
export const constants = {
  maxRate: 2000,                        // °/s; anything larger is a sensor spike
  minDt: 4, maxDt: 50,                  // ms between samples
  gravityTau: 150,                      // ms low-pass for the gravity vector
  gravityMin: 6, gravityMax: 14,        // m/s²; outside this band the phone is shaking, keep the estimate
  signConfidence: 4,                    // m/s² of projection needed to resolve the gravity sign
  rightAxisMin: 0.35,                   // below this (about 70° roll) keep the last screen-right axis
  deadzone: 2,                          // °/s, subtracted after filtering (measured hand-held noise peaks ±4 °/s raw)
  minCutoff: 1, beta: 0.02, dCutoff: 1, // One-Euro filter
  basePxPerDeg: 40, accelStart: 20, accelEnd: 200, accelMax: 2,
  minSpeed: 0.5, maxSpeed: 2.5,
  maxOutput: 500,                       // protocol bound per pointer.move
  settleMs: 100, tapMs: 250, tapTravelPx: 8, tapRotationDeg: 1.5, stallMs: 300,
} as const;
const finite = (value: unknown): value is number => typeof value === 'number' && Number.isFinite(value);
export const dot = (a: Vec3, b: Vec3) => a.x * b.x + a.y * b.y + a.z * b.z;
export const scale = (a: Vec3, s: number): Vec3 => ({ x: a.x * s, y: a.y * s, z: a.z * s });
export const add = (a: Vec3, b: Vec3): Vec3 => ({ x: a.x + b.x, y: a.y + b.y, z: a.z + b.z });
export const sub = (a: Vec3, b: Vec3): Vec3 => ({ x: a.x - b.x, y: a.y - b.y, z: a.z - b.z });
export const magnitude = (a: Vec3) => Math.hypot(a.x, a.y, a.z);
export const normalize = (a: Vec3): Vec3 => { const m = magnitude(a); return m > 0 ? scale(a, 1 / m) : { x: 0, y: 0, z: 0 }; };
export function normalizeSettings(raw: unknown): AirSettings {
  const r = (raw && typeof raw === 'object' ? raw : {}) as Record<string, unknown>;
  const speed = finite(r.speed) ? Math.min(constants.maxSpeed, Math.max(constants.minSpeed, Math.round(r.speed * 10) / 10)) : 1;
  return { speed, invertX: r.invertX === true, invertY: r.invertY === true };
}
// null when the sample carries no usable gyroscope reading (no sensor, non-finite values or a spike).
export function sanitize(sample: MotionSample): { omega: Vec3; gravity: Vec3 | null } | null {
  const r = sample.rotationRate;
  if (!r || !finite(r.alpha) || !finite(r.beta) || !finite(r.gamma)) return null;
  const omega = { x: r.alpha, y: r.beta, z: r.gamma };
  if (magnitude(omega) > constants.maxRate) return null;
  const g = sample.gravity;
  return { omega, gravity: g && finite(g.x) && finite(g.y) && finite(g.z) ? { x: g.x, y: g.y, z: g.z } : null };
}
export function clampDt(dt: number | null | undefined, fallback?: number | null): number {
  // iOS reports event.interval in seconds (0.0167); a sub-millisecond interval is never real, so treat it as seconds.
  const interval = finite(fallback) && fallback > 0 ? (fallback < 1 ? fallback * 1000 : fallback) : null;
  const value = finite(dt) && dt > 0 ? dt : interval ?? 16;
  return Math.min(constants.maxDt, Math.max(constants.minDt, value));
}
// Screen right/up in the device frame for a clockwise screen rotation (screen.orientation.angle).
export function screenAxes(angle: number): { right: Vec3; up: Vec3 } {
  const a = finite(angle) ? (((Math.round(angle / 90) * 90) % 360) + 360) % 360 : 0;
  const c = Math.round(Math.cos(a * Math.PI / 180)), s = Math.round(Math.sin(a * Math.PI / 180));
  return { right: { x: c || 0, y: -s || 0, z: 0 }, up: { x: s || 0, y: c || 0, z: 0 } }; // `|| 0` avoids -0
}
export class GravityEstimator {
  vector: Vec3 | null = null;
  update(gravity: Vec3 | null, dt: number) {
    if (!gravity) return;
    const m = magnitude(gravity);
    if (m < constants.gravityMin || m > constants.gravityMax) return;
    if (!this.vector) { this.vector = gravity; return; } // seed, no warm-up
    const a = dt / (constants.gravityTau + dt);
    this.vector = add(scale(this.vector, 1 - a), scale(gravity, a));
  }
  reset() { this.vector = null; }
}
// +1 when "up" lies along the measured vector (W3C), -1 when opposite. Assumes the phone is held
// screen-up or upright, never face-down. null when the grip is too ambiguous to decide.
export function resolveGravitySign(vector: Vec3 | null, axes: { up: Vec3 }): 1 | -1 | null {
  if (!vector) return null;
  const s = dot(vector, add(axes.up, { x: 0, y: 0, z: 1 }));
  return Math.abs(s) < constants.signConfidence ? null : s > 0 ? 1 : -1;
}
// yaw: positive when turning left; pitch: positive when aiming up; both in °/s.
export function project(omega: Vec3, up: Vec3 | null, screenRight: Vec3, lastRight: Vec3 | null) {
  const u = up ?? { x: 0, y: 0, z: 1 }; // fixed remote grip when gravity is unavailable
  const yaw = dot(omega, u);
  const r = sub(screenRight, scale(u, dot(screenRight, u)));
  const length = magnitude(r);
  const right = length >= constants.rightAxisMin ? scale(r, 1 / length) : lastRight;
  return { yaw, pitch: right ? dot(omega, right) : null, right };
}
export function softDeadzone(value: number, threshold: number = constants.deadzone) {
  return Math.abs(value) <= threshold ? 0 : value - Math.sign(value) * threshold;
}
// Rates start from rest when a hold begins, so the filter is seeded at zero rather than at the first sample.
export class OneEuro {
  private x = 0;
  private dx = 0;
  constructor(private readonly minCutoff: number = constants.minCutoff, private readonly beta: number = constants.beta, private readonly dCutoff: number = constants.dCutoff) {}
  reset() { this.x = 0; this.dx = 0; }
  filter(value: number, dtSeconds: number) {
    const alpha = (cutoff: number) => 1 / (1 + 1 / (2 * Math.PI * cutoff * dtSeconds));
    this.dx += alpha(this.dCutoff) * ((value - this.x) / dtSeconds - this.dx);
    this.x += alpha(this.minCutoff + this.beta * Math.abs(this.dx)) * (value - this.x);
    return this.x;
  }
}
export function accel(rate: number) {
  const r = Math.abs(rate);
  if (r <= constants.accelStart) return 1;
  return Math.min(constants.accelMax, 1 + (constants.accelMax - 1) * (r - constants.accelStart) / (constants.accelEnd - constants.accelStart));
}
export const gain = (rate: number, dt: number, speed: number) => rate * (dt / 1000) * constants.basePxPerDeg * speed * accel(rate);
export const quantize = (value: number) => Math.round(value * 100) / 100;
export const clampOutput = (value: number) => Math.max(-constants.maxOutput, Math.min(constants.maxOutput, value));
export const isTap = (durationMs: number, travelPx: number, rotationDeg: number) =>
  durationMs < constants.tapMs && travelPx < constants.tapTravelPx && rotationDeg < constants.tapRotationDeg;
export class AirPipeline {
  private readonly gravity = new GravityEstimator();
  private readonly yaw = new OneEuro();
  private readonly pitch = new OneEuro();
  private lastTime: number | null = null;
  private lastRight: Vec3 | null = null;
  private engagedAt = -Infinity;
  private first = true;
  private sign: GravitySign;
  /** Degrees of rotation since the hold began, for tap classification. */
  rotation = 0;
  constructor(gravitySign: GravitySign = 1) { this.sign = gravitySign; }
  reset() { this.gravity.reset(); this.lastTime = null; this.lastRight = null; this.engagedAt = -Infinity; this.rotation = 0; }
  engage(now: number, angle: number) {
    this.engagedAt = now; this.first = true; this.rotation = 0; this.lastRight = null;
    this.yaw.reset(); this.pitch.reset();
    this.sign = resolveGravitySign(this.gravity.vector, screenAxes(angle)) ?? this.sign;
  }
  /** undefined: no usable gyroscope data; null: nothing to emit; otherwise pixel deltas (±500). */
  feed(sample: MotionSample, now: number, settings: AirSettings, angle: number, holding: boolean): { dx: number; dy: number } | null | undefined {
    const clean = sanitize(sample);
    if (!clean) return undefined;
    const dt = clampDt(this.lastTime === null ? null : sample.timeStamp - this.lastTime, sample.interval);
    this.lastTime = sample.timeStamp;
    this.gravity.update(clean.gravity, dt);
    if (!holding) return null;
    if (this.first) { this.first = false; return null; } // no trustworthy dt for the first sample
    const axes = screenAxes(angle);
    const up = this.gravity.vector ? scale(normalize(this.gravity.vector), this.sign) : null;
    const { yaw, pitch, right } = project(clean.omega, up, axes.right, this.lastRight);
    this.lastRight = right;
    this.rotation += magnitude(clean.omega) * dt / 1000;
    // Filter first, then the deadzone: raw noise peaks would otherwise leak through the deadzone as a slow random walk.
    const yawRate = softDeadzone(this.yaw.filter(yaw, dt / 1000));
    const pitchRate = pitch === null ? 0 : softDeadzone(this.pitch.filter(pitch, dt / 1000));
    if (now - this.engagedAt < constants.settleMs) return null; // pressing the pad moves the phone
    const dx = -gain(yawRate, dt, settings.speed) * (settings.invertX ? -1 : 1);
    const dy = -gain(pitchRate, dt, settings.speed) * (settings.invertY ? -1 : 1);
    return { dx: clampOutput(dx), dy: clampOutput(dy) };
  }
}
