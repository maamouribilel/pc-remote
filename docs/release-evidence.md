# Release evidence

> Publication note: `192.0.2.10` is a documentation-only replacement for the original LAN address. Use the live address shown by your companion.
## Current release — 0.1.2, 2026-10-12

Approved graphite iPhone interface and coordinated Windows dashboard implemented without changing the production authentication, encrypted protocol, player allowlists or real SendInput adapter. All four panels, connection dialog, shared release/tab dock, paired-device controls and preferences remain functional. [Design evidence](ui-design.md) records concept/render comparison, intentional functional adaptations and corrected regressions.

Current output is `artifacts/release-0.1.2-20261012-164259/`; `artifacts/latest-release.txt` points to it. Use `PCRemote-0.1.2-win-x64-setup.exe` or `PCRemote-0.1.2-win-x64.zip`. The installed normal-user executable is `%LOCALAPPDATA%/Programs/PCRemote/PcController.App.exe`, version 0.1.2.0, serving `https://192.0.2.10:8443/`. Older paths below are historical evidence and superseded.

| Check | Current result |
| --- | --- |
| Build | Strict TypeScript/Vite and Release solution pass; zero warnings/errors. |
| Core/security/input | 120 default, 155 certificate-validated pairing, 130 actual native mouse and 135 actual native keyboard checks pass. |
| Browser/UI | Six Edge/Playwright tests pass over real loopback HTTPS/WSS with test-only recording input; Browser plugin unavailable. Explicit loopback UI TLS bypass is not trust evidence. Native 363×874 concept comparison plus 390×844, 390×700, 844×390, 1280×800 and 390×320 checks pass. |
| Installed desktop/LAN | 134 desktop lifecycle and 135 root/EKU/IP-name-validated LAN checks pass. Final logs: `%TEMP%/pc-ui-installed-desktop-final.log`, `%TEMP%/pc-ui-installed-lan-final.log`. |
| Installation/audit | Current-account installer exit 0; `%TEMP%/pc-remote-0.1.2-ui-approved-install.log`. All 636 published files match installed files, both artifact SHA-256 hashes validate, no private certificate/profile/credential files in package, existing public CA hash preserved. Existing scoped firewall rule retained; this update did not repeat administrator setup. |
| Native appearance | Final actual dashboard compared with Windows concept; dark Settings/preferences/dropdown inspected. Fixed inherited primary text, padding, scrollbar and expander/footer contrast. Snapshot: `%TEMP%/pc-ui-windows-final.jpg`. |
| Actual iPhone redesign | User confirms both Safari/Home Screen four-tab/landscape/real keyboard/Send/status/release/dock usability, and new unsaved Notepad mouse/Unicode-once, Shift → gear reset, drag → tab/explicit release, focused Stremio controls. Fresh physical iPhone 15 Pro results against installed 0.1.2; prior broader player/lock/reconnect evidence below remains separate. |
| Remaining release gates | Final packaged physical revoke, reboot/sign-in and manual uninstall/firewall removal pending. Clean-account/VM unavailable. Unsigned release, numerical input latency and AZERTY remain limitations. |

## Previous release — 0.1.1, 2026-10-11

Milestones 0–5 pass. Milestone 6 implementation is under final verification. Host: normal unelevated user, Windows 11 Pro x64 10.0.26300, two active monitors, US QWERTY. Actual device: iPhone 15 Pro / iOS 27 (user report). User confirms only this Windows account is available; clean-account/VM testing remains unverified.

Current package is `artifacts/release-0.1.1-20261011-213623/`; use its `PCRemote-0.1.1-win-x64-setup.exe` for further reinstall checks. Older 0.1.0 paths below preserve historical evidence and are superseded.

The actual installer-error screenshot followed installation of the old 210630 package (confirmed by installed app/input hashes). Version 0.1.1 includes the existing missing-rule fix, displays its version in PC setup, and reports safe error codes on firewall failure. Actual elevated helper creation now succeeds: enabled Inbound/Allow rule for the installed executable, TCP 8443, Private, LocalSubnet. The developer rule is preserved; actual remove/uninstall remains pending.

Version 0.1.1: build zero warnings/errors, 120 default checks, 134 installed desktop checks and 135 verified LAN checks pass. Installed/published files and installer/ZIP SHA-256 hashes match. First upgrade returned Inno exit 4 while saving uninstall information and rolled back; retry and repeat installation both returned 0. Exact transient cause is unverified; logs are `%TEMP%\pc-remote-0.1.1-install*.log` and `pc-remote-0.1.1-upgrade.log`. No user certificate/device data was included in installer file operations. Final physical and clean-environment gates remain unchecked.

| Check | Evidence / limits |
| --- | --- |
| Actual iPhone | User confirms full trust/no bypass, PC approval/remembered reconnect, Safari/Home Screen, mouse/cross-monitor drag, Unicode once in Notepad/browser, keyboard/navigation, all General/YouTube/VLC/Stremio actions, landscape/phone keyboard, tab cleanup and repeated lock/app-switch reset. |
| Actual cleanup | Both phone lock and Wi-Fi loss release/reset passed; actual tray pause/resume clears drag and Shift. Final packaged device-revoke/reboot remain pending. |
| Security integration | 154 checks pass over chain/EKU/name-validated HTTPS/WSS, including authenticated malformed/binary/oversized/replayed/rate-excess frames, recording-adapter hold cleanup, reconnect/revoke. Production uses real SendInput. |
| Native input | Guarded disposable native targets and actual user-operated iPhone Notepad/browser tests pass; no arbitrary focused-app typing. |
| Browser | Edge/Playwright real loopback WSS, four tabs/quick controls/44-pixel targets, touch/text/nav/media, manifest/icons and 390×700, 844×390, 1280×800, 390×320 layouts pass. Screenshots inspected; Browser plugin absent. Explicit UI-only TLS bypass is not trust evidence. |
| Release | Self-contained win-x64 .NET/ASP.NET/Windows Desktop 10.0.12 plus assets publish; strict TS and Release build pass with zero warnings/errors. |
| Current-account installer | Exit 0, per-user files/HKCU uninstall entry. Launched with DOTNET_ROOT variables removed and PATH restricted to Windows; actual coreclr.dll loads from installed directory. Global .NET 10 absent. Silent test skipped firewall setup, so admin rule path is pending. |
| Installed app | Final corrected package: 134 checks pass singleton, verified-HTTPS pause/resume, close-to-tray, Open and graceful Exit, including the Exit helper waiting for full primary teardown. LAN: 135 passes. |
| Firewall absence regression | Real Windows COM maps missing-rule HRESULT 0x80070002 to FileNotFoundException; helper now handles that and COMException safely. Default suite: 120 checks pass, including actual unelevated absent-rule lookup. Administrator install/removal still requires manual validation. |
| Uninstall | Automatic approval review rejected scripted test with only “blocked by policy”; no test registry mutation/uninstall/deletion executed. Manual uninstall/startup/firewall cleanup remains pending. |
| Clean account / reboot | Clean account/VM unavailable; reboot/sign-in/packaged physical revoke await user test. Registry-write and network revoke tests are separate evidence. |
| Signing | Official compiler signature verified; generated app/installer unsigned. Silent local install gives no interactive SmartScreen evidence. |
| Latency/layout | Smooth by user report; early RTT 11 ms is not input latency. Numerical input latency/AZERTY unverified. |

Verified official Inno 7.1.0 generates installer/ZIP/SHA256SUMS; NSIS's HTML download was rejected. Previous outputs are retained. Distribution excludes private keys/credentials. Normal LAN control serves local assets without cloud service; media content itself may require internet.

Final package: `artifacts/release-0.1.0-20261011-212255/`. Installer/ZIP hashes validate; 636 published files contain no private key/certificate/profile/credential data, and installed binary/web asset hashes match published files.

Updating while the old firewall-helper dialog was open failed with installer exit 5 (locked clrjit.dll); Inno rolled back. The user closed the dialogs; retry returned exit 0 and final checks passed. Finish/close firewall dialogs before update or uninstall. No forced termination was used. Final installer log: `%TEMP%\pc-remote-m6-install-final.log`.

Commands: `scripts/package.ps1`, `scripts/test.ps1 -Pairing`, `scripts/test.ps1 -Lifecycle`, `scripts/test.ps1 -Lan`, `scripts/test-browser-mouse.ps1`, `scripts/test-desktop.ps1 -Executable <installed exe>`. Installer log: `%TEMP%\pc-remote-m6-install.log`. Latest directory: `artifacts/latest-release.txt`. Remaining exact steps: [manual checklist](release-checklist.md).

## Version 0.2.0 — apps, exact volume and power (2026-10-12)

Implemented the user-requested app launcher, Windows output-volume slider, and Sleep/Restart/Shut down controls. The phone adds a fifth PC tab; Windows Settings manages up to 24 locally selected executable entries and offers matching audio/power controls. Launching selects the saved media profile. Power requires confirmation and a cancellable 30-second countdown; phone requests cancel on loss/pause/revoke/takeover. No forced application termination or remote command-line input was added.

Final evidence: strict TS/Vite and Release solution build pass with zero warnings/errors; 170 core/verified-HTTPS feature checks pass, including actual countdown delivery once to a recording adapter; six Edge browser checks pass across portrait/landscape/desktop and reduced-height layouts, including launcher/profile selection, exact-volume read-back, dismissed confirmation and cancelled power. Browser plugin unavailable; loopback-only Playwright TLS bypass is visual evidence, while .NET validates fixture root/hostname. Native default audio read/exact percentage write/restoration pass (144-check smoke). Windows Computer Use confirms launcher entries for Stremio/VLC/Edge, existing Stremio launch, Settings interaction and the mint slider showing actual 46%.

Installed 0.2.0 update exit 0. Installed desktop: 155 checks pass; verified Private-LAN transport: 156 checks pass. All 649 published files match the installation, installer/ZIP SHA-256 hashes validate, and no private credential files are packaged. The existing public CA hash is unchanged. Release: `artifacts/release-0.2.0-20261012-180606`; latest pointer updated. Companion runs from `%LOCALAPPDATA%/Programs/PCRemote/PcController.App.exe` at `https://192.0.2.10:8443/`.

Evidence logs: `%TEMP%/pc-new-features-endpoints-final.log`, `pc-features-browser-final.log`, `pc-native-features.log`, `pc-features-installed-desktop.log`, `pc-features-installed-lan.log`, and `pc-remote-0.2.0-features-install.log`. Phone screenshots: `%TEMP%/pc-features-launcher.png` and `pc-features-volume.png`. Actual iPhone use of these new controls and actual sleep/restart/shutdown remain manual device checks; power validation used recording adapters. Existing clean-account/reboot/uninstall gates remain unverified. See [PC controls](pc-features.md).

### Release process maintenance — 2026-10-12

Added shared `AGENTS.md` and `CLAUDE.md` instructions requiring every new version to synchronize source, README/downloads, both agent files, affected `.github/` files, release evidence, tags, and public assets. Added a GitHub pull request checklist and `Release documentation` workflow; packaging now checks documentation before building.

Validation: the existing default suite passed all 141 checks. The documentation guard passed 14 isolated scenarios covering Windows PowerShell/PowerShell 7, current and future-version agreement, stale defaults/downloads/badges/commands/agent markers, missing evidence, and a version bump without fresh evidence changes. This is repository maintenance for version 0.2.0; no new binary build, physical acceptance, or replacement of published v0.2.0 assets is claimed.
