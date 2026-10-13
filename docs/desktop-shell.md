# Shared desktop shell — Phase 2 development

The published download remains Windows **0.2.1 preview**, with its existing WPF companion. `PcController.Desktop` is an Avalonia development shell for macOS/Linux; it does not yet provide native control or a production secure store. Phase 3/4 implement those adapters separately. No public Mac/Linux assets have been published.

## Dependency and baseline

Pinned **Avalonia 12.1.4**, the stable release published 2026-10-09, targets `net10.0` with the unchanged SDK **10.0.401**. Avalonia desktop requires .NET 8 or later. Its current support table lists macOS 26 ARM64/x64 at Tier 1, macOS 14/15 at Tier 2, and Ubuntu 24.x x64/ARM64 at Tier 2. This preserves the proposed Ubuntu 24.04 LTS baseline; Tier 2 means best effort rather than upstream release-matrix coverage. macOS 27 ARM64 startup is local evidence, not a new upstream support claim. Intel, Ubuntu x64/GNOME and other desktops need separate acceptance.

Sources reviewed on 2026-10-13: [release 12.1.4](https://github.com/AvaloniaUI/Avalonia/releases/tag/12.1.4), [supported platforms](https://docs.avaloniaui.net/docs/supported-platforms), [tray behavior](https://docs.avaloniaui.net/controls/navigation/trayicon). Build entry points opt out of Avalonia build-time telemetry; for direct dotnet commands set `AVALONIA_TELEMETRY_OPTOUT=1`. There is no deployed-app telemetry. Direct packages and transitive versions/hashes are recorded in the Desktop and DesktopTests `packages.lock.json` files. See [third-party notices](third-party.md).

The shell uses Avalonia's native macOS backend and X11 on Linux. On a GNOME Wayland session, shell rendering requires XWayland; it does **not** grant input permissions for other Wayland apps. This phase does not opt into Avalonia's new native Wayland rendering package. Ubuntu prerequisites include `libx11-6 libice6 libsm6 libfontconfig1`, graphics libraries, and an interactive display. Xvfb startup checks are isolated virtual-display evidence, not GNOME/X11/Wayland acceptance.

## Build, run and check

With native pinned .NET/Node/npm tools, Python 3.9+ and all companions closed:

```sh
python3 scripts/portable.py build
python3 scripts/portable.py desktop
python3 scripts/portable.py desktop-test
python3 scripts/portable.py browser-capabilities
python3 scripts/portable.py test
python3 scripts/portable.py browser
python3 scripts/portable.py audit
# Python 3.13+:
python3 scripts/portable.py tls
```

Install the pinned Playwright Chromium browser first (`cd src/remote && npx playwright install chromium`). PowerShell 7 also supports `./scripts/test.ps1 -Desktop`. Run HTTP fixture suites sequentially: they share loopback port 8443. `PcController.Portable.slnx` now includes Desktop and the isolated DesktopTests runner. The full solution still builds Windows WPF and native regressions.

Normal startup opens an accessible setup window with a menu-bar/tray icon, Open, Pause/Resume, Release input and Exit actions. The macOS application menu provides Open and native Settings (Command-comma), opening Preferences; native Hide/Quit remain. The window is the default way to reach all actions. Keep-running-on-close is opt-in after checking the menu-bar icon; Linux retains a minimized taskbar window even when this option is selected, so a missing tray host cannot strand the app. Open restores a minimized window. Use **Exit** to stop the host and deny pending approvals. Native Quit also disposes the controller. There is no start-at-login setting yet.

## Readiness and network trust

Normal Phase 2 startup explicitly reports missing Keychain/Secret Service storage and missing Accessibility/X11/Wayland native input. It creates **no HTTPS listener, certificate, remembered credential, temporary secret store or recording adapter**. Network approval cannot override this state. This is stricter than the roadmap's allowed loopback-only exposure before trust, because certificates cannot be protected properly yet.

`DesktopController` accepts an explicit platform-owned host factory. A future production factory must pass real services to `HostServices`; neither Desktop nor Host references the test assembly. The fixture factory is only in DesktopTests. No default plaintext or recording provider is available in normal startup.

Network setup enumerates suitable IPv4 Ethernet/Wi-Fi interface candidates, without assigning trust based on RFC1918 ranges. Selection opens an explicit local trust confirmation. Approval is session-only and binds only the selected candidate's addresses. Identity includes interface ID, physical interface address, gateways, local addresses and the observable Wi-Fi network (macOS networksetup network name; Linux NetworkManager active SSID/BSSID). Wi-Fi without an observable network identity is omitted from approval candidates; no Location permission is requested or private-IP identity invented. Wired approval is bound to the current interface/connectivity session. OS address/availability notifications invalidate approval even if those values stay unchanged. Periodic refresh detects signature changes; **Block LAN** also invalidates approval. Changes pause ownership, deny local approval prompts, stop old listeners and replace with loopback-only bindings. Resume is a separate deliberate action.

This interface/connectivity identity is conservative, not cryptographic network authentication. Before production adapters start, Phase 3/4 must validate actual Wi-Fi roaming, wired network replacement, route changes and OS notification coverage; strengthen the platform identity source where those notifications cannot identify a same-subnet move. LAN acceptance remains pending. Production secure storage, permission checks, suspend/lock handling and single-instance integration remain Phase 3/4 prerequisites.

The factory must derive certificate SANs from exactly the approved snapshot plus loopback/localhost. The host rejects a certificate whose SANs do not cover any listener binding; it never falls back to name verification through a common name. Connection address and short-lived QR/manual pairing links derive from the same bound snapshot. Network replacement renews only the leaf when needed and preserves the installation root/device store.

## Pairing and control workflow

With a properly configured host, the shell shows the current connection address, pairing QR/manual link and expiry, certificate SHA-256 fingerprint, public iPhone trust-profile export, ownership status, remembered devices and revocation. The native save picker copies only the public profile, never private keys; verify the fingerprint before trusting it on a phone. Pointer sensitivity/natural scroll and media profile persist through Host preferences. Pairing challenges and pending requests expire in two minutes. Dialog buttons explicitly allow or deny; dismissal, Escape, expiry, replacement and Exit deny. Host shutdown closes pairing before listeners stop, preventing a late UI result from storing authorization. Failed protected storage never issues a device credential. Failed revocation pauses control and cancels the owner; retry persistence before resuming.

Pause/Resume releases input first; failed cleanup retains pause. Release input is independent of native action availability. Exit stops listening and cancels ownership/power and all pending dialogs. UI checks use the real shell controls and temporary loopback host, without injecting into user applications or executing power actions.

For a visible recording fixture, run **only** this test entry point:

```sh
python3 scripts/portable.py desktop-fixture
```

Its title says `LOOPBACK RECORDING FIXTURE`, its certificate/device files are unique temporary data, and its native capabilities remain unavailable. It supports testing the shell/approval flow locally. It does not install certificate trust or establish phone/LAN acceptance. Close it with Exit before other fixture tests. Test-only auto-approval exists in the separate browser fixture, never in this shell fixture or normal startup.

## Platform-aware browser

Authenticated status and pong responses add capabilities/readiness with consistent camel-case fields; messages remain protocol **v1** with unchanged exact client-command fields, 1024-byte/depth-four bounds and units. Server replies carrying additive metadata have a separate 4096-byte bound; test consumers assemble fragments. macOS labels use Command/Option and Command shortcuts; Linux uses Super and Ctrl/Alt shortcuts; Windows retains Win/Ctrl/Alt. The historical `Windows` wire key still means logical Meta. Native adapters own translation, not the browser.

Pointer, physical keyboard, text insertion and logical shortcuts are gated independently. Media options/actions reflect the advertised profile allowlist; unavailable volume, launcher and power controls remain disabled. Tabs remain reachable for help and setup, and Release input remains available on an authenticated session. Missing capabilities on an older v1 companion use its historical Windows contract; malformed present capabilities fail closed. Unsupported protocol status is rejected before session claim. Backend authorization and action checks remain authoritative even if browser controls are modified.

Dated check results, real startup evidence and remaining native/manual gaps are in [the roadmap log](../MULTIPLATFORM_PLAN.md#execution-log).
