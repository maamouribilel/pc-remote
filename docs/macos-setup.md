# macOS companion — experimental preview

Version **0.3.0** publishes this companion as **experimental** Apple Silicon and Intel ZIPs; Windows remains the primary platform. Phase 3 adds real macOS pointer/keyboard and Keychain adapters to the shared Avalonia shell. Native input, physical-phone/LAN and installed-app acceptance are tracked separately in [the roadmap](../MULTIPLATFORM_PLAN.md#execution-log) and [release readiness](release-readiness.md). Linux source composition now exists; real native baseline acceptance is pending in [Linux setup](linux-setup.md). Phase 5 now adds supported media, writable system volume, configured app launching, optional login registration and sleep; see the feature/acceptance boundaries below. Restart/shutdown remain unavailable.

## Install the 0.3.0 preview

1. Download `PCRemote-0.3.0-osx-arm64.zip` (Apple Silicon) or `PCRemote-0.3.0-osx-x64.zip` (Intel, not yet run on Intel hardware) and `SHA256SUMS.txt` from the [v0.3.0 release](releases.md). Compare `shasum -a 256 <zip>` with the matching line.
2. Extract with Finder or `ditto -x -k <zip> <folder>`. Do not use `unzip`: the flat .NET bundle keeps required code-signature metadata in extended attributes that `unzip` does not restore. `codesign --verify --deep --strict 'PC Remote.app'` should succeed.
3. Move `PC Remote.app` to a stable folder such as `/Applications` before the first launch, so Accessibility, Keychain access and optional login registration refer to one location.
4. The app has only a local ad-hoc seal (no Developer ID or notarization). Gatekeeper is expected to block the first launch of the downloaded app; after confirming its source and checksum, choose **Open Anyway** under **System Settings → Privacy & Security**. These prompts were not observed for this release.
5. The companion starts paused with loopback HTTPS only. Continue with Accessibility, trusted-network approval and phone pairing below. When updating an earlier PC Remote for macOS, use **Allow Keychain access after update…** (see [Storage and recovery](#storage-and-recovery)).

The bundle identifier is still `local.pc-remote.desktop.dev`; treat it as a preview identity that a future signed build may change.

## Build and launch from source

Use the pinned .NET SDK **10.0.402**, Node **24.21.0**, npm **11.19.0**, Python 3.9+, and Xcode Command Line Tools (`clang` and the macOS SDK). macOS 14 or later is the candidate deployment minimum; see [Avalonia's baselines](desktop-shell.md#dependency-and-baseline). Apple Silicon is the initial target. Intel compilation is separate from execution on Intel hardware.

```sh
python3 scripts/portable.py build
python3 scripts/package-macos.py
# Open the .app path that packaging prints (a new timestamped artifacts/ directory).
# Alternate source run (permission identity differs from the .app):
python3 scripts/portable.py desktop
# Intel candidate, built on a Mac:
python3 scripts/package-macos.py --rid osx-x64
```

The app bundles the browser, .NET runtime, Avalonia native dependencies, the native bridge and notices. Local ZIPs/checksums under `artifacts/` are candidate outputs, excluded from Git; only `--final` outputs from the exact release commit are published. There is no Developer ID signature or notarization; SDK/linker ad-hoc signatures on individual Mach-O files do not make this a signed distribution. Gatekeeper, clean-account installation, quarantine and signing/notarization onboarding still require acceptance. Run as the signed-in user, never with `sudo`. Keep a stable app location while testing Accessibility and Keychain identity.

Only one production instance per user may hold the installation's advisory file lock. A second launch reports that the companion is already running; use its menu-bar window. The kernel releases the lock on process termination, so an old lock file is harmless. Default window close and native Quit request graceful cleanup; keep-running-on-close remains opt-in. If native cleanup fails, Exit stays open/paused so permission can be restored and cleanup retried. Forced process termination cannot provide the same guarantee.

## Accessibility and control readiness

1. Open **Trusted network & permissions → Open Accessibility settings**. This explicitly requests Accessibility and opens System Settings → Privacy & Security → Accessibility.
   On macOS 27 the OS labels this gate **Device Control and Data Access**; the app requests the Accessibility/Post Event API gate only.
2. Add the exact source-built **PC Remote.app**, enable it, then quit and reopen the same app. A source run may instead inherit its terminal/runtime's privacy identity; the smoke workflow uses its own bundle so it need not grant the terminal/runtime broad access. Granting that broad development identity is different from granting the app bundle.
3. Review the target app on the Mac, then choose **Resume control**. Startup, host restart, permission/session transitions and network replacement start or remain paused. Native posting independently rechecks permission and the interactive session before each event.
4. If a rebuild invalidates the entry, remove the stale entry, add the current candidate and restart. The app cannot grant itself permission. It requests no Input Monitoring, Screen Recording, Location, microphone or unrelated permissions.

Revocation, Secure Input/password entry, an inactive session, failed Accessibility focus inspection or failed native submission/cleanup blocks input. The shell polls permission/storage as well as listening for lifecycle events. Restore permission/session, use **Release input**, then deliberately Resume; failed cleanup prevents Resume and keeps possible held events tracked. The native API reports submission, not confirmation that a particular app accepted the text/key. A text acknowledgement means submission succeeded and is never permission to replay it automatically.

AppKit workspace sleep/display-sleep/session-switch/Space notifications synchronously release tracked input and pause ownership. Only events that can change the trusted network revoke the session's LAN approval: SystemConfiguration network keys, system sleep/wake, fast-user-switching and power-off. Space or full-screen changes, display sleep/wake and Secure Input/Accessibility readiness changes pause and release input but keep the approved LAN, so the phone stays reachable (version 0.3.0 revoked approval on all of them, which sent the address back to `localhost`). The host may restart on the same approved address when readiness changes; the phone reconnects automatically and control stays paused until Resume. SystemConfiguration notifications invalidate network approval; a native generation check rejects input after a change, including a change racing Resume. Wake does not automatically resume control. Public focused-application/session and Secure Input checks also deny login UI. Physical lock-screen and suspend/resume coverage still needs acceptance on supported macOS versions; no undocumented lock dictionary keys or privileged lock-control APIs are used. No sleep, reboot, lock, trust installation or Accessibility grant is performed by the automated suites.

## Trusted LAN and phone setup

The normal app can serve **loopback only** once its Keychain-backed certificate store is available, even before Accessibility permission. Until you approve a network, the address is `https://localhost:8443` and only this Mac can connect; the window says so under the address. Select the current Wi-Fi/Ethernet interface and approve the local confirmation only on your own trusted LAN. Approval is explicit and session-only, not inferred from private addresses. It binds exactly the selected IPv4 addresses plus loopback; the address, QR/manual link and certificate SANs derive from those bindings.

The network signature includes interface, link address, routes/gateway and local addresses. SystemConfiguration watches `State:/Network/.*` and `Setup:/Network/.*`, including interface/association changes, alongside .NET address/availability notifications. Any detected change cancels ownership, denies pending approvals, releases input, invalidates trust and stops old listeners before replacement. Reapprove and Resume separately after a change. If macOS redacts the Wi-Fi name, setup uses the explicitly selected interface/connectivity session without requesting Location; it does not claim to know an SSID or authenticate a router. Same-subnet roaming, cable replacement, VPN/route changes and notification coverage remain physical acceptance checks. Until verified in your environment, use loopback for development; network identity is not cryptographic network authentication.

Save the public iPhone trust profile, compare its SHA-256 fingerprint locally, and follow [certificate installation/full trust](certificates.md#iphone-onboarding). This is a manual user trust decision. The app never installs trust on the Mac or phone. Open the exact displayed HTTPS address, initiate pairing, and approve the request in the Mac's local dialog. Safari/Home Screen cookies can require separate approval. Remembered devices can be revoked in the shell; failed storage denies approval, and failed revocation pauses control. Never bypass certificate warnings as acceptance. Address/leaf renewal retains the original root and remembered records; a new IP may require a new phone bookmark.

## Storage and recovery

Application data lives in `~/Library/Application Support/PC Remote/`, with directories **0700** and files **0600**. A random 256-bit installation encryption key is stored as a non-synchronizing generic-password item in the user's default **file-based Keychain**, service `local.pc-remote.storage.v1`, account derived from the installation data path. This deliberately supports unsigned development composition; it is not an iCloud/data-protection Keychain entitlement design. System Keychain fallback is absent. Do not move/copy the data directory expecting its path-bound key or Keychain ACL identity to migrate automatically.

`devices.protected` and `certificates/root.protected` / `server.protected` contain versioned AES-256-GCM envelopes: random 96-bit nonce, 128-bit tag and relative-path authenticated data. Writes use a private temporary file, flush and atomic rename. PFX/device clear buffers and loaded encryption keys are zeroed after use. Public `.cer`/`.mobileconfig`, SAN addresses and preferences are separate; the phone receives only public root material. Symlinks in application paths/files are refused. The process uses umask 077. Same-user malware or administrators remain outside this protection boundary; this is not protection against a compromised login session.

A locked/unavailable default Keychain, denied access or a missing encryption key fails explicitly, without an implicit unlock dialog or plaintext fallback. Monitoring stops all listeners and cancels ownership on storage failure; unlocking/recovery requires a fresh safety-paused host. Unlock your own default Keychain through Keychain Access, then retry the same candidate. Restoring protected files requires the matching Keychain item. Existing ciphertext without its key, missing installation roots and envelope authentication failures never cause an automatic new CA. Do not delete the installation or replace trust as a shortcut.

**Updates and rebuilds.** The app has only an ad-hoc code signature, so every new build or release has a different code identity. The existing storage item's access list and partition list name the build that created it (its `cdhash`). A different build is refused without a dialog (`OSStatus -25293` was observed for the Phase 5 candidate) and listeners stay stopped. Fresh installations with no existing protected data create their own key and are unaffected. To keep an existing installation's root and remembered devices after an update, place the new `PC Remote.app` in its stable location, open it and choose **Trusted network & permissions → Allow Keychain access after update…**. After a local confirmation, macOS shows its own Keychain dialog for the `local.pc-remote.storage.v1` item and asks for your login Keychain password. Choose **Always Allow** only for the PC Remote.app you installed; **Deny** keeps storage locked. PC Remote does not edit the access list itself, unlock the Keychain or create/replace/reset keys, roots or devices in this flow. Ordinary startup and background checks still never show a dialog, and they fail closed while the explicit request is open. A locked login Keychain must first be unlocked through Keychain Access. Approval of the real dialog after an update remains a manual acceptance check; Developer ID signing would change the identity again.

Certificate signing/leaf-renewal stays in Host. Default macOS .NET key import is temporary (not `PersistKeySet`; `EphemeralKeySet` is unsupported on macOS), while persistence is protected by the Keychain-backed envelope. The CA stays stable across leaf SAN/expiry/AKI-SKI renewal. Root expiry still requires deliberate trust replacement as on Windows. [Certificate details](certificates.md) explain removal and retirement; retiring this Mac installation also requires deleting its exact Keychain item only after the app exits and every trusting device removes that root. No automated retirement tool is added in Phase 3.

## Input semantics and limits

- Relative movement uses current Quartz coordinates, clamps to active display bounds and emits left/right dragged events while the corresponding remote button is held. Click/double-click include Quartz click count. Multi-monitor/scaled display acceptance remains pending.
- Frozen v1 scroll units are **120 per detent**; the Mac adapter accumulates fractions and converts to **12 Quartz pixels per detent**, with the existing Host natural-scroll conversion and the native horizontal sign conversion. These are application units, not Windows native wheel events; sensitivity/scroll calibration needs target-app acceptance.
- Navigation maps macOS key codes (Return 36, Delete/Backspace 51, arrows 123–126 and dedicated Home/End/Page keys). Home/End follow macOS/app document-scrolling semantics, which differ from Windows caret behavior. Meta's historical `Windows` wire token maps Command; Alt maps Option; Control retains Control.
- Logical copy/paste/select-all/undo use Command plus a key resolved from the current Unicode keyboard-layout table, including Command-specific layout behavior. Switch-app maps Command+Tab. Missing layout resolution disables logical shortcuts at host creation and fails safely if the layout later becomes unavailable. Read-only US/French layout-table checks do not prove native insertion under every input source or IME.
- Text uses Quartz Unicode strings by scalar (surrogate pairs kept together), with Return/Tab key pairs for normalized line breaks/tabs; it does not alter the clipboard or change the input source. Cocoa text/IME support, secure fields, games, terminals, web editors and custom controls can differ. Verify the focused target; text acknowledgement cannot prove app acceptance. IME composition and universal target compatibility are not claimed.
- Held modifiers/buttons/physical keys/Unicode packets are tracked before submission and removed only after successful up submission. Cleanup tries every held category, preserves failures for retry and is idempotent. Repeat bounds, 1200 ms held lease/watchdog and phone heartbeat/reconnect semantics stay in shared Core. Pause, revoke, disconnect, takeover, invalid frames, watchdog, replacement and exit use the same tracker.

## Checks and pending native acceptance

On 2026-10-13, the Apple Silicon macOS 27 guarded smoke run passed **41 adapter checks**, including actual Cocoa receipt of Unicode, navigation, Command shortcuts, relative movement, clicks, scroll, drag, held modifiers, and cleanup on pause/revoke/disconnect/takeover/watchdog/restart. Command+Tab switched between two disposable apps. This is separate from phone/LAN and installed distribution acceptance.

Close all companions first; HTTP suites share loopback port **8443** and must run sequentially.

```sh
python3 scripts/portable.py test
python3 scripts/portable.py desktop-test
python3 scripts/portable.py mac-test       # fault injection/semantics; no real input
python3 scripts/portable.py mac-storage    # real disposable Keychain/files, restart, lock, certificates/HTTPS/WSS
# Python 3.13+:
python3.13 scripts/portable.py mac-tls      # strict chain/name plus wrong-host/untrusted-root negatives
python3.13 scripts/portable.py tls
python3 scripts/portable.py browser
python3 scripts/portable.py browser-capabilities
python3 scripts/portable.py audit
# Build/open a dedicated test bundle and request its Accessibility entry:
python3 scripts/portable.py mac-onboard
# After enabling PC Remote Smoke, keep the Mac unlocked and the target focused:
python3 scripts/portable.py mac-smoke
```

The smoke command builds a self-contained **PC Remote Smoke.app** with a dedicated privacy identity and compiles/creates its own disposable Cocoa target app through LaunchServices and refuses input unless its live Accessibility focused PID is frontmost before **every input-producing native post**, with pointer events additionally confined to that target window. Focus loss aborts test actions and permits only the tracked up events required for final cleanup; do not interact with other apps while holds are being exercised. The target uses a private scratch clipboard for copy/paste, leaving the user's general clipboard untouched. It checks actual Unicode/navigation and pointer/drag/scroll/modifier receipt plus native cleanup through host pause/revoke/disconnect/takeover/watchdog/restart. It never starts LAN listeners or alters trust, keyboard layout, Accessibility settings or the active machine's sleep state. If permission is absent it reports **UNAVAILABLE** and sends nothing. Command+Tab is tested between two explicitly created disposable apps; permission revocation during a hold and physical lock/suspend require supervised acceptance. Permission loss can prevent even those releases; restore Accessibility and verify held input locally, and do not treat that run as a passed cleanup result.

Physical acceptance still requires a real phone with explicit root trust, HTTPS/WSS without warnings, local approval, remembered restart/revoke, all native actions and interruption cleanup on the selected LAN. Test Safari and Home Screen separately. Record current/alternate keyboard layouts and target apps; test switch-app only with disposable apps. Intel hardware/native execution, supported macOS versions, roaming/route changes, permission loss/recovery, real lock/suspend, menu-bar hide/reopen and clean-account/installed/signed/notarized distribution need separate evidence. No recording, headless or browser-emulated result substitutes for these checks.

Phase 4 should reuse shared lifecycle/readiness/ownership fixes and the shell contract, then implement **Linux** native input/keyring services. It must keep the macOS bridge and Windows DPAPI/adapters intact. Do not copy file-based Keychain assumptions into Secret Service, and do not start Phase 5 features automatically.


## Phase 5 supported features and packaging

[Release readiness](release-readiness.md) defines actual feature and acceptance boundaries. System volume uses writable Core Audio default output controls/read-back (read-only HDMI unavailable); General sends native system media keys and changes system volume. Player shortcuts use macOS VLC defaults and current layout resolution, requiring matching frontmost bundle/browser and player/tab focus. Locally choose `.app` bundles in Preferences; paths never reach the phone and LaunchServices requests activation without arguments. Optional start-at-login uses SMAppService main-app registration; it needs a stable installed/code-signed eligible bundle and may require local Login Items approval. No signing credential is provided or invented. Unsigned candidate eligibility/sign-in remains pending. Sleep is console-user IOKit; restart/shutdown remain backend-denied. Save work; physical power tests need separate disposable-target authorization.

`python3 scripts/package-macos.py --rid osx-arm64` or `--rid osx-x64` now creates a fresh `artifacts/candidate-<version>-<rid>-<timestamp>/` `.app`/ZIP with checksums and manifest, rather than overwriting Phase 3 candidates. Run `python3 scripts/check-package.py --rid osx-arm64` for inventory/hash/runtime/version/source-revision startup without input/listeners. The package bundles .NET/ASP.NET, the native bridge, remote and actual dependency notices. No Developer ID signature/notarization or public release is claimed; no DMG is needed before installer acceptance. Hardware/GUI/clean-account/Keychain/TCC/player/login/phone/LAN/physical-power gates remain explicit in the handoff. Never enable unrelated permissions or bypass TLS/Gatekeeper as a compatibility claim.
