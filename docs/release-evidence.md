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
