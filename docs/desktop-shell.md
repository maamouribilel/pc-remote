# Shared desktop shell — Phase 2 development

Since version **0.3.0**, releases publish this shell as experimental macOS/Ubuntu archives; Windows keeps its existing WPF companion. `PcController.Desktop` is an Avalonia development shell for macOS/Linux; Phase 3 now composes real macOS Accessibility/Quartz input, Keychain storage and native lifecycle services. Linux source composition now exists; [Linux setup](linux-setup.md) records its pending native baseline and Wayland gates. [Mac setup](macos-setup.md) documents the development candidate and separate acceptance gaps. The Phase 2 sections below describe their original fixture/readiness baseline. No public Mac/Linux assets have been published.

## Dependency and baseline

Pinned **Avalonia 12.1.4**, the stable release published 2026-10-09, targets `net10.0` with the pinned SDK **10.0.402** (updated from 10.0.401 for the 2026-10-13 .NET security release). Avalonia desktop requires .NET 8 or later. Its current support table lists macOS 26 ARM64/x64 at Tier 1, macOS 14/15 at Tier 2, and Ubuntu 24.x x64/ARM64 at Tier 2. This preserves the proposed Ubuntu 24.04 LTS baseline; Tier 2 means best effort rather than upstream release-matrix coverage. macOS 27 ARM64 startup is local evidence, not a new upstream support claim. Intel, Ubuntu x64/GNOME and other desktops need separate acceptance.

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

Normal startup opens an accessible setup window with a menu-bar/tray icon, Open, Pause/Resume, Release input and Exit actions. The macOS application menu provides Open and native Settings (Command-comma), opening Preferences; native Hide/Quit remain. The window is the default way to reach all actions. Keep-running-on-close is opt-in after checking the menu-bar icon; Linux retains a minimized taskbar window even when this option is selected, so a missing tray host cannot strand the app. Open restores a minimized window. Use **Exit** to stop the host and deny pending approvals. Native Quit, logout, restart and shutdown dispose the controller synchronously and are cancelled only when input cleanup fails (the logout/restart path has not yet been exercised on a real session) (earlier builds always cancelled the first request, so macOS reported that PC Remote stopped the logout). SIGTERM, SIGINT and SIGHUP request the same graceful Exit on macOS and Linux. Start-at-login is available in Preferences (see Phase 5 below).

### Window layout (2026-10-14)

The window follows the Windows companion's dashboard and palette (`Theme.axaml` ports the WPF colours and overrides the Fluent hover, pressed, focus and disabled states):

- **Header:** logo, product name, a status dot and text (*Setup required*, *Local only · approve a network for your phone* or *Secure LAN ready*), and **Settings**, which opens or closes Preferences (macOS Command-comma does the same).
- **Readiness banner:** the platform/readiness message, amber when setup, input permission or storage needs attention. While input permission is missing it also offers the permission action.
- **Update banner** (since 0.4.3; [AUTO_UPDATE_PLAN.md](../AUTO_UPDATE_PLAN.md)): asks once whether to check GitHub automatically (**Don't check** / **Check automatically**), then shows a verified newer release (green, or amber for a security release) with **Later**, **Skip this version** and **View release**. It is hidden otherwise. The tray/menu-bar menu adds *PC Remote X.Y.Z available…* while a notice is shown.
- **Pair your phone:** the QR code in a white well, the link expiry, **Create new pairing link**, and the connection address with **Copy** and a scope explanation.
- **Control:** owner status (*No active controller*, *Connecting/Controlling: device*, *Control paused*, input failure), **Pause/Resume control** (primary when paused) and **Release input**.
- **Trusted network:** approval state, interface selection, **Approve network…** (primary while LAN is blocked) and **Block LAN**.
- **Paired devices:** one row per remembered device with expiry and **Revoke**; the controlling device is highlighted.
- **Expandable sections:** *Certificate & phone setup* (numbered steps, trust-profile export, the SHA-256 fingerprint grouped in byte pairs as iOS shows it, and the manual link with Copy), *Permissions & platform setup*, and *Preferences* (pointer sensitivity and system volume sliders, natural scrolling, media profile, app launcher, power controls, **Updates** (automatic-check switch, **Check now** and the last result), keep running, sign-in startup and **Quit PC Remote**). Guidance sections open once when first needed (certificate setup when no device is paired; permissions only for a real missing permission or locked storage, not while the host is still starting) and are not reopened after a person collapses them. Before a network is approved, the address and Trusted network cards name the selected (or only) interface and the exact `https://<address>:8443` the phone will use after approval; this preview grants nothing. A composed host shows *Starting secure setup…* until its first refresh (0.3.3).
- **Footer:** what closing the window does with the current keep-running choice.

Below 880 px wide the dashboard and Preferences use one column. Read-only fields are only reassigned when their value changes, so a selection survives the one-second refresh. The volume slider commits when the drag, click or key interaction ends, like the Windows slider. Automation IDs from earlier builds are unchanged; the QR image is `pairing-qr` and the volume slider `system-volume` (the separate *Set volume* button was removed). Update controls use `update-banner`, `update-title`, `update-text`, `update-consent-on`, `update-consent-off`, `update-view`, `update-skip`, `update-later`, `update-automatic`, `update-check-now` and `update-status`; the Windows window uses the same IDs and adds `update-download`, `update-cancel` and `update-restart` for Download update, Cancel download and Restart to update (installer copies only). Confirmation dialogs share one layout with Cancel before the action; power confirmations use a destructive style. The production title is `PC Remote <version> · experimental preview`.

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


## Phase 3 composition changes

Normal macOS startup now supplies `MacHostFactory` and `MacDesktopEnvironment`; it never references the test assembly. The native bridge builds on macOS and is copied/published with the shell. All starts/replacements preserve a platform safety pause. Environment notifications release/pause synchronously; permission/storage polling updates readiness, invalidates trust on transitions, and stops listeners for locked/unavailable storage. Resume validates/arms the inspected generation and cannot race a newer transition. Graceful Exit stays open on failed cleanup, and Release retries the same native tracker even when no listener exists. Keychain/certificate setup runs off the UI context; shared controller awaits avoid synchronous UI-test/startup deadlocks.

The earlier no-listener statement describes the Phase 2 baseline and still applies to explicitly uncomposed controllers. Linux composition now requires reviewed storage and local session/network lifecycle observation. Mac LAN approval can use an explicitly selected Wi-Fi connectivity session when the OS redacts its name; native SystemConfiguration notifications watch association/route/configuration changes without requesting Location. Validate real roaming/lock/suspend behavior before relying on it. See [Mac commands and recovery](macos-setup.md), including provisional `.app` outputs and guarded tests. Native input, phone/LAN and signed distribution acceptance are independent of successful build/headless/Keychain checks.

## Phase 4 Linux composition

The shell now supplies `LinuxHostFactory`/`LinuxDesktopEnvironment`, a per-user lock, consent request off the UI thread, GTK-text opt-in and explicit local recovery dialogs. Startup/replacement is paused, missing/locked storage stops listeners, and network/lifecycle/provider changes invalidate trust and disarm input. SIGTERM/SIGINT/SIGHUP request graceful Linux Exit; failed cleanup keeps the window open. The taskbar window remains the fallback without a GNOME tray host. Portal/GTK mode changes cancel ownership and rebuild capability snapshots. No recording adapter or temporary store is wired into production. Native X11/Wayland UI/permission/lock/roaming/tray/phone acceptance remains pending; see [Linux setup and test boundaries](linux-setup.md).


## Phase 5 source candidates (2026-10-13)

Supported native feature adapters and provisional packages are described in [the release-readiness handoff](release-readiness.md). Since version 0.3.0, releases publish experimental macOS/Ubuntu archives alongside the Windows companion; their native/phone/LAN acceptance remains pending. Mac Core Audio/native system media/player shortcuts/LaunchServices/login registration/sleep and Linux libpulse/MPRIS/player shortcuts/restricted GIO/XDG/logind use real platform services with backend denial and final native readiness/permission checks. Mac restart/shutdown stay unavailable; Linux power requires `Can* == yes`, `interactive=false`, no inhibitor override. Launcher wire input is still configured IDs only; no remote path/arguments/shell. Native dependencies/signing/login eligibility/actual player and physical power acceptance remain separate from recording/source-table/protocol/package checks.

Permission and storage readiness transitions force replacement of the capability snapshot even on unchanged loopback bindings; old ownership/listeners stop and replacement starts paused. Since 0.3.6, pause-only transitions (`CapabilitiesChanged == false`: Space, display, Secure Input, login screen, inactive session) keep the running host, its pairing link and pending pairing approvals, and `PermissionReady` separates a missing permission (banner action, permissions section) from a transient block (message only). v1 fields, budgets/reserved cancellation, original-session authentication checks, device-bound confirmation/countdown/cancellation and failed cleanup retention remain. Optional login start never preserves LAN approval/Wayland consent or resumes control. Provisional manifests record version/base revision/dirty source/file hashes; no tag/public release is created. Windows adapters, DPAPI and packaging remain intact. Real GNOME/phone/LAN/installed/Intel/CI gates stay visible.
