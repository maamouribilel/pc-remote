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

Published as a **pre-release**: [v0.3.0](https://github.com/maamouribilel/windows-pc-mobile-controller/releases/tag/v0.3.0). The annotated tag `v0.3.0` (tag object `dbdcbd3`) points to commit `6bf908c030f7f8c7903c4a4cc0e71fa186be183a`. Existing v0.2.0/v0.2.1 tags and assets were not changed.

Actual hosted runs on `6bf908c`: push run [38463186338](https://github.com/maamouribilel/windows-pc-mobile-controller/actions/runs/38463186338) and `final` dispatch [38463184359](https://github.com/maamouribilel/windows-pc-mobile-controller/actions/runs/38463184359) passed every job on Windows, macOS ARM64 and Ubuntu 24.04 x64 (same suites and counts as the `826e46c` row above); the tag's release-documentation run [38464949711](https://github.com/maamouribilel/windows-pc-mobile-controller/actions/runs/38464949711) passed for `v0.3.0`. All assets come from the `final` dispatch artifacts:

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

**Hosted CI on fix commit `62644b2` (run [38471786573](https://github.com/maamouribilel/windows-pc-mobile-controller/actions/runs/38471786573)), all jobs passed:** Windows validation default 147, security 181, pairing 187, lifecycle 154, PC feature 176, browser 6, strict TLS and candidate packaging; portable shared 265 and desktop 74 on Windows, macOS and Ubuntu x64 (Mac adapter 59 on macOS); Ubuntu x64 private Xvfb/GTK US 41 and French 41, private portal/MPRIS/logind 29, disposable keyring/strict TLS 24; all three candidate packages. The release commit's own runs and final digests follow in the publication entry after it is built.

On version commit `35bad7c` the push run [38472970739](https://github.com/maamouribilel/windows-pc-mobile-controller/actions/runs/38472970739) passed every job, but the `final` dispatch [38472969018](https://github.com/maamouribilel/windows-pc-mobile-controller/actions/runs/38472969018) failed one Windows browser check: the loopback test fixture crashed with `UnauthorizedAccessException` replacing its event snapshot while Playwright was reading it. The fixture now skips a refused replacement until its next 50 ms snapshot (the first metadata write must still succeed). That dispatch's artifacts were not used.

**Not established for 0.3.1:** real Space/full-screen/display/Secure Input transitions on the installed app with a connected phone, approval of the Keychain update dialog for this build, and every gap listed for 0.3.0 (Windows installation/iPhone, macOS Gatekeeper/installed/Accessibility/player/login/sleep/phone, Intel hardware, Ubuntu GNOME desktop). Taking a player full-screen still pauses control by design; Resume is local.


### Version 0.3.1 publication and public verification (2026-10-13)

Published as a **pre-release**: [v0.3.1](https://github.com/maamouribilel/windows-pc-mobile-controller/releases/tag/v0.3.1). The annotated tag `v0.3.1` points to commit `8fc1d1cf1e3f2f2d270086bada541af5ac024942`. The v0.2.0, v0.2.1 and v0.3.0 tags and assets were not changed.

Hosted runs on `8fc1d1c`: the `final` dispatch [38474378271](https://github.com/maamouribilel/windows-pc-mobile-controller/actions/runs/38474378271) passed every job and supplied all assets. The push run [38474378406](https://github.com/maamouribilel/windows-pc-mobile-controller/actions/runs/38474378406) passed every job on attempt 2. On attempt 1, the macOS portable job's `dotnet list package --vulnerable` exited 1 without reporting an advisory. The same audit passed in five parallel jobs and in a local re-check, and only that job was re-run. The tag documentation run [38475858646](https://github.com/maamouribilel/windows-pc-mobile-controller/actions/runs/38475858646) passed.

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

**Hosted CI on fix commit `0601761`** (run [38539382226](https://github.com/maamouribilel/windows-pc-mobile-controller/actions/runs/38539382226)) passed every job:
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

Published as a **pre-release**: [v0.3.2](https://github.com/maamouribilel/windows-pc-mobile-controller/releases/tag/v0.3.2). The annotated tag `v0.3.2` points to commit `ece5d4c463034141ad0bcf49c22310775521e8d8`. Earlier tags and assets were not changed.

Hosted runs on `ece5d4c`, all of which passed every job:
- The `final` dispatch [38540711618](https://github.com/maamouribilel/windows-pc-mobile-controller/actions/runs/38540711618) supplied all assets. Windows default 151, security 185, pairing 193, lifecycle 158, PC feature 180, browser 6; shared 272 and desktop 79 on all three OSes; Mac adapter 62; Linux faults 88; Xvfb/GTK US 41 and French 41; portal/MPRIS/logind 31; keyring/strict TLS 24.
- The push run [38540702103](https://github.com/maamouribilel/windows-pc-mobile-controller/actions/runs/38540702103).
- The documentation runs for the commit and for the tag ([38542158676](https://github.com/maamouribilel/windows-pc-mobile-controller/actions/runs/38542158676)).

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

The documentation commit `dd547d2` that recorded this publication passed the Security and regression checks (run [38542724706](https://github.com/maamouribilel/windows-pc-mobile-controller/actions/runs/38542724706)) and Release documentation workflows. Afterwards, at the user's request:
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
- Hosted CI on fix commit `bc4ff3f` (run [38597846729](https://github.com/maamouribilel/windows-pc-mobile-controller/actions/runs/38597846729)) passed every job:
  - Windows default 151, security 185, pairing 193, lifecycle 158, PC feature 180, browser 6.
  - Shared 272 and desktop 85 on Windows, macOS and Ubuntu x64; Mac adapter 62; Linux faults 88.
  - Ubuntu x64 isolated suites.
  - All provisional packages.
- On release day the runtime advisory feed still had no advisories for .NET 10.0.13.

**Not established:** the 0.3.2 gaps remain, including native appearance on a real display, real macOS logout, real GNOME, Windows installation and physical-phone/LAN use. The final assets and digests follow in the publication entry.


### Version 0.3.3 publication and public verification (2026-10-14)

Published as a **pre-release**: [v0.3.3](https://github.com/maamouribilel/windows-pc-mobile-controller/releases/tag/v0.3.3). The annotated tag `v0.3.3` points to commit `83130897a3d126d1f5837ad22e2b8813d2fdd6ea`. Earlier tags and assets were not changed.

Hosted runs on `8313089` all passed every job:
- The `final` dispatch [38599277310](https://github.com/maamouribilel/windows-pc-mobile-controller/actions/runs/38599277310) supplied the assets.
- The push run [38599266569](https://github.com/maamouribilel/windows-pc-mobile-controller/actions/runs/38599266569).
- The documentation runs for the commit and for the tag ([38600703519](https://github.com/maamouribilel/windows-pc-mobile-controller/actions/runs/38600703519)).

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
