# Windows PC Controller — implementation plan

> Publication note: `192.0.2.10` is a documentation-only replacement for the original LAN address. Use the live address shown by your companion.
## Goal

Build a Windows desktop companion that lets an iPhone control the PC through a mobile web interface: touchpad, keyboard, large navigation keys, and media controls.

The first release works on the same private local network. The PC may use Ethernet and the iPhone Wi-Fi, provided the network allows the devices to reach each other. The user watches the PC screen while operating the phone.

## Instructions for the coding agent

1. Read this entire plan and any applicable `AGENTS.md` before starting.
2. Inspect the repository and installed tools. Preserve existing user work.
3. Implement the milestones in order. Update the checkboxes and implementation log as work progresses.
4. Complete each milestone's acceptance checks before proceeding. Record evidence, failures, and any checks requiring a physical iPhone.
5. Make routine implementation decisions independently. Ask only about missing requirements or a genuine blocker; do not ask for approval between every milestone.
6. Build working behavior throughout. Do not substitute mock controls or simulated connectivity for the actual Windows integration.
7. Keep dependencies small, use supported stable releases, and pin the versions selected during implementation.
8. Do not claim physical-device validation, successful input injection, or security properties without evidence.
9. Never commit credentials, pairing codes, private keys, device tokens, or build output.
10. Stop scope expansion until the first-release checklist passes. If physical-device testing needs the user, finish independent work and provide precise test instructions.

## First-release scope

### Windows companion

- Tray application with Open, Pause control, Resume control, and Exit actions.
- Setup window showing the connection address, QR code, pairing state, and connected device.
- Explicit approval of new devices on the PC.
- Settings for pointer sensitivity, scrolling, media profile, and optional startup at sign-in.
- List of paired devices with revoke-access actions.
- Clear feedback for firewall, network, certificate, and connection failures.

### iPhone remote

- Touchpad: relative pointer movement, tap to click, double-click, right-click, two-finger scrolling, and dragging.
- Keyboard: text entry and dedicated Enter, Backspace, Escape, Tab, Ctrl, Alt, Shift, and Windows controls.
- Navigation: large arrow pad, central Enter, Home, End, Page Up, and Page Down.
- Media: play/pause, volume up/down, mute, next/previous, seeking, and fullscreen shortcuts.
- Dark, readable interface with large touch targets and a bottom tab bar.
- Persistent connection indicator and quick access to play/pause, volume, and keyboard.
- Safari support and documented Add to Home Screen setup.

### Deferred features

Remote screen streaming, internet access, cloud accounts, Bluetooth, Wake-on-LAN, file transfer, clipboard synchronization, arbitrary shell commands, multiple simultaneous controllers, native iOS distribution, and control of the Windows lock screen or secure desktop.

## Architecture

```text
iPhone Safari / Home Screen web app
        |
        | HTTPS: web assets and pairing
        | WSS: authenticated control session
        v
Windows companion running in the signed-in user's session
        |
        +-- C# / .NET + WPF: window, settings, tray integration
        +-- ASP.NET Core: local web server and WebSocket endpoint
        +-- Input service: Windows SendInput + validated command mapping
        +-- Device store: protected credentials, settings, trust records
```

Use TypeScript and Vite for the mobile client. A lightweight vanilla TypeScript interface is sufficient; add a framework only if it provides a clear benefit. Bundle all assets locally so the remote does not depend on a CDN or internet connection.

Run the companion as a normal user process, not a Windows service. Keep input injection on the interactive desktop. Document that normal privileges cannot inject input into higher-privilege applications. Do not automatically elevate the whole application.

Suggested repository layout:

```text
src/
  PcController.App/          WPF host, tray, setup, application lifetime
  PcController.Core/         Protocol validation, sessions, input abstractions
  PcController.Windows/      Win32 input and Windows-specific integration
  remote/                   TypeScript mobile interface
tests/
  PcController.Tests/        Protocol, pairing, session, and input-service tests
  remote/                   Browser integration tests
scripts/                    Development, build, and packaging helpers
docs/                       Setup, troubleshooting, protocol, manual testing
IMPLEMENTATION_PLAN.md
README.md
```

Adjust the layout if an existing repository structure provides a better fit.

## Connection and security decisions

- The Windows companion serves the phone interface and control endpoint from the same origin.
- Ship HTTPS/WSS with a documented certificate trust strategy. A private LAN address does not automatically have a certificate trusted by iPhone.
- Investigate and validate a per-installation local certificate authority and server certificate with matching address/hostname names. Explain the required iPhone installation/trust steps and how to remove trust later. Protect signing keys on Windows and never send private keys to the phone.
- Do not treat clicking through a certificate warning as a completed transport solution. Validate trusted WSS and Home Screen operation on a physical iPhone before committing to the final onboarding flow.
- If this trust flow proves too cumbersome or unworkable, record the evidence and get the user's preference on the material tradeoff before changing the product architecture. Do not silently ship plaintext control traffic.
- A development-only insecure mode may exist behind an explicit flag for isolated testing. It must be disabled by default and excluded from normal release onboarding.
- Use an expiring, single-use pairing challenge plus approval on the PC. A QR link may carry a temporary challenge in its URL fragment; never place a reusable credential in a URL.
- Rate-limit pairing attempts. Use cryptographically random device credentials, protect them at rest, and support revocation.
- Prefer a Secure, HttpOnly, SameSite cookie for browser authentication. Validate the exact allowed Origin on HTTP mutations and WebSocket upgrades; reject unexpected or missing browser origins. Do not enable wildcard CORS.
- Permit one active controller. Make session takeover explicit. Revocation, pause, disconnect, timeout, and application exit must release held input.
- Limit message size, command rate, text size, and numeric ranges. Accept only defined actions; never expose an arbitrary command-execution endpoint.
- Limit Windows Firewall rules to private profiles and the necessary application/port. Do not enable router port forwarding.
- Do not log typed text, tokens, pairing challenges, cookies, or key material.

## Milestone 0 — establish the project and feasibility

- [x] Inspect available .NET, Node.js, package manager, Windows version, and repository state.
- [x] Select and record supported stable toolchain versions.
- [x] Create the solution, mobile project, `.gitignore`, and basic README.
- [x] Start the WPF application and host a simple mobile page locally.
- [x] Add a health endpoint containing only non-sensitive readiness information.
- [x] Validate iPhone reachability on the actual LAN, including Windows Firewall and Wi-Fi client isolation. User-observed Safari HTTPS/WSS succeeds on the Private LAN with the scoped firewall rule; one RTT reading is 11 ms.
- [x] Prototype trusted HTTPS/WSS and document certificate onboarding, address changes, and certificate renewal. Desktop custom-root and actual iPhone Safari/Home Screen trust pass without warning bypass.

Milestone 0 status (2026-10-11): **complete for its stated acceptance check**. User confirms actual iPhone Safari HTTPS/WSS ping/pong, 11 ms RTT, full certificate trust and no warning bypass. The PC is Private with its scoped firewall rule enabled and HTTPS listening at `192.0.2.10:8443`. Home Screen testing is still pending before committing the final onboarding flow; it is not marked passed and will be retained in the Milestone 5 physical-device matrix. Milestone 1 is starting automatically because the stated Milestone 0 acceptance prerequisite now passes.

Acceptance: the Windows app builds and opens; the actual iPhone loads the local page and exchanges a WebSocket ping/pong over the intended release transport. If no physical iPhone is available, record this gate as pending rather than passed.

## Milestone 1 — pairing and authenticated sessions

- [x] Display the current reachable connection address and QR code; provide a manual address fallback.
- [x] Implement expiring, single-use pairing and a PC approval dialog.
- [x] Add protected persistent device records and remembered browser authentication.
- [x] Reject input from unauthenticated, expired, or revoked sessions.
- [x] Implement heartbeat, disconnect detection, backoff-based reconnect, and explicit connection states.
- [x] Add device revocation and one-controller session ownership.
- [x] Document protocol versioning and supported message types in `docs/protocol.md`.

Suggested client messages: `pointer.move`, `pointer.button`, `pointer.scroll`, `key.down`, `key.up`, `text.insert`, `media.action`, `input.releaseAll`, and `ping`.

Every message should have a protocol version, sequence number, type, and validated payload. Server responses should identify the active session and expose actionable error codes. Do not retain an offline input queue or replay commands after reconnection.

Acceptance: a new device requires PC approval; a remembered device reconnects; expired pairing attempts fail; revoked devices lose access immediately; malformed messages cannot crash the host.

## Milestone 2 — actual mouse control

- [x] Implement a Windows input abstraction backed by `SendInput`, checking return values and native structure sizes.
- [x] Support relative pointer movement, left/right click, double-click, scroll, and explicit drag mode.
- [x] Implement touch gestures with clear rules that avoid accidental clicks during scrolling or dragging.
- [x] Batch pointer deltas per animation frame and handle backpressure by merging movement rather than building a stale queue.
- [x] Preserve ordering between movement and button transitions.
- [x] Add pointer sensitivity and scroll-direction settings.
- [x] Release pressed mouse buttons on touch cancellation, page hiding, session loss, watchdog timeout, pause, and exit.

Status (2026-10-11): complete. User confirms all actual iPhone mouse actions, cross-monitor dragging, and cleanup after both phone lock and Wi-Fi loss. Movement is subjectively smooth. Numeric latency remains unmeasured; do not infer it from RTT.

Acceptance: the phone moves the real Windows cursor, selects items, right-clicks, scrolls, and drags across a multi-monitor desktop. Disconnecting during a drag leaves no button held. Measure responsiveness on the actual network; aim for typical perceived latency below 100 ms, and document the measurement method rather than assuming the target is met.

## Milestone 3 — keyboard and navigation

- [x] Separate text insertion from physical key/shortcut events.
- [x] Start with an explicit text field and Send action for reliable iPhone typing; add live typing only after handling composition, autocorrect, and deletion correctly.
- [x] Support Unicode text, including accented text and surrogate pairs, without duplicate insertion.
- [x] Provide dedicated special keys and clearly visible modifier state.
- [x] Implement large arrow buttons and hold-to-repeat with bounded repeat rates.
- [x] Add common shortcuts such as Ctrl+C, Ctrl+V, Ctrl+A, Ctrl+Z, and Alt+Tab. Ctrl+V uses the PC's existing clipboard; it does not synchronize the phone clipboard.
- [x] Implement safe key release on touch cancellation, navigation, phone locking, connection loss, and session replacement.
- [x] Test keyboard layouts used by the user; include both AZERTY and QWERTY in the initial test matrix where available. User confirms actual US QWERTY target-app tests; AZERTY remains in the matrix as unavailable/unverified.

Status: complete. User confirms actual iPhone Notepad/browser text and key tests and interrupted-key release/reset under US QWERTY. Continue automatically to Milestone 4. AZERTY is unavailable/unverified, not falsely marked tested.

Acceptance: type ordinary and accented text in Notepad and a browser field; use arrows and shortcuts; interrupt a held arrow/modifier and verify Windows receives a release. Document applications where Unicode injection behaves differently.

## Milestone 4 — media and video profiles

- [x] Implement standard media actions: play/pause, next/previous, volume up/down, and mute.
- [x] Provide General, YouTube/browser, VLC, and the user-requested Stremio profile using verified application shortcuts.
- [x] Map seek and fullscreen actions per profile. Show the selected profile clearly.
- [x] Explain that profile shortcuts target the focused application and may require the video player to have focus.
- [x] Avoid universal seek-duration labels unless the selected application mapping guarantees that duration.
- [x] Keep media shortcuts declarative and allowlisted, without executable script strings.

Status: complete. User confirms actual iPhone actions in YouTube, VLC, Stremio and General, including matching labels and unsupported-action hiding. Milestone 5 starts automatically.

Acceptance: test playback, seeking, fullscreen, and volume in the supported apps. Confirm the UI labels match actual behavior and that unsupported actions are hidden or clearly unavailable.

## Milestone 5 — mobile usability and Windows integration

- [x] Build the final four-tab phone layout: Touchpad, Keyboard, Navigation, Media.
- [x] Use touch targets at least 44 by 44 CSS pixels, adequate spacing, and accessible contrast.
- [x] Account for iPhone safe areas, landscape orientation, and the on-screen keyboard reducing viewport height. Desktop reduced-viewport and actual Safari/Home Screen keyboard acceptance pass.
- [x] Prevent page scrolling and browser gesture interference only on control surfaces; preserve normal scrolling elsewhere.
- [x] Add web manifest, icons, and Home Screen instructions. No service worker or authenticated caching is used.
- [x] Restore the connection after phone lock/unlock or returning from another app. Do not promise background control while iOS suspends the page. Regression checks and repeated actual iPhone cycles pass.
- [x] Implement tray actions, launch-at-sign-in preference, single application instance, and graceful shutdown.
- [x] Show actionable network errors and update the address/QR code when the PC's network changes.

Status: complete. User confirms the final four-tab layout, landscape, actual keyboard/Send and tab-release work in both Safari and Home Screen; repeated lock/unlock and app-switch reconnect/reset pass; actual tray Open/Pause/Resume clears real mouse/key holds. Milestone 6 starts automatically.

Acceptance: the interface works in Safari and from the Home Screen, with the iPhone keyboard open and after repeated lock/unlock cycles. Pausing from the Windows tray immediately disables control and clears held input.

## Milestone 6 — testing and release packaging

- [x] Unit-test command validation, bounds, pairing expiry, one-time challenge consumption, revocation, session ownership, and input cleanup.
- [x] Integration-test authentication, origin rejection, invalid messages, rate limits, reconnects, and disconnect cleanup using a fake input adapter.
- [x] Browser-test touch gestures, navigation, responsive layout, and connection states. Emulation supplements physical iPhone testing; it does not replace it.
- [x] Run a deliberate real-input smoke test against a disposable Notepad window. Actual iPhone Notepad/browser Unicode and keys pass in Milestone 3; the user confirms packaged 0.1.2 mouse/Unicode and release/reset in a new unsaved Notepad tab. Automated native tests use guarded disposable targets.
- [x] Publish a self-contained Windows build for the user's architecture, including built mobile assets.
- [x] Create an installer and uninstall flow. Application-owned startup/firewall removal and certificate cleanup are implemented/documented; actual manual uninstall/admin rule validation remains pending.
- [ ] Test installation on a clean Windows account or VM, and document any unsigned installer warnings accurately.
- [x] Write setup, usage, troubleshooting, limitations, and manual-test documentation.
- [x] Record release build commands and output paths in the README.

Status: in progress. Current-account installer and bundled-runtime operation pass. The user confirms redesigned Safari/Home Screen and packaged 0.1.2 mouse/text/Stremio controls with input release/reset. Physical revocation, reboot/sign-in and manual uninstall remain acceptance gates. Clean-account/VM is unavailable by user report and remains unchecked. See docs/release-evidence.md and docs/release-checklist.md.

Acceptance: install the packaged build, pair the iPhone, complete all core controls, restart the PC, reconnect, revoke the device, and uninstall successfully. Verify that normal operation needs neither Node.js nor the .NET SDK installed on the user's PC.

## Required failure cases

| Scenario | Expected behavior |
| --- | --- |
| Phone locks while a key/button is held | Server watchdog releases held input; no replay after reconnect |
| PC sleeps or changes network | Remote shows disconnected and reconnects when reachable; setup address is refreshed |
| Wi-Fi blocks communication between devices | Setup provides specific troubleshooting without claiming the app is connected |
| Second phone requests control | Existing session ownership is respected; takeover requires explicit action |
| Paired device is revoked | Connection closes and future authentication fails |
| Certificate is untrusted or address does not match | Setup identifies the transport problem; release does not silently fall back to HTTP |
| Target app is elevated | Documented limitation and useful feedback where detectable; no automatic elevation |
| Wrong media profile or player lacks focus | User can identify and correct profile/focus; no promise of universal shortcuts |
| Invalid or excessive messages arrive | Reject or throttle safely without crashing or injecting unintended input |

## First-release completion checklist

- [x] Windows build and installer are reproducible. Repeated pinned-toolchain Release/package and current-account install/update pass; no byte-identical archive/installer claim.
- [x] Release connection is encrypted, authenticated, and approved by the PC user. Production transport passes actual trusted iPhone and validated .NET integration tests; installed 0.1.2 LAN verification passes with the preserved CA and paired-device records.
- [x] Real iPhone can control mouse, keyboard, arrows, and supported media apps. Actual results pass in Milestones 2–5; the user also confirms packaged 0.1.2 mouse/text/Stremio and redesigned controls/release-reset.
- [ ] Phone lock, connection loss, and revocation never leave held input behind.
- [x] No cloud service or internet connection is required during normal LAN use. All control assets/runtime are bundled and communication is LAN-only; streaming media content itself can require internet.
- [x] Actual-device results and any remaining limitations are documented.
- [ ] No simulated controls, unresolved critical defects, or embedded secrets ship.

## Suggested prompt to start implementation

> Read IMPLEMENTATION_PLAN.md and applicable repository instructions. Implement this application milestone by milestone, starting with repository inspection and Milestone 0. Update the plan with progress and test evidence. Continue through independent work without asking for routine confirmations. Do not mark physical iPhone checks complete unless actually verified. If device access or a material architecture decision blocks progress, explain the exact issue and the smallest user action needed. Keep the first release focused on reliable local mouse, keyboard, arrow, and media control.

## Implementation log

Append one entry per milestone:

```text
Milestone:
Status: not started / in progress / blocked / complete
Changes:
Commands and checks run:
Results:
Physical-device checks still pending:
Decisions or deviations and reasons:
Next action:
```

### Milestone 0 — 2026-10-11

Status: complete — independent implementation verified and user confirms physical iPhone trusted Safari/WSS acceptance. Home Screen validation remains pending before final onboarding.

Changes:
- Preserved the original plan. No applicable AGENTS.md and no existing Git checkout were found; the initial repository held only this file.
- Created .NET Core/Windows/WPF projects and solution, strict vanilla TypeScript/Vite mobile page, gitignore, README, reproducible portable tool bootstrap/build/run/test scripts and scoped firewall/trust helpers.
- Implemented real HTTPS/WSS connection diagnostics, non-sensitive `/health`, exact Host/Origin checks, schema/sequence/message/rate/concurrency bounds, timeout and reconnect behavior. No input endpoint or simulated controls exist.
- Created installation-specific CA/server certificates, DPAPI-protected persistence, restricted certificate-directory ACL, public iPhone profile export, SAN/address regeneration and startup leaf renewal. Restricted LAN listeners to Private IPv4 networks and TLS to 1.2/1.3.
- Documented iPhone onboarding, trust removal, failure troubleshooting, protocol, design reference and test evidence.

Commands and checks run:
- Inspected tools, Windows version, permissions, interfaces and firewall profiles; verified current official tool/release documentation and archive checksums.
- `scripts/install-tools.ps1`: .NET SDK 10.0.401/runtime 10.0.12, Node 24.21.0/npm 11.19.0; exact Vite 8.3.4, TypeScript 7.0.2, Playwright 1.64.0 with HTTPS registry/lockfile.
- Debug and Release solution builds: zero warnings/errors. `scripts/run.ps1 -Configuration Release`: actual WPF setup window opened, HTTPS listener verified.
- `scripts/test.ps1 -Integration`: 35 checks pass against real HTTPS/WSS with custom-root chain and hostname verification, including rejection and rate limiting. Four Edge browser tests pass across portrait/landscape/desktop and network-loss recovery. Earlier browser runs used normal TLS validation; final UI-only QA uses explicit loopback test-context TLS bypass after Windows root re-import failed to persist. This is not certificate onboarding evidence; see `docs/milestone-0-evidence.md`.
- Temporary Windows CurrentUser CA trust installed only for browser QA and removed afterwards. No iPhone trust or firewall rule was silently installed.

Results:
- Independent Milestone 0 work is ready. Release prototype output is `src/PcController.App/bin/Release/net10.0-windows/`; mobile build is `src/remote/dist/` and bundled `wwwroot/`. Normal development launch uses the ignored portable runtime.
- Fixed toolchain path/registry, compiler, Schannel key-loading, HTTP/2 WebSocket upgrade, CSP and favicon failures before final checks. See `docs/milestone-0-evidence.md` for evidence and limits.
- Current network: Wi-Fi 3, 192.0.2.10, Public; app serves only 127.0.0.1:8443. User confirmed trusted home LAN and availability of a physical iPhone.

Physical-device checks still pending:
- Private profile / scoped firewall setup verified; user-observed iPhone Safari HTTPS/WSS ping/pong succeeds. Remaining: explicit certificate full-trust/no-warning confirmation, Home Screen operation, additional RTT readings and lock/unlock recovery.

Decisions or deviations and reasons:
- Kept Milestones 1–6 unstarted to respect the explicit Milestone 0 acceptance prerequisite. Authentication and real input must follow the physical encrypted-transport feasibility check.
- Installed checksum-verified toolchains locally rather than changing global tools. Selected current stable .NET 10 LTS and Node 24 LTS.
- In-app browser rejected the private CA even after Windows trust; used installed Edge/Playwright for desktop QA. Optional Windows root re-import retention remains unresolved; the helper detects failure, and final UI-only tests use a documented explicit test-context bypass. Real .NET HTTPS/WSS tests retain root/hostname verification. None of these establishes iPhone compatibility.
- Certificate trust strategy remains a prototype. No insecure mode/fallback or architecture change was introduced. Address refresh requires restart until Milestone 5.

Next action:
- User completes `docs/iphone-milestone-0.md` and reports physical-device results. After the gate passes, automatically continue Milestone 1 pairing/approval, protected authentication and session ownership.

Physical-device attempt follow-up — 2026-10-11:
- User reports Safari cannot open the page because it could not connect to the server. This is a failed reachability attempt; the iPhone acceptance gate remains pending.
- Live PC inspection confirms the companion is running but listens only on `127.0.0.1:8443`; Wi-Fi 3 / `192.0.2.10` is still Public, and `PcController-Private-TLS-8443` is not installed. LAN reachability cannot pass in this state.
- Required user action in administrator PowerShell: change the previously confirmed trusted home interface to Private and run `scripts/firewall.ps1 -Action Install`. Then close/restart the unelevated companion using `scripts/run.ps1 -Configuration Release` and open the displayed LAN address on the iPhone. `localhost`/`127.0.0.1` on the phone refers to the phone, not this PC.
- Updated the device checklist to select the verified Release build and explain the loopback-address failure. No firewall/network settings or certificate-validation policy changed automatically.

LAN startup fix and successful iPhone attempt — 2026-10-11:
- On the user's request to execute the setup commands, live inspection found the network already Private and the named firewall rule enabled for TCP 8443, Private profiles, LocalSubnet. No additional elevation or network changes were required.
- Closed the companion normally and restarted the Release build. The newly exercised Private-interface path failed because dynamic COM automation could not marshal the native adapter GUID returned by `GetAdapterId`.
- Fixed that call using the SDK's typed `INetworkConnection` dual interface and GUID out parameter, preserving Private-only selection and safe loopback fallback. Added a `--network` diagnostic option and `scripts/test.ps1 -Lan` to exercise the real interface and certificate IP SAN.
- Rebuilt Release successfully with zero warnings/errors. Restarted the companion normally; verified listeners on `192.0.2.10:8443` and `127.0.0.1:8443`. All 35 checks pass against real HTTPS/WSS at the LAN IP with certificate chain and IP-name validation enabled. This PC-originated test alone did not claim phone reachability.
- User then reported seeing “connected securely” and “11ms” on the iPhone. Record as user-observed physical Safari/WSS ping/pong success, one 11 ms diagnostic RTT sample; not Windows input latency or a typical-network latency measurement.
- Still need explicit confirmation of full certificate trust/no warning bypass and a successful Home Screen connection before finalizing this feasibility gate and continuing Milestone 1. Phone model/iOS version and lock/unlock evidence remain to be recorded.

Trusted Safari confirmation — 2026-10-11:
- User explicitly confirms full root trust enabled and Safari connects without bypassing a certificate warning. The actual iPhone page and WSS ping/pong therefore satisfy Milestone 0's stated acceptance check.
- User has not yet tested Home Screen. Keep that check pending under the architecture's final-onboarding requirement and Milestone 5 acceptance; do not infer a pass or freeze the final onboarding flow.
- Continue automatically with Milestone 1's independent pairing/authentication/session implementation. Earlier progress notes conservatively treated Home Screen as a Milestone 0 blocking gate; the plan's explicit Milestone 0 acceptance requires the actual trusted page and ping/pong, while Home Screen must pass before final onboarding is committed.

### Milestone 1 — 2026-10-11

Status: complete for automated acceptance; physical phone pairing/PC-dialog/reconnect confirmation requested and still pending.

Changes:
- Added locally generated QRCoder 1.8.0 QR with pinned NuGet lockfile, manual link/challenge fallback, two-minute single-use challenges, bounded attempts, actual PC approve/deny window with expiry, and protected persistent device records.
- Remembered credentials are random 256-bit values sent only in Secure/HttpOnly/Strict host cookies; only hashes persist within DPAPI-protected data. Every mutation/upgrade enforces exact Origin.
- Added device revocation, pause/resume and one-controller ownership with explicit takeover; release hooks run before replacement. Authenticated control accepts only validated ping until the input milestones.
- Phone shows unpaired, pending approval, authenticated, busy, paused and disconnected states; it strips URL fragments and reconnects without replay. Updated protocol, README and device checklist.

Commands and checks run:
- Release build: zero warnings/errors. `scripts/test.ps1 -Pairing`: 64 checks pass, including real Kestrel HTTPS/WSS certificate chain/hostname validation, pending approval, cookie protections, challenge replay rejection, remembered reconnect, ownership and immediate revocation. Test-only controlled approval callback does not replace production's WPF dialog and is not shipped as an auto-approval endpoint.
- `scripts/test.ps1 -Lan -Browser`: 54 core/transport checks and five Edge tests pass across portrait/landscape/desktop, offline recovery and expired pairing feedback. Browser plugin not available; Edge/Playwright used. Explicit test-context TLS bypass is UI evidence only; LAN .NET tests retain full chain/IP-name checks.

Results:
- Automated Milestone 1 acceptance passes: no authentication before approval, remembered reconnect, expired pairing refusal, immediate revoked-session loss and safe malformed request rejection. Production WPF approval wiring is implemented; actual user interaction is a separate pending device check.
- No Windows input was injected. Release build remains running at the trusted Private LAN address for the user's physical pairing test.

Physical-device checks still pending:
- New QR/request/PC approval and Safari refresh without another approval; actual-device revoke/pause/takeover; Home Screen trust and lock/unlock. Exact steps supplied asynchronously and in docs/milestone-1-evidence.md. Do not infer them from desktop tests.

Decisions or deviations and reasons:
- Maintain a separate ping-only unauthenticated diagnostics endpoint for transport troubleshooting. It never authorizes control.
- Home Screen check remains a final-onboarding prerequisite, not falsely completed. Input cleanup hook is tested for order/cancellation now; actual SendInput release verification belongs to Milestone 2.

Next action:
- Begin Milestone 2 actual Windows mouse integration, gesture rules, bounds, settings and cleanup. Its physical cursor/drag/multi-monitor acceptance must pass before implementing Milestone 3.
### Milestone 2 — 2026-10-11

Status: in progress — implemented, independent checks pass, physical acceptance mostly confirmed; cross-monitor/precise interruption result pending.

Changes:
- Real Windows SendInput adapter, native ABI/return-count checks, bounded move/button/scroll/release messages, fractional sensitivity and persisted natural-scroll settings.
- Touchpad gestures, click/right/double and explicit drag, per-frame bounded aggregation, transition ordering and cancellation. No offline replay.
- Ownership release hooks on pause/revoke/takeover/disconnect/exit; 1200 ms held-input lease inspected every 100 ms with 250 ms phone drag heartbeat. Release failure blocks replacement, pauses control and retries cleanup.

Commands and checks run:
- Release build: zero warnings/errors, strict TS/Vite passes.
- `scripts/test.ps1 -MouseSmoke`: 70 checks pass, including real disposable-window movement/click/scroll/drag and actual timer release verified with GetAsyncKeyState.
- `scripts/test.ps1 -Pairing`: 85 checks pass, including certificate-validated real HTTPS/WSS and revocation during a held button using the test recording adapter.
- `scripts/test-browser-mouse.ps1`: real WSS pairing/reload/gestures/order/cancellation/offline cleanup passes. Three viewport screenshots visually inspected. Browser plugin absent; Edge/Playwright used. Explicit browser TLS bypass is UI evidence only. Fixture listens only on loopback and is not a production auto-approval mode.
- Fixed WPF test-harness IO/HTTP build failure and stale browser touch coordinates before passing. Production uses the real adapter; recording adapters only isolate automated network/UI tests from arbitrary focused apps.

Results:
- User confirms all actual PC mouse actions work from the iPhone, gives a positive interrupted-drag result, and reports iPhone 15 Pro / iOS 27 with smooth movement.
- Read-only PC inspection finds two active monitors. Requested explicit cross-monitor drag and which interruption method passed. No quantitative latency claim.
- See docs/milestone-2-evidence.md for commands, output paths, device instructions and limits. Final four-tab ergonomics remain Milestone 5.

Physical checks still pending:
- Cross-monitor drag and exact phone-lock/Wi-Fi-loss result; detailed pause/revoke/exit repetitions and numeric finger-to-cursor measurement remain release evidence work. Earlier trusted Home Screen and remembered pairing passed separately.

Next action:
- Complete the pending Milestone 2 acceptance detail, fix any failure, then automatically begin Milestone 3 keyboard/navigation. Continue independent documentation/regression work while awaiting the answer.

Milestone 2 physical follow-up — 2026-10-11:
- User explicitly confirms cross-monitor drag works and both phone lock and Wi-Fi-loss cleanup passed. The actual-device acceptance gate passes; automatically begin Milestone 3.
- Preserve subjective smoothness and unmeasured numeric latency as separate evidence. Detailed pause/revoke/exit repetitions remain in final release testing.

### Milestone 3 — 2026-10-11

Status: in progress — implementation/native/browser checks pass, physical acceptance requested and pending.

Changes:
- Separate Unicode text packets and allowlisted physical keys/chords; explicit Send with composition protection, acknowledgement, delivery-uncertain feedback and no replay.
- Dedicated keys, visible modifiers, navigation pad, 8/sec phone repeat with server bounds, declared Ctrl+C/V/A/Z and Alt+Tab. Clipboard remains entirely on the PC.
- Shared held-key/button tracking and watchdog; safe cleanup on cancellation, visibility, ownership loss and failure. Renamed shared adapters/controllers to reflect both mouse and keyboard.

Commands and checks run:
- Release build: zero warnings/errors; strict TS/Vite passes.
- `scripts/test.ps1 -KeyboardSmoke`: 83 checks pass including actual Unicode accent/emoji single insertion, multiline text, navigation, Ctrl+A, Shift selection, Backspace and real timer release of Control in a guarded disposable WPF field.
- Browser WSS gesture suite additionally passes composition/Send acknowledgement, modifier feedback, shortcut, hold repeat and offline cleanup with both mouse and Shift held. Recording fixture is test-only; explicit browser TLS bypass is UI evidence only.
- Registry and actual target thread identify current US QWERTY (0409); AZERTY is recorded as unavailable/unverified, not installed implicitly.

Physical checks still pending:
- Notepad and browser field text/keys on actual iPhone; interrupted key/modifier release and user's layout. Exact steps supplied asynchronously and in docs/milestone-3-evidence.md.

Next action:
- Complete these checks, resolve failures, then automatically start Milestone 4 media profiles. Final layout/tray/packaging remain later milestones.

Milestone 3 physical follow-up — 2026-10-11:
- User explicitly confirms both Notepad and browser text/key checks pass, including one-time accented/emoji Send, navigation and tested shortcuts.
- User confirms held key/modifier release and reconnect reset, using US QWERTY. Milestone 3 acceptance passes; begin Milestone 4.
- Preserve AZERTY as unavailable/unverified and application compatibility limits in the release matrix.

### Milestone 4 — 2026-10-11

Status: in progress — implemented and independent checks pass; physical supported-player acceptance pending.

Changes:
- General Windows media keys and app-specific YouTube/VLC allowlisted chords; persisted PC/phone profile, clear focus/playlist caveats, hidden General unsupported actions and duration-free seek labels.
- Explicit profile per action prevents silent mapping changes during selection races. Setting changes require an authenticated owner and exact Origin. Ten media actions/sec, checked native batches and shared cleanup.
- Held-input clock is now monotonic so PC wall-clock changes cannot prolong button/key leases.

Commands and checks run:
- Official YouTube, VideoLAN 3.0 and Windows shortcut/key references checked. Installed VLC version 3.0.24 inspected; playback is not inferred from documentation.
- Release build zero warnings/errors; strict TS/Vite passes.
- `scripts/test.ps1 -KeyboardSmoke`: 114 checks pass, including media allowlists/maps/rate/persistence and actual K / Shift+Left chords in a guarded disposable field, plus prior native keyboard cleanup.
- Browser WSS suite passes profile selection/persistence, profile-specific commands, unsupported-action hiding and keyboard/mouse/cleanup regressions. Fixed a test timing issue that counted a prior release rather than the network-loss release; now waits for held input and observes disconnection.

Physical checks pending:
- Actual YouTube playlist, VLC playback/seek/fullscreen/volume and General system media/volume. Exact steps in docs/milestone-4-evidence.md; do not mark app behavior passed from native/recording tests.

Next action:
- Resolve actual supported-app results, then automatically begin Milestone 5 final phone usability, tray/startup/single instance and network refresh.

Milestone 4 supported-player follow-up — 2026-10-11:
- User confirms the actual YouTube and VLC actions match their labels. The General question was answered with a request to control Stremio; do not treat it as a General pass.
- Inspected installed Stremio 5.0.26 and its official shortcut reference/source. Added an allowlisted Stremio profile: Space, Shift+N (next episode), volume arrows, M, seek arrows and F. No previous-episode shortcut is published; both protocol validation and the phone hide/reject that action. Existing Touchpad/Keyboard/Navigation support browsing and search without adding shell commands or application automation.
- Fixed the observed arrow-pad placement so Up/Down align with central Enter and Left/Right sit beside it. Added browser geometry checks and visually inspected the portrait result.
- Release build: zero warnings/errors. Pairing integration: 136 checks pass with verified HTTPS/WSS. Browser WSS flow passes including Stremio selection, persistence, dispatch, unsupported-action hiding, arrow-pad geometry and prior cleanup regressions. Native keyboard smoke: 126 checks pass, including actual Shift+N and Left in the disposable field. First native attempt aborted safely on lost focus; rerun passed with the guard intact. LAN transport: 126 checks pass with certificate chain and IP-name validation.
- The updated Release companion is running at https://192.0.2.10:8443/ with existing trust and device records preserved. Exact Stremio and General iPhone checks requested asynchronously. Milestone 5 implementation waits for this milestone's acceptance gate.

Milestone 4 final physical follow-up — 2026-10-11:
- User explicitly confirms all Stremio actions and General actions work on the actual iPhone. All supported-player acceptance checks pass; begin Milestone 5 automatically.

### Milestone 5 — 2026-10-11

Status: implemented; independent checks pass, physical acceptance pending.

Changes:
- Four accessible tabs, persistent compact status/RTT, quick profile-based playback/volume/keyboard, release on tab change, normal panel scrolling, safe-area padding and visual viewport sizing. Manifest and locally rendered monitor icons; no service worker, credentials in manifest or authenticated cache.
- Real Windows notification tray Open/Pause/Resume/Exit, close-to-tray behavior, explicit Exit button, optional quoted HKCU Run entry for background sign-in, and a same-user/session mutex with CurrentUserOnly named-pipe allowlisted local commands. `scripts/stop.ps1` shuts down gracefully; no network command-execution endpoint was added.
- Two-second network-profile/address inspection plus address/availability/resume events; suspend releases ownership. Refresh releases/cancels input and stops old HTTPS listeners before replacing them, reissues matching leaf SANs, updates address/QR and retains CA/devices/preferences/pause. Shared native adapter retains tracked input across refresh failures. Failure retries remain HTTPS-only and Private-only.

Evidence:
- Release build zero warnings/errors. Fixed an Application.Exit member-name collision and missing IO import before passing.
- `scripts/test.ps1 -Lifecycle`: 126 checks pass including real same-user IPC, optional startup write/remove with the user's original registry value restored, loopback listener replacement over certificate-validated HTTPS, release ordering, remembered auth/root/settings and pause preservation. Controlled snapshots exercise rebind; they do not claim actual DHCP/profile-switch testing.
- `scripts/test-desktop.ps1`: 132 checks pass against the real Release WPF/tray process: second launch reuses it, local pause/resume reflected over verified HTTPS, X hides setup while the listener remains live, Open restores it, Exit closes gracefully. Tray menu shares these handlers; actual menu click with phone-held input remains a physical check.
- `scripts/test.ps1 -Pairing`: 144 checks pass over validated HTTPS/WSS. `scripts/test.ps1 -Lan`: 134 checks pass with root/leaf/EKU/IP-name validation.
- Browser WSS flow passes all prior control/cleanup regressions plus tab release, quick controls, profile persistence, 44-pixel targets, manifest/icon serving, four tabs at 390×700, 844×390 and 1280×800, and a 390×320 reduced viewport. Screenshots visually inspected. Browser plugin not available; installed Edge/Playwright used with explicit loopback UI-only TLS bypass and test recording adapter. Actual Safari keyboard behavior is not inferred from emulation.

Physical checks pending:
- Safari and Home Screen four tabs, landscape and actual iPhone keyboard/Send scrolling; tab-switch release; three lock/unlock cycles and app switching; tray Open/Pause/Resume clearing real drag and Shift. Exact steps requested asynchronously and recorded in docs/milestone-5-evidence.md.
- Updated Release app runs at https://192.0.2.10:8443/ with prior pairing and root trust preserved. Next action: resolve physical results, then automatically begin Milestone 6 release verification/packaging.

Milestone 5 physical follow-up — 2026-10-11:
- User confirms both Safari and Home Screen usability, landscape, actual iPhone keyboard/Send scrolling and tab-switch release.
- User confirms both browsers reconnect/reset after three lock/unlock cycles and app switching, without reapproval or replay.
- User confirms actual Windows tray Open, pause/release and resume with held mouse and Shift. Acceptance passes; begin Milestone 6.

### Milestone 6 — 2026-10-11

Status: implementation complete; final acceptance in progress. Self-contained x64 app, Inno per-user installer/portable ZIP/hashes, program-specific Private LocalSubnet firewall helper, owned startup/firewall uninstall and product documentation are implemented. The short exclusive firewall helper can elevate; normal server/input/tray stay unelevated. User data/CA/devices are retained for reinstall; explicit trust retirement is documented.

Evidence:
- Release build zero warnings/errors and strict TS/Vite passes; self-contained win-x64 publish includes .NET/ASP.NET/Windows Desktop 10.0.12 and local assets. Official Inno 7.1.0 download hash/Authenticode verified; rejected NSIS HTML download was never executed.
- Pairing integration: 154 checks pass, including authenticated malformed/binary/oversized/replayed/rate-excess frames and release/cancellation, remembered reconnect and revoke over verified HTTPS/WSS with test-only recording input.
- Installer exit 0 on this account. Start menu/HKCU uninstall registration created. Installed app launched with DOTNET_ROOT variables removed and PATH limited to Windows; actual coreclr.dll loads from its own install directory. No globally installed .NET 10 is present. Silent test skipped administrator firewall setup.
- Installed desktop suite: 133 checks pass singleton, pause/resume over verified HTTPS, X-to-tray/Open and graceful Exit. Local Exit acknowledgement includes primary PID and the helper waits for actual process exit before file operations; regression passes. Installed LAN transport: 134 passes.
- Real unelevated firewall absent-rule inspection exposed HRESULT 0x80070002 mapped to FileNotFoundException rather than COMException. Fixed both install/remove lookup paths; 120 default checks pass including actual missing-rule COM regression. Administrator rule creation/removal remains a manual test.
- Published/installed executable, app/input assembly and mobile files match; installer/ZIP SHA-256 hashes validate; 636 published files contain no .protected/PFX/private-key/certificate/profile data. Runtime/QRCoder license notices are present.
- Scripted uninstall/test registry and deletion command rejected by automatic approval review, with only "blocked by policy" given. None of those actions executed. Manual Settings uninstall/startup/firewall checks are required.
- User confirms only this Windows account is available. Clean-account/VM check remains unavailable/unverified; do not infer it from bundled-runtime evidence.
- README/setup/troubleshooting/limits/manual checklist/certificate/protocol docs updated; release includes runtime/QRCoder license notices. Previous build outputs and protected user data preserved.

Final package/installed follow-up:
- Output: artifacts/release-0.1.0-20261011-212255, containing self-contained directory, installer, portable ZIP, SHA256SUMS.txt and bundled setup/certificate/troubleshooting/manual/license docs. Latest pointer: artifacts/latest-release.txt. Installed executable: %LOCALAPPDATA%/Programs/PCRemote/PcController.App.exe.
- Final current-account install/update exit 0. Installed desktop: 134 checks pass; verified Private LAN: 135 checks pass. Previous update returned exit 5 while the old firewall-helper dialog locked runtime files; Inno rolled back. User closed dialogs and retry passed without force-killing processes. Close helper dialogs before update/uninstall.
- Corrected app runs at https://192.0.2.10:8443/ with bundled runtime and preserved trust/devices. Exact actual iPhone controls/Notepad/firewall and drag/Shift revocation tests requested asynchronously. Reboot/sign-in and manual uninstall/reinstall remain required; clean-account/VM stays unavailable/unverified.

Final full-directory comparison: all 636 installed published files match, and installer/ZIP SHA-256 hashes validate. Exact reboot/sign-in and Settings uninstall/reinstall steps also supplied asynchronously, including saved-work reminder, scoped firewall cleanup prompt, final installer path, and retained trust/device reconnect check.

Next action: record actual packaged iPhone/firewall/revoke, reboot/sign-in and manual uninstall/reinstall results; fix any failure before Milestone 6 acceptance. Independent implementation/build/regression work is complete; unavailable clean-account/VM and pending physical checks are not marked passed.

Milestone 6 installer/firewall follow-up — 2026-10-11:
- User supplied an actual installer firewall-error screenshot. Installed app/input assembly hashes matched the older release-0.1.0-20261011-210630 package, which predates the absent-rule FileNotFoundException fix. Do not count that installation as a pass or infer an exact exception from the generic dialog.
- Published distinct version 0.1.1 to avoid confusing the earlier 0.1.0 installers. PC setup title now shows its assembly version, and firewall errors include a safe Windows error code/type without payloads or credentials. Updated README/manual installer filenames.
- Release build: zero warnings/errors. Default suite: 120 checks pass, including actual unelevated absent-rule lookup. Installed desktop: 134 checks pass, including full-teardown Exit; verified Private-LAN transport: 135 checks pass.
- First update attempt returned exit 4 with an Inno EFileError while saving uninstall information; rollback ran. Retrying installation returned exit 0. A subsequent repeat 0.1.1 install/update also returned exit 0. Logs: %TEMP%/pc-remote-0.1.1-install.log, pc-remote-0.1.1-install-retry.log and pc-remote-0.1.1-upgrade.log. Initial uninstall-log error is recorded as an observed transient; no clean-account claim.
- Actual 0.1.1 firewall helper ran with Windows administrator approval and successfully created PcRemote-Private-ECCF210EC3A01EFD. Read-back confirms enabled Allow/Inbound, THIS installed executable, TCP 8443, Private only and LocalSubnet. Developer rule is preserved. This completes real administrator rule-creation validation, not rule-removal/uninstall validation.
- Current output: artifacts/release-0.1.1-20261011-213623 (installer, ZIP, self-contained directory and SHA256SUMS.txt). All published/installed files match and both artifact hashes validate. Companion is running as the normal user with title PC Remote 0.1.1 at https://192.0.2.10:8443/. Protected certificate/device data is outside installer-owned files.
- Use ONLY the current 0.1.1 installer for further reinstall checks; the earlier asynchronous 0.1.0 reinstall path is superseded. Final packaged iPhone input/physical revoke, reboot/sign-in, manual uninstall/removal and clean-account/VM remain pending/unavailable as recorded.

## Reference documentation

### Approved UI redesign — 2026-10-12

User explicitly approved the four-screen visual concept and requested implementation. This updates the Milestone 5 interface while retaining the Milestone 6 release gates.

- [x] Record the approved colors, typography, components, labels and responsive requirements in docs/ui-design.md; preserve original UI sources in a uniquely named temporary backup.
- [x] Implement graphite/tactile phone controls, large dotted touchpad, compact status/gear, local SVG icons, modifier/drag states, keyboard composer, directional pad and media layout.
- [x] Preserve quick keyboard access and all existing shortcuts, player allowlists, real input, authentication and encrypted network behavior.
- [x] Move phone onboarding/RTT/help to an accessible connection dialog; release held input on opening/closing it. Keep a shared fixed Release input control and tab dock.
- [x] Implement matching WPF pairing/control/device dashboard, dark controls/approval window, retained certificate/network setup and preferences, and native dark Windows 11 title bars.
- [x] Pass strict TS/Vite and Release solution build (zero warnings/errors); 120 core, 155 verified-TLS pairing, 130 actual native mouse and 135 actual native keyboard checks.
- [x] Pass the six Edge browser checks and direct concept/render comparison at 363×874, plus 390×844, 390×700, 844×390, 1280×800 and 390×320 reduced viewport. Browser TLS bypass is loopback UI QA only; it is not trust evidence. Native desktop lifecycle: 134 checks pass.
- [x] Package/install 0.1.2, verify the installed LAN listener and final Windows appearance, and update output locations.
- [x] Re-test redesigned Safari/Home Screen layout and actual software keyboard on the user's physical iPhone. The user confirms both browsers, four tabs, landscape, reachable Send/status/release/tabs with the keyboard open, and actual mouse/text/Stremio with modifier/dialog/tab/explicit release reset.

Fixed before advancement: broad tab selector accidentally matched the page's data-panel attribute, causing bubbled clicks to release input; limited it to actual tab buttons. Corrected navigation height and control typography after visual comparison. docs/ui-design.md records the fidelity ledger and required functional differences from the illustration.

Final 0.1.2 evidence: installer succeeded on this account; installed desktop lifecycle 134 checks and certificate-validated Private-LAN HTTPS/WSS 135 checks pass. All 636 published files match the installed files, both installer/archive SHA-256 hashes validate, and the package contains no private certificates/credentials. The existing public CA hash is unchanged. Output: artifacts/release-0.1.2-20261012-164259; artifacts/latest-release.txt points to it. The normal-user installed app is running at https://192.0.2.10:8443/.

Compared the final installed Windows screenshot with the coordinated concept, including Settings/preferences and dark dropdown behavior. Fixed primary-button text contrast, text-field padding, scrollbar/header/footer contrast and excess initial height. Native Windows chrome and real device names/expiry replace illustrative states. Final dashboard screenshot: %TEMP%/pc-ui-windows-final.jpg. Exact physical Safari/Home Screen layout, software-keyboard and real input/release retest instructions were sent asynchronously; results remain pending.

Physical redesign follow-up — 2026-10-12: the user answered “Both Safari and Home Screen work” to the exact four-tab/landscape/actual keyboard/Send/status/release/dock checklist, and “All controls and release/reset work” to the new unsaved Notepad mouse/Unicode-once, Shift → gear release, drag → tab/Release input and focused Stremio checklist. This establishes actual iPhone 15 Pro acceptance for the redesigned interface and packaged controls tested. Revocation, reboot/sign-in, manual uninstall/removal and clean-environment results are still not inferred.

Clean-account/VM, final physical revocation, reboot/sign-in and manual uninstall/removal remain pending/unavailable as recorded. This visual revision does not complete those gates.

Verify the current documentation when implementing; use the version matching the selected toolchain.

- WPF: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/
- ASP.NET Core WebSockets: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/websockets
- Windows SendInput and privilege limitations: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput
- iPhone Home Screen web apps: https://support.apple.com/guide/iphone/open-as-web-app-iphea86e5236/ios


Physical pairing and Home Screen follow-up — 2026-10-11:
- User confirms actual iPhone QR/request/PC approval succeeds and refreshing Safari reconnects without another approval.
- User confirms the Home Screen web app connects without a certificate warning. This is actual-device trust/connectivity evidence; it does not complete the entire Milestone 5 usability/keyboard/lock-unlock matrix.
- Milestone 1 acceptance is now supplemented by actual PC approval and remembered-browser reconnection. Continue Milestone 2 real mouse work. Real mouse, multi-monitor drag, lock/disconnect cleanup and responsiveness remain untested.

Security maintenance — 2026-10-12, version 0.2.1:
- Reproduced and fixed shared HTTP-budget exhaustion and media authorization across disconnected/replaced sessions. Added reserved power-cancel/logout capacity, bounded connections, and 28 security checks beyond the repeated core suite.
- Release build and local core/security/pairing/lifecycle/PC-feature suites passed. Added dependency audits, GitHub regression automation, private reporting, and synchronized README/AGENTS/CLAUDE/release instructions. See the dated 0.2.1 entry in docs/release-evidence.md for final browser/package/transport results and remaining manual gates.
- Strict TLS follow-up: added AKI/SKI to server leaves and safe migration without root replacement; default suite now 144 checks. Three strict TLS trust/name probes and the six browser checks pass. Adjusted the pairing suite's total deadline for cold GitHub TLS connections; rerun CI before publication.

Release 0.3.0 — 2026-10-13:
- Windows companion now runs on the shared portable host with unchanged DPAPI/root/device formats; PC actions bind to the original connected session before body reads. Bundles .NET 10.0.13 (SDK 10.0.402) from Microsoft's 2026-10-13 security release.
- Hosted `windows-latest` CI ran the Release build, default (including DPAPI migration), security, pairing, lifecycle, PC-feature, browser and strict TLS suites plus packaging/content/revision checks. No installation, physical iPhone, reboot, uninstall or power acceptance was performed on a Windows PC for this version; those manual checks remain open. See the dated 0.3.0 entry in docs/release-evidence.md.

Release 0.3.1 — 2026-10-13:
- macOS-only fix: LAN approval survives Space/full-screen, display and readiness changes (still revoked by network, sleep and session events). Windows behavior unchanged; Windows physical/installed acceptance remains open as for 0.3.0. See docs/release-evidence.md.

Release 0.3.2 — 2026-10-14:
- Shared host fixes (leaf reissued when not issued by the current root, undecodable JSON strings rejected as 400, approved-but-unclaimed devices discarded, monotonic power countdown and launch limit) also ship in the Windows companion; the WPF UI is unchanged. Hosted `windows-latest` CI is the Windows evidence; no installation, physical iPhone, reboot, uninstall or power acceptance was performed. Phone remote: two held repeat keys no longer pause control. See docs/release-evidence.md.
