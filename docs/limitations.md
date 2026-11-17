# Current limitations

Current public release: **0.4.3 preview**. Windows is the primary platform; macOS and Ubuntu are experimental. This page states current limits first. Dated roadmap notes below describe earlier stages and are retained as history.

## Connection and trust

- Use a trusted local network and a signed-in interactive desktop. Windows requires a Private network; macOS/Ubuntu require explicit session network approval. No router forwarding, internet relay, Bluetooth, screen streaming, file transfer, clipboard synchronization or Wake-on-LAN is included.
- Every browser device must trust the installation's public root certificate after a fingerprint comparison. iPhone Safari/Home Screen trust is verified on hardware; Android, iPad, Firefox and their onboarding remain unverified. Certificate warnings need to be resolved rather than bypassed.
- One approved browser owns input. Remembered authentication lasts 30 days and can be revoked locally. Backgrounding or locking the phone suspends the page; reopening reconnects without replay. The held-input watchdog is 1200 ms plus scheduling delay.
- An address change renews the server leaf while retaining the root and remembered devices. Open the companion's new address; bookmarks cannot discover it. Mac/Ubuntu network changes, sleep and user switches require approval again. The macOS 0.4.2 peer-to-peer Wi-Fi fix has observer evidence and one installed-build check with a paired iPhone across ten bursts over twelve minutes. Longer sessions and other Macs remain unverified.

## Controls

- Input targets the focused, signed-in desktop. Elevated Windows apps, secure desktop and the Windows lock screen cannot be controlled. macOS requires Accessibility and pauses on session/readiness blocks. Ubuntu Wayland requires Remote Desktop consent. Failed cleanup keeps control paused.
- Text sends deliberately, up to 256 characters per submission. Linux text is unavailable until explicit local GTK Unicode opt-in and remains target-specific. Arbitrary layouts, IMEs and custom controls need acceptance.
- **Air pointer** is relative pointing, not absolute aiming. Hold to move, lift to re-grip. Gyroscope bias can drift while held, and phone speed combines with companion sensitivity/OS acceleration. Devices without usable sensors fall back to Touch. A denied iOS motion request needs a reload before retrying. Version 0.4.0 had incorrect axis mapping; use 0.4.1 or later.
- Air flat-grip directions were confirmed on an iPhone against macOS. Upright/rolled/landscape grips, measured precision/drift/tap/lift checks, lifecycle, Safari/Home Screen permission recovery and Android remain pending. Synthetic sensor tests and the README demo are browser UI evidence. [Air acceptance log](../AIR_POINTER_PLAN.md).
- Player shortcuts need the matching focused player and its default hotkeys. General uses system media keys on Windows/macOS and an unambiguous controllable MPRIS player on Linux. YouTube Previous needs a playlist; Stremio Next needs another episode and has no supported Previous action. Seek distance follows player settings.
- System volume controls the current output, separately from a player's volume, and can unmute it. Output-device selection is not included. Missing/read-only outputs make the control unavailable.
- Launcher requests use locally configured app IDs. OS focus rules may require selecting the app locally; opening it does not start a video. Linux supports restricted ELF-backed `.desktop` launchers, excluding many wrappers, Snap/Flatpak entries, terminals, interpreters and arbitrary arguments.

## Power and startup

Power requires confirmation and a 30-second cancellable countdown. Phone-originated requests cancel on disconnect, pause, revoke, takeover or host shutdown. Opening or dismissing a confirmation schedules nothing. After dispatch, OS completion and prompts are local; waking a sleeping computer is local too.

| Platform | Limits |
| :--- | :--- |
| Windows | Sleep depends on hardware/policy. Restart/shutdown use normal unsaved-work handling, without forced termination. |
| macOS | Sleep only. Restart/shutdown remain unavailable. Login startup needs OS approval/signing eligibility. |
| Ubuntu | Only logind `Can* == yes`, without prompting or inhibitor override. OS unsaved-work handling is not guaranteed. Login startup needs a stable path and local opt-in. |

Mac/Ubuntu login starts paused with fresh network approval and Wayland consent where applicable. Recording tests, including the demo, never establish physical power execution.

## Distribution and acceptance

All downloads lack distribution signing. Windows may show SmartScreen/Unknown publisher. Mac packages have an ad-hoc seal, without Developer ID or notarization; updates may require the explicit existing-Keychain approval and Accessibility review. There is no Linux distribution signature.

From 0.4.3 the companion can show opt-in update notices; 0.4.2 and earlier never check, so update them by hand once. On macOS, Ubuntu and the Windows portable ZIP the notice only links to the release; a Windows installer copy can also download a verified update and restart into it, which has passed hosted Windows checks but has not yet run on a physical PC. Updates need GitHub to be reachable; there is no other update source.

Windows native input/audio/launcher/power and iPhone results are historical 0.2.x evidence; 0.3.x/0.4.x physical Windows installation/phone revalidation remains pending. macOS Apple Silicon has native and iPhone evidence on one Mac; Intel hardware, installed-current-version lifecycle and broader device coverage remain pending. Ubuntu has isolated automated evidence; real GNOME X11/Wayland desktop acceptance remains pending.

Current-account and hosted checks cannot establish clean-account installation, reboot, uninstall, all native player/login/power combinations or phone/LAN use. RTT is not measured input latency. [Release readiness](release-readiness.md) lists exact gaps; [release evidence](release-evidence.md) records observed checks by date. The [security policy](../.github/SECURITY.md) describes the trust boundary.

## Historical notes

<details>
<summary>Earlier limits and Phase 1–5 scope, retained for provenance</summary>

The text below records earlier milestones. Statements such as a feature being unavailable describe that stage; use the current sections above and release readiness for current behavior.

# First-release limits

Current public preview: **0.4.3** (Windows companion plus experimental macOS/Ubuntu archives). It bundles .NET 10.0.13 from the 2026-10-13 security release; 0.2.1 bundled the affected 10.0.12 runtime. It fixes the shared HTTP-budget and late media-authorization issues in 0.2.0; upgrade older builds. Automated dependency/security checks cannot guarantee absence of unknown bugs. Trusted-network, trusted-browser, certificate, and manual acceptance limits below still apply. See [security policy](../.github/SECURITY.md) and [current release evidence](release-evidence.md#version-021--security-hardening-2026-10-12).

- Trusted local network and a signed-in interactive desktop session only (Windows, macOS or Ubuntu). No internet routing, Bluetooth, Wake-on-LAN, screen streaming, file/clipboard transfer, arbitrary commands, simultaneous controllers or native phone apps; the remote is a browser page. Version 0.2.0 can open PC-configured executables, set Windows output volume, and request Sleep/Restart/Shut down after confirmation and a 30-second countdown.
- Launcher focus is subject to Windows foreground activation restrictions; if focus is denied, the existing app flashes and the phone advises selecting it on the PC. Launcher buttons do not navigate to a video or start playback automatically. The volume slider controls the current Windows output, separately from app-specific volume; moving it unmutes Windows. Output-device switching is not included.
- Phone power countdowns require the active controller to stay connected and cancel on disconnect, pause, revoke, takeover or server shutdown. Sleep requires Windows hardware/policy support; restart/shutdown allow Windows to ask about unsaved work. Once a request is sent to Windows, cancel any Windows prompt locally. Waking a sleeping PC is not included; reopen the remote after waking it locally.
- Normal privileges cannot control elevated windows, secure desktop or Windows lock screen. Partial SendInput failures release tracked input and pause control. Resolve the target/desktop before resuming.
- One approved browser owns input. Phones suspend a backgrounded or locked page (observed on iOS Safari/Home Screen); reopening reconnects. No stale input is queued or replayed. Held-input watchdog is 1200 ms plus scheduling delay.
- Real iPhone 15 Pro / iOS 27 controls, trusted Safari/Home Screen, multi-monitor dragging, US QWERTY Unicode/keys, media profiles and cleanup pass by user report. Chromium-based browsers are covered at phone and desktop viewports by automated checks only; Android, iPad, Firefox, AZERTY and other devices/layouts remain unverified. Smooth responsiveness is subjective; numerical finger-to-cursor latency has not been measured. RTT is not input latency.
- Air pointer (gyroscope mouse, since 0.4.0): relative pointing like a TV magic remote or Wii MotionPlus, not absolute aiming, so lift your thumb to re-grip as with a mouse. Gyroscope bias can creep slowly while held; the phone's mild speed curve stacks with the PC's pointer acceleration and sensitivity; iOS asks for motion access (per page load or launch, and a denial lasts until the page is reloaded); desktop browsers and some tablets have no motion sensor and keep Touch. **0.4.0** read the gyroscope axes in a stale order (alpha as a turn, beta as a tilt), so on a real iPhone a tilt moved the pointer sideways and the invert toggles could not fix it. **0.4.1** reads alpha/beta/gamma as rotation about x/y/z per the current specification, seeds the gravity sign per platform (iOS reports it toward the ground) and smooths before a 2 °/s deadzone; it was confirmed on an iPhone (iOS 18.7) in the flat grip against the macOS companion (see `AIR_POINTER_PLAN.md`, 2026-10-15). Upright/rolled/landscape grips, the measured corner/precision/drift/tap/lift checks, lifecycle, Safari/Home Screen permission recovery and Android remain untested.
- Media chords require the matching focused player/default hotkeys. General uses the active Windows media session/system volume. YouTube previous requires a playlist; Stremio Next requires another episode and has no supported Previous shortcut. Seek duration can vary with player settings. External Stremio players require their matching profile.
- Per-installation CA trust requires explicit trust on each device: the iPhone profile with full trust (verified), or importing the public root certificate elsewhere (unverified). Never bypass a warning as acceptance. Root expiry/replacement requires new trust. Address changes keep the root and renew leaf SANs, but phone bookmarks must use the new IP.
- No installer/application code signing is configured. Windows warnings are possible. Current-account tests do not prove clean-account/VM installation, reboot or uninstall. Record those actual tests before marking release acceptance complete.

- Development Phase 1 provides a portable shared backend and isolated macOS/Linux loopback tests. It does not add a usable Mac/Linux companion, native input, protected production storage, desktop UI or LAN trust setup. At that point the published release was still Windows-only; see [Phase 1 scope/evidence](../MULTIPLATFORM_PLAN.md#execution-log).

## Phase 2 development shell

A source-built Avalonia macOS/Linux shell and platform-aware browser are available for development. The normal shell reports setup-required and has no HTTPS listener until Keychain/keyring storage and real platform services exist. Approval/pause/revoke workflows can be checked with the separate loopback fixture; they do not establish native input, physical-phone, GNOME Wayland, LAN or installed acceptance. Network identity/notification coverage needs platform acceptance before a production factory is enabled. [Desktop shell guide](desktop-shell.md) documents dependency baselines, window/tray fallback and commands. Since version 0.3.0, releases publish these as experimental archives.

## Phase 3 macOS source candidate

Phase 3 adds real macOS Quartz pointer/keyboard and Keychain-backed production services to the shell; it does not change the Windows public release. Accessibility is explicit, startups/transitions remain paused, and unsupported media/volume/launcher/power/start-at-login features stay unavailable. Unicode/IME/custom-control behavior, document Home/End semantics, calibrated Quartz scroll, multi-monitor input and Command layouts require target-app acceptance. `CGEventPost` establishes submission, not target receipt. Read-only US/French layout-table checks are not native app insertion.

Keychain identity after rebuild, Accessibility revocation/cleanup, physical lock/suspend, same-subnet Wi-Fi/cable/route changes, real phone Safari/Home Screen strict trust/pairing/control and Intel hardware remain independent acceptance checks. Forced termination cannot guarantee graceful cleanup. Unsigned development `.app` startup is distinct from Developer ID/notarized/clean-account distribution. Linux remains setup-required until Phase 4. [macOS setup](macos-setup.md) lists commands, storage recovery, native smoke guard and exact pending steps; historical Phase 1/2 statements above describe their dated scope.

## Phase 4 Linux candidate (2026-10-13)

Linux source startup now uses real XCB/XTEST or explicit GNOME RemoteDesktop portal consent, reviewed encrypted persistent GNOME login-keyring storage, local lifecycle/network monitoring and per-user instance lock. It starts paused, requires explicit session LAN approval, stops listeners on storage failure and retains failed input cleanup. Text defaults unavailable until local GTK Unicode opt-in; it is target-specific. Interrupted composition or unreachable ended-session holds need verified local recovery; that is not automatic cleanup evidence. Media/volume/launcher/power/start-at-login remain unavailable.

Phase 4 remains **In progress**: actual Ubuntu 24.04 x64/GNOME X11 and Wayland Unicode/shortcut/permission-loss/cleanup/lock/suspend/tray/roaming checks and strict physical-phone/LAN acceptance are pending. Xvfb and fake-portal checks cannot establish those gates. Secret Service is not DPAPI-equivalent; blank-password/plaintext, locked/missing and unreviewed providers fail. Read [Linux prerequisites, storage properties, recovery and support matrix](linux-setup.md). Since version 0.3.0, releases publish an experimental Ubuntu 24.04 x64 archive with these limits.


## Phase 5 source candidates (2026-10-13)

Supported native feature adapters and provisional packages are described in [the release-readiness handoff](release-readiness.md). Since version 0.3.0, releases publish experimental macOS/Ubuntu archives alongside the Windows companion; their native/phone/LAN acceptance remains pending. Mac Core Audio/native system media/player shortcuts/LaunchServices/login registration/sleep and Linux libpulse/MPRIS/player shortcuts/restricted GIO/XDG/logind use real platform services with backend denial and final native readiness/permission checks. Mac restart/shutdown stay unavailable; Linux power requires `Can* == yes`, `interactive=false`, no inhibitor override. Launcher wire input is still configured IDs only; no remote path/arguments/shell. Native dependencies/signing/login eligibility/actual player and physical power acceptance remain separate from recording/source-table/protocol/package checks.

Readiness transitions now force replacement of the capability snapshot even on unchanged loopback bindings; old ownership/listeners stop and replacement starts paused. v1 fields, budgets/reserved cancellation, original-session authentication checks, device-bound confirmation/countdown/cancellation and failed cleanup retention remain. Optional login start never preserves LAN approval/Wayland consent or resumes control. Provisional manifests record version/base revision/dirty source/file hashes; no tag/public release is created. Windows adapters, DPAPI and packaging remain intact. Real GNOME/phone/LAN/installed/Intel/CI gates stay visible.

</details>
