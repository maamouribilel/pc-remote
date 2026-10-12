# Milestone 5 — phone usability and Windows integration

> Publication note: `192.0.2.10` is a documentation-only replacement for the original LAN address. Use the live address shown by your companion.
Four tabs replace the long combined page. Connection state/RTT and quick profile-based play/pause, volume and Keyboard remain at the top; tabs stay at the bottom. Tab changes release held input and blur the phone keyboard. Panels scroll normally; only touchpad and physical-key surfaces suppress control gestures. All buttons/selects measure at least 44×44 CSS pixels in desktop tests. Dark text/accent contrast follows the existing palette.

The visual viewport sizes the app while Safari's keyboard reduces available height; safe-area insets protect landscape/notch edges. Very short viewports collapse quick actions while retaining status/tabs. Keyboard text is explicit Send with acknowledgements and no automatic replay. Real iPhone keyboard geometry and Safari gesture behavior require the physical checks below.

Manifest/icons are bundled locally. Source geometry is reproducibly rasterized by `scripts/generate-icons.ps1`; no external images or CDN are needed. Safari → Share → Add to Home Screen → Open as Web App if shown. The Home Screen browser can need its own approval. No service worker is registered and no authenticated response is cached. iOS background suspension is expected; reopen to reconnect.

The normal-user Windows app uses NotifyIcon with Open, Pause control, Resume control and Exit. X hides setup while the HTTPS server remains running. An explicit window Exit button is also available. Optional sign-in startup writes only this account's `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\PcController`, a quoted executable path with `--background`. It defaults off when absent. Build/run paths must remain installed at that location for startup to work.

A local per-user/session mutex selects one primary process. Secondary launch opens setup through a bounded, CurrentUserOnly named pipe; only Open/Pause/Resume/Exit are accepted. CLI flags `--pause`, `--resume`, `--exit` use the same handlers as the tray. No shell strings or LAN admin endpoint exist. `scripts/stop.ps1 -Configuration Release` waits for graceful exit before rebuilding.

Every two seconds, real Windows Private profiles/IPs are inspected; network events prompt an immediate refresh and PC suspend drops ownership. Changing addresses/profiles releases input and cancels ownership before disposing old listeners. Replacements serve only loopback and currently Private IPv4 addresses, renew matching server SANs and retain the per-installation root, devices and preferences. Pause is preserved. The phone must open the updated displayed address if DHCP changes its IP; an old bookmarked IP cannot discover the new one. The app never enables Public profiles or router forwarding. Failures show actionable setup messages and retry; no plaintext fallback exists.

## Independent checks — 2026-10-11

| Check | Result / limit |
| --- | --- |
| Release build | Zero warnings/errors; strict TS and pinned dependencies |
| Lifecycle suite | 126 checks: local IPC/startup, real verified loopback HTTPS rebind, release ordering, root/device/settings/pause preservation. Controlled snapshots do not prove live DHCP/profile switching. Startup registry preference restored exactly after tests. |
| Real desktop integration | 132 checks: actual WPF process singleton, pause/resume over verified HTTPS status, close-to-tray, restore/Open and graceful Exit. Physical tray click/phone-held cleanup still pending. |
| Pairing HTTPS/WSS | 144 checks, custom installation-root, server EKU and hostname validation |
| Private LAN HTTPS/WSS | 134 checks at 192.0.2.10 with IP-name validation |
| Browser flow | Approved real loopback WSS, touch/text/nav/media regressions, tab-switch cleanup, quick playback/keyboard, profile persistence, manifest/icon, one selected panel, 44-pixel controls, reduced viewport Send/status/tabs |
| Visual QA | 390×700 portrait, 844×390 landscape, 1280×800 desktop and 390×320 reduced viewport screenshots inspected; panels scroll under persistent chrome. No blank page, error overlay or unexpected runtime/console errors. |

Browser plugin not available; installed Edge/Playwright used. Browser tests explicitly bypass their temporary loopback certificate and use a recording input adapter/controlled approval. This is UI evidence only; production always uses trusted HTTPS/WSS and real SendInput, and .NET network suites retain certificate verification. Tests do not replace physical iPhone acceptance. The first compiler run exposed an Exit member-name collision and missing IO import; both fixed before checks passed.

Commands: `scripts/build.ps1 -Configuration Release`; with the companion closed, `scripts/test.ps1 -Lifecycle`, `scripts/test.ps1 -Pairing`, `scripts/test-browser-mouse.ps1`; with Release running, `scripts/test-desktop.ps1` (exits it), restart, then `scripts/test.ps1 -Lan`. Production run: `scripts/run.ps1 -Configuration Release`. Development output: `src/PcController.App/bin/Release/net10.0-windows/`; mobile: `src/remote/dist/` bundled into `wwwroot/`.

## Actual iPhone checks — passed by user report

Use iPhone 15 Pro / iOS 27 and the already trusted HTTPS address displayed by the PC.

1. Refresh Safari and reopen the Home Screen icon. In both, try all four tabs and landscape. Open Keyboard and the actual iPhone keyboard, type into the phone field, scroll to Send and send once into a new disposable Notepad tab. Status/tabs must remain usable. Verify switching away from Start drag or a lit modifier releases it on the PC.
2. In each browser, lock/unlock three times and switch away to another app. Reopen and verify secure remembered reconnect, reset drag/modifiers and no replay. Background input is not promised.
3. On PC, X closes setup but remote stays connected. Find PC Remote in notification tray (^ overflow if needed), right-click → Open. Start drag on a disposable target, then tray → Pause control; mouse must release and phone control disable. Resume, then repeat pause with lit Shift. Verify release and resume/reset.

Physical follow-up: user confirms all three check groups pass in both Safari and Home Screen, including actual keyboard/landscape/tab release, repeated interruption and real tray pause/release/resume. Milestone 5 acceptance is complete. Actual login/reboot startup, live network-address change, clean installation and final uninstall remain release verification work; Milestone 6 starts automatically.
