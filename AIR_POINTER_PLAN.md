# PC Remote — Air pointer (gyroscope mouse) plan

Created: 2026-10-13. Baseline at creation: committed app version **0.3.0**, commit `56fa60d` (an uncommitted 0.3.1 version bump was in progress in the working tree), public [v0.3.0 preview](https://github.com/maamouribilel/pc-remote/releases/tag/v0.3.0).

This is a plan. **Nothing in it has been implemented or validated by creating this document.** The feature, its tests, and any device results must be recorded in the execution log at the end as they actually happen.

## The idea

Point the phone at the screen, hold your thumb on the pad, and turn your wrist. The cursor follows, like a Wii Remote, an LG Magic Remote, or a Gyration air mouse. Lift your thumb and the cursor stops. Tap to click.

Precisely: this is **relative** gyroscope pointing (Wii MotionPlus / LG / Gyration style), not absolute pointing. The Wii's "point exactly where you aim" relies on an infrared sensor bar and camera that a phone doesn't have. Absolute aiming is a stretch goal (see "Later").

Three choices make it feel like a real remote rather than a gimmick. All three are in scope for v1:

1. **A hold-to-point clutch.** The cursor moves only while the thumb is down. This removes drift, pocket motion, and "where did my cursor go" moments, and lets the user re-grip, just as you lift a mouse.
2. **Tilt-compensated motion.** Turning left/right always moves the cursor horizontally, whether the phone is held flat (remote grip), upright (camera toward the screen), or partly rolled. This uses the gravity vector, the way commercial air mice do.
3. **Adaptive filtering.** A One-Euro filter, a soft deadzone, and a gentle gain curve keep slow movement steady and fast movement responsive. Tap-to-click is protected against the "click jitter" of pressing the screen.

## How to use this document

1. Run the phases in order. Each has a copyable prompt. Do not start the next phase automatically.
2. Phase 0 settles facts that cannot be known from the repository: real sensor axes, signs, sample rates, iOS permission behavior, and test-injection support. Later phases depend on its recorded results. Do not skip it.
3. Track implementation, automated checks, and real-device acceptance separately. A missing physical result stays **Pending**. It is never relabeled as passed.
4. This plan does not authorize a version bump or public release. Release work follows `AGENTS.md` / `CLAUDE.md` and needs separate authorization.

## Key decisions

| # | Decision | Why |
| --- | --- | --- |
| D1 | **Phone-client only. Reuse `pointer.move {dx,dy}` from protocol v1, with the same units and meaning.** No new message, field, capability, endpoint, or server setting. | The server already accepts finite fractional deltas (±500 per message), applies the PC's 0.25–4 sensitivity, and accumulates fractions (`ControlProtocol.cs`). AGENTS.md requires a protocol version for any unit or meaning change, and none is needed. Older Windows companions work automatically. |
| D2 | **Hold-to-point clutch is the only v1 mode.** Air mode stays off by default; Touch remains the default. | It is the safest design and fits the repo's "starts paused / explicit action" philosophy. A latched "always on" mode is deferred. |
| D3 | **Gravity-projected yaw/pitch, with a fixed-grip fallback.** | Handles flat, upright, and rolled grips. Falls back to the remote grip when gravity data is missing. |
| D4 | **One-Euro filter + soft deadzone + mild gain curve.** Air-specific tuning (speed, invert vertical) is stored **locally on the phone** (`localStorage`, wrapped in try/catch). | Gyro noise, bias, and feel are properties of the phone's hardware, so per-phone storage fits. PC sensitivity in Host still applies on top. A Host setting would need a new authenticated mutation with ownership rechecks, which is out of scope. |
| D5 | **A `Touch | Air` switch inside the Touchpad tab**, not a sixth tab. | The tab bar is already five columns wide. Air mode shares the click/drag buttons. |
| D6 | **Sensors are read only while Air mode is selected, the Touchpad tab is visible, control is enabled, and the page is visible.** Only rounded pixel deltas leave the phone, and only while the clutch is held. Air mode never re-engages on its own. | Privacy (motion data can leak typing) and safety. A resume or reconnect requires a new deliberate hold. |
| D7 | **Separate `#air-pad` element** next to `#touchpad`, toggled with `hidden`. It feeds the **existing** touchpad accumulator. | Leaves the proven touch gesture code untouched. Keeps one `pointer.move` per animation frame, `congested()` backpressure, and the `flush(true)` ordering before button transitions. |

Naming: "Air pointer" in docs, "Air" on the switch. This can be renamed before the UI is final.

## Architecture

```
devicemotion (rotationRate °/s, accelerationIncludingGravity m/s², timeStamp)
   │  air-pointer.ts: subscribe only per D6; detect "no sensor"; permission flow
   ▼
air-math.ts (pure, DOM-free, unit-tested)
   sanitize → dt clamp → gravity estimate u → screen-axis remap
   → yaw = ω·u, pitch = ω·r → soft deadzone → One-Euro → gain → px (dx, dy)
   │  only while the clutch is held and past the settle window
   ▼
touchpad.ts accumulator (moveX/moveY) ── rAF flush ── existing sendControl()
   ▼
WSS pointer.move {dx,dy}  →  Host: × PC sensitivity, fractional accumulation  →  native adapter
```

### Files (planned)

| File | Change |
| --- | --- |
| `src/remote/src/air-math.ts` | **New.** Pure functions/classes with no DOM and no imports: vector helpers, gravity low-pass, axis remap, projection, deadzone, `OneEuro`, gain, quantize/clamp. Imported directly by a server-free Playwright spec. |
| `src/remote/src/air-pointer.ts` | **New.** DOM/sensor glue: mode switch, permission and "no sensor" states, clutch pointer handling, settle window, tap-to-click, stall watchdog, local settings, lifecycle `stop()`. |
| `src/remote/src/touchpad.ts` | Small additive hooks: `motion(dx, dy)` adds to `moveX/moveY` (with the existing ±500 clamp) and schedules a frame; `click()` reuses `button('left','click')`; `reset()` also calls an `onReset` callback so Air mode stops on every existing release path. The touch gesture code is unchanged. |
| `src/remote/src/main.ts` | Construct Air mode next to the touchpad; include it in `enableControl`, `visibilitychange`, `pagehide`, socket close, and `connect()` disable paths (most already go through `controls.release()`/`setEnabled(false)`). |
| `src/remote/index.html` | Segmented switch, `#air-pad`, an "Enable motion" button, an `Air settings` `<details>` (speed slider, invert vertical), and updated Control help text. |
| `src/remote/src/style.css` | Switch, clutch idle/held/needs-permission/unavailable states, and the 390×700, 844×390 and compact layouts. |
| `src/remote/src/icons.ts` | One `air` icon (a phone with motion arcs), in the existing inline-SVG style. |
| `tests/remote/air-math.spec.ts` | **New.** Unit tests as a Playwright spec that imports `../../src/remote/src/air-math` and uses only `test`/`expect` (no `page` fixture, no server, no `test.skip`). There is no new dependency. A `*.test.ts` name is deliberately avoided: Playwright's default `testMatch` would pick it up on a bare `npm test`. |
| `tests/remote/air.spec.ts` | **New.** Playwright tests against the recording WSS fixture. |
| `scripts/portable.py`, `scripts/test.ps1` / CI | Run the unit tests and the new spec with the existing browser suite, sequentially on port 8443. |

## Motion pipeline specification

Device frame (W3C): **x** = right, **y** = top of the screen, **z** = out of the screen toward the user. `rotationRate.alpha` is around z, `beta` around x, `gamma` around y, in °/s. So ω = (beta, gamma, alpha) in (x, y, z). **Phase 0 must confirm the axis order, sign, and units on real iOS and Android**, because historical browser differences exist.

1. **Sanitize.** Drop the sample if any component is null or non-finite. Drop it if |ω| > 2000 °/s (a spike). `rotationRate == null` on the first events means no gyroscope.
2. **dt.** Use the difference between `event.timeStamp` values, falling back to `event.interval`. Clamp to 4–50 ms. **Discard the first sample after engaging or resuming**, because there is no valid previous timestamp, which prevents jumps after suspend.
3. **Gravity / up vector u.** Low-pass `accelerationIncludingGravity` (τ ≈ 150 ms), seeded with the first sample so no warm-up is needed. Normalize, then apply the per-platform sign constant from Phase 0. The W3C convention is that a phone lying face-up reads ≈ (0, 0, +9.8). If the magnitude is outside 6–14 m/s² (shaking), keep the previous u. If gravity is unavailable, use **fixed remote grip**: u = (0, 0, 1).
4. **Screen axes.** Remap device x/y to screen right/up using `screen.orientation.angle`, so landscape works. Phase 0 confirms the rotation direction.
5. **Project.**
   - yaw rate = ω·u. Turning left is positive, so it moves the cursor left.
   - right axis r = normalize(sx − (sx·u)u), where sx is screen-right. If |sx − (sx·u)u| < 0.35 (phone rolled about 70° or more), keep the last valid r. If there is none, suppress vertical motion.
   - pitch rate = ω·r. Pointing up is positive, in both flat and upright grips, so it moves the cursor up.
6. **Soft deadzone.** Starting value 1.5 °/s per axis. Subtract the threshold instead of clamping, so there is no step. If Phase 0 shows resting bias above the deadzone, add bounded online bias estimation (≤ 3 °/s, updated only while the phone is still and the clutch is not held).
7. **One-Euro filter** per axis. Starting values: minCutoff 1.0 Hz, beta 0.02, dCutoff 1.0 Hz. These are tuned in Phase 4.
8. **Gain.** px = rate × dt × base × speed × accel(|rate|). Starting values: base 40 px/° (about 48° sweeps a 1920 px screen at PC sensitivity 1); accel = 1 below 20 °/s, rising linearly to 2 at 200 °/s; speed slider 0.5–2.5× (default 1). Windows "Enhance pointer precision" and macOS/GNOME acceleration also apply to relative moves, so the curve should stay mild and be tuned on default OS settings.
9. **Output.** dx = −yaw px, dy = −pitch px, with an optional vertical invert. Add to the touchpad accumulator. The existing code clamps to ±500. **In the shared flush**, round each sent value to 2 decimals and carry the remainder in `moveX/moveY`, so nothing is lost. This keeps frames well under the 1024-byte limit. It also applies to touch output, which is harmless; record it as a deliberate change next to the flush spacing below. Fractions continue to accumulate on the server.
10. **Message rate.** One `pointer.move` per animation frame, unchanged. 120 Hz ProMotion is fine under the 180 msg/s budget. **New guard:** limit flushes to ≥ 8 ms apart (≤ 125 Hz) so 144 Hz Android displays plus heartbeats and button events stay well under budget. This guard lives in the shared flush, so the touchpad benefits too. Record this as a deliberate behavior change.

### Clutch, click and jitter rules

- **Engage** on `pointerdown` (touch, first pointer only) on `#air-pad`, with pointer capture. Extra pointers are ignored.
- **Settle window:** no motion is sent for the first **100 ms** after touch-down. Pressing the screen rotates the phone slightly.
- **Tap to click:** a press shorter than 250 ms, with finger travel under 8 px and total rotation under 1.5°, sends `left click` **with zero movement**. The settle window covers most taps.
- **Lift jitter (tunable, Phase 4):** an optional 40 ms trailing hold-back that is dropped on `pointerup`. Enable it only if real-device tests show visible jumps on lift, because it adds latency.
- **Stop** on `pointerup`, `pointercancel`, `lostpointercapture`, a sensor stall (no sample for 300 ms; show "Motion paused"), `setEnabled(false)`, any `release()`, tab switch, settings dialog opening, visibility hidden, `pagehide`, socket close, or a mode switch.
- **Drag:** the existing Start/End drag button works unchanged. Press Start drag, hold the clutch to move, then press End drag. Held-button heartbeats (250 ms) already follow `controls.held()`. Pointing alone holds nothing, so the normal 5 s ping applies.
- **Never auto re-engage** after resume, reconnect, takeover, or permission grant. The user must hold again.

### Permission and availability states

The `#air-pad` shows one of these states:

| State | When | UI |
| --- | --- | --- |
| Needs permission | iOS: `typeof DeviceMotionEvent.requestPermission === 'function'` and no sample arrived within 500 ms of entering Air mode | Pad shows **Enable motion**. Its `click` handler calls `DeviceMotionEvent.requestPermission()` **synchronously as its first action** (no `await` before it). Touch `pointerdown` does not count as a user activation, but `click` does. |
| Denied | `requestPermission()` resolves to `denied` | Explains how to allow motion access and offers **Use Touch**. The exact iOS steps (Safari tab vs. Home Screen app) are recorded in Phase 0, not guessed. |
| No sensor | No non-null `rotationRate` within 1 s (desktop browsers, some tablets) | "No motion sensor on this device." The Air switch stays visible but the pad is disabled. |
| Ready / Holding | Samples flowing | "Hold to point · tap to click" / accent border plus "Pointing". Android `navigator.vibrate(10)` on engage, where supported. |
| Unavailable | `capabilities.pointer` false or control disabled | Same `inert`/disabled treatment as the touchpad (`capabilities.spec.ts` expectations still hold). |

The server sends no `Permissions-Policy` header today (only CSP, which does not affect sensors), so top-level access is allowed by default. **If a Permissions-Policy header is ever added, it must allow `accelerometer` and `gyroscope` for `self`.** Add this as a comment and a security check.

## Rules for every phase

- Read `AGENTS.md`, `CLAUDE.md`, `README.md`, `docs/protocol.md`, `.github/SECURITY.md`, and this file first. They remain authoritative.
- Protocol v1 stays exactly as it is. Do not change Host/Core/native adapters for this feature. If a phase finds that a server change seems necessary, stop and record why instead of making it.
- Preserve all existing touchpad, keyboard, media, PC, and release behavior, and every existing browser test.
- Use the pinned toolchain: `.tools/dotnet` 10.0.402 and `.tools/node-v24.21.0-*` are picked up by `scripts/portable.py`. Add no new npm dependency without a recorded reason, license check, and audit.
- Run HTTP/browser suites sequentially on 8443, with the companion closed.
- Synthetic or virtual-sensor tests prove pipeline math, gating, and release behavior. They **do not** prove iOS permission behavior, real-hand pointing, or native input. Keep those as separate, visible gates.
- Do not inject input into the user's active work during manual tests. Use a disposable target window or the recording fixture.
- Update the Progress table and append a dated execution-log entry at the end of each phase. Finish each phase with a handoff, and do not start the next one automatically.

## Progress

| Phase | Implementation | Automated checks | Real-device acceptance |
| --- | --- | --- | --- |
| 0. Spikes & measurements | Measured on an iPhone 2026-10-15 (sensor lab outside the repository; table in the Phase 4 entry): the axis order (x/y/z per the current specification, on every platform) and the iOS gravity sign differ from the stated assumptions | Recorded flat windows replay in `air-math.spec.ts` | iPhone done (flat grip); upright/roll/landscape captures, Safari/Home Screen permission and Android pending |
| 1. Motion math | Done 2026-10-14 (`air-math.ts`) | 18 page-less checks pass locally | — |
| 2. UI & integration | Done 2026-10-14 | `tsc`/Vite build; local mocked-companion check with synthetic sensors | — |
| 3. Automated browser tests | Done 2026-10-14 (`air.spec.ts`, capability assertion, scripts/CI wired) | Passed locally 2026-10-15 on the fixture (first run); hosted run pending in the 0.4.0 final dispatch | — |
| 4. Real-device tuning & acceptance | Partly done 2026-10-15: axis order, gravity sign, filter order, deadzone and interval corrected from iPhone measurements (commit `995539d` on a branch, unreleased) | `remote-unit` 19, `browser` 19 + 6 + 1, strict build | iPhone flat-grip directions confirmed with the corrected build; the measured table, other grips, lifecycle, Safari/Home Screen permission, Android, Windows 11 and Ubuntu remain **Pending** (table in the 2026-10-15 Phase 4 entry) |
| 5. Docs & release integration | Done 2026-10-15: published as [v0.4.0](https://github.com/maamouribilel/pc-remote/releases/tag/v0.4.0) | Final dispatch 38707534711 passed on every OS, including `air.spec.ts` on Windows | — |

## Phase 0 — spikes and measurements

**Outcome:** recorded facts that replace every "Phase 0 confirms" above. No production code is merged.

1. **Baseline.** `python3 scripts/portable.py build`, then `python3 scripts/portable.py browser` (port 8443 free). Record pass/fail before any change.
2. **Sensor lab (separate `air-sensor-lab` branch, never merged to `main`).** Add a temporary debug readout to the remote that shows raw ω, raw gravity, u, yaw/pitch rates, sample interval, and rAF rate. Serve it through a real companion on a trusted LAN: the macOS shell via `python3 scripts/portable.py desktop` with explicit network approval, or the Windows companion. The loopback fixture cannot reach a phone. Measure on iPhone (Safari tab **and** Home Screen web app) and on an Android phone with Chrome, if available:
   - axis order, signs, and units for each grip (flat remote grip, upright, 30° roll, landscape);
   - gravity sign convention per platform → the sign constant;
   - sample rate and jitter; resting noise and bias (°/s) over 30 s; rAF rate (60/120/144 Hz);
   - iOS permission: does it prompt once per page load, per launch, or persist? Behavior after lock/unlock and background; reset steps after a denial; Home Screen vs. Safari differences;
   - whether `screen.orientation.angle` is available in the Home Screen app.
3. **Test injection spike.** In Playwright 1.64 Chromium, check whether CDP `Emulation.setSensorOverrideEnabled` / `setSensorOverrideReadings` (`gyroscope`, `accelerometer`, `linear-acceleration`) produces `devicemotion` events. If not, use `window.dispatchEvent(new DeviceMotionEvent('devicemotion', {...}))`. Also confirm that a page-less Playwright spec can import `src/remote/src/air-math.ts` from outside `testDir`.
4. **Decide and record:** the sign constants, the injection method, starting constants (adjusted from the defaults above if the measurements say so), whether bias estimation is needed, and the final name.

Exit gate: a filled-in measurement table in the execution log. **Keep** the `air-sensor-lab` branch unmerged for Phase 4 tuning, and delete it after Phase 4.

**Copy this prompt:**

```text
Execute Phase 0 of AIR_POINTER_PLAN.md: spikes and measurements.
Read AIR_POINTER_PLAN.md in full, AGENTS.md, CLAUDE.md and docs/protocol.md.
Run the baseline build and browser suite and record results. On the unmerged
air-sensor-lab branch only, build the temporary sensor readout and the Playwright injection
spike described in Phase 0. Give me precise steps to run the real-device
measurements and record exactly what I report; never invent device results.
Record decisions, constants and the measurement table in the execution log,
update Progress, keep the lab branch unmerged for Phase 4, and stop.
Do not start Phase 1.
```

## Phase 1 — motion math (`air-math.ts`) and unit tests

**Outcome:** a pure, fully tested pipeline that matches the specification with Phase 0 constants.

Unit tests (`tests/remote/air-math.spec.ts`, a page-less Playwright spec that runs without the fixture):

- flat grip: positive yaw → negative dx; positive pitch → negative dy (up);
- upright grip gives the same screen directions; 30° roll: pure yaw → |dy| ≈ 0 (tilt compensation);
- landscape remap; rolled-90° degeneracy keeps the last r or suppresses vertical motion;
- missing gravity → fixed remote grip; shaking magnitude keeps the previous u;
- first sample after engage is dropped; dt clamps to 4–50 ms; spike and NaN rejection;
- soft deadzone is continuous; One-Euro steady state, jitter attenuation, and bounded lag on a step;
- gain is monotonic; speed/invert settings; output rounds to 2 decimals and clamps to ±500;
- settle-window and tap classification helpers (if implemented as pure functions).

**Copy this prompt:**

```text
Implement Phase 1 of AIR_POINTER_PLAN.md. Read the plan (including the Phase 0
log), AGENTS.md and CLAUDE.md. Create src/remote/src/air-math.ts as a pure,
DOM-free module per the "Motion pipeline specification", using the Phase 0
constants, and tests/remote/air-math.spec.ts (page-less Playwright spec, no
server) covering every listed case. No new dependencies. Run `npm run build`
(tsc) and `npm test -- air-math.spec.ts` with the pinned Node. Update Progress and the execution log, then stop.
```

## Phase 2 — UI and integration

**Outcome:** a working Air mode in the remote that obeys every clutch, lifecycle, and permission rule.

- Make the touchpad hooks (`motion`, `click`, `onReset`) additive. Add the shared ≥ 8 ms flush spacing.
- `air-pointer.ts`: state machine (Off → NeedsPermission/NoSensor/Denied → Ready ⇄ Holding), subscription per D6, clutch, settle window, tap-click, stall watchdog, local settings with try/catch storage.
- HTML/CSS/icon. Accessibility: the switch is a `radiogroup` with arrow-key support (same pattern as the tabs); the clutch has an `aria-label`; status changes use the existing polite live region sparingly; focus-visible styles; 44 px minimum targets.
- Control help text: add one Air paragraph in the existing dialog.
- Self-check by hand in a desktop browser with the recording fixture (`No sensor` state, switch, layout at 390×700 / 844×390 / 1280×800).

**Copy this prompt:**

```text
Implement Phase 2 of AIR_POINTER_PLAN.md. Read the plan and its log, AGENTS.md,
CLAUDE.md, docs/protocol.md and the current src/remote sources. Build Air mode
per D1–D7, the clutch/jitter rules and the permission states, changing only the
files listed in the plan. Keep protocol v1 and all touch gestures unchanged.
Run `npm run build`, the unit tests and the existing browser suite
(`python3 scripts/portable.py browser`). Update Progress and the log, then stop.
```

## Phase 3 — automated browser tests

**Outcome:** regression coverage against the real HTTPS/WSS recording fixture.

`tests/remote/air.spec.ts`, using the Phase 0 injection method and `addInitScript` stubs for `DeviceMotionEvent.requestPermission`:

- Hold the clutch and inject yaw → `events.json` gains `move:x,y` with the expected sign. Pitch likewise.
- Inject motion with the clutch **not** held → no `move:` events.
- A tap on the clutch → exactly one `click:left` and **no** `move:` events.
- Stall (stop injecting) → pointing stops; resumed samples do not move the cursor until a new hold.
- Tab switch, settings dialog, `visibilitychange` hidden, and `pagehide` mid-hold → movement stops and `release` is recorded where the existing code sends it; no auto re-engage afterwards.
- Drag: Start drag → hold + move → End drag gives the order `left:True`, `move:…`, `left:False`.
- Permission stub `denied` → Denied state and no events. With `granted`, assert `requestPermission` was called while `navigator.userActivation.isActive` (an approximation of the iOS gesture rule).
- No-sensor state when nothing is injected. Platform `unavailable` → pad disabled (extend `capabilities.spec.ts`).
- Message-rate check: under sustained injection, sent `pointer.move` frames ≤ 125/s.
- Screenshots of the Air states at 390×700 and 844×390.

Then run the full required set: `npm run build`, unit tests, `portable.py browser`, `browser-capabilities`, `test`, and `audit`. Run the Windows regression job where a runner is available. Wire the new tests into `portable.py`, `test.ps1`, and the CI workflows; configuring CI is not evidence that it ran.

**Copy this prompt:**

```text
Implement Phase 3 of AIR_POINTER_PLAN.md. Read the plan and its log, AGENTS.md
and CLAUDE.md. Add tests/remote/air.spec.ts and capability assertions covering
every listed case, using the Phase 0 injection method, and wire the unit tests
and spec into portable.py, test.ps1 and CI. Run all listed checks sequentially
on 8443 and record actual results. Synthetic sensors are not device evidence.
Update Progress and the log, then stop.
```

## Phase 4 — real-device tuning and acceptance

**Outcome:** tuned constants and an honest, dated evidence record. Use a disposable target (for example a full-screen target-practice page or an empty text editor), never the user's active work.

| Check | Pass criterion (starting proposal) |
| --- | --- |
| Corner reach | Reach all four corners and the centre of a 1080p screen with ≤ 2 re-grips each |
| Precision | Hit 10 random 32 px targets in ≤ 30 s with ≤ 2 misses |
| Rest drift | Clutch held, phone resting still for 10 s → ≤ 5 px drift |
| Tap jitter | 20 taps → cursor displacement ≤ 2 px each |
| Lift jitter | No visible jump on lift; otherwise enable and tune the 40 ms hold-back |
| Grips | Flat, upright, 30° roll, and landscape all give the expected screen directions |
| Lifecycle | Lock, background, tab switch, PC Pause, takeover by another device, and Wi-Fi drop mid-hold → motion stops, no stuck button, no auto re-engage |
| Permission | First prompt, deny, and recovery; Safari tab vs. Home Screen app (iOS); no prompt on Android |
| Drag | Drag a file or window with Start drag + clutch, then End drag |

Matrix: iPhone (Safari tab and Home Screen app) and Android Chrome, against Windows 11 (primary; "Enhance pointer precision" on and off), macOS (Quartz, Accessibility granted), and Ubuntu X11. Wayland stays pending, as it is for the rest of the app. Record the device model and OS version (no personal data), the constants used, and the results. Mark anything not run as **Pending**.

**Copy this prompt:**

```text
Execute Phase 4 of AIR_POINTER_PLAN.md. Read the plan and its log. Prepare a
disposable target and give me step-by-step real-device test instructions for
the acceptance table and matrix. Tune constants only from my reported results,
re-run unit/browser tests after each change, and record exactly what was and
was not tested. Update Progress and the log, then stop.
```

## Phase 5 — documentation and release integration

Only with the user's separate release authorization. A user-visible feature suggests the next **minor** version after whatever is published at that time (a 0.3.1 bump was in progress in the working tree when this plan was written); decide it at release time.

- `README.md`: a features row (Air pointer), a real Air-mode screenshot with provenance in `docs/assets/README.md`, and an optional walkthrough refresh.
- `docs/protocol.md`: one client-behavior paragraph next to "Motion is aggregated per animation frame". It states that there is **no wire change** and that raw sensor data never leaves the phone.
- `docs/ui-design.md`, `docs/limitations.md` (relative not absolute pointing, drift, iOS permission, no sensors on desktop browsers, OS acceleration interplay), `docs/troubleshooting.md` (motion permission recovery), `.github/SECURITY.md` review (sensor privacy note; trust boundary unchanged).
- A dated `docs/release-evidence.md` entry that keeps synthetic and physical results separate.
- Then the full AGENTS.md "Required for every new version" list: versions in `Directory.Build.props`, App csproj, `package.ps1`, and `installer/PCRemote.iss`; **both** `AGENTS.md` and `CLAUDE.md`; `docs/releases.md`; `.github/` review; every release check; packaging, hashes, and secret scans; publication only after authorization.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| iOS re-asks for motion permission on every launch | Detect samples first, then show a one-tap **Enable motion**. Phase 0 measures the behavior and Phase 5 documents it. |
| Axis/sign differences between iOS and Android | Phase 0 measurements; sign constants live in one place; covered by unit tests; invert-vertical setting. |
| Gyro bias causes slow creep while held | Soft deadzone; conditional bounded bias estimation; the clutch limits exposure. |
| Click/lift jitter makes clicks miss | Settle window, zero-move tap click, optional lift hold-back. |
| Double acceleration (phone curve + OS curve) | Mild phone curve; tuned on default OS settings; speed slider. |
| Message budget on high-refresh phones | ≥ 8 ms flush spacing (≤ 125 Hz) shared with the touchpad. |
| Motion data as a side channel (typing inference) | Sensors are read only in Air mode on the Touchpad tab (D6), never while the Keyboard tab is visible; nothing raw is transmitted. |
| A future Permissions-Policy header silently disables the feature | Code comment plus a security check asserting `accelerometer`/`gyroscope` stay allowed for `self`. |
| Tests overclaim | Synthetic/virtual sensors are labeled as such; physical gates stay Pending until run. |

## Later (not v1)

- Latched "air mode on" with a 10 s idle auto-off.
- Air scroll: hold a Scroll zone and tilt → `pointer.scroll`.
- Presentation / laser mode: a large cursor highlight would need a PC-side overlay, which is a server change and needs its own plan.
- Absolute "aim" re-centering: tap to snap the cursor to the screen centre while pointing at it. This needs a server-side absolute move, i.e. a new protocol capability and version decision.
- Flick gestures for media (for example a quick wrist flick → next track) through existing `media.action` gating.

## Execution log

### Template for a future execution

- Date / phase / status:
- Implemented behavior and changed files:
- Environment (devices, OS, browser, companion platform; redact personal details):
- Commands/checks and actual results:
- Synthetic / virtual-sensor evidence:
- Real-device evidence:
- Pending or unavailable checks and exact follow-up steps:
- Decisions, constants, deviations:
- Handoff for the next phase:

### 2026-10-13 — plan created

- Status: plan only. No source, test, version, or documentation changes besides this file.
- Repository facts that shaped the plan: `pointer.move` accepts finite ±500 fractional deltas, and Host multiplies by the PC sensitivity (0.25–4) and accumulates fractions (`src/PcController.Core/ControlProtocol.cs`). The remote aggregates motion per animation frame with backpressure (`src/remote/src/touchpad.ts`). The CSP has no effect on sensors, and no `Permissions-Policy` header exists (`src/PcController.Host/CompanionServer.cs`). The remote does not use `localStorage` yet. The browser fixture records `move:x,y` after server accumulation (`tests/PcController.PortableTests/MouseChecks.cs`). The pinned .NET 10.0.402 and Node 24.21.0 are present in `.tools/`.
- Next: Phase 0.

### 2026-10-14 — Phases 1–3 implemented; Phase 0 replaced by stated assumptions; fixture run pending

- Status: feature implemented in source on `main` after v0.3.6 (version unchanged, nothing packaged or published). Phase 0's physical measurements could not be made in this session, so the plan's "Phase 0 confirms" items are implemented as **explicit assumptions** that the Phase 4 device session must confirm or correct.
- Implemented behavior and changed files:
  - `src/remote/src/air-math.ts` (new, pure): sanitize/spike rejection, 4–50 ms dt clamp with `interval` fallback, gravity low-pass (τ 150 ms, 6–14 m/s² band, seeded), screen-axis remap from the clockwise orientation angle, yaw/pitch projection with the 0.35 right-axis guard, soft 1.5 °/s deadzone, One-Euro (1 Hz / 0.02 / 1 Hz), 40 px/° gain with a 1→2 curve between 20 and 200 °/s, two-decimal quantize, ±500 clamp, tap and settle rules, and `AirPipeline` (first sample after engage dropped, 100 ms settle, rotation integral).
  - `src/remote/src/air-pointer.ts` (new): `Touch | Air` radiogroup, `#air-pad` clutch with pointer capture, states Idle → Probing → NeedsPermission / Denied / NoSensor / Ready ⇄ Holding, 300 ms stall watchdog ("Motion paused · hold again"), tap-to-click, Air settings dialog (speed 0.5–2.5×, invert horizontal, invert vertical) in `localStorage` (`pc-remote.air`, try/catch), subscription only while enabled ∧ Air ∧ Touchpad tab visible ∧ page visible (MutationObserver on the panel plus `visibilitychange`), stop on every touchpad reset path.
  - `src/remote/src/touchpad.ts` (additive): `motion()`, `click()`, `hook({reset, enabled})`, dragging class on both pads, shared flush now rounds to two decimals, carries sub-0.01 remainders and spaces aggregates ≥ 8 ms apart (deliberate behavior change for touch output too; integer touch deltas are unaffected).
  - `src/remote/index.html`, `style.css`, `icons.ts` (`air`, `tune`), `main.ts` (one construction line; lifecycle paths flow through the touchpad hooks). Control help gained an Air paragraph.
  - `tests/remote/air-math.spec.ts` (new, page-less), `tests/remote/air.spec.ts` (new, fixture), `tests/remote/capabilities.spec.ts` (`#mode-air` follows the pointer capability), `scripts/portable.py` (`remote-unit` action; `browser` runs the unit spec, then `mouse`/`connection` on one fixture instance and `air.spec.ts` on a second because each fixture issues one single-use challenge), `.github/workflows/security-checks.yml` (`remote-unit` runs in the shared job's browser step, after Playwright installs Chromium), `src/PcController.Host/CompanionServer.cs` (comment only: a future Permissions-Policy must keep `accelerometer`/`gyroscope` for `self`; `air.spec.ts` asserts it).
- Environment: macOS (Apple Silicon), pinned `.tools` .NET SDK 10.0.402 and Node 24.21.0/npm 11.19.0, Playwright 1.64 Chromium headless. The user's production companion (0.3.6) was running and holding port 8443 the whole session.
- Commands/checks and actual results:
  - `python3 scripts/portable.py remote-unit` → 18 passed.
  - `npm run build` (strict `tsc --noEmit` + Vite) → built; `dotnet build tests/PcController.PortableTests -c Release` → 0 errors (bundle copied to `wwwroot`).
  - Local throwaway check (scratch directory, not in the repository): the built bundle served over a self-signed HTTPS static server with `/api/*` and `/control/ws` answered by Playwright mocks, synthetic `devicemotion` events and CDP touch. Verified: permission states (needs-permission → denied → Use Touch → granted while `navigator.userActivation.isActive`), hold + yaw → negative `dx` only, hold + pitch → negative `dy` only, no motion without a hold, a tap sends exactly one left click and no movement, invert vertical flips `dy` and persists, stall pauses pointing without auto-resume, tab switch / connection settings / hidden page mid-hold stop pointing and send `input.releaseAll`, drag keeps `down → move → up` order, ~60 aggregates/s under 250 Hz injection (headless rAF), arrow keys toggle the switch, every visible control ≥ 44 px at 363/390/844/1280 widths, no console errors. This is **not** fixture evidence and is recorded only to explain why the untested fixture spec is expected to pass.
- Synthetic / virtual-sensor evidence: injection method = `window.dispatchEvent(new DeviceMotionEvent('devicemotion', {rotationRate, accelerationIncludingGravity, interval}))` every 16 ms from an `addInitScript` helper; `DeviceMotionEvent.requestPermission` stubbed the same way. Headless Chromium itself exposes `requestPermission()` and answers `"prompt"` without a dialog, so the no-sensor path on desktop Chromium shows **Enable motion** first and reaches "No motion sensor" after the 1 s probe.
- Real-device evidence: **none**. Pending.
- Pending or unavailable checks and exact follow-up steps:
  1. Quit the companion (or free port 8443), then `python3 scripts/portable.py browser` (unit spec, `mouse`/`connection`, then `air.spec.ts`) and `python3 scripts/portable.py browser-capabilities`. Record the results here. Windows `./scripts/test-browser-mouse.ps1` runs the same `browser` action.
  2. Phase 0/4 on a phone against a disposable target (see Phase 4): hold the pad flat like a remote and turn left → the cursor must move left; raise the top edge → the cursor must move up. If a direction is mirrored, note the device/OS/browser and flip the matching invert toggle; a consistent mirror on a platform means the sign assumption below is wrong for it and should become a constant. Then upright and 30° roll grips, landscape, rest drift, tap jitter, lift jitter, lock/background/takeover/Wi-Fi drop mid-hold, iOS Safari vs Home Screen permission prompts and the deny/reload recovery, Android (no prompt expected).
- Decisions, constants, deviations:
  - Sensor axes: W3C `rotationRate` (alpha about z, beta about x, gamma about y, right-hand rule). Gravity sign is **auto-resolved per hold** from the low-passed vector by assuming the phone is held screen-up or upright (`resolveGravitySign`; needs ≥ 4 m/s² of projection, otherwise the previous sign is kept, default +1). This replaces the planned per-platform constant because iOS/Android conventions were not measured; both outputs are consistent for flat and upright grips under either convention.
  - Both axes have invert toggles (the plan had vertical only): a wrong rotation-rate sign can mirror either axis and could not be measured.
  - Air settings live in a `<dialog>` behind a gear button beside the switch instead of a `<details>` block, so the pad keeps its height at 390×700.
  - The selected mode is not persisted; Touch is selected on every load (D2/D6). A later convenience could remember it.
  - Switching Touch/Air calls the touchpad's `release()`: held input (an active drag) is released like on a tab switch, and pointing stops, before the surfaces swap.
  - `requestPermission()` results other than `"denied"` fall through to the sample probe (Chromium answers `"prompt"`).
  - Constants are the plan's starting values, untuned.
- Handoff for the next phase: run the fixture suite first (step 1 above), fix anything it finds, then Phase 4 on real devices. Phase 5 (README row, screenshot, release integration) needs separate release authorization; README still describes the published 0.3.6.

### 2026-10-15 — Phase 3 fixture run passed; Phase 5 release 0.4.0 started

- Status: the user authorized committing and publishing so the feature can be tried on a phone. Version bumped to **0.4.0** (new user-visible feature → next minor, as planned).
- Commands/checks and actual results (macOS 27 Apple Silicon, pinned toolchain, the user's 0.3.6 companion quit with SIGTERM to free port 8443 and relaunched afterwards): `portable.py build`; `test` 272; `mac-test` 69; `desktop-test` 92; `browser` → air-math 18, connection 5 + mouse 1, **`air.spec.ts` 1 passed in 14.5 s on the first run**; `browser-capabilities` all four fixtures; `audit` and `audit-runtime.py` clean; Gitleaks 8.30.1 no leaks in 252 files. `docs/assets/mobile-air.png` captured through the fixture with a zero-rate synthetic sensor (Ready state).
- Synthetic / virtual-sensor evidence: same injection method as the 2026-10-14 entry, now against the real HTTPS/WSS recording fixture. Headless Chromium's own `requestPermission()` answered `"prompt"` before the stub was installed, and the pad reached "No motion sensor" after the probe, as designed.
- Real-device evidence: still **none**; Phase 4 stays Pending. The 0.4.0 public build is what the user will try first.
- Phase 5 work in this release: version files, README (download links, "New in 0.4.0", Features row, Air capture section, validation commands), both agent files, `docs/releases.md`, `docs/limitations.md`, `docs/protocol.md`, `docs/troubleshooting.md`, `docs/release-readiness.md`, `.github/SECURITY.md`, platform guides' download names, `docs/assets/README.md`, `IMPLEMENTATION_PLAN.md`, dated `docs/release-evidence.md`. Publication steps (final dispatch, tag, assets, public verification) are recorded in `docs/release-evidence.md`.
- Publication (same day): v0.4.0 pre-release from commit `13aa270`, assets verified locally and publicly (see `docs/release-evidence.md`), the public Apple Silicon build installed and signed on the user's Mac.
- Handoff: Phase 4 on the user's phone against a disposable target, recording device/OS/browser, constants and results here; tune constants only from reported results.

### 2026-10-15 — Phase 4 started: baseline rerun, disposable target, device session pending

- Status: Phase 4 **in progress** on branch `claude/nostalgic-noyce-efbe99` (a worktree, not `main`). No source constant has changed yet; the version stays 0.4.0 and nothing was packaged or published.
- Environment: macOS 27.0 (26A428), Apple Silicon, built-in Retina display 2560×1600. The user's installed public 0.4.0 companion (`/Applications/PC Remote.app`, locally re-signed) was running at session start but had **no listener on 8443** (most likely awaiting the per-build Keychain approval), so the port was free for the suites. The pinned toolchain is reached through a git-excluded `.tools` link to the main checkout. Phone: iOS 18.7 from the lab page's user agent; the model and the browser used for the remote were not reported.
- Commands/checks and actual results (synthetic, not device evidence): `python3 scripts/portable.py build` succeeded; `remote-unit` 18 passed; `browser` → page-less 18, connection 5 + mouse 1, then `air.spec.ts` 1 passed (15.1 s), sequential on 8443.
- Disposable target: a target-practice page kept **outside the repository** (session scratchpad, copied with a README to the shared side-session notes folder), served loopback-only at `http://127.0.0.1:8765/` by a tiny Python collector that appends each result as one JSON line. The page is opened full screen in Safari on the Mac so every phone click lands on it, never on the user's work. It measures in CSS px of the Mac's logical resolution: corner reach (five 64 px targets, time, misses; re-grips are counted by the user), precision (ten random 32 px targets, time, misses), rest drift (max and final displacement over 10 s), tap jitter (displacement in the 300 ms before and 350 ms after each click, 20 taps, a proxy), lift jitter (a final-step spike within the last 150 ms of a stroke, a proxy, plus the user's observation), drag (box into a drop zone, release event recorded) and a live direction readout plus a left-button up/down readout for the grip and stuck-button checks. Its logic was exercised with the in-app browser's mouse, which is not phone evidence.
- Real-device evidence: **none yet**. Step-by-step instructions were handed to the user for grips/directions, the measurement table, lifecycle (lock, background, tab switch, PC Pause, takeover, Wi-Fi drop), iOS Safari vs Home Screen permission and recovery, drag and speed feel; availability of Android, Windows 11 and Ubuntu was asked so missing matrix rows stay Pending honestly.
- Decisions so far: the first pass runs the shipped 0.4.0 bundle with the untuned starting constants. Gain can be derived from the Air speed slider and the PC sensitivity the user settles on, without a redeploy. Deadzone, filter, settle-window or sign changes need a rebuilt bundle served by a companion: the installed bundle's `wwwroot` is never swapped (it would break the seal and the Keychain/TCC match), and the from-source `portable.py desktop` route costs the user one Keychain dialog plus an Accessibility entry for the development identity, so it is offered as a choice after the first pass rather than assumed.

### 2026-10-15 — Phase 4: the "random" pointer explained by a stale axis assumption; pipeline corrected and verified on the phone

- Status: the user's first try of the shipped 0.4.0 Air pointer on an iPhone against the macOS companion was "the air pointer is moving randomly". A disposable HTTPS sensor-lab page (scratchpad only, self-signed certificate on the Mac's LAN address, raw samples posted to the Mac) recorded `devicemotion` captures per grip and motion; replaying them through the real `air-math.ts` pipeline reproduced the fault and validated the correction below. Version unchanged (0.4.0); nothing packaged or published; the fix is **not yet verified on the phone** (see handoff).
- Environment: physical iPhone, iOS 18.7, lab page loaded in **Brave for iOS** (WebKit; `permission: granted` after the Enable tap, secure context, portrait `screen.orientation.angle` 0). The browser used for the remote itself and the iPhone model were not reported. Companion: the installed public 0.4.0 on macOS 27.0, LAN approved during the session.
- **Measurement table (physical iPhone, 2026-10-15; replaces the stated assumptions of the 2026-10-14 entry):**

  | Fact | Measured |
  | --- | --- |
  | Sample rate | 60 Hz; `event.timeStamp` deltas 16.67 ms mean (12–21 ms); `event.interval` = **0.01667, i.e. seconds**, not milliseconds |
  | `rotationRate` axis order | **alpha = rotation about device x** (tilt top edge up → alpha ≈ +20 °/s, down → negative), **gamma = rotation about device z** (turn left → gamma ≈ +20 °/s, turn right → negative), so beta = rotation about y by elimination (roll capture pending). Right-hand rule, °/s. This matches the **current specification** (W3C Candidate Recommendation Draft 2025-02-12: "the alpha getter steps are to return the value of this's x axis rotation rate", beta y, gamma z) and the engine sources (Chromium `device_motion_event_pump.cc` passes gyro x/y/z, WebKit `WebCoreMotionManager.mm` passes `rotationRate.x/y/z`). Version 0.4.0 had implemented the older "alpha about z, beta about x, gamma about y" wording still shown by MDN, so a tilt was read as a turn on every platform, not only on iOS. |
  | `accelerationIncludingGravity` | Points **toward the ground**: face-up (0.3, −1.3, −9.7) m/s², tilt top up shifts y negative; magnitude 9.5–9.8. The W3C example (+9.8 face-up) is the opposite sign. The per-hold sign resolver already answered −1 on every capture. |
  | Rest noise, flat on a surface, 3 s | std 1.3–1.6 °/s per axis, peaks ±4.4 °/s, mean bias ≤ 0.3 °/s |
  | Rest noise, held still in hand, 10 s | alpha std 2.7 °/s (peaks ±8), beta 1.0, gamma 1.3; bias ≤ 0.15 °/s |
  | Permission (Brave iOS) | `DeviceMotionEvent.requestPermission` present; no samples before the tap; `granted` after the Enable tap. Safari tab, Home Screen app, reload/relaunch and deny recovery: **pending** |
  | Landscape | not observed yet (every capture reported angle 0; rotation lock suspected) |

- Replay evidence (recorded samples through the pipeline, speed 1, no OS acceleration): shipped 0.4.0 order on "Flat tilt top up" → dx −1069 px, dy −35 px (a tilt moved the pointer sideways); on "Flat turn right" → dx +277, dy +142. With the iOS axis order: tilt up → dx +19, dy −1163; turn left → dx +239, dy +1; turn right → dx +974, dy −56. Rest drift, "held still in hand 10 s": shipped order 35 px; filter-then-deadzone 9 px at 1.5 °/s and 5 px at 2 °/s; "flat still" 3 s: 18 px → 5.4 / 4.0 px. A synthetic 5 °/s turn still moves 242 px in 2 s with the new order and deadzone.
- Implemented (source, this branch): `air-math.ts` now reads `rotationRate` as (alpha, beta, gamma) = rotation about (x, y, z) on every platform, per the current specification; `detectGravitySign(userAgent, platform, maxTouchPoints)` seeds the gravity "up" sign (−1 for any iPhone/iPad/iPod user agent or `MacIntel` with touch points, i.e. WebKit on iOS/iPadOS; +1 elsewhere) and the per-hold resolver remains as a safety net; **One-Euro before the soft deadzone** (raw noise peaks no longer random-walk through the deadzone), the deadzone raised 1.5 → **2 °/s**, the One-Euro seeded at zero on engage, and `clampDt` treating a sub-1 `interval` as seconds. `air-pointer.ts` passes the detected sign. `tests/remote/air-math.spec.ts` (19 checks) is rewritten for the x/y/z order (flat, upright, 30° roll, landscape, degenerate roll, missing gravity), adds the platform sign detection with iPhone/Brave/iPad/iPadOS-desktop/Mac/Android user agents, the WebKit ground-pointing gravity with and without the seeded sign, the seconds-valued interval, a 10 s ±4 °/s jitter drift bound (< 5 px), and a replay of `tests/remote/fixtures/ios-flat-motions.json` (one-second windows of the real captures, rounded; still, turn left/right, tilt up/down) asserting the directions and that the stale "alpha about z" reading puts a tilt on the horizontal axis. A first version of the fix in this session had modelled the order as an iOS-only convention; a check of the specification and the Chromium/WebKit sources (prompted by the user's suggestion to look at open-source implementations) showed the order is universal, so that per-platform axis switch was removed before commit. Android remains unmeasured: its gravity sign (+9.8 face-up per the Android sensor documentation and Chromium's pass-through) is assumed, and the resolver covers a surprise.
- Commands/checks and actual results: first fix version `remote-unit` 21 passed, `npm run build` succeeded, `browser` passed with the installed companion quit (page-less 21, connection 5 + mouse 1, `air.spec.ts` 1); after the simplification `remote-unit` 19 passed, `npm run build` succeeded, and with the development shell stopped `browser` passed on the final source (page-less 19, connection 5 + mouse 1, `air.spec.ts` 1; its synthetic rotations were switched to the x/y/z order, which had correctly failed the spec first).
- Real-device evidence: the sensor captures above are physical-iPhone evidence for the conventions and noise. **Pointing with the corrected pipeline has not been tried on the phone yet.** The acceptance table (corners, precision, drift, taps, lift, grips, lifecycle, permission, drag) is still Pending.
- Decisions, deviations: the planned per-platform sign constant is the gravity sign (iOS −1 measured, others +1 per specification), the resolver stays for unmeasured platforms; the invert toggles remain as a user fallback. The plan's "deadzone → One-Euro" order was changed deliberately after the drift measurement. Raw samples stay in the session scratchpad only (not committed, not personal data: no identifiers beyond the generic user agent). Open-source survey (research agent, same day): gyronorm.js/FULLTILT (MIT) pass `rotationRate` through and FULLTILT still assumes alpha = z; `phonepad` (MIT, iPhone → macOS) uses the same gravity-projected yaw/pitch with a One-Euro filter, a 0.6 °/s soft deadzone and a 60–140 ms click "hush"; several small projects use raw `deviceorientation` deltas. Nothing found contradicts the measured conventions.
- Device verification, same day: the user quit the installed 0.4.0 companion; `python3 scripts/portable.py browser` then passed on the corrected bundle (page-less 21, connection 5 + mouse 1, `air.spec.ts` 1); the macOS development shell was started from this checkout (`portable.py desktop`, same data directory, Keychain access allowed for the development build, Accessibility granted to it, network approved, `/health` ready with input enabled). **Physical result:** with the corrected pipeline, flat grip, speed 1.0×, no invert: *"left goes left, right goes right, tilt up goes up, tilt down goes down"* (user's words). This confirms the iOS convention on the phone; feel and the acceptance table follow below.
- **Phase 4 results table (2026-10-15; physical iPhone, iOS 18.7, against the macOS development shell built from commit `995539d`'s source; PC sensitivity and Air speed at their defaults unless noted):**

  | Check | Result | Status |
  | --- | --- | --- |
  | Corner reach | Not run. The page recorded only quick selections of the Free/Corners/Precision/Drag buttons (10:48, 11:04) in a 563×726 Safari window and no completed test; a later reply reported circle 1 turning green but the page recorded no Corners selection, so it is not counted. | Pending |
  | Precision | Not run | Pending |
  | Rest drift | Live measurement not run. Replay of the phone's own recorded samples through the final pipeline: held still in hand 10 s → 5 px, flat on a surface 3 s → 0 px (shipped 0.4.0 order: 35 px / 18 px). | Pending live; replay passes |
  | Tap jitter | Not run | Pending |
  | Lift jitter | Not run | Pending |
  | Grips | Flat remote grip: "left goes left, right goes right, tilt up goes up, tilt down goes down" with the corrected build (user's words). Upright, 30° roll and landscape: not run (the lab captures labelled that way were made with the phone flat, and no landscape rotation was observed). | Flat done; others Pending |
  | Lifecycle | Not run (lock, background, tab switch, PC Pause, takeover, Wi-Fi drop) | Pending |
  | Permission | Lab page in Brave for iOS: `requestPermission` present, no samples before the tap, `granted` after it. The remote's own Enable motion flow, Safari tab vs Home Screen app, reload/relaunch and the deny recovery: not run. | Partial |
  | Drag | With the shipped 0.4.0 build at 10:16 the target page recorded one box dragged into the zone (Start drag + clutch + End drag, `pointerup`, 2.6 s hold), so the drag order works mechanically; not repeated with the corrected build. | Partial |
  | Speed feel | Not reported; gain constants unchanged (40 px/°, 1→2 curve) | Pending |

  Matrix: iPhone (lab in Brave; the browser used for the remote was not stated) × macOS companion from source: partial as above. iPhone × Windows 11 (both pointer-precision settings), iPhone × Ubuntu X11, Android × anything: **Pending**. Nothing was run in Safari or the Home Screen app explicitly.
- Session close-out: the development shell was stopped with SIGTERM (graceful Exit) and the installed 0.4.0 companion relaunched; the disposable lab/target servers were stopped and the lab certificate key deleted; the raw captures (sensor numbers only) and the target/lab pages were copied to the side-session notes folder outside the repository for a later session. Documentation got one honest line each in `docs/limitations.md` and `docs/troubleshooting.md` (0.4.0 reads the axes in the wrong order; corrected in source, unreleased) and a dated `docs/release-evidence.md` entry; README still describes the published 0.4.0 and is updated with the release that ships the fix.
- Handoff: (1) a release (0.4.1 or next minor) with the corrected pipeline needs separate authorization and the full AGENTS.md release list; (2) the remaining Phase 4 rows need a phone session of about 20 minutes with the target page (Corners → Drag), then grips, lifecycle and Safari/Home Screen permission; redo the lab captures with the grip-gated page for upright/rolled/landscape and the two roll motions (beta's sign is still inferred by elimination).
