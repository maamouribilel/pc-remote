# README media

## Current captures — 0.4.2, 2026-10-15

The README and [hands-on demo](../demo.md) use the new `v0.4.2/` directory. Older files below retain their original bytes and dates.

| Asset | Source |
| :--- | :--- |
| `v0.4.2/mobile-{touchpad,air,keyboard,navigation,media,pc}.png` | Actual 0.4.2 production-built browser remote in Playwright 1.64 / Microsoft Edge at 390 × 844 CSS pixels, 2× density; direct PNG screenshot bytes |
| `v0.4.2/desktop-remote.png` | The same paired remote at 1280 × 860 CSS pixels, 2× density |
| `v0.4.2/demo.gif` | 13 actual viewport screenshots at 390 × 844, framed with external captions on a 1100 × 900 canvas; 26.6 seconds, looping; no UI reconstruction or labels painted over the app |
| `v0.4.2/demo-preview.png` | Static full-color preview of the Air hold frame, with the same external captions |

Source baseline: `0ed707c` (0.4.2). Exact SDK 10.0.402, Node 24.21.0/npm 11.19.0 and locked frontend dependencies. The frontend passed TypeScript checking and a production Vite build. Browser plugin not available; capture used the project's pinned Playwright with installed Edge. Its exact browser version is recorded with the dated documentation-refresh entry in [release evidence](../release-evidence.md).

The existing `MouseBrowserFixture` now lives in `tests/PcController.PortableTests`. To leave the active companion on 8443 untouched, the capture used a disposable copy of Core/Host/PortableTests with **only the host port constant changed to 18443 in that temporary copy**. The current built `src/remote/dist` was copied unchanged. The fixture remained loopback-only, with temporary root/device storage, explicit test approval and recording input/PC adapters. No tracked runtime source, live companion, installed trust, production settings or browser profile was changed. The fixture was stopped and its temporary source/keys removed afterward.

Only the isolated capture browser ignores its temporary fixture's certificate warning, matching existing UI tests. This is not certificate-onboarding or strict-TLS acceptance. Production HTTPS validation is unchanged. Air receives **synthetic** `devicemotion` samples; its Ready/Pointing states establish UI behavior, not physical sensor feel, directions or permission-dialog acceptance. **Test player**, initial **42%** volume and the illustrated **36%** read-back are recording-fixture data, not a native app/audio session.

The capture paired a fresh context and checked Touch movement, explicit drag/release, Air hold/lift/settings, one composed Unicode text submission, navigation, VLC selection, volume read-back, test launcher selection (switching to Media), and dismissal of a sleep confirmation. Recording output included movement, held/released input, text length, navigation/media commands, `volume:36` and `launch:Test player`; **no power command** was dispatched. Page identity, meaningful content, no Vite error overlay, no console/page errors and no horizontal overflow were checked; additional 844 × 390 landscape checks passed.

All public frames were captured after the pairing fragment/form disappeared, with no browser chrome, address, QR, certificate, cookie, device record or local path visible. The animation is assembled from screenshots with a shared palette; captions sit beside the app. Historical Windows native images remain historical; no current macOS/Ubuntu companion screenshot or physical-phone capture was added.

### Re-capture safely

Use the production frontend build and the test-project-owned `MouseBrowserFixture`, temporary storage and a new isolated browser context. The normal fixture uses 8443 and requires companions closed; if one must stay running, build a disposable source copy with a separate loopback port and disclose that capture-only change. Never pair the capture browser with the production companion or install fixture trust. Follow the interactions in [the demo](../demo.md), verify recorded commands and zero power dispatch, and save only approved paired screens into a **new** version/date directory. Stop the fixture and remove its unique temporary source/storage after capture. Keep raw metadata, credentials, logs and temporary scripts outside tracked files.

## Historical captures — original notes

These assets were captured from PC Remote 0.2.0 on 2026-10-12.

| Asset | Source |
| --- | --- |
| `desktop-controls.jpg` | Actual installed Windows WPF companion, showing preferences without the pairing QR, LAN address, device names, or executable paths |
| `mobile-*.png` | Actual bundled browser UI in Microsoft Edge, at a 390 × 844 CSS-pixel portrait viewport and 2× pixel density |
| `desktop-remote.png` | The same browser application at a 1280 × 860 CSS-pixel viewport and 2× pixel density |
| `walkthrough.gif` | Actual browser screenshots assembled into a looping GIF, at 390 × 844 pixels |
| `mobile-air.png` | Actual bundled browser UI of version 0.4.0 in Playwright 1.64 Chromium on macOS, captured 2026-10-15 at a 390 × 844 CSS-pixel portrait viewport and 2× pixel density through the same loopback fixture. A page script dispatched zero-rate synthetic `devicemotion` events so the Air pad shows its Ready state; no physical gyroscope was involved |

The browser media uses the existing `MouseBrowserFixture` in `tests/PcController.Tests`, with isolated temporary storage and real loopback HTTPS/WSS on port 8443. That fixture approves its own demo browser and uses recording input/power adapters. The displayed **Test player** launcher entry and **42%** browser volume come from that fixture, not a claimed physical player/audio session. No production browser cookies, credentials, certificate keys, or active pairing links are included.

The walkthrough exercises actual touch gestures, drag/release, Unicode text submission, navigation, VLC profile selection, fixture launcher selection, and dismissing a power confirmation. Commands were checked against the server's recording adapters, and the dismissed confirmation produced no power command. Its screens were not reconstructed in HTML or altered into mockups. Browser console/page errors and horizontal overflow were checked during capture.

Browser plugin was not available; captures used the project's pinned Playwright/Edge tooling. Only the isolated loopback browser context ignores the fixture certificate warning, matching the existing browser tests. This is UI evidence; production certificate validation and iPhone trust are unchanged. The separate .NET integration/LAN checks validate certificate chains and names.

For a fresh capture, close all companions, run the project's isolated fixture, pair an isolated browser with its temporary challenge, navigate the real tabs, and save screenshots without exposing the pairing form or connection details. Shut down the fixture and remove its unique temporary storage when finished. Do not publish private fixture metadata or reuse screenshots containing live pairing links.

## Version note

These real captures were made from version 0.2.0. Version 0.2.1 changes server request limits and authorization; the pictured controls and layout are unchanged. Version 0.3.0 keeps the Windows companion and phone layout; the experimental macOS/Ubuntu Avalonia shell is not pictured. They are visual evidence, not proof of the newer security checks. The 0.4.0 Air pointer capture was taken from the 0.4.0 source on the pinned toolchain the same way; the other images were not re-captured and the pictured controls are otherwise unchanged. Version 0.4.1 changes only the Air pointer's motion math; the pictured Air mode is unchanged. Version 0.4.2 changes only the macOS companion's native network-change filter; nothing pictured changed.
