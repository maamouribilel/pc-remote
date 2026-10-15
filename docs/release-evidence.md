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

## Version 0.2.1 — security hardening (2026-10-12)

Reviewed pairing, remembered credentials, session ownership, HTTP/WebSocket validation, input cleanup, PC actions, certificate/DPAPI storage, launcher paths, firewall scope, browser flows, and packaging. The following issues were reproduced and fixed:

1. The global HTTP quota allowed unpaired traffic to exhaust the paired controller's request budget, including a power-cancel request. Quotas now separate unpaired TCP peers and authenticated device IDs. Power cancel/logout use reserved capacity, and server connection counts are bounded. Invalid cookies and spoofed forwarded-address headers cannot create a fresh budget.
2. Media settings accepted a reserved but disconnected owner and checked ownership before reading the body. A delayed request could finish after ownership changed. The endpoint now captures a connected session and rechecks authentication/ownership under the session lock after parsing. Tests cover pause, revocation, and reconnection during chunked JSON delivery.
3. Server certificates omitted authority/subject key identifiers and failed Python 3.13/OpenSSL strict verification. New leaves include both extensions; legacy leaves renew without changing the trusted root. Added migration checks and three strict TLS probes: trusted chain/hostname succeeds, wrong hostname and missing trust fail. Reference: [Microsoft AKI API](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509authoritykeyidentifierextension.createfromcertificate?view=net-10.0) and [Python strict TLS defaults](https://docs.python.org/3/library/ssl.html#ssl.create_default_context).

Initial hardening validation: Release TypeScript/Vite/.NET build passed with zero warnings/errors; default 141, security 169, pairing 176, lifecycle 148, and PC feature 170 checks passed. The subsequent strict-certificate change adds three default checks; final counts and TLS/browser results are recorded after the final rerun. Counts include repeated core checks. New security fixtures use certificate-validated HTTPS and recording input/power; they neither inject native input nor execute sleep/restart/shutdown. The earlier failures and final regression output are retained in ignored local publication logs.

npm and NuGet dependency audits reported no vulnerabilities in their current feeds. GitHub's advisory API reported no matching advisory for the packaged Windows x64 .NET/ASP.NET/Windows Desktop runtime packages at 10.0.12 on this date. This is a feed check, not a claim about unknown vulnerabilities. Sources: [Microsoft rate limiting guidance](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0), [runtime advisories](https://github.com/dotnet/runtime/security/advisories), [ASP.NET Core advisories](https://github.com/dotnet/aspnetcore/security/advisories), and [GitHub's advisory API](https://docs.github.com/en/rest/security-advisories/global-advisories).

Added `scripts/audit-dependencies.ps1`, the GitHub Security and regression checks workflow, and `.github/SECURITY.md`. Private vulnerability reporting is enabled; existing secret scanning and push protection remain enabled. README, both agent instruction files, download documentation, protocol, limitations, and the GitHub pull request checklist are updated together for 0.2.1.

All six Edge/Playwright browser checks passed, including approved WSS control, media profile persistence, cancelled power, gestures/text, disconnect recovery, and portrait/landscape/desktop/compact layouts. Browser plugin was unavailable; the loopback-only browser fixture bypasses its test certificate for UI QA, while .NET endpoint tests validate the certificate chain and name. No unexpected application console/page errors were reported; deliberate offline errors and the runner's own Node color-environment warning are separate.

Provisional 0.2.1 package validation: ZIP integrity, both SHA-256 hashes, MIT/.NET/QRCoder notices, absence of private files/PDBs/local build markers, and packaged guide links passed. Packaging now resolves source-relative guide links to this release's GitHub tag, fixing links broken by flattened/renamed distribution guides. Installer update on the current Windows account returned exit 0; all 647 installed files matched the package tree and the original public CA hash was unchanged. Installed desktop 155 and certificate-validated loopback/Private-LAN transport 156/156 checks passed. The installed companion was reopened normally after testing. Source and package Gitleaks scans found no leaks.

Final assets are built after committing this source/documentation so their DLL informational revision matches the release tag, then rechecked before publication. `artifacts/latest-release.txt` names the final local directory. This corrects the older workflow's misleading pre-commit informational revision; it does not claim byte-identical builds. Final digests and public availability are recorded in GitHub Release assets and `SHA256SUMS.txt`.

The first fresh GitHub runner exposed a ten-second deadline shared by the whole pairing/TLS suite; its several cold TLS connections exceeded that bound although local checks passed. The test now uses a bounded 60-second suite deadline, and the strict TLS probes run in CI. The earlier failed run remains visible; it is not treated as acceptance evidence.

Final certificate-inclusive local validation: Release build zero warnings/errors; default 144, security 172, pairing 179, lifecycle 151, PC features 173; all six browser tests and all three strict TLS trust/hostname probes passed. Counts include repeated core checks. The final installer and public assets are reverified from this committed source before publication.

The interface layout is unchanged, so existing README media remains explicitly attributed to the 0.2.0 capture. Fresh physical iPhone/new power execution, clean-account/VM, reboot, and manual uninstall acceptance remain unverified; 0.2.1 retains preview status. Trusted devices and a trusted Private LAN remain required. Users should update from 0.2.0; its published assets are preserved.

## Development — Phase 1 portable foundation (2026-10-13)

Public release remains v0.2.1; no published binary changed. New Host/Core separation and explicit Windows composition are documented in [portable host](portable-host.md). macOS ARM64 and an Ubuntu 24.04.5 ARM64 container built shared Release code and each passed 254 portable checks. macOS strict TLS positive/negative checks and six browser fixture checks passed; npm/NuGet audits found no current advisories. The full WPF/Windows solution cross-compiled from macOS with zero warnings/errors. Cross-compilation does not establish Windows runtime, DPAPI migration, native input or installed-app behavior. New CI jobs are configured but have not run for this work. All physical-phone/LAN/native/manual acceptance remains separate; see [the dated roadmap log](../MULTIPLATFORM_PLAN.md#execution-log) for commands, scope and pending Windows regression/migration checks.


### 2026-10-13 — macOS Phase 3 local candidate (not a release)

Public Windows **0.2.1 preview** and its released assets remain unchanged. Phase 3 adds real Quartz/Accessibility input and Keychain-protected installation storage to the shared shell. On Apple Silicon macOS 27: 260 shared, 60 desktop, 36 adapter/storage and 13 browser checks passed; strict Keychain-backed HTTPS/WSS chain/name validation passed. A separately guarded Cocoa smoke run passed 41 adapter checks including actual Unicode/shortcuts/pointer and interruption cleanup. Production `.app` startup and graceful Exit were observed with loopback HTTPS, LAN blocked and control paused pending its own permission. See [the dated roadmap evidence](../MULTIPLATFORM_PLAN.md#execution-log) and [macOS setup](macos-setup.md) for final candidate digests and exact pending phone/LAN, permission-loss/lock/suspend, Intel hardware and signed/notarized/installed acceptance. No recording or browser emulation establishes physical-phone acceptance. Linux Phase 4 has not started.


## 2026-10-13 — Phase 4 Linux development evidence (not a release)

Public app/assets remain Windows **0.2.1 preview**. Linux source X11/consented GNOME portal/keyring composition and guarded isolated checks were added; Phase 4 stays **In progress** until real Ubuntu x64/GNOME X11 and Wayland text/cleanup acceptance. Actual results: 260 shared, 60 desktop, 47 Linux faults; private Xvfb/GTK US/French 30 each; private fake-portal protocol 14; real disposable encrypted GNOME keyring/instance/strict-TLS 24. Browser 13, Mac adapter/layout 18, portable strict TLS, npm/NuGet audits and Windows Release cross-build pass. These results are separate from native GNOME, Windows execution, installed app, physical phone/LAN or power acceptance. CI is configured, not observed. No assets/packages/tag/version bump or Phase 5 work. Read the [dated support matrix and remaining gates](../MULTIPLATFORM_PLAN.md#phase-4-support-and-evidence-matrix-2026-10-13) and [Linux setup/storage/text limits](linux-setup.md). Historical evidence above remains unchanged.


## 2026-10-13 — Phase 5 feature and release-readiness candidates (not a release)

Public Windows **0.2.1 preview**, downloads and assets remain unchanged. Implemented supported native Mac/Linux audio/media/configured launcher/login/power adapters, independent backend/UI gating, readiness snapshot refresh, Keychain no-interaction refusal and native feature teardown protection. Mac sleep only; restart/shutdown remain unavailable. Linux power requires logind `Can* == yes` and uses `interactive=false`, tested only on private fake logind. No physical power/uninstall/login registration, permission expansion, public tag or release was performed.

Actual evidence: Mac/emulated Ubuntu x64 260 shared checks; Mac adapter/media/layout and real disposable Keychain/storage checks; headless shell workflows; 13 browser fixture checks; strict portable/Mac-Keychain/Linux-shared/native ARM64-keyring TLS positives/negatives, npm/NuGet and queried .NET 10.0.12 runtime advisories. x64 private Xvfb/GTK US/French each 41 checks include isolated native null-sink volume/read-back and no-op GIO launch/refusal; private portal/MPRIS/logind tests remain protocol-only. Native ARM64 keyring/strict TLS 24 checks pass. x64 emulated keyring correctly rejected Rosetta as an unreviewed provider, without weakening production. Full Windows Release cross-build succeeds; execution remains unavailable. Final counts, package paths/sizes/digests and source/artifact scans are appended to [the Phase 5 roadmap entry](../MULTIPLATFORM_PLAN.md#2026-10-13--phase-5-supported-features-and-provisional-release-readiness).

Provisional Mac ARM64/Intel ZIP and Ubuntu x64 TAR.GZ bundle runtime/remote/native bridge/license inventories and candidate manifests. Content, native architecture, secret/debug exclusion, archive integrity, source revision, self-contained metadata startup where available and SHA-256 checks are separate from actual installed acceptance. Mac bundles are locally ad-hoc sealed; no Developer ID/notarization/distribution signature is claimed. A current-account Mac startup now fails closed on denied Keychain access, with no listener, and native Exit completes. Secure Input/LaunchServices prevented new guarded smoke receipt; earlier Phase 3 native evidence remains historical. Real Intel/GNOME X11/Wayland/player/audio-write/login/permission/lock/roaming/tray/installed/Safari/Home Screen/LAN/power gates and observed hosted CI remain pending. [Release readiness](release-readiness.md) supplies exact platform/manual/publication steps and the support matrix. No source-table, recording, emulation, Xvfb or private-bus result establishes full native parity.


### Phase 5 final automated results — 2026-10-13

Mac Release/shared **265**, desktop **61**, adapter/layout/media **57**, real disposable Keychain **75** checks pass. Emulated Ubuntu x64 Release/shared **265**, headless **61**, feature faults **88**, private portal/MPRIS/logind **29** pass; native ARM64 encrypted keyring/instance/TLS **24** pass. Windows full Release cross-build passes with zero warnings/errors; Windows execution remains unavailable. Browser **6 + 7**, strict TLS positive/negative probes, dependency/runtime advisory feeds and source/artifact scans pass. Exact final provisional paths/digests, validation-tool inputs and manual gaps are appended to the [Phase 5 execution log](../MULTIPLATFORM_PLAN.md#final-automated-verification--2026-10-13). Published Windows **0.2.1 preview** remains unchanged. Supported source implementation and candidate verification are complete for the recorded environments; native/installed/player/login/phone/LAN/power acceptance and observed CI remain pending. The current-account Mac candidate refuses denied legacy Keychain ACL access without a listener; review exact local app identity/access before native onboarding, without resetting trust.


Final candidates and full SHA-256 digests are recorded in [the final provisional package table](../MULTIPLATFORM_PLAN.md#final-provisional-packages-and-handoff--2026-10-13). Mac ZIP extraction now preserves only inventoried public code-signature attributes and verifies the extracted seal; unrelated local xattrs and Linux build-account ownership are excluded. Packaging tests exposed and corrected the lost-signature-attribute defect; the earlier stripped-metadata attempt is not acceptance. The final source/evidence appendix follows the recorded dirty candidate snapshot and does not relabel it a clean committed release.


## Version 0.3.0 — experimental macOS and Ubuntu preview (2026-10-13)

Version 0.3.0 publishes the Windows companion on the shared portable host plus **experimental** macOS ARM64/Intel and Ubuntu 24.04 x64 archives of the Avalonia companion (Phases 1–5). Windows keeps its WPF companion, native adapters, DPAPI formats, trusted root and remembered devices. Historical evidence above is unchanged; nothing below relabels earlier development results.

**Release-blocking findings fixed during preparation:**

1. **Bundled runtime advisories.** On the release day, Microsoft's .NET security release (runtime 10.0.13, SDK 10.0.402) published CVE-2026-58280 / [GHSA-mc4q-93x5-f6hj](https://github.com/advisories/GHSA-mc4q-93x5-f6hj) (High) and other CVEs affecting the previously bundled 10.0.12. The hosted CI advisory step caught it. The SDK pin moved to 10.0.402; queries for 10.0.13 runtime packages return no matching advisories. Version 0.2.1 bundles the affected runtime.
2. **macOS Keychain after updates.** The existing storage item's access/partition list names the creating build's `cdhash`; every ad-hoc-sealed rebuild or update has a new identity and was refused with no in-app recovery. Startup and background reads still never prompt; a new explicit **Allow Keychain access after update…** action lets macOS show its own Keychain dialog (login password, **Always Allow**) for the existing item. Nothing edits ACLs, unlocks the Keychain or resets keys/roots/devices. A real disposable-Keychain check confirms an item owned by another code identity is refused promptly without a dialog or new key. Approving the real dialog after an update is still a manual check.
3. A parallel solution-build race (second MacOS project instance) and two CI prerequisites (missing `.tools/` for the verified Inno download; Linux keyring fixture without the built web remote) were fixed.

**Local results, macOS 27 Apple Silicon (SDK 10.0.402 / runtime 10.0.13):** Release build zero warnings/errors; shared **265**, desktop **65**, Mac adapter **57**, real disposable Keychain/storage **78** checks; browser **6** interaction + **7** capability checks; portable and Keychain-backed strict TLS positives/negatives; npm, NuGet and runtime advisory feeds; full Windows solution Release cross-build; locked restore; Gitleaks 8.30.1 over all committed source files.

**Hosted CI actually observed:**

| Run / commit | Windows (`windows-latest`) | macOS (`macos-latest`, ARM64) | Ubuntu 24.04 x64 |
| --- | --- | --- | --- |
| 38459644695 / `d828db9` (runtime 10.0.12) | Release build, audits, default 147, security 181, pairing 187, lifecycle 154, PC feature 176, browser 6, strict TLS passed; **failed** runtime advisory and packaging (`.tools/` missing) | All suites passed; **failed** runtime advisory | Portable suites passed, **failed** runtime advisory; isolated X11 41 + 41 and portal 29 passed, keyring step **failed** (web remote not built) |
| 38460878703 / `826e46c` (runtime 10.0.13) | **All passed:** Release build, audits, default 147 (incl. DPAPI legacy-device and leaf-renewal migration), security 181, pairing 187, lifecycle 154, PC feature 176, browser 6, strict TLS; shared 265, desktop 65, capability 7; candidate installer/ZIP content, license, checksum and source-revision checks | **All passed:** shared 265, Mac adapter 57, Linux faults 81, desktop 65, capability 7, strict TLS, advisories; ARM64 and Intel candidate packaging with ARM64 self-contained metadata startup | **All passed:** shared 265, Linux faults 88, desktop 65, capability 7, strict TLS, advisories; private Xvfb/GTK US 41 and French 41, private portal/MPRIS/logind 29, disposable encrypted keyring/TLS 24; candidate packaging with x64 metadata startup |
| 38462165237 / `88ad0e5` (version bump, push) | All passed (same suites and counts as above) | All passed | All passed |
| 38462165384 / `88ad0e5` (`final` dispatch) | Validation and final installer/ZIP packaging passed; the portable desktop headless check **failed** (`NullReferenceException`: the test read the host while an un-awaited refresh was replacing it) | All passed, final ARM64/Intel packaging passed | Portable suites and final packaging passed; Xvfb check **failed** sampling a native hold once after 300 ms |

Both failures were test timing races that had passed on every earlier run; the tests now await the pending refresh and poll the private X11 target for up to five seconds. Assets from that dispatch were not used.

The release commit's own runs and final asset digests are recorded in the GitHub release notes and a documentation-only follow-up entry, because they are produced after this commit. Final assets are built by the `final` workflow dispatch from that exact clean commit, never from renamed candidates.

**Not established for 0.3.0:** installation or upgrade of the Windows 0.3.0 build on a physical PC, physical iPhone use, clean account/VM, reboot/sign-in, uninstall/firewall removal and physical power on Windows; Gatekeeper/installed launch, the Keychain update dialog, Accessibility on the installed app, player/audio/launcher/login/sleep, lock/suspend, roaming and phone/LAN on macOS; any execution on Intel Mac hardware; Ubuntu GNOME X11/Wayland desktop input/text/cleanup, real login keyring, tray, player/audio/launcher/login/power and phone/LAN. Hosted runners, Rosetta, Xvfb, private D-Bus fixtures and recording adapters are automated evidence only. See [release readiness](release-readiness.md) for the support matrix and exact manual steps.


### Version 0.3.0 publication and public verification (2026-10-13)

Published as a **pre-release**: [v0.3.0](https://github.com/maamouribilel/pc-remote/releases/tag/v0.3.0). The annotated tag `v0.3.0` (tag object `dbdcbd3`) points to commit `6bf908c030f7f8c7903c4a4cc0e71fa186be183a`. Existing v0.2.0/v0.2.1 tags and assets were not changed.

Actual hosted runs on `6bf908c`: push run [38463186338](https://github.com/maamouribilel/pc-remote/actions/runs/38463186338) and `final` dispatch [38463184359](https://github.com/maamouribilel/pc-remote/actions/runs/38463184359) passed every job on Windows, macOS ARM64 and Ubuntu 24.04 x64 (same suites and counts as the `826e46c` row above); the tag's release-documentation run [38464949711](https://github.com/maamouribilel/pc-remote/actions/runs/38464949711) passed for `v0.3.0`. All assets come from the `final` dispatch artifacts:

| Asset | Bytes | SHA-256 |
| --- | ---: | --- |
| `PCRemote-0.3.0-win-x64-setup.exe` | 62,694,906 | `e8975a749c0742c53582819d6c20e74736ac7cd1f5579e5ad200b625ffa9f1f2` |
| `PCRemote-0.3.0-win-x64.zip` | 89,738,769 | `1f5b5ae70e84bc04fc5f9b83e88ce5eb6815f9178764f68fe9dae52c47b3219f` |
| `PCRemote-0.3.0-osx-arm64.zip` | 60,664,694 | `68e0f0935e62b840c38fe71585d6c34ccf77001552d0570a7fd9d0a4774674fd` |
| `PCRemote-0.3.0-osx-x64.zip` | 63,373,853 | `0027d24e95437f738a2796f43e757214ecea5ab3125c62f33a58ba4c67f5f4e3` |
| `PCRemote-0.3.0-linux-x64.tar.gz` | 60,538,249 | `7492b199dd7cb4acb5b722d75452377f25206a25053513506fba02b827580180` |
| `PCRemote-0.3.0-osx-arm64-manifest.json` | 109,626 | `635cc1988b6e7f3794d8c71700459ab60e2cd5a0c34f900ed90dda619da03f5f` |
| `PCRemote-0.3.0-osx-x64-manifest.json` | 109,624 | `c6517f5de93acb76a3ef506279e58cc2faaa9433882fc7238cccbc7625f9af7f` |
| `PCRemote-0.3.0-linux-x64-manifest.json` | 50,478 | `f922899269d19c3c2165d4aa40efbc63838e67cf83d7d9f42b63f2df9b07db2f` |
| `SHA256SUMS.txt` | 791 | covers the eight files above |

Local verification of the downloaded artifacts (new empty directory, verification script run with `python -I`): per-platform and combined checksums; every extracted file matched its manifest (`candidate: false`, `sourceDirty: false`, revision `6bf908c`; 464 macOS and 463 Ubuntu files, 648 Windows ZIP files); both extracted macOS bundles passed `codesign --verify --deep --strict` (ad-hoc, identifier `local.pc-remote.desktop.dev`, no Team ID); native architectures (Avalonia/Skia/HarfBuzz libraries are universal); `--package-info` reported `0.3.0+6bf908c`, .NET 10.0.13 and the bundled remote on Apple Silicon natively and for Intel only under Rosetta emulation; Windows and Ubuntu assemblies carry `0.3.0+6bf908c`; 29 license notices per macOS/Ubuntu package and the Windows .NET/QRCoder/MIT notices; no PDB, protected, certificate or credential files. Gitleaks 8.30.1 found nothing in the extracted trees; its 60 `generic-api-key` hits in the three manifests were each confirmed to be the SHA-256 digest of a listed file whose name contains words such as Authentication, Secrets or KeyDerivation.

Native observation on this Mac (Apple Silicon, macOS 27): the final ARM64 app was started from the extracted archive with `DOTNET_ROOT` removed and a system-only `PATH`. With this account's existing storage item bound to an earlier development build, it refused the key without any dialog and opened no TCP socket or 8443 listener; a second launch printed that PC Remote is already running and exited 0; SIGTERM ended the first process. SHA-256 of the protected root/server files and public root, the data file list and the Keychain item's modification date were unchanged. The window was not inspected, the app was not installed in `/Applications`, and the Keychain update dialog was not approved (that needs the user's own login password).

Public checks after publication: release is a non-draft pre-release for tag `v0.3.0`; all nine asset URLs returned HTTP 200 to anonymous downloads into a new empty directory with matching sizes and SHA-256 digests; the public `SHA256SUMS.txt` is byte-identical to the verified file; GitHub's asset `digest` fields match. The rendered release notes, the README download table/badge/anchors on `main` and this file's `v0.3.0` anchor were checked in a browser.

All gaps listed in the 0.3.0 entry above remain open, including Windows installation/physical iPhone use, macOS Gatekeeper/installed/Keychain-dialog/Accessibility/player/login/sleep/phone acceptance, Intel hardware, and Ubuntu GNOME X11/Wayland desktop acceptance.


## Version 0.3.1 — macOS LAN approval fix (2026-10-13)

Bug-fix preview for the experimental macOS companion; Windows and Ubuntu behavior is unchanged from 0.3.0 and their packages are rebuilt only for the version number. All 0.3.x builds bundle .NET 10.0.13.

**Defect (reported by the user on the installed 0.3.0 Apple Silicon app):** the address kept showing `https://localhost:8443`. Investigation on that Mac: the Keychain update approval had succeeded and `en0` was offered for approval; a 90-second read-only observation of the installed app's native bridge and of .NET network events recorded no network events. Code review then showed that every native unsafe callback (Space/full-screen change, display sleep/wake, any `State:/Network/*` or `Setup:/Network/*` key), every readiness change (for example Secure Input from any password field once Accessibility is granted) and a Resume attempt after such a change revoked the session's LAN approval and stopped the LAN listener. A player going full-screen therefore disconnected the phone.

**Fix:** a separate native network-trust generation advances only for SystemConfiguration network keys, system sleep/wake, session switch and power-off. Space/full-screen and display transitions still pause control, release held input and require Resume, but keep the approved LAN. The shell revokes approval synchronously only for network-trust events or storage failure; Linux and environments without the distinction keep revoking on every event. While no network is approved the window explains that only this computer can connect and how to approve a network. Per-event console, Secure Input, login-window and armed-generation input checks are unchanged.

**Local results (macOS 27 Apple Silicon):** Release build zero warnings/errors; shared **265**, desktop **74** (eight new: pause-only events keep approval, readiness changes keep approval, network/sleep/session events revoke synchronously and at inspection, storage failure revokes, default environments still revoke, pause-only callback still pauses, loopback address explanation), Mac adapter **59** (native generation split), real disposable Keychain/storage **82** (including `MacEnvironment` pause-only versus network-trust reporting), Linux faults **81**; Windows cross-build, runtime advisories (10.0.13: none), Gitleaks source scan.

**Hosted CI on fix commit `62644b2` (run [38471786573](https://github.com/maamouribilel/pc-remote/actions/runs/38471786573)), all jobs passed:** Windows validation default 147, security 181, pairing 187, lifecycle 154, PC feature 176, browser 6, strict TLS and candidate packaging; portable shared 265 and desktop 74 on Windows, macOS and Ubuntu x64 (Mac adapter 59 on macOS); Ubuntu x64 private Xvfb/GTK US 41 and French 41, private portal/MPRIS/logind 29, disposable keyring/strict TLS 24; all three candidate packages. The release commit's own runs and final digests follow in the publication entry after it is built.

On version commit `35bad7c` the push run [38472970739](https://github.com/maamouribilel/pc-remote/actions/runs/38472970739) passed every job, but the `final` dispatch [38472969018](https://github.com/maamouribilel/pc-remote/actions/runs/38472969018) failed one Windows browser check: the loopback test fixture crashed with `UnauthorizedAccessException` replacing its event snapshot while Playwright was reading it. The fixture now skips a refused replacement until its next 50 ms snapshot (the first metadata write must still succeed). That dispatch's artifacts were not used.

**Not established for 0.3.1:** real Space/full-screen/display/Secure Input transitions on the installed app with a connected phone, approval of the Keychain update dialog for this build, and every gap listed for 0.3.0 (Windows installation/iPhone, macOS Gatekeeper/installed/Accessibility/player/login/sleep/phone, Intel hardware, Ubuntu GNOME desktop). Taking a player full-screen still pauses control by design; Resume is local.


### Version 0.3.1 publication and public verification (2026-10-13)

Published as a **pre-release**: [v0.3.1](https://github.com/maamouribilel/pc-remote/releases/tag/v0.3.1). The annotated tag `v0.3.1` points to commit `8fc1d1cf1e3f2f2d270086bada541af5ac024942`. The v0.2.0, v0.2.1 and v0.3.0 tags and assets were not changed.

Hosted runs on `8fc1d1c`: the `final` dispatch [38474378271](https://github.com/maamouribilel/pc-remote/actions/runs/38474378271) passed every job and supplied all assets. The push run [38474378406](https://github.com/maamouribilel/pc-remote/actions/runs/38474378406) passed every job on attempt 2. On attempt 1, the macOS portable job's `dotnet list package --vulnerable` exited 1 without reporting an advisory. The same audit passed in five parallel jobs and in a local re-check, and only that job was re-run. The tag documentation run [38475858646](https://github.com/maamouribilel/pc-remote/actions/runs/38475858646) passed.

| Asset | Bytes | SHA-256 |
| --- | ---: | --- |
| `PCRemote-0.3.1-win-x64-setup.exe` | 62,692,719 | `988853720e7873659af4ad83181ea6960edc756fe49589307c004b3ec42c62c0` |
| `PCRemote-0.3.1-win-x64.zip` | 89,738,832 | `d8d811888e6a5e68ebfdc2a1e057d5154b1df24795a8d3751a438deda6b567c6` |
| `PCRemote-0.3.1-osx-arm64.zip` | 60,670,702 | `dba4f083dea4e8d106d9dfaf2eb3ce6cb7fe3908233651ef48398ba619d11b3e` |
| `PCRemote-0.3.1-osx-x64.zip` | 63,379,797 | `2e35b91e6054cec8b359031e0c55829a6ae6c70c5679b5c0aa88f92d625ae7e9` |
| `PCRemote-0.3.1-linux-x64.tar.gz` | 60,546,463 | `e20399800dd70d2b3c89e43b461873bb9c1e8998b2f9737f66ff13406e457207` |
| `PCRemote-0.3.1-osx-arm64-manifest.json` | 109,626 | `be2a6dc4cc9c4eb7bd9e1467f3acd41ea51d5aff8fb2334ca1a067e6ec468621` |
| `PCRemote-0.3.1-osx-x64-manifest.json` | 109,624 | `7c966828f156c13504ce3b1278cff90ea744750f178af5e12a0628a887dc62f2` |
| `PCRemote-0.3.1-linux-x64-manifest.json` | 50,478 | `b01f3bcbdf3a2159b768ee7cb3325bad89c99b05a4732ea1db9246356cd0eb7f` |
| `SHA256SUMS.txt` | 791 | covers the eight files above |

Local verification used the same independent script as 0.3.0: checksums, manifest provenance (`candidate: false`, `sourceDirty: false`, revision `8fc1d1c`), extracted macOS seals, native architectures, ARM64 native and Intel Rosetta-emulated `--package-info` (`0.3.1+8fc1d1c`, .NET 10.0.13), Windows/Ubuntu informational revisions, licenses and secret scans. The shipped macOS bridge exports the network-trust generation, and the shell contains the new loopback explanation. After publication, all nine public URLs returned HTTP 200 to anonymous downloads with matching sizes and SHA-256 digests. The public `SHA256SUMS.txt` is byte-identical, GitHub's asset digests match, and the rendered release notes and README were checked in a browser.

On the user's Mac, the verified public Apple Silicon ZIP replaced the earlier 0.3.0 installation in `/Applications` (the old bundle was moved aside, and the application data directory was not touched). The seal and `--package-info` passed and the app was launched. Because the code identity changed again, the user must re-approve Keychain access and re-enable Accessibility. Real Space/full-screen/display/Secure Input transitions with a connected phone remain the outstanding manual check for this fix.

## 2026-10-14 — bug fixes and macOS/Linux window redesign (unreleased, after 0.3.1)

Source changes on `main` after v0.3.1. Version stays **0.3.1**; no package was built or published, and the v0.3.1 tag and assets are unchanged. A review of the host, desktop shell, native bridges and phone remote found the defects below. Each fix has a regression check unless stated otherwise.

**Host and core**
- A `server.protected` leaf issued by a different root (for example after `root.protected` was lost on Windows) kept being served while the UI showed the new root's fingerprint and profile. The leaf is now reissued unless its issuer and authority key identifier match the current root. Remembered devices and the root are untouched.
- Request bodies with lone surrogate escapes (`"\ud800"`) parsed but threw when read, producing 500 errors or internal .NET messages. They now return `400 invalid_request`.
- A device approved on the PC whose browser never collected the credential (ticket expired, a new pairing link, or shutdown) stayed listed as remembered for 30 days. It is now removed.
- The 30-second power countdown and the app-launch rate limit used the wall clock, so a clock step could run a power action early or block launches. Both are now monotonic; `executeAt` is display-only. The power confirmation token is compared in constant time.
- A throwing `Ready` handler could leave a disposed server published. The lifecycle now clears it.

**Phone remote**
- Holding two repeat keys (for example two arrows) sent about 16 repeats per second against the PC's 10-per-second session bound, which paused control. Only the newest held key now repeats, every 150 ms. The browser regression check fails on the old code and passes now.
- A ping timeout left a dead connection looking usable until the browser's close timeout; it now disconnects and retries immediately.
- Power confirmation expiry and the countdown compare PC times using the HTTP `Date` header offset, not the phone's clock.
- The `launch_rate_limit` message is now shown instead of the global "wait one minute" message.
- Unpaired heartbeats no longer erase pairing errors or reopen a closed panel.
- `powerMessage` no longer overwrites newer feedback on every heartbeat.
- The volume slider no longer stops updating after a drag that ends on its starting value.
- Background retries keep **Take over** in place, and **Forget this browser** asks for confirmation.
- Duplicate reconnect timers are cleared.

**macOS/Linux shell**
- macOS logout, restart, shutdown and Quit were cancelled the first time (Avalonia answered `NSTerminateCancel`). Cleanup now runs synchronously and cancels only when input cleanup fails.
- macOS ignored SIGTERM. SIGTERM, SIGINT and SIGHUP now request graceful Exit on both platforms. Natively verified: the fixture stopped its listener and removed its temporary data.
- Resume right after a permission grant could consume the one-shot change and keep the stale `permission-required` host. It now forces a capability rebuild.
- Unexpected exceptions in timer, open and exit handlers now pause control with a message instead of terminating the app.
- Overlapping Exit, OS-shutdown and lifetime disposals share one cleanup, which can be retried after a failure.
- Network approval runs off the UI thread.
- The GTK text checkbox re-reads the real mode after a refused change.
- A failed Exit keeps the keep-running choice.
- The macOS single-instance lock sets 0600 with `fchmod`, because the variadic `open` mode is unreliable on arm64.

**Linux native bridge**
- logind session lookup falls back to `GetSession("auto")` when `GetSessionByPID` fails, which happens for apps started from GNOME Shell, a terminal or autostart in user scopes.
- NetworkManager access-point, statistics and Wi-Fi bitrate/scan property updates no longer pause control or revoke LAN approval; a roaming (`ActiveAccessPoint`) change still does.
- Portal `Notify*` calls use a private copy of the session path taken under its mutex.

**Window redesign**
- The Avalonia window now follows the Windows dashboard and palette; see [desktop-shell.md](desktop-shell.md#window-layout-2026-10-14). The production title is `PC Remote 0.3.1 · experimental preview` instead of "development shell".

**Results (macOS 27 Apple Silicon unless stated)**
- Release build zero warnings/errors; the full Windows solution cross-builds with zero warnings/errors.
- Portable **272** (7 new), desktop **79** (5 new, including the real platform composition with Keychain consent and the permission banner; volume slider and QR assertions updated), Mac adapter **62** (3 new single-instance lock checks), real disposable Keychain storage **85**, browser **6** (two-key repeat assertion added), browser capabilities, strict TLS and Mac TLS (Python 3.13), npm/NuGet audit (no advisories), release-documentation check.
- Headless Skia renders were taken at 1000×820, 600 wide and fully expanded, for the setup-required, missing-permission and fixture states on both platform variants.
- The native visible fixture window started on macOS and exited gracefully on SIGTERM. Screen capture was not permitted in this session, so native appearance was not photographed.
- Isolated Ubuntu 24.04.5 ARM64 container (SDK 10.0.402): the bridge compiles with `-Werror` (production and test seams). Linux faults **88**, private Xvfb X11 **41**, private fake-portal/MPRIS/logind **31** (including the new NetworkManager noise/roaming check) and disposable keyring storage passed.

**Not established:** Windows execution of the host changes, real GNOME (x64, X11 or Wayland) session lookup and NetworkManager behaviour, the macOS logout/restart path and native appearance on a real display, Linux rendering on a real desktop, and physical-phone/LAN use of the remote fixes. The new GetSession fallback and NetworkManager filter are container/fixture evidence only.

## Version 0.3.2 — bug fixes and macOS/Linux window redesign (2026-10-14)

Preview release of the fixes and the macOS/Linux window redesign described in the [2026-10-14 entry](#2026-10-14--bug-fixes-and-macoslinux-window-redesign-unreleased-after-031). The shared host fixes also ship in the Windows companion; its WPF window is unchanged. All 0.3.x builds bundle .NET 10.0.13. On 2026-10-14 the public advisory feed had no advisories for the bundled 10.0.13 runtime packages (`scripts/audit-runtime.py`), and the npm/NuGet audits were clean.

**Hosted CI on fix commit `0601761`** (run [38539382226](https://github.com/maamouribilel/pc-remote/actions/runs/38539382226)) passed every job:
- Windows validation: default **151**, security **185**, pairing **193**, lifecycle **158**, PC feature **180**, browser **6**, strict TLS, provisional packaging and content checks.
- Portable on Windows, macOS and Ubuntu x64: shared **272** and desktop **79** each. Mac adapter **62** on macOS; Linux faults **88** on Ubuntu.
- Ubuntu x64 isolated: private Xvfb/GTK **41**, private portal/MPRIS/logind **31** (including the NetworkManager noise/roaming check) and disposable keyring/strict TLS.
- All three provisional packages.

The release commit's own runs, the final assets and their digests are recorded in the publication entry that follows.

**Not established for 0.3.2:**
- Windows installation, iPhone use or power execution.
- The real GNOME (x64, X11 and Wayland) session lookup and NetworkManager behaviour.
- Real macOS logout and restart with the app running.
- Native appearance of the redesigned window on a real display (only headless renders and native startup were checked).
- Gatekeeper and the installed Keychain-update dialog.
- Intel hardware.
- Physical-phone and LAN use of the phone-remote fixes.
- Every earlier 0.3.x gap.

### Version 0.3.2 publication and public verification (2026-10-14)

Published as a **pre-release**: [v0.3.2](https://github.com/maamouribilel/pc-remote/releases/tag/v0.3.2). The annotated tag `v0.3.2` points to commit `ece5d4c463034141ad0bcf49c22310775521e8d8`. Earlier tags and assets were not changed.

Hosted runs on `ece5d4c`, all of which passed every job:
- The `final` dispatch [38540711618](https://github.com/maamouribilel/pc-remote/actions/runs/38540711618) supplied all assets. Windows default 151, security 185, pairing 193, lifecycle 158, PC feature 180, browser 6; shared 272 and desktop 79 on all three OSes; Mac adapter 62; Linux faults 88; Xvfb/GTK US 41 and French 41; portal/MPRIS/logind 31; keyring/strict TLS 24.
- The push run [38540702103](https://github.com/maamouribilel/pc-remote/actions/runs/38540702103).
- The documentation runs for the commit and for the tag ([38542158676](https://github.com/maamouribilel/pc-remote/actions/runs/38542158676)).

| Asset | Bytes | SHA-256 |
| --- | ---: | --- |
| `PCRemote-0.3.2-win-x64-setup.exe` | 62,683,606 | `a91c952580d4e44d1bab98c8df1aaf4832650886cc4efb6fee981424b9afdbb2` |
| `PCRemote-0.3.2-win-x64.zip` | 89,739,659 | `39067453d787928cb1fc1f4819ebd65eae34a9922128acc3d38db90b4f0bb89a` |
| `PCRemote-0.3.2-osx-arm64.zip` | 60,691,631 | `8c336590ea30281a3da3625491690410b66d384455e49c60761460797743a677` |
| `PCRemote-0.3.2-osx-x64.zip` | 63,400,728 | `fd7b7bfb7d456e5b898dcbce3fffc547675d78ca59013a1be9a7db49003023d0` |
| `PCRemote-0.3.2-linux-x64.tar.gz` | 60,552,399 | `f4eadfb13801a610252a49013d07eed4ac22b818c04c27a0d81892b37005787e` |
| `PCRemote-0.3.2-osx-arm64-manifest.json` | 109,626 | `e87a65d1b34b4b9a666b34a9eeda280cdc28a802c8a0743114c715e30ed9dc04` |
| `PCRemote-0.3.2-osx-x64-manifest.json` | 109,624 | `57c4c1eb0478b11683d404e6f69452c10df209700911f67ccd820f5b4f14aadc` |
| `PCRemote-0.3.2-linux-x64-manifest.json` | 50,478 | `0b03e9aaaa5e2183cb3a2ec9393fb3988698ddecefb1f27c54d53691b71ee33d` |
| `SHA256SUMS.txt` | 791 | covers the eight files above |

Local verification of the downloaded workflow artifacts, using an independent script:
- Per-platform and combined checksums match.
- Manifest provenance: `candidate: false`, `sourceDirty: false`, revision `ece5d4c`, version 0.3.2.
- Every extracted file matches its manifest inventory (464/464/463 files).
- Extracted macOS seals pass `codesign --verify --deep --strict`.
- Native architectures are correct for the executables and native bridges.
- `--package-info` reports `0.3.2+ece5d4c` and .NET 10.0.13 natively on Apple Silicon; the Intel build was run under Rosetta emulation only.
- The Windows and Ubuntu informational revisions match, and Ubuntu TAR ownership is normalized.
- License notices are present, there are no PDB, private-key or protected files, and a Gitleaks scan of the extracted packages is clean.
- Byte-pattern checks (not runtime verification) confirm that the shipped assemblies, bridge and remote bundle include the redesigned shell, the macOS lock fix, the Linux session fallback and NetworkManager filter, and the phone-remote fixes.

After publication:
- All nine public URLs returned HTTP 200 to anonymous downloads, with matching sizes and SHA-256 digests.
- The public `SHA256SUMS.txt` is byte-identical, and GitHub's asset digests match.
- The release is marked pre-release.
- The rendered release notes (all sections, both tables and the checksum block) and the README's v0.3.2 badge and download links were checked in a browser.

The documentation commit `dd547d2` that recorded this publication passed the Security and regression checks (run [38542724706](https://github.com/maamouribilel/pc-remote/actions/runs/38542724706)) and Release documentation workflows. Afterwards, at the user's request:
- The verified public Apple Silicon ZIP replaced the installed 0.3.1 app in `/Applications`. Its SHA-256 matched `SHA256SUMS.txt`, the seal verified after extraction and in place, and `--package-info` reported `0.3.2+ece5d4c`.
- The old 0.3.1 bundle and the local 0.2.1 candidate/development builds were moved to the Trash, not permanently deleted.
- The application data directory and the Keychain item were not touched.
- The app launched and keeps its listeners stopped until the user approves Keychain access for the new code identity; Accessibility must be re-enabled for this build.

The gaps listed under [Version 0.3.2](#version-032--bug-fixes-and-macoslinux-window-redesign-2026-10-14) remain open.

## Version 0.3.3 — clearer macOS/Ubuntu setup state (2026-10-14)

After installing 0.3.2, the user reported seeing `https://localhost:8443` instead of the Mac's router-assigned address. A read-only check on that Mac showed the companion listening only on `127.0.0.1:8443`, with `en0` at a private address selected but not yet approved. That is the designed, session-only LAN approval, not a fault.

The same screenshot showed two display defects in the 0.3.2 window, and review found a third:
- The paired-devices empty state kept the "no host" text after setup completed.
- The permissions section opened on every launch, because the window rendered before the host's first refresh.
- The "no platform host" setup text briefly showed at startup.

Fixes in this release (macOS/Ubuntu window only):
- Before approval, the address and Trusted network cards name the selected (or only) interface and the exact `https://<address>:8443` the phone will use. The host stays loopback-only until explicit approval.
- The device empty state refreshes when the host becomes available.
- The permissions section opens only for a real missing permission or locked storage.
- A composed host shows *Starting secure setup…* until its first refresh.

Windows behavior is unchanged from 0.3.2.

**Results:**
- Desktop UI checks **85** (6 new). Because the user's installed app held loopback port 8443 on this Mac, the 8443-dependent suites ran in an isolated Ubuntu 24.04 ARM64 container: shared **272**, desktop **85**, Linux faults **88**; the address-preview render was also taken there.
- Hosted CI on fix commit `bc4ff3f` (run [38597846729](https://github.com/maamouribilel/pc-remote/actions/runs/38597846729)) passed every job:
  - Windows default 151, security 185, pairing 193, lifecycle 158, PC feature 180, browser 6.
  - Shared 272 and desktop 85 on Windows, macOS and Ubuntu x64; Mac adapter 62; Linux faults 88.
  - Ubuntu x64 isolated suites.
  - All provisional packages.
- On release day the runtime advisory feed still had no advisories for .NET 10.0.13.

**Not established:** the 0.3.2 gaps remain, including native appearance on a real display, real macOS logout, real GNOME, Windows installation and physical-phone/LAN use. The final assets and digests follow in the publication entry.


### Version 0.3.3 publication and public verification (2026-10-14)

Published as a **pre-release**: [v0.3.3](https://github.com/maamouribilel/pc-remote/releases/tag/v0.3.3). The annotated tag `v0.3.3` points to commit `83130897a3d126d1f5837ad22e2b8813d2fdd6ea`. Earlier tags and assets were not changed.

Hosted runs on `8313089` all passed every job:
- The `final` dispatch [38599277310](https://github.com/maamouribilel/pc-remote/actions/runs/38599277310) supplied the assets.
- The push run [38599266569](https://github.com/maamouribilel/pc-remote/actions/runs/38599266569).
- The documentation runs for the commit and for the tag ([38600703519](https://github.com/maamouribilel/pc-remote/actions/runs/38600703519)).

| Asset | Bytes | SHA-256 |
| --- | ---: | --- |
| `PCRemote-0.3.3-win-x64-setup.exe` | 62,692,777 | `6b515a78e163d285f1e8eabf06026c93930573bc72d453c2326fd769efca416b` |
| `PCRemote-0.3.3-win-x64.zip` | 89,739,620 | `7d76a51debe934b463226277e8be8f4d032446181ca04af88de7db600f486680` |
| `PCRemote-0.3.3-osx-arm64.zip` | 60,694,450 | `ca04bf1b192f6b84e8da4cdbcd4c490e97a05c2f77210a3eac5ca78439d6caa7` |
| `PCRemote-0.3.3-osx-x64.zip` | 63,403,557 | `e8940a9d98e7615eda862c7c4542daada5bee401774d8c18b913a23dc51392c9` |
| `PCRemote-0.3.3-linux-x64.tar.gz` | 60,555,330 | `387c2122afbff6b9a9c0bd8c8429fa4b099c8a929965f7cf681810edb522c50c` |
| `PCRemote-0.3.3-osx-arm64-manifest.json` | 109,626 | `d2d98ff596c2b169fc97fa1960390450ef979b5e875e9d40f290983d4a85dc18` |
| `PCRemote-0.3.3-osx-x64-manifest.json` | 109,624 | `09c1a534bd4ee5655f89e5cb4bd8ea67bf856519f71e53ab66c941142166f946` |
| `PCRemote-0.3.3-linux-x64-manifest.json` | 50,478 | `c2cb0c466b30576c507dc18ab71f165f6fc873421d13d1423977715ed84eca78` |
| `SHA256SUMS.txt` | 791 | covers the eight files above |

Local verification of the downloaded workflow artifacts, using the same independent script as 0.3.2:
- Checksums match.
- Manifest provenance: `candidate: false`, `sourceDirty: false`, revision `8313089`, version 0.3.3.
- Every extracted file matches its manifest inventory.
- Extracted macOS seals verify, and native architectures are correct.
- `--package-info` reports `0.3.3+8313089` and .NET 10.0.13: natively on Apple Silicon, and for the Intel build under Rosetta emulation only.
- The Windows and Ubuntu informational revisions match.
- License notices are present, there are no PDB or private files, and a Gitleaks scan is clean.
- Byte-pattern checks (not runtime verification) confirm that the macOS and Ubuntu shells include the address-preview and starting texts.

After publication:
- All nine public URLs returned HTTP 200 to anonymous downloads, with matching sizes and SHA-256 digests.
- The public `SHA256SUMS.txt` is byte-identical, and GitHub's asset digests match.
- The README on `main` links only v0.3.3 assets.

Afterwards, at the user's request:
- The verified public Apple Silicon ZIP replaced the installed 0.3.2 app in `/Applications`. The SHA-256 matched, the seal verified before and after installation, and `--package-info` reported `0.3.3+8313089`.
- 0.3.2 exited gracefully on SIGTERM (the 0.3.2 fix, observed natively on this installed build) and was moved to the Trash, not permanently deleted.
- The application data directory and Keychain item were not touched.
- 0.3.3 launched and keeps its listeners stopped until the user approves Keychain access for the new code identity. Accessibility must be re-enabled.

## 2026-10-14 — macOS server never started once Accessibility was granted (fixed for 0.3.4)

**User report on the installed 0.3.3 app.** After the user granted Accessibility, the banner said "macOS input ready", but the window showed *Setup required*, the address stayed unavailable and no listener ran. Read-only diagnosis on that Mac:

- The tccd log showed the first Accessibility toggles failing with "Failed to match existing code requirement". The entry belonged to the previous ad-hoc build. The user removed and re-added the entry, and after a relaunch tccd granted `kTCCServiceAccessibility` and `kTCCServicePostEvent` (`authValue=2`).
- A `sample` and `dotnet-stack` capture showed the host-setup worker stuck in `MacHostFactory.Create → MacInput.ShortcutsAvailable → pc_command_key → TSMGetInputSourceProperty`. On this macOS 27 build, HIToolbox's `islGetInputSourceListWithAdditions` calls `dispatch_assert_queue(main)`. Off the main queue the assertion traps, the .NET runtime's exception thread spins, and the call never returns.
- The controller's refresh gate stayed held: no host, no retries and no visible error. Graceful Exit would also wait forever.
- The same lookup runs for phone Copy/Paste/Select-all/Undo shortcuts and player shortcuts on Kestrel threads.
- This path runs only when input is ready, which is why 0.3.2 and 0.3.3 reached *Local only* before Accessibility was granted.

**Fix (macOS bridge and shell):**
- The Command and plain keyboard maps are now resolved only on the main thread: when observation starts, on every selected-keyboard-layout change, and on any direct main-thread lookup. Worker threads read an atomic cache. Results are identical for ASCII characters (first key 0–127 with an exact single-character match); non-ASCII characters were never used by these callers.
- A host-setup failure message now stays in the banner until a host starts.
- The in-app Accessibility text explains that after an update an existing entry can look enabled but no longer apply.

**Verification:**
- The clang `-Werror` bridge build passes.
- Mac adapter **63**: the new worker-thread lookup check passes, but a console process does not reproduce the trap (it passed against the old bridge too).
- In the real native app (`--native-startup-check` with a running `NSApplication`), the new check passes with the fixed bridge. With the previous bridge the process hung and was still running after 40 s.
- Desktop **86**, including a check that a host-setup failure stays visible across readiness refreshes.

**Not established:** physical input with a paired phone and a real shortcut from the phone; behaviour on macOS versions without the main-queue assertion is expected to be unchanged (cache populated at startup).

## Version 0.3.4 — macOS server start fix (2026-10-14)

Preview release of the fix described in the [2026-10-14 entry](#2026-10-14--macos-server-never-started-once-accessibility-was-granted-fixed-for-034). Windows and Ubuntu behavior is unchanged from 0.3.3.

**Hosted CI on fix commit `f527b88`** (run [38613509433](https://github.com/maamouribilel/pc-remote/actions/runs/38613509433)) passed every job:
- Portable on Windows, macOS and Ubuntu x64: shared **272** and desktop **86** each.
- Mac adapter **63** on macOS (the hosted runner does not run the native-app check).
- Linux faults **88**; Ubuntu x64 isolated suites (Xvfb/GTK US and French **41** each, portal **31**, keyring and strict TLS).
- Windows validation and all provisional packages.

On release day the runtime advisory feed again had no advisories for .NET 10.0.13.

**Not established:**
- A real phone shortcut on the installed app.
- Every earlier 0.3.x gap.

The final assets and digests follow in the publication entry.


### Version 0.3.4 publication and public verification (2026-10-14)

Published as a **pre-release**: [v0.3.4](https://github.com/maamouribilel/pc-remote/releases/tag/v0.3.4). The annotated tag `v0.3.4` points to commit `c36c69ca683c31c8c287dfefc295a8dfa00e3ca7`. Earlier tags and assets were not changed.

Hosted runs on `c36c69c`:
- The `final` dispatch [38614827128](https://github.com/maamouribilel/pc-remote/actions/runs/38614827128) passed every job and supplied the assets.
- The push run [38614817206](https://github.com/maamouribilel/pc-remote/actions/runs/38614817206) passed every job on attempt 2. On attempt 1, the Windows portable job's capability-compatibility browser test timed out while Playwright was *setting up the page* (30 s), before the test body ran. The same test passed in the final run on this commit. No product code for Windows or the remote changed in this release, and only that job was re-run.
- The documentation runs for the commit and for the tag ([38617881726](https://github.com/maamouribilel/pc-remote/actions/runs/38617881726)) passed.

| Asset | Bytes | SHA-256 |
| --- | ---: | --- |
| `PCRemote-0.3.4-win-x64-setup.exe` | 62,698,315 | `5a750365bc3b1b9b1f8233f7f03b237b80b6469544ddd7fdb9fe16ba6bcd0c78` |
| `PCRemote-0.3.4-win-x64.zip` | 89,739,616 | `650329209d5db53298526a4488be390bb7268f28998be638851950d2d7043d45` |
| `PCRemote-0.3.4-osx-arm64.zip` | 60,697,601 | `7ba027a88df39aad5fcaf92fa89229d580d2bd2b17145d3aefa3fef140ac07f5` |
| `PCRemote-0.3.4-osx-x64.zip` | 63,406,692 | `c4f63329ffe52cfc7ed801de655582185aac002a3ae941bb907110aa4480b220` |
| `PCRemote-0.3.4-linux-x64.tar.gz` | 60,556,923 | `3f091abaccd45101be592740705b377bb60175e85cb02d3a174545b681d36574` |
| `PCRemote-0.3.4-osx-arm64-manifest.json` | 109,626 | `ddfc8c0efad6c6e8f2c9f60b76290a1bd4ac03af11b557811f60acdf22de77e1` |
| `PCRemote-0.3.4-osx-x64-manifest.json` | 109,624 | `8fc00f346671d142ffcf2046c3890f8c84e0a770e60f90613430f6e7113bbd17` |
| `PCRemote-0.3.4-linux-x64-manifest.json` | 50,478 | `fff9f7fd86113d87cd5247436900adf6fc36ba15a9e7aacc3afbd8a64e79c62c` |
| `SHA256SUMS.txt` | 791 | covers the eight files above |

Local verification of the downloaded workflow artifacts used the same independent script as earlier releases:
- Checksums match.
- Manifest provenance: `candidate: false`, `sourceDirty: false`, revision `c36c69c`, version 0.3.4. Every extracted file matches its manifest inventory.
- The extracted macOS seals verify and native architectures are correct.
- `--package-info` reports `0.3.4+c36c69c` and .NET 10.0.13: natively on Apple Silicon, and for Intel under Rosetta emulation only.
- Byte-pattern checks (not runtime verification) confirm both macOS bridges contain the layout-change observer and the shell contains the new Accessibility text.
- License notices are present, there are no PDB or private files, and the Gitleaks scan is clean.

After publication, all nine public URLs returned HTTP 200 to anonymous downloads, with matching sizes and SHA-256 digests. The public `SHA256SUMS.txt` is byte-identical, GitHub's asset digests match, and the README on `main` links only v0.3.4 assets.

Afterwards, at the user's request:
- The user had already force-quit the stuck 0.3.3 app.
- The verified public Apple Silicon ZIP replaced 0.3.3 in `/Applications`. The SHA-256 matched, the seal verified before and after installation, and `--package-info` reported `0.3.4+c36c69c`.
- 0.3.3 was moved to the Trash, not permanently deleted.
- The application data directory and the Keychain item were not touched.
- 0.3.4 launched. It needs Keychain approval and a re-added Accessibility entry for its new code identity.
- Native server start with Accessibility granted on the installed app remains to be observed.

## 2026-10-14 — macOS LAN approval dropped on Wi-Fi signal updates (fixed for 0.3.5)

**User report on the installed 0.3.4 app.** The user approved `en0`; the window showed *Secure LAN ready*, then fell back to `https://localhost:8443`. Brave on the Mac reported *TypeError: Failed to fetch*, and the Accessibility banner sometimes returned although permission was granted.

Read-only diagnosis on that Mac (screen-recording frames, Accessibility reading of the window, `scutil` notifications, tccd log):

- macOS updates `State:/Network/Interface/en0/LinkQuality` every few seconds on Wi-Fi.
- The bridge watched all of `State:/Network/.*` and treated every change as a network-trust event. Each signal-quality update therefore revoked the session's LAN approval, paused control and rebuilt the host, which is the browser's *Failed to fetch*.
- The Wi-Fi identity lookup was stable: `networksetup` reports no observable network name, so the connectivity-session identity is used.
- TCC granted Accessibility (`authValue=2`). While the screen recording was active, the system-wide AX focused-application query intermittently returned `kAXErrorCannotComplete`. The interactive-session check required that query, so readiness flipped to *not ready* and back.

**Fixes (macOS bridge):**
- Change notifications whose only changed keys end in `/LinkQuality` no longer advance the network-trust generation. Any other key (AirPort association, addresses, routes, configuration) still revokes approval.
- When the system-wide focus query cannot complete, the session check uses the frontmost application. Other AX errors still fail. Secure Input still blocks password fields, including the lock screen, and the login window is still excluded.

**Verification:**
- Mac adapter **65**:
  - a deterministic key-filter check: LinkQuality kept; AirPort and IPv4 revoke;
  - an interactive-session check on an AX-trusted active console session, which is skipped without trust, as on CI.
- On the user's live Wi-Fi over the same 40 s, the previous bridge's real observer counted **3** network-trust invalidations and the fixed bridge **0**. The only changed key was `LinkQuality`.
- The intermittent focus-query failure could not be reproduced on demand after the recording ended. A GUI probe succeeded with the probe itself and with Claude in front, so that fix is defensive.

**Not established:** a paired phone keeping control across Wi-Fi signal changes on the installed app; real roaming between access points, which should still revoke approval.

## Version 0.3.5 — macOS LAN approval on Wi-Fi (2026-10-14)

Preview release of the fixes described in the [2026-10-14 entry](#2026-10-14--macos-lan-approval-dropped-on-wi-fi-signal-updates-fixed-for-035). Windows and Ubuntu behavior is unchanged from 0.3.4.

**Hosted CI on fix commit `8f3af0e`** (run [38620935396](https://github.com/maamouribilel/pc-remote/actions/runs/38620935396)) passed every job on attempt 2:
- Portable on Windows, macOS and Ubuntu x64: shared **272** and desktop **86** each.
- Mac adapter **65** on macOS, including both new checks.
- Linux faults **88**; Ubuntu x64 isolated suites.
- Windows validation and all provisional packages.

On attempt 1, the Ubuntu private-Xvfb check *native right click and recognized GTK double click* failed. That commit changed no Linux code. The check read the GTK target once immediately after the click, the same single-sample pattern `0cff900` fixed for the other X11 end-state checks. Only that job was re-run, and test-only commit `161739d` now polls it for up to five seconds.

On release day the runtime advisory feed again had no advisories for .NET 10.0.13.

**Not established:**
- A paired phone keeping control across Wi-Fi signal changes on the installed app.
- Real roaming between access points.
- Every earlier 0.3.x gap.

The final assets and digests follow in the publication entry.


### Version 0.3.5 publication and public verification (2026-10-14)

Published as a **pre-release**: [v0.3.5](https://github.com/maamouribilel/pc-remote/releases/tag/v0.3.5). The annotated tag `v0.3.5` points to commit `f2980d2a19518f962baff5527946bbc1acb432bb`, not to the version-bump commit `5aedec6`. Earlier tags and assets were not changed.

Hosted runs:
- **Version commit `5aedec6`:** both the push run 38622426722 and the final run 38622434838 failed the new interactive-session check on hosted macOS. The runner had no ordinary frontmost app or AX focus value, which the product correctly treats as not interactive.
- **`f62008a`:** gated the check on an ordinary frontmost app. Its push run 38623765869 passed, but its final run 38623777679 failed the same way.
- **`f2980d2`:** skips this local-desktop check under `CI=true`. Its final dispatch [38625592883](https://github.com/maamouribilel/pc-remote/actions/runs/38625592883) passed every job and supplied the assets: shared 272 and desktop 86 on all three OSes, Mac adapter 64 with the local-only check skipped, Linux faults 88, the Windows suites and the Ubuntu isolated suites. Its push run was cancelled as a duplicate at the user's request.
- **`84ee50a`:** at the user's request, the workflow no longer runs on every push to `main`; the `final` dispatch is the release gate.
- None of these failed runs produced published assets.

| Asset | Bytes | SHA-256 |
| --- | ---: | --- |
| `PCRemote-0.3.5-win-x64-setup.exe` | 62,689,229 | `7f252a52e4eff8bf44696e946af55548faa563a5db85c918c6fa60df5dda4474` |
| `PCRemote-0.3.5-win-x64.zip` | 89,739,659 | `65779c0348d2a8b50540589414c96c2c3914eb307687301b9dc014eabb492305` |
| `PCRemote-0.3.5-osx-arm64.zip` | 60,700,745 | `a2038a5eba258ac4daeab03d46155e8efbe7add3cff487fe1abbef03f117a290` |
| `PCRemote-0.3.5-osx-x64.zip` | 63,409,772 | `c7ac3cc9e2856b4f98bc79b5745f26b377213b53155430fa4dded1ad193b9a82` |
| `PCRemote-0.3.5-linux-x64.tar.gz` | 60,560,412 | `7b804b2c257466fb0fbfeabe204079d2263cf2b138fc1703f62897e496b4c75c` |
| `PCRemote-0.3.5-osx-arm64-manifest.json` | 109,626 | `64e8ffae7d39e67d66d15a7531f7d210586ddd54c8b2db6a6b603348d13c8077` |
| `PCRemote-0.3.5-osx-x64-manifest.json` | 109,624 | `bb3a800d94d2c75c61b7343e4335a0e8df68ae4cdffe278af98087639b3468f3` |
| `PCRemote-0.3.5-linux-x64-manifest.json` | 50,478 | `c595ad9c647f9036e5b36faa5d79c1ab71a37a87e8889de2214bbfbec64ec85a` |
| `SHA256SUMS.txt` | 791 | covers the eight files above |

Local verification used the same independent script as before:
- Checksums match.
- Manifests: `candidate: false`, `sourceDirty: false`, revision `f2980d2`, version 0.3.5. Inventories match.
- Extracted macOS seals and architectures verify.
- `--package-info` reports `0.3.5+f2980d2` with .NET 10.0.13: natively on Apple Silicon, and for Intel under Rosetta emulation only.
- Byte-pattern checks (not runtime verification) confirm both macOS bridges contain the LinkQuality filter and the layout observer.
- License notices are present, there are no PDB or private files, and the Gitleaks scan is clean.

After publication, all nine public URLs returned HTTP 200 with matching sizes and SHA-256 digests. The public `SHA256SUMS.txt` is byte-identical, and GitHub's asset digests match.

At the user's request, the verified public Apple Silicon ZIP replaced 0.3.4 in `/Applications`:
- The SHA-256 matched, the seal verified before and after installation, and `--package-info` reported `0.3.5+f2980d2`.
- 0.3.4 was not running and was moved to the Trash, not permanently deleted.
- The application data directory and the Keychain item were not touched.
- 0.3.5 launched. It needs Keychain approval and a re-added Accessibility entry for its new code identity.

## 2026-10-14 — macOS pairing approvals, transient pauses and local signing (unreleased, after 0.3.5)

Source changes on `main` after v0.3.5. Version stays **0.3.5**; nothing was published, and the v0.3.5 tag and assets are unchanged. The user reported on the installed public 0.3.5: the Accessibility alert kept coming back, the window seemed to refresh and lose its state, and every pairing request from the phone was denied.

**Findings on the user's Mac (macOS 27.0, Apple Silicon)**
- tccd log: at the first start of each new build, `Failed to match existing code requirement for subject local.pc-remote.desktop.dev and service kTCCServiceAccessibility`; after the user removed and re-added the entry, requests were `Allowed (System Set)`. The ad-hoc seal's designated requirement is the build's `cdhash`, so every install repeats this.
- The readiness monitor showed the Accessibility recovery text whenever the session check failed (Secure Input, login screen, inactive session, no focused application), even with the permission granted.
- Every lifecycle generation bump (Space/full-screen change, display sleep/wake) and every readiness flip forced a host replacement: the pairing link was discarded, control paused, and all pending approvals were denied. The approval dialog activates the PC Remote window; from a full-screen app that switches Spaces, so a phone request was denied as soon as it appeared. A refresh while input was not ready also denied pending approvals every second.
- Not the cause: SystemConfiguration keys at idle (150 s and 120 s watches) changed only `State:/Network/Interface/en0/LinkQuality` (7 times in 120 s), which 0.3.5 already filters; .NET `NetworkChange` raised no event in 120 s; the system-wide AX focused-application query answered `kAXErrorCannotComplete` in all 469 samples over 120 s with an Electron app in front (handled by the 0.3.5 fallback); Secure Input stayed off and the console session active.
- A 0.3.3 crash report from 17:59 (SIGABRT after an unhandled managed exception) predates the 0.3.4 handlers; there is no 0.3.5 crash report.

**Fixes**
- `MacHostFactory`/`MacEnvironment`: capabilities and readiness follow the Accessibility permission only; `Inspect` reports `PermissionReady` and `CapabilitiesChanged` separately, and a transient block shows "Control is paused: macOS reports Secure Input…" instead of the recovery text.
- `DesktopController`: only a capability (permission/mode) or storage change rebuilds the host; pause-only transitions pause and release input but keep the host, pairing link and pending approvals; pending approvals are denied only on network-trust changes, storage failure, host replacement, a new link or Exit. The permission banner button and the automatic opening of the permissions section follow `PermissionReady`.
- `scripts/sign-macos-local.py` (new): re-signs an installed bundle with a per-Mac self-signed identity in a dedicated keychain so the designated requirement stays the same across builds; see [the macOS guide](macos-setup.md#keep-one-code-identity-across-updates).

**Results (this Mac)**
- Desktop checks **92** (6 new: pending approval and host kept across Space/display and Secure Input pauses, same host after recovery, approval still grantable, permission reported granted, Secure Input window state). With the previous controller logic restored temporarily, the new check "Space/display transition pauses but keeps the host and the pending approval" fails; with the fix all 92 pass.
- Mac adapter **65**; real disposable Keychain storage and host **88** (includes `MacHostFactory.Create` against a real Keychain).
- Signing: a copy of the installed public 0.3.5 bundle was signed twice with the same requirement `identifier "local.pc-remote.desktop.dev" and certificate root = H"e17212802cbe6c6021d17b11d50b635d047d0c23"`; `codesign --verify --deep --strict` and `--package-info` were unchanged; about 29 s per bundle. The identity was created on this Mac without any dialog (keychain `pc-remote-local-signing.keychain-db`, random password in the login keychain). Importing the key without the partition-list step made `codesign` fail with `errSecInternalComponent`; the script sets it.
- Candidate `artifacts/candidate-0.3.5-osx-arm64-20261014-203252-595874` was built from the fixed, uncommitted source (informational version `0.3.5+d9e3ad2`), signed with the local identity, verified and installed as `/Applications/PC Remote.app`. The public 0.3.5 bundle was moved to the Trash, not deleted, and the move is recorded under the ignored `.tools/mac-install-0.3.5-postfix/`. The app launched; tccd logged `Failed to match existing code requirement` once more because the identity changed from ad-hoc to local, and the window asks for **Allow Keychain access after update…** (OSStatus -25293). Both are the expected one-time approvals for the new identity.
- `scripts/check-release-docs.ps1` was not run (PowerShell is not installed on this Mac); the version and release links are unchanged. The shared portable and browser suites were not rerun because no Host, Core or remote source changed.

**Second round, same evening (after the user's one-time approvals)**
- With the first locally signed build running, the user re-added the Accessibility entry at 21:40:40 while the process was already running (tccd `Modify` event, then `Allowed`). The phone paired ("My iPhone" remembered, HTTPS + WSS, 17 ms round trip) and showed "Resume control on the Mac", but the Mac stayed paused with the new transient-pause text, which listed four possible causes without saying which one applied.
- Live sampling at that moment (from another process) found Secure Input off, the console session active and the system-wide focus query answering `kAXErrorCannotComplete`, so the remaining process-specific candidates were the bridge's inactive flag or a focus query that stays `kAXErrorAPIDisabled` in a process that was granted Accessibility while running.
- Bridge: `pc_session_state` returns the first blocking reason (root, asleep/inactive, Secure Input, not the console session, login window, no focused application) and `pc_focus_status` the last focus-query result; `pc_session` derives from it. Any failed system-wide focus query, not only `CannotComplete`, now falls back to the frontmost application for `pc_session` and `pc_frontmost`; permission is still rechecked live before every post and Secure Input still blocks password fields and the lock screen. `MacPermissions.SessionBlocked(state, focusStatus)` names the reason and reports only unexpected focus-query error codes.
- Mac adapter checks **67** (2 new). The desktop checks were not rerun in this round: the app was relaunched within seconds of being quit and held port 8443 (the rerun aborted at the fixture host), and the Desktop sources are unchanged since the 92-check run.
- Second candidate `candidate-0.3.5-osx-arm64-20261014-204740-103808`, signed with the same local identity, installed (the first candidate moved to the Trash and recorded) and launched: tccd answered `Allowed (System Set)` 15 times for the new process with no `Failed to match`. This is the first observed Accessibility grant surviving a different build on this Mac.
- The Keychain approval did not carry over. `security dump-keychain -a` shows the storage item's access list already contains the local identity's requirement (status OK), but the item's partition list holds one `cdhash:` entry per approved build (nine so far) and the new build's cdhash is not among them, so the key read fails with -25293 until **Allow Keychain access after update…** is approved once for this build. An app without an Apple Team ID cannot get a `teamid:` partition, so this dialog stays once per build; the script, guide and README now say so.

**Third round: the approved LAN dropped to `localhost` after about two minutes of phone use**
- The user reported "TypeError: Load failed" on the phone after almost two minutes and then a window showing "LAN blocked · this computer only" with `https://localhost:8443`. "Load failed" is WebKit's message for a request that could not be sent at all, which fits a listener that stopped.
- Live capture: with the interface re-approved and `/health` polled every second, the listener answered from 22:03:32 and stopped at 22:06:49. At 22:06:48.955–49.057 `scutil` recorded a burst of 38 `State:/Network/Interface/en0/AirPort` keys (the record plus `AutoJoinTimestamp`, `Busy`, `BusyUI`, `AWDLRealTimeMode`, `BluetoothAudioInUse`, `AirPlay`, `BSSID`, `SSID_STR`, `CHANNEL`, `Power Status`, `ProfileID`, …), the only non-`LinkQuality` keys in six minutes (29 `LinkQuality` updates alongside). The record's `AutoJoinTimestamp` had read 21:50:50 earlier, one minute after the user's approval that evening, so the burst recurs every few minutes on this Wi-Fi (a third one followed at 22:14:48). .NET `NetworkChange` raised no event in the same six minutes, and the one-second interface re-read (identity, gateways, addresses) is reported separately below; the bridge's SystemConfiguration watch was therefore the only revocation path.
- Bridge: the Wi-Fi record and each of its field keys are judged by the joined network name (`SSID_STR`) remembered per record, primed at observe time; only a different name revokes. Moving between access points or bands of the same network keeps the approval, while any other key (addresses, routes, services, global, setup) still revokes. Test hooks `pc_test_airport` and `pc_test_airport_key` feed the real comparison and key classification; Mac adapter checks **69** (2 new, the 0.3.5 key check renamed). Desktop checks rerun on the final source while the third build waited for its Keychain approval (loopback port free): **92**.
- Third candidate `candidate-0.3.5-osx-arm64-20261014-211010-753716`, signed with the same local identity, installed (second candidate moved to the Trash and recorded) and launched; it waits for the per-build Keychain approval.
- Earlier "not the cause" statement about SystemConfiguration keys stands for the idle windows measured then; the quarter-hour burst simply fell outside them.

**Not established**
- Which of the six reasons blocked the session in the user's first screenshot: the named reason only becomes visible after the Keychain approval for this build, and the frontmost-application fallback may already have removed it. After the second build's Keychain approval the banner read "macOS input ready".
- The listener itself surviving a burst on the installed third build: it waits for the per-build Keychain approval, so the native result below stands in for it until the user approves and uses the phone across a burst.

**Side-by-side native verification (22:12–22:29)**
- Two watcher processes loaded the previous bridge (second candidate) and the new bridge (third candidate), each calling `pc_observe` and polling `pc_network_generation`, while `scutil` recorded keys. At 22:14:48 the same burst arrived (2 record keys plus 36 field keys): the previous bridge advanced its network-trust generation 38 times, one revocation per key; the new bridge stayed at 0. A hash of the record's `SSID_STR` and `BSSID` was identical before and after the burst, so the comparison kept the approval for the case it was designed for. The installed bundle differs from the committed source only by a comment that now cites this third burst.
- Physical phone pairing after the fix, a Space switch from a real full-screen app during a request, Secure Input in a real password field, and lock/sleep. The checks drive the shell's fake environment.
- tccd also logs preflight checks for `kTCCServiceScreenCapture` and `kTCCServiceDeveloperTool` from the app process ("Auth Right: Unknown", no prompt) on 0.3.5 before and after the fix; their origin (Avalonia, AppKit or the runtime) was not investigated.

## Version 0.3.6 — macOS LAN approval, pairing approvals and local signing (2026-10-14)

Preview release of the fixes described in the [2026-10-14 entry](#2026-10-14--macos-pairing-approvals-transient-pauses-and-local-signing-unreleased-after-035) and its second and third rounds. Windows and Ubuntu behavior is unchanged from 0.3.5: no Host, Core, Windows, Linux or phone-remote source changed, and the shared desktop-shell changes are exercised by the desktop checks on every OS. All 0.3.x builds bundle .NET 10.0.13.

**What changed on macOS**
- Wi-Fi record (`State:/Network/Interface/<if>/AirPort` and its field keys) bursts revoke LAN approval only when the joined network name changed.
- Pause-only transitions (Space/full-screen, display sleep, Secure Input, login screen) keep the host, its pairing link and pending phone approvals; only a permission or storage change rebuilds the capability snapshot; `PermissionReady` separates onboarding from a transient block.
- The pause text names the one blocking reason; any failed system-wide focus query falls back to the frontmost application, so a grant while running needs no relaunch.
- `scripts/sign-macos-local.py` keeps the Accessibility entry across updates; the Keychain dialog stays once per build.

**Local results before the release commit (macOS 27 Apple Silicon, fix commit `31fa79c`)**
- Desktop **92** and Mac adapter **69** on the final source. The real disposable Keychain storage/host suite (**88**) ran earlier the same day, before the bridge's session and Wi-Fi changes; the shared portable suite and that suite were not rerun at the end because the user's installed build held port 8443 with the phone controlling. The final dispatch runs both on all three OSes.
- npm/NuGet audits: no advisories. Bundled runtime advisory feed: no matching advisories for 10.0.13 (run with the system CA bundle; the python.org 3.9 installation has none). Gitleaks 8.30.1 (archive checksum verified): no leaks in the 247 tracked files or in the fix commit.
- Side-by-side bridge watchers over 16 minutes: two Wi-Fi bursts (22:14:48 and 22:22:49, 38 keys each). The 0.3.5 bridge advanced its network-trust generation 38 times per burst; the 0.3.6 bridge not at all.
- Installed, locally signed third candidate: after the user's Keychain approval and network approval at 22:16:49, the LAN listener answered every second without interruption through the 22:22:49 burst while the phone was controlling the Mac ("Controlling: My iPhone"). Earlier the same evening the same class of burst dropped it within the second.
- `check-release-docs.ps1` runs in the Release documentation workflow on push; PowerShell is not installed on this Mac.

**Not established**
- Hosted CI for this commit and the final assets: recorded in the publication entry below once the final dispatch has run.
- Windows execution, a real Ubuntu desktop, macOS Intel hardware and every earlier 0.3.x gap.

### Version 0.3.6 publication and public verification (2026-10-14)

Published as a **pre-release**: [v0.3.6](https://github.com/maamouribilel/pc-remote/releases/tag/v0.3.6). The annotated tag `v0.3.6` points to the release commit `49205979fbafbbc3f392a464cdba5aa98568f9a5` (tagged 21:43 UTC). Earlier tags and assets were not changed.

Hosted runs on the release commit:
- The `final` dispatch [38640378193](https://github.com/maamouribilel/pc-remote/actions/runs/38640378193) passed every job on the first attempt and supplied the assets: shared **272** and desktop **92** on Windows, macOS and Ubuntu; Mac adapter **68** on hosted macOS (the local-desktop interactive-session check skipped) and the non-native subset **54** on Windows and Ubuntu; Linux adapter **88** on Ubuntu (81 elsewhere); Ubuntu private Xvfb/GTK US and French **41** each, portal/MPRIS/logind **31** and disposable encrypted keyring/TLS; the Windows default, security, pairing, lifecycle and PC-feature suites (151, 185, 193, 158, 180), browser and strict TLS suites, installer, content/license/checksum checks; strict TLS probes, npm/NuGet audits and the runtime advisory feed on every OS.
- The Release documentation workflow passed on the push (38640379142) and on the tag (38642387244).

| Asset | Bytes | SHA-256 |
| --- | ---: | --- |
| `PCRemote-0.3.6-win-x64-setup.exe` | 62,693,054 | `e1a3a833f23e17d72d10579be6e41a4343500904226ad9b4cf507246822266b4` |
| `PCRemote-0.3.6-win-x64.zip` | 89,740,341 | `7133e8f0cfd14534864d8ffee5cde8cfb9f8491ac7832b0fbbfc1ffb68ebed3d` |
| `PCRemote-0.3.6-osx-arm64.zip` | 60,713,795 | `f9c6409130faad51eadc4572679ba4444b18d76b26caeb31e29c3bf653456d98` |
| `PCRemote-0.3.6-osx-x64.zip` | 63,422,741 | `3b3f3d3257b8abfb4285a07d44e4f722d6aaf7234dd093791a7a730435f25a1e` |
| `PCRemote-0.3.6-linux-x64.tar.gz` | 60,571,608 | `799f4dd85e88fd95475681c26a8592f5f39036ce3647f8d2f3aa904bf5a9c982` |
| `PCRemote-0.3.6-osx-arm64-manifest.json` | 109,626 | `62d7daeea4f5a256ca2ce330215d2f881bd33a286abe92ab4cc3d1a31fd4d91d` |
| `PCRemote-0.3.6-osx-x64-manifest.json` | 109,624 | `ca51e96b60479446e46dfa44a1412a0448d44a171c313c40955da24deaaccaff` |
| `PCRemote-0.3.6-linux-x64-manifest.json` | 50,478 | `21f7e31e063fb1f12861c209cb83d95154f2dd0b004f3b676dc72885f667b624` |
| `SHA256SUMS.txt` | 791 | covers the eight files above |

Local verification of the downloaded workflow artifacts (new empty directory, independent script run with `python3 -I`, plus the repository's `scripts/check-package.py --final` for the three macOS/Ubuntu packages):
- Every file matches its per-platform CI checksum; the combined `SHA256SUMS.txt` was built from those files and verified with `shasum -c`.
- Manifests: `candidate: false`, `sourceDirty: false`, revision `4920597`, version 0.3.6. Every extracted file (464 per macOS bundle, 463 for Ubuntu) matches its manifest and nothing is uninventoried.
- Both macOS bundles pass `codesign --verify --deep --strict` with an ad-hoc cdhash requirement and the correct architectures (main binary plus 17 dylibs); `--package-info` reports `0.3.6+4920597…` with .NET 10.0.13, natively on Apple Silicon and under Rosetta emulation for Intel.
- Byte checks (not runtime verification) confirm the 0.3.6 bridge symbols and the named pause text in both macOS bundles.
- The Ubuntu archive has normalized ownership and no links, and its launcher is an x86-64 ELF; the Windows ZIP and installer have the expected layout.
- License notices are present, there are no debug symbols or private files, and a Gitleaks 8.30.1 scan of every extracted package is clean.

After publication, all nine public URLs returned HTTP 200 with matching sizes and SHA-256 digests when downloaded anonymously into a new directory. The public `SHA256SUMS.txt` is byte-identical, GitHub's stored asset digests match, and the tag object resolves to the release commit.

At the user's request, the verified public Apple Silicon ZIP replaced the locally signed third candidate in `/Applications`:
- The anonymous download's SHA-256 matched, the seal verified after extraction, and `--package-info` reported `0.3.6+4920597…`.
- The staged bundle was re-signed with the local identity (`scripts/sign-macos-local.py`) before the swap; the replaced bundle was moved to the Trash, not permanently deleted, and the move is recorded under the ignored `.tools/mac-install-0.3.6/`.
- The application data directory and the Keychain item were not touched.
- 0.3.6 launched; tccd answered `Allowed (System Set)` for the new process with no `Failed to match` (the third different build matched by the local identity's requirement), and the window asks only for the per-build **Allow Keychain access after update…**.
- After the user approved the Keychain dialog and the network, the public build reported Secure LAN ready and input ready, and a ten-minute watch logged one Wi-Fi record burst (22:54:50, 38 keys) with the LAN listener answering throughout and the approval kept.

## 2026-10-14 — Air pointer (gyroscope mouse) in the phone remote (unreleased, after 0.3.6)

Source changes on `main` after v0.3.6. Version stays **0.3.6**; no package was built or published, and the v0.3.6 tag and assets are unchanged. The phone remote gains an **Air** mode on the Touchpad tab: hold the pad and turn the phone to move the pointer, like a TV magic remote; lift to stop, tap to click. It is phone-client only (protocol v1 `pointer.move`, no new message, capability or companion setting), reads sensors only while Air is selected on a visible Touchpad tab with control enabled, and sends only rounded pixel deltas. Design and the dated execution log are in `AIR_POINTER_PLAN.md`.

**Checks run on macOS with the pinned toolchain (synthetic sensors, not device evidence)**
- `python3 scripts/portable.py remote-unit`: 18 page-less motion-math checks pass (grips, roll compensation, landscape remap, degeneracy, gravity sign resolution, dt clamps, spike/NaN rejection, deadzone, One-Euro, gain curve, settings, output bounds, tap rules).
- `npm run build` (strict TypeScript + Vite) and `dotnet build tests/PcController.PortableTests -c Release` succeed.
- A throwaway local check outside the repository (built bundle on a self-signed HTTPS static server, `/api/*` and `/control/ws` mocked by Playwright, synthetic `devicemotion` events, CDP touch) exercised every scenario of the new fixture spec and passed with no console errors; it explains why the fixture spec is expected to pass but is not fixture evidence.

**Not run**
- `python3 scripts/portable.py browser` (now: unit spec, `mouse`/`connection`, then `air.spec.ts` on a second fixture instance) and `browser-capabilities`: port 8443 was held by the running 0.3.6 companion. These must pass before the change is relied on.
- Hosted CI for this commit (the shared job runs `remote-unit` after installing Chromium; the Windows browser job runs the extended `browser` action).
- Real iPhone/Android pointing: axis signs, iOS Safari/Home Screen permission prompts and recovery, rest drift, tap/lift jitter, lifecycle under lock/background/takeover/Wi-Fi loss, and Windows/macOS/Ubuntu acceptance. Pending per the plan's Phase 4.
- Release integration (README feature row and screenshot, version bump, packaging): needs separate authorization.

## Version 0.4.0 — Air pointer (gyroscope mouse) in the phone remote (2026-10-15)

Preview release of the feature described in the [2026-10-14 entry](#2026-10-14--air-pointer-gyroscope-mouse-in-the-phone-remote-unreleased-after-036). The companion is unchanged on every platform: no Host, Core, Windows, macOS, Linux or desktop-shell behavior changed (one comment in `CompanionServer.cs`), the protocol stays v1, and all 0.3.x fixes remain. The phone remote gains the **Air** mode on the Touchpad tab, the shared flush spacing/rounding described in `docs/protocol.md`, and new browser checks.

**Local results before the release commit (macOS 27 Apple Silicon, pinned .NET 10.0.402 and Node 24.21.0, user's 0.3.6 companion quit for the run)**
- `portable.py build` (Release), shared portable **272**, Mac adapter **69**, desktop **92**.
- `portable.py browser`: Air pointer math **18** (page-less), connection **5** and touch gestures **1** on the first fixture instance, `air.spec.ts` **1** (14.5 s) on a second fixture instance: permission states with a stubbed `requestPermission` (denied, then granted inside a user activation), hold + yaw → `move:` events left only, hold + pitch → up only, no movement without a hold, a tap → one `click:left` and no movement, invert vertical flips `dy` and persists across a reload, a sensor stall pauses pointing without auto-resume, tab switch / connection settings / hidden page mid-hold stop pointing and record `release`, drag keeps `left:True → move → left:False`, 20–130 aggregates per second under 250 Hz injection (about 60 on headless Chromium), arrow keys on the switch, 44 px targets and no overflow at 363/390/844/1280 widths, no console errors, and the companion sends no `Permissions-Policy` header.
- `portable.py browser-capabilities`: recording compatibility spec, then macOS, Linux and unavailable fixtures at 390 and 1280 widths; `#mode-air` follows the pointer capability.
- npm/NuGet audits: no advisories. Bundled runtime advisory feed (`audit-runtime.py` with the system CA bundle and a read-only token): no matching advisories for 10.0.13.
- Gitleaks 8.30.1 over the 252 tracked and new files of the working tree: no leaks.
- `docs/assets/mobile-air.png` captured through the loopback fixture (see `docs/assets/README.md`).
- Not run locally: `check-release-docs.ps1` (PowerShell is not installed on this Mac; the Release documentation workflow runs it on push) and strict TLS (`tls`, Python 3.13+; the final dispatch runs it on every OS).

**Not established**
- Hosted CI for this commit and the final assets: recorded in the publication entry below once the final dispatch has run.
- Pointing with a physical iPhone or Android phone: axis directions, iOS Safari/Home Screen permission prompts and the deny/reload recovery, rest drift, tap/lift jitter, lifecycle under lock/background/takeover/Wi-Fi loss. Synthetic sensors prove the pipeline and gating, not sensor behavior (plan Phase 4).
- Windows installation on a physical PC, a real Ubuntu desktop, macOS Intel hardware and every earlier 0.3.x gap.

### Version 0.4.0 publication and public verification (2026-10-15)

Published as a **pre-release**: [v0.4.0](https://github.com/maamouribilel/pc-remote/releases/tag/v0.4.0). The annotated tag `v0.4.0` (object `14a74c0`, 08:53 UTC) points to the release commit `13aa270eae00fe6a119ecfcbd3825ff79c339a30` (feature commit `04c02ce`, release commit `13aa270`). Earlier tags and assets were not changed.

Hosted runs on the release commit:
- The `final` dispatch [38707534711](https://github.com/maamouribilel/pc-remote/actions/runs/38707534711) passed every job on the first attempt and supplied the assets: shared **272** and desktop **92** on Windows, macOS and Ubuntu; Air pointer math **18** on every OS; Mac adapter **68** on hosted macOS and the non-native subset **54** on Windows and Ubuntu; Linux adapter **88** on Ubuntu (81 elsewhere); Ubuntu private Xvfb/GTK US and French **41** each, portal/MPRIS/logind **31** and disposable encrypted keyring/TLS; platform-aware browser checks on every OS; the Windows default, security, pairing, lifecycle and PC-feature suites (151, 185, 193, 158, 180), the browser suite in Edge (connection and touch 6, **Air pointer 1**), strict TLS, installer and content/license/checksum checks; strict TLS probes, npm/NuGet audits and the runtime advisory feed on every OS.
- The Release documentation workflow passed on the push (38707534469) and on the tag (38708977948).

| Asset | Bytes | SHA-256 |
| --- | ---: | --- |
| `PCRemote-0.4.0-win-x64-setup.exe` | 62,703,920 | `0b7ec7be7bc18f67eef78edcde3558a45427ac39ae21a8a98273ed0bd7373c2b` |
| `PCRemote-0.4.0-win-x64.zip` | 89,746,104 | `aa4ebb3c32d81ccf420f64c5f9a22478cff3ae0efbdd62c8a494b81a59bb1b71` |
| `PCRemote-0.4.0-osx-arm64.zip` | 61,161,556 | `e5d6cd23c94f0d60efccf9b7d694c48e27c6b2353da59239c73ed2ef3e2d1f3e` |
| `PCRemote-0.4.0-osx-x64.zip` | 63,870,508 | `c84a350e4e1573a7904b169cdfded8fe8289db64a924eb4d1db813310b7deefd` |
| `PCRemote-0.4.0-linux-x64.tar.gz` | 61,028,514 | `b5f7654fdf5d04c8aed992ffa15f5da67ef5366497ece245a77a592e31d537bf` |
| `PCRemote-0.4.0-osx-arm64-manifest.json` | 109,747 | `694d59531537ac70461df1bd2d4f744d0e058dc5f59efa0d1dc4e73ff72ad3a0` |
| `PCRemote-0.4.0-osx-x64-manifest.json` | 109,745 | `98a97aa2dfeee9bb7edf5c1a00da464c87a382c24f2a2371049a385fb2a36907` |
| `PCRemote-0.4.0-linux-x64-manifest.json` | 50,478 | `1729de606f477d0bb6066c99429d9c391ad42b58361a31c07f7abf46e0e9eb07` |
| `SHA256SUMS.txt` | 791 | covers the eight files above |

Local verification of the downloaded workflow artifacts (new empty directory, independent script run with `python3 -I`, plus the repository's `scripts/check-package.py --final --output` for the three macOS/Ubuntu packages):
- Every file matches its per-platform CI checksum; the combined `SHA256SUMS.txt` was built from those files and verified with `shasum -c`.
- Manifests: `candidate: false`, `sourceDirty: false`, revision `13aa270`, version 0.4.0. Every extracted file (465 per macOS bundle, 464 for Ubuntu) matches its manifest and nothing is uninventoried.
- Both macOS bundles pass `codesign --verify --deep --strict` with an ad-hoc seal and the correct architectures (main binary plus 17 dylibs); `--package-info` reports `0.4.0+13aa270…` with .NET 10.0.13, natively on Apple Silicon and under Rosetta emulation for Intel.
- The bundled remote in all four packages contains the Air pointer (mode switch and pad markup in `index.html` and the built script).
- The Ubuntu archive has normalized ownership (0/0), no links and an x86-64 launcher; the Windows ZIP contains `PcController.App.exe` and `wwwroot` with no debug symbols; the installer is a PE executable.
- Gitleaks 8.30.1 over the four extracted package trees: no leaks. Scanning the build manifests as well reports 60 `generic-api-key` matches that are SHA-256 digests of .NET assemblies whose names contain "Authentication" or "Token" (file hashes, not secrets); the 0.3.6 scan had covered the trees only.

After publication, all nine public URLs returned HTTP 200 with matching sizes and SHA-256 digests when downloaded anonymously into a new directory. The public `SHA256SUMS.txt` is byte-identical, GitHub's stored asset digests match, and the tag object resolves to the release commit.

At the user's request ("so I can try it with my phone"), the verified public Apple Silicon ZIP replaced the installed 0.3.6 in `/Applications`:
- The anonymous download's SHA-256 matched, the seal verified after extraction, and `--package-info` reported `0.4.0+13aa270…`.
- The staged bundle was re-signed with the local identity (`scripts/sign-macos-local.py`, 26 s) before the swap; the 0.3.6 bundle was moved to a dated folder in the Trash, not permanently deleted, and the move is recorded under the ignored `.tools/mac-install-0.4.0/`.
- The application data directory and the Keychain item were not touched. 0.4.0 launched; the per-build **Allow Keychain access after update…** dialog and the session-only network approval remain the user's steps.
- Pointing with the phone had not yet been tried when this entry was written; the Air pointer plan's Phase 4 records that result.

## 2026-10-15 — Air pointer Phase 4 on a physical iPhone (unreleased, after 0.4.0)

Source changes on a branch after v0.4.0 (commit `995539d`); version stays **0.4.0**, no package built or published, the v0.4.0 tag and assets are unchanged. Full record: `AIR_POINTER_PLAN.md`, 2026-10-15 Phase 4 entries.

**Physical evidence (iPhone, iOS 18.7; macOS 27 companion)**
- The published 0.4.0 Air pointer "moves randomly" on the phone. Raw `devicemotion` captures (disposable lab page, kept outside the repository) show `rotationRate.alpha` is the rotation about the device x axis and `gamma` about z, at 60 Hz in °/s, with `accelerationIncludingGravity` pointing toward the ground (face-up z ≈ −9.7) and `interval` reported in seconds. The shipped code used the older "alpha about z" reading, so a tilt was sent as a turn; replaying the captures through the shipped pipeline reproduces it.
- With the corrected pipeline served by the development shell from this source, the user reports the flat grip moves the pointer left, right, up and down correctly.
- Rest noise measured at ±1.5 °/s (peaks ±4 °/s, hand-held alpha peaks ±8 °/s); replaying the 10 s hand-held capture gives 5 px drift with the new filter order and 2 °/s deadzone (35 px before).

**Automated checks on the corrected source (macOS, pinned toolchain)**
- `remote-unit` 19 (rewritten for the x/y/z order, platform gravity sign, seconds-valued interval, jitter drift bound, replay of recorded windows in `tests/remote/fixtures/ios-flat-motions.json`), `browser` connection 5 + mouse 1 and `air.spec.ts` 1, strict `tsc` + Vite build.

**Not established**
- The Phase 4 acceptance table (corner reach, precision, live rest drift, tap and lift jitter, drag with the corrected build), upright/rolled/landscape grips, lifecycle (lock, background, tab switch, PC Pause, takeover, Wi-Fi drop), the remote's permission flow in Safari and the Home Screen app, the preferred speed, Android, Windows 11 and Ubuntu rows: all pending.
- Hosted CI for the branch (the `final` dispatch is the single gate and only runs for a release).

## Version 0.4.1 — Air pointer axis fix (2026-10-15)

Bug-fix release of the phone remote's Air pointer; the companion is unchanged on every platform (no Host, Core, Windows, macOS, Linux or desktop-shell behavior changed, protocol v1). It ships the source recorded in the [2026-10-15 Phase 4 entry](#2026-10-15--air-pointer-phase-4-on-a-physical-iphone-unreleased-after-040): `rotationRate` alpha/beta/gamma read as rotation about x/y/z per the current W3C draft (0.4.0 used the stale alpha-about-z wording, so a tilt moved the pointer sideways on a real iPhone), the gravity sign seeded per platform (WebKit on iOS reports it toward the ground) with the per-hold resolver kept, One-Euro before a 2 °/s soft deadzone, the filter seeded at rest, and a seconds-valued `event.interval` handled. Physical evidence: an iPhone (iOS 18.7) against the macOS companion served from this source moved the pointer left, right, up and down correctly in the flat grip; the remaining Phase 4 rows stay pending.

**Local results on the release source (macOS 27.0 Apple Silicon, pinned .NET 10.0.402 and Node 24.21.0, the user's 0.4.0 companion quit with SIGTERM for the loopback suites and relaunched afterwards)**
- `portable.py build` (Release) with version 0.4.1 in every assembly.
- Shared portable **272**, Mac adapter **69**, desktop **92**.
- `portable.py browser`: Air pointer math **19** (page-less, including the replay of recorded iPhone windows), connection **5** + touch gestures **1**, `air.spec.ts` **1** (15.1 s) on the real loopback fixture, sequential on 8443.
- `portable.py browser-capabilities`: recording compatibility, then the macOS, Linux and unavailable fixtures at 390 and 1280 widths.
- Strict TLS with the pinned Python 3.13.16: `tls` (chain and hostname verification, incorrect-hostname and untrusted-certificate rejection) and `mac-tls` pass.
- npm/NuGet audits: no advisories. Bundled runtime advisory feed (`audit-runtime.py` with the system CA bundle, anonymous): no matching advisories for 10.0.13.
- Gitleaks 8.30.1 over the 253 tracked and new source files: no leaks.
- Not run locally: `check-release-docs.ps1` (no PowerShell on this Mac; the Release documentation workflow runs it on push) and the Windows suites (the final dispatch runs them).

**Not established before publication**
- Hosted CI for the release commit and the final assets: recorded in the publication entry below once the final dispatch has run.
- The Phase 4 acceptance table beyond the flat grip (measured corners/precision/drift/taps/lift/drag, other grips, lifecycle, Safari and Home Screen permission recovery, Android), Windows installation on a physical PC, a real Ubuntu desktop, macOS Intel hardware and every earlier 0.3.x/0.4.0 gap.

### Version 0.4.1 publication and public verification (2026-10-15)

Published as a **pre-release**: [v0.4.1](https://github.com/maamouribilel/pc-remote/releases/tag/v0.4.1). The annotated tag `v0.4.1` (object `cefdd7f`, 13:25 UTC) points to the release commit `cab1524db8bdce5c737a371d64fdf0517b7ee456` (fix commit `995539d`, release commit `cab1524`). Earlier tags and assets were not changed.

Hosted runs on the release commit:
- The `final` dispatch [38740418954](https://github.com/maamouribilel/pc-remote/actions/runs/38740418954) passed every job on the first attempt in eight minutes and supplied the assets: shared **272** and desktop **92** on Windows, macOS and Ubuntu; Air pointer math **19** on every OS; Mac adapter **68** on hosted macOS and the non-native subset **54** on Windows and Ubuntu; Linux adapter **88** on Ubuntu (81 elsewhere); the isolated Ubuntu private Xvfb/GTK, portal/MPRIS/logind and disposable encrypted keyring/TLS checks; platform-aware browser checks on every OS; the Windows default, security, pairing, lifecycle and PC-feature suites (151, 185, 193, 158, 180), the browser suite in Edge (connection and touch 6, **Air pointer 1**), strict TLS, installer and content/license/checksum checks; strict TLS probes, npm/NuGet audits and the runtime advisory feed on every OS.
- The Release documentation workflow passed on the push (38740409642) and on the tag (38742568946).

| Asset | Bytes | SHA-256 |
| --- | ---: | --- |
| `PCRemote-0.4.1-win-x64-setup.exe` | 62,699,058 | `682a1f69cac1416d009824111342ea3fb29f40642c4800608e4c61e08072a24b` |
| `PCRemote-0.4.1-win-x64.zip` | 89,746,539 | `79847dd20f3212b0788b81d2f4560981c451025fec9a22d0bb210b8549a47cad` |
| `PCRemote-0.4.1-osx-arm64.zip` | 61,165,708 | `9b1b4658bf99b65ff8141c753f3f48c504b426cc73ae9b5c4ea4781d28854a5e` |
| `PCRemote-0.4.1-osx-x64.zip` | 63,874,660 | `a2df9dd50bb924a7721e9a312fd461e1d7577b0145c13275c8dae1c0dd8d9c39` |
| `PCRemote-0.4.1-linux-x64.tar.gz` | 61,034,227 | `7394a3b82a01f5a9ea3ed157780b638c4772efaf6ae26f6265f72db4121d2472` |
| `PCRemote-0.4.1-osx-arm64-manifest.json` | 109,747 | `d8b2111feb7c526b7dae0371a7a7991aa46ac4803bc3ef14a0a29fad380f3590` |
| `PCRemote-0.4.1-osx-x64-manifest.json` | 109,745 | `8acde062ed369fc42d9a614af58a05ee6a55c44c1073dd5874d8a898acd78d36` |
| `PCRemote-0.4.1-linux-x64-manifest.json` | 50,580 | `50ec8c241c453555dac2682c9bd43f8d61e3f396b4737c29b29bc48d662437c8` |
| `SHA256SUMS.txt` | 791 | covers the eight files above |

Local verification of the downloaded workflow artifacts (new empty directory, independent script run with `python3 -I`, plus the repository's `scripts/check-package.py --final --output` for the three macOS/Ubuntu packages after extracting each archive into its artifact directory):
- Every file matches its per-platform CI checksum; the combined `SHA256SUMS.txt` was built from those files and verified with `shasum -c`.
- Manifests: `candidate: false`, `sourceDirty: false`, revision `cab1524`, version 0.4.1. Every extracted file (465 per macOS bundle, 464 for Ubuntu) matches its manifest and nothing is uninventoried.
- Both macOS bundles pass `codesign --verify --deep --strict` with an ad-hoc seal and the correct architectures; `--package-info` reports `0.4.1+cab1524…` with .NET 10.0.13, natively on Apple Silicon and under Rosetta emulation for Intel.
- The bundled remote in all four packages contains the corrected Air pointer (platform gravity-sign detection present in the built script).
- The Ubuntu archive has normalized ownership (0/0), no links and an x86-64 launcher; the Windows ZIP contains `PcController.App.exe` and `wwwroot` with no debug symbols; the installer is a PE executable.
- Gitleaks 8.30.1 over the four extracted package trees: no leaks.

After publication, all nine public URLs returned HTTP 200 with matching sizes and SHA-256 digests when downloaded anonymously into a new directory. The public `SHA256SUMS.txt` is byte-identical, GitHub's stored asset digests match, and the tag object resolves to the release commit.

The verified public Apple Silicon ZIP replaced the installed 0.4.0 in `/Applications` at the user's request:
- The anonymous download's SHA-256 matched, the seal verified after extraction, and `--package-info` reported `0.4.1+cab1524…`.
- The staged bundle was re-signed with the local identity (`scripts/sign-macos-local.py`, 28 s) before the swap; the running 0.4.0 was quit gracefully (SIGTERM), moved to a dated folder in the Trash (not permanently deleted), the move recorded under the ignored `.tools/mac-install-0.4.1/`, and 0.4.1 relaunched.
- The application data directory and the Keychain item were not touched. The per-build **Allow Keychain access after update…** dialog and the session-only network approval remain the user's steps; until then the new build has no listener.


## 2026-10-15 — macOS LAN approval dropped on peer-to-peer Wi-Fi (awdl/llw) record updates (unreleased, after 0.4.1)

**User report on the installed 0.4.1 app.** After approving the Wi-Fi interface, the window fell back to `https://localhost:8443` and the phone reported *TypeError: Load failed*.

Read-only diagnosis on that Mac (macOS 27 Apple Silicon; the installed 0.4.1 kept running untouched; unified log, a SystemConfiguration observer built outside the repository, the user data directory's timestamps):
- The server certificate had been reissued for `127.0.0.1` only at 16:22:13, i.e. the LAN host was replaced by the loopback host then.
- configd processed `IPv6 address change: awdl0` / `llw0` at 16:22:12–16:22:13 and about every 76 s before and after (16:18:23, 16:19:39, 16:20:56, 16:22:12, 16:23:29, 16:24:45, 16:26:02, 16:27:18, …). The observer saw each burst as five changed keys, `State:/Network/Interface/awdl0/IPv6` twice and `State:/Network/Interface/llw0/IPv6` three times, with a new randomized link-local address each time. `awdl0`/`llw0` are Apple's peer-to-peer Wi-Fi interfaces (AirDrop, AirPlay, Handoff/Continuity, Sidecar): no IPv4 address, no network service, never an approval candidate. During the observer's six minutes (16:26–16:32) no other key changed besides `LinkQuality`, and the unified log's kernel-event entries since the app's start at 14:29 show no other interface or address event; Docker's `bridge100`/`vmenet0` and eight `utun` tunnels were present and silent.
- The 0.3.5/0.3.6 filter ignores only `LinkQuality` and same-name `AirPort` updates, so every burst advanced the bridge's network-trust generation, which revoked the session's LAN approval, denied pending phone approvals and rebuilt the host on loopback. Approval therefore lasted at most about 76 s: the window "returns to localhost after approve", and the phone shows WebKit's *Load failed* for a listener that stopped. The ten-minute watch of 2026-10-14 recorded no `awdl`/`llw` key.
- .NET's `NetworkChange`, the other revoke path (`DesktopController.ConnectivityChangedAsync`), does not fire on these keys: a throwaway console probe subscribed to `NetworkAddressChanged` and `NetworkAvailabilityChanged` for six minutes (16:28:50–16:34:50, four bursts) and logged no event. It watches the `State:/Network/Service/*/IPv4|IPv6` entities, and the peer-to-peer interfaces have no service. The managed code is unchanged.

**Fix (macOS bridge):** `State:/Network/Interface/awdl<n>/…` and `llw<n>/…` keys no longer advance the network-trust generation. `Setup:` keys, `Service` and `Global` keys and the keys of every other interface (Wi-Fi, Ethernet, `utun` tunnels, VM bridges) still revoke, as do system sleep/wake, session switch and power-off. Version stays **0.4.1**; no package was built or published; the v0.4.1 tag and assets are unchanged, and the installed app still carries the old filter.

**Verification:**
- Mac adapter suite **70** (`portable.py mac-test`, Release, Keychain-backed): the new deterministic row checks that the `awdl0`/`llw0`/`awdl12` IPv6, Link and record keys are kept while `en0` IPv4/IPv6, `utun4`, `bridge100`, a Service key, `Setup:/…/awdl0/IPv6` and the non-interface names `awdl`/`llwx` revoke; every earlier row still passes.
- Live comparison on the user's Wi-Fi with the real observers (`pc_observe` without a callback, each library loaded in its own throwaway process from the installed 0.4.1 bundle and from the rebuilt `obj/Release` output, the running app untouched): over the three bursts both processes observed (16:36:13, 16:37:30, 16:38:46), the installed 0.4.1 bridge advanced its network-trust generation **15** times (5 per burst, each a LAN revocation) and the rebuilt bridge **0** times; the 0.4.1 process alone had also logged 5 at 16:34:57 before the second process started. Neither logged a pause-only event.

**Not run:** `desktop-test`, `browser`, `browser-capabilities`, strict TLS and hosted CI. Port 8443 and the single-instance lock are held by the running 0.4.1 companion, and no managed or web code changed.

**Not established:** a paired phone keeping control across the bursts on an installed build with this bridge (needs a rebuilt companion); whether other Macs publish further churn keys (VPN tunnels, VM bridges), which still revoke by design.

## Version 0.4.2 — macOS LAN approval across peer-to-peer Wi-Fi record updates (2026-10-15)

Bug-fix release of the macOS companion's native bridge; Windows, Ubuntu, the phone remote, the shared host and the protocol (v1) are unchanged. It ships the source recorded in the [2026-10-15 entry](#2026-10-15--macos-lan-approval-dropped-on-peer-to-peer-wi-fi-awdlllw-record-updates-unreleased-after-041): the `State:/Network/Interface/awdl<n>/…` and `llw<n>/…` records of Apple's peer-to-peer Wi-Fi interfaces (AirDrop, AirPlay, Handoff/Continuity, Sidecar), which carry only a randomized link-local IPv6 address and no network service and which macOS re-published about every 76 s on the user's Mac, no longer advance the network-trust generation, so they no longer revoke the session's LAN approval, deny pending phone approvals or rebuild the host on loopback. `Setup:`, `Service` and `Global` keys, the keys of every other interface (Wi-Fi, Ethernet, `utun` tunnels, VM bridges), system sleep/wake, session switch and power-off still revoke. Runtime evidence: the side-by-side observer comparison on the user's Wi-Fi recorded in that entry (installed 0.4.1 bridge 15 revocations over three bursts, rebuilt bridge 0); no phone test was run for this version.

**Local results on the release source (macOS 27.0 Apple Silicon, pinned .NET 10.0.402 and Node 24.21.0; the user's installed 0.4.1 companion was quit gracefully with SIGTERM, with the user's approval, so the loopback suites could use port 8443, and was not relaunched)**
- `portable.py build` (Release) with version 0.4.2 in every assembly.
- Shared portable **272**, Mac adapter **70** (including the peer-to-peer `awdl`/`llw` row), desktop **92**.
- `portable.py browser`: Air pointer math **19** (page-less), connection **5** + touch gestures **1**, `air.spec.ts` **1** (15.1 s) on the real loopback fixture, sequential on 8443.
- `portable.py browser-capabilities`: recording compatibility (1), then the macOS, Linux and unavailable fixtures (2 each) at 390 and 1280 widths.
- Strict TLS with the pinned Python 3.13.16: `tls` and `mac-tls` pass (chain and hostname verification, incorrect-hostname and untrusted-certificate rejection).
- The rebuilt bridge library, loaded in a throwaway Python process (`pc_test_network_key` through ctypes, no observer or store), keeps the `awdl0`/`llw0`/`awdl12` IPv6 and Link keys (0) and revokes on `en0` IPv4/IPv6, `utun4`, `bridge100`, a Service key, `Setup:/…/awdl0/IPv6`, `awdl`/`llwx` and `State:/Network/Global/IPv4` (1), 0 mismatches; the installed 0.4.1 library probed the same way returns 1 for the four `awdl`/`llw` keys, which is the defect.
- npm/NuGet audits: no advisories. Bundled runtime advisory feed (`audit-runtime.py` with the system CA bundle, anonymous): no matching advisories for 10.0.13.
- Gitleaks 8.30.1 over the 253 tracked and new source files: no leaks.
- `check-release-docs.ps1` ran locally for the first time, with the portable PowerShell 7.6.6 in `.tools/pwsh` (synchronized for v0.4.2); its base-revision comparison against `84ef9b5` (`-BaseRef`) passed on the release commit.
- Not run locally: the Windows suites and the Linux suites (the final dispatch runs them on hosted runners).

**Not established before publication**
- Hosted CI for the release commit and the final assets: recorded in the publication entry below once the final dispatch has run.
- A paired phone keeping control across the peer-to-peer record bursts on an installed 0.4.2 build (no phone test was run for this version); whether other Macs publish further churn keys (VPN tunnels, VM bridges), which still revoke by design; the Air pointer Phase 4 rows beyond the flat grip, Windows installation on a physical PC, a real Ubuntu desktop, macOS Intel hardware and every earlier 0.3.x/0.4.x gap.

### Version 0.4.2 publication and public verification (2026-10-15)

Published as a **pre-release**: [v0.4.2](https://github.com/maamouribilel/pc-remote/releases/tag/v0.4.2). The annotated tag `v0.4.2` (object `65e9cff`, 16:14 UTC) points to the release commit `8c1bba059d9cdf0581a5c2f88ffc73b86e2c8d6e` (the bridge fix, its adapter row, the diagnosis record and the release synchronization were committed together as `8c1bba0`). The release was published at 16:16 UTC. Earlier tags and assets were not changed (the v0.4.1 release still lists its nine assets with their original sizes and the v0.4.1 tag still resolves to `cab1524`).

Hosted runs on the release commit:
- The `final` dispatch [38765461369](https://github.com/maamouribilel/pc-remote/actions/runs/38765461369) passed every job on the first attempt in eight minutes and supplied the assets: shared **272** and desktop **92** on Windows, macOS and Ubuntu; Air pointer math **19** on every OS; Mac adapter **69** on hosted macOS (including the new peer-to-peer `awdl`/`llw` row; the local-desktop interactive-session check is skipped under `CI=true`) and the non-native subset **54** on Windows and Ubuntu; Linux adapter **88** on Ubuntu (81 elsewhere); the isolated Ubuntu job's private Xvfb/GTK US and French layouts (41 each), portal/MPRIS/logind protocol (31) and disposable encrypted keyring/TLS checks; platform-aware browser checks on every OS; the Windows default, security, pairing, lifecycle and PC-feature suites (151, 185, 193, 158, 180), the browser suite in Edge (Air pointer math 19, connection and touch 6, Air pointer 1), strict TLS, installer and content/license/checksum/source-revision checks; strict TLS probes, npm/NuGet audits, the runtime advisory feed and the release-documentation check on every OS.
- The Release documentation workflow passed on the push (38765462260) and on the tag (38767270415).

| Asset | Bytes | SHA-256 |
| --- | ---: | --- |
| `PCRemote-0.4.2-win-x64-setup.exe` | 62,690,740 | `55909b526538120c83fa752078607ac2e3e08ecf9c8fdb9d9a1f7dbd0252db93` |
| `PCRemote-0.4.2-win-x64.zip` | 89,746,709 | `34797f69c458719c2717e2f2067ce3ed4b2a6c927dcc96c16fad2d77d08ac74a` |
| `PCRemote-0.4.2-osx-arm64.zip` | 61,170,956 | `0c357b3b8bb95b20da88117a91ae484e6bfeb6b0f8b6717a5d1db33abb19564b` |
| `PCRemote-0.4.2-osx-x64.zip` | 63,879,924 | `e8fe2606609e688347c410fc270cc8f13a4e29d2e185a97784f754b3ddedbb4d` |
| `PCRemote-0.4.2-linux-x64.tar.gz` | 61,040,825 | `57db378155fd58657c09918699d8b531ee7c2eb296516a9d12566961534fcec1` |
| `PCRemote-0.4.2-osx-arm64-manifest.json` | 109,747 | `faebc7018c1eb40068b16d8bb6ac55cbe559f3ac26b6a06be36c4847b40716e6` |
| `PCRemote-0.4.2-osx-x64-manifest.json` | 109,745 | `b9f752e2f3bec7bb8be714f9217829b826be28fa73d152e1b553458c46544fb4` |
| `PCRemote-0.4.2-linux-x64-manifest.json` | 50,580 | `5816252f2f6d76b9a7c1a5f0c4d6c221e5fa713176ede6cf672cbea07049be64` |
| `SHA256SUMS.txt` | 791 | covers the eight files above |

Local verification of the downloaded workflow artifacts (new empty directory, independent script run with `python3 -I`, plus the repository's `scripts/check-package.py --final --output` for the three macOS/Ubuntu packages after extracting each archive into its artifact directory):
- Every file matches its per-platform CI checksum (the Windows job's checksum file has CRLF line endings and was compared after stripping the carriage returns); the combined `SHA256SUMS.txt` was built from those files and verified with `shasum -c`.
- Manifests: `candidate: false`, `sourceDirty: false`, revision `8c1bba0`, version 0.4.2. Every extracted file (465 per macOS bundle, 464 for Ubuntu) matches its manifest and nothing is uninventoried.
- Both macOS bundles pass `codesign --verify --deep --strict` with an ad-hoc seal and the correct architectures (main binary and 17 dylibs); `--package-info` reports `0.4.2+8c1bba0…` with .NET 10.0.13, natively on Apple Silicon and under Rosetta emulation for Intel.
- The fix is in the packages: the bridge library `libPcControllerMac.dylib` of each extracted bundle, loaded in a throwaway C probe (arm64 natively, x86_64 under Rosetta) that calls `pc_test_network_key`, keeps the `awdl0`/`llw0`/`awdl12` IPv6, Link and record keys and still revokes on `en0` IPv4/IPv6, `utun4`, `bridge100`, a Service key, `Setup:/…/awdl0/IPv6`, `awdl`/`llwx` and `State:/Network/Global/IPv4` (0 mismatches each). Its manifest digest differs from the 0.4.1 manifests' (arm64 `1c66d264…` → `eae071ab…`, Intel `baf776b5…` → `164e4d17…`); the bundled remote is the unchanged 0.4.1 Air pointer build in all four packages.
- The Ubuntu archive has normalized ownership (0/0), no links and an x86-64 launcher; the Windows ZIP contains `PcController.App.exe` and `wwwroot` with no debug symbols; the installer is a PE executable.
- Gitleaks 8.30.1 over the four extracted package trees: no leaks.

After publication, all nine public URLs returned HTTP 200 with matching sizes and SHA-256 digests when downloaded anonymously (no token) into a new directory and compared with an independent `python3 -I` script. The public `SHA256SUMS.txt` is byte-identical, GitHub's stored asset digests match every file, the tag reference resolves to the annotated tag object `65e9cff` and that object to the release commit `8c1bba0`, and every v0.4.2 download and release-notes link in the published README answers 200.

The verified public Apple Silicon ZIP replaced the installed 0.4.1 in `/Applications` at the user's request (approved in chat after publication):
- The anonymous download's SHA-256 matched the public `SHA256SUMS.txt`, the seal verified after extraction with `ditto`, `--package-info` reported `0.4.2+8c1bba0…` and the staged bundle's bridge library kept the `awdl`/`llw` keys in the throwaway probe.
- The staged bundle was re-signed with the local identity (`scripts/sign-macos-local.py`, 28 s) before the swap; its designated requirement is unchanged from the earlier locally signed builds. The 0.4.1 bundle had been quit gracefully (SIGTERM) at 16:58 for the loopback suites and was not running at swap time; it was moved to a dated folder in the Trash (not permanently deleted), the move recorded under the ignored `.tools/mac-install-0.4.2/`, and 0.4.2 launched from `/Applications` at 17:27.
- The application data directory and the Keychain item were not touched. The per-build **Allow Keychain access after update…** dialog and the session-only network approval remain the user's steps; until then the new build has no LAN listener. Whether the approval now survives the peer-to-peer record bursts with a paired phone on this installed build is still to be observed.

**Installed-build check with the paired iPhone (2026-10-15, 17:29–17:42 local time, read-only on the running app).** The user approved the Keychain dialog, the Wi-Fi interface and Resume before this session asked for them, by 17:30:09: the `/Applications` process (PID 96523, launched 17:29:30 according to `ps`, so a relaunch after the 17:27 start recorded above) started the host on loopback at 17:29:38 and issued the LAN certificate at 17:30:09 (`server.protected`, `addresses.txt` and the exported root/profile rewritten; timestamps only, no file in the data directory was opened), after which `lsof` showed the Mac's Wi-Fi address on 8443 beside `127.0.0.1:8443`. Observed from the Mac side:
- A 15-second `lsof`/`stat` watcher recorded 47 identical samples from 17:30:51 to 17:42:26: both binds, the same process and no rewrite of the certificate files (still at 17:30:09 in a listing taken after the last burst below). The remembered-device store `devices.protected` was rewritten once, at 17:31:48 (timestamp only).
- A `log stream --info` capture of configd's `IPv6 address change` messages from 17:30:48 recorded ten peer-to-peer bursts, all after the approval: 17:31:00, 17:32:16, 17:33:33, 17:34:49, 17:36:05, 17:37:22, 17:38:38, 17:39:55, 17:41:11 and 17:42:28, five lines each (`awdl0` twice, `llw0` three times) and 76–77 s apart. `log show --info` also lists bursts at 17:25:54, 17:27:10, 17:28:39, 17:29:00, 17:29:24 and 17:29:58 around the relaunch (plain `log show` without `--info` does not return these info-level lines). None of them changed the listener, the process or the certificate files.
- The phone, a second LAN host, was already connected when the connection watcher started at 17:33:36. At about 17:34 the user answered that the phone moves the pointer and the window shows the Wi-Fi address; the five-minute hold then ran from 17:34:27 to 17:39:48 with four bursts inside it (17:34:49, 17:36:05, 17:37:22, 17:38:38). The phone's TCP connection was present from 17:33:38 to 17:36:24, absent from 17:36:39 to 17:40:25, and new connections from the same phone appeared at 17:40:40 and 17:41:55, to the same LAN address, with no pairing or certificate change on the Mac.
- Asked afterwards, the user answered that the phone stayed connected and in control the whole time and that the window kept the LAN address; that the phone's screen turned off (auto-lock) at some point during the five minutes without the user leaving the remote page; and, in their own words, "turned off but when i turned on the app still working".

Result: on this installed 0.4.2 build the session's LAN approval survived more than twelve minutes and ten peer-to-peer bursts with the paired phone, so the 0.3.6–0.4.1 failure (loopback fallback within about a minute, "TypeError: Load failed" on the phone) did not reproduce. The gap in the phone's connection coincides with the auto-lock the user reported, and the remote reconnected to the same address when the screen came back; the Mac-side records cannot distinguish that further, and no error was reported on the phone. Not observed: a longer session, a second Mac's churn keys, and the remaining gaps listed under "Not established before publication".

## 2026-10-15 — Documentation refresh and browser demo (0.4.2)

Documentation-only maintenance on source baseline `0ed707c`, retaining app/release version **0.4.2** and its published tag/assets. The README now leads with the actual Touch/Air, keyboard/navigation, media, launcher and session controls; platform downloads; four-step pairing; and a captioned demo. Setup documents Air permission/recovery and browser-local settings. Current limitations are separated from preserved historical notes. Detailed development commands moved to `docs/development.md`; `docs/demo.md` provides a two-minute hands-on tour. Both agent files describe the media/documentation workflow. No tracked runtime/build/package source changed.

Fresh media: seven direct browser screenshots plus a static demo preview and a **26.6-second, 13-frame looping GIF (416,961 bytes)** under `docs/assets/v0.4.2/`. Older captures are preserved. The GIF adds captions beside actual app screenshots; the UI is not reconstructed. See [media provenance](assets/README.md) and [the tour](demo.md).

Observed local checks:

- Exact SDK **10.0.402**, Node **24.21.0**, npm **11.19.0**. Locked `npm ci` reported zero advisories; TypeScript checking and production Vite build passed. A Release build of the disposable portable capture fixture passed with zero warnings/errors. These are frontend/fixture checks, not a new full Windows release build or native regression run.
- Playwright **1.64.0** with Microsoft Edge **155.0.4283.45**. Browser plugin not available; the pinned Playwright/Edge fallback was used. The real browser remote ran at **390×844**, **1280×860**, and **844×390**. Page identity, meaningful content, no framework overlay, no console/page errors and no horizontal overflow passed.
- The existing portable `MouseBrowserFixture` ran with recording adapters and temporary storage. A disposable Core/Host/test-project copy changed only its host port constant to **18443**, so the user's running companion retained 8443 without interruption. The current built frontend was copied unchanged. The isolated browser alone ignored the temporary fixture certificate; no installation trust or production profile was used. The fixture and temporary source/keys were removed afterward.
- Capture interactions: fresh pairing, Touch movement, explicit drag/release, Air ready/hold/lift/settings with **synthetic motion**, Unicode text submission/acknowledgement, navigation, VLC profile selection, volume read-back, test launcher selection and automatic Media navigation, then dismissal of a sleep confirmation. Recording output included movement, held/released input, text length, arrow/media commands, `volume:36` and `launch:Test player`. **No power command was dispatched.**
- GitHub Markdown API rendered README, demo, setup and development pages. Local browser previews with a GitHub-style stylesheet passed at **1200×1000** and **390×844**, including all image loads, no body overflow/console errors, the Demo anchor and expanded screenshot details. This is local preview evidence, not an observed post-push GitHub page.
- All **173** local paths/anchors in the final refreshed documents resolved, including the evidence additions and incoming README anchors. Six current README release download URLs returned **HTTP 200** and their Content-Length values matched the public release asset metadata. These were link/size probes, not a new binary digest audit or replacement of publication verification. All eight public PNGs (seven screenshots and the poster) have no embedded metadata.
- `scripts/check-release-docs.ps1`, `git diff --check` and the staged diff check passed. Gitleaks **8.30.1** scanned the staged documentation/media before commit and found no leaks; binary screenshots were also visually inspected and checked for embedded metadata.

No new physical-phone, Android/iPad/Firefox, native input/audio/player/login/power, clean-account/reboot/uninstall, macOS Intel or real Ubuntu/GNOME acceptance was performed by this documentation refresh. Air sensor samples in this capture are synthetic; historical iPhone flat-grip direction evidence remains separate. During this work, upstream documentation commit `08ed1c4` added the installed 0.4.2 paired-iPhone check across ten peer-to-peer bursts over twelve minutes, with the user reporting continued control and reconnection after auto-lock. That entry is preserved above and incorporated into current user-facing status. Longer sessions and other Macs remain unverified. Published binaries and tags are unchanged.
