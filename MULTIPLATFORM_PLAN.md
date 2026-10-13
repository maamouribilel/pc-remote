# PC Remote — macOS and Linux implementation phases

Created: 2026-10-13. Baseline at creation: app version **0.2.1**, public [v0.2.1 preview](https://github.com/maamouribilel/windows-pc-mobile-controller/releases/tag/v0.2.1).

This is an implementation roadmap. macOS/Linux support and the checks below have **not** been implemented or validated by creating this document. Android and Android TV are deferred.

## How to use this document

1. Run the phases in order. Copy the complete prompt under the phase you want to execute into a chat opened in this repository.
2. Each prompt requests implementation of that phase, including appropriate checks, documentation, and a handoff. Do not automatically start the next phase.
3. Before a later phase, read the previous phase's progress and execution log. Inspect the actual code rather than assuming that a checked box establishes compatibility.
4. A phase can finish its implementation while some physical-device checks remain pending. Track implementation, automated verification, and native/LAN acceptance separately. A missing manual result cannot be described as passed or erased by the next phase.
5. If work is interrupted, rerun the same prompt. Resume from the repository and log; preserve completed work and unrelated user changes.

Public release publication is a separate authorized action under `AGENTS.md`. These phase prompts authorize development and provisional packaging; they do not authorize a new public release or replacement of existing release assets.

## Scope and architecture

Keep the existing TypeScript browser remote, C#/.NET core, local HTTPS/WSS transport, and desktop-approved pairing. Add a shared Avalonia desktop interface for macOS and Linux. Keep the Windows WPF companion functioning during the migration.

Proposed project boundaries; equivalent names are acceptable if recorded in the execution log:

| Project | Responsibility |
| --- | --- |
| `PcController.Core` | Validated commands, logical actions, pairing, ownership, cleanup, and platform contracts |
| `PcController.Host` | Portable HTTPS/WSS hosting, endpoints, certificate orchestration, and server lifecycle |
| `PcController.Windows` | Existing Windows input, DPAPI, network, volume, launcher, and OS integration |
| `PcController.MacOS` | macOS native input, permission checks, protected storage, and OS integration |
| `PcController.Linux` | X11/Wayland input, protected storage, and Linux desktop integration |
| `PcController.Desktop` | Shared Avalonia setup window, local approval UI, preferences, and tray/menu-bar actions |
| `src/remote` | Bundled browser remote with platform-aware labels and capability-driven controls |

Initial validation targets are macOS Apple Silicon, with Intel builds and native acceptance tracked separately, and Ubuntu 24.04 LTS x64/GNOME on X11 and Wayland. Recheck supported dependency/OS combinations when selecting versions. Other Linux distributions, KDE, headless sessions, and additional architectures need their own evidence before support is advertised.

The first usable milestone is Phase 3: a Mac pairs with the existing phone remote and supports pointer, keyboard, and safe cleanup. Phase 4 establishes the equivalent Linux milestone. Phase 5 completes supported desktop features and release readiness.

## Rules for every phase

- Read the current `AGENTS.md`, `CLAUDE.md`, `README.md`, `docs/protocol.md`, and `.github/SECURITY.md`. They remain authoritative; this roadmap does not override their security or release rules. Read the phase-specific files below as needed.
- Inspect Git status, the existing implementation, pinned toolchain, and available runtimes before editing. Preserve unrelated changes. Make routine implementation decisions independently and record material deviations from this plan.
- Keep normal operation local and bundled. Preserve exact Origin/Host checks, secure HttpOnly cookies, desktop approval, single-controller ownership, revocation, allowlisted commands, bounded requests, separate unpaired/device HTTP budgets, and reserved cancellation/logout capacity.
- Preserve ownership-lock rechecks after asynchronous request-body reads. Permission loss, disconnect, takeover, pause, suspend, server replacement, and exit must release tracked input or keep control paused when cleanup fails.
- Require strict certificate chain/name validation with appropriate server AKI/SKI extensions. Retain existing Windows roots and remembered devices during refactoring. Never add production HTTP or certificate-verification bypasses.
- Protect certificate private keys and remembered authorization data with a reviewed platform storage design. Maintain Windows user-bound DPAPI protection. Use macOS Keychain and a supported Linux desktop keyring; handle locked/unavailable stores explicitly instead of silently storing secrets in plaintext.
- Treat LAN exposure as an explicit trust decision. A private IP alone is insufficient. macOS/Linux setup must select and approve the trusted network/interface and recheck trust when connectivity changes. Until trust is established, bind only to loopback. Keep certificate SANs and displayed addresses consistent with the chosen bindings.
- Expose platform capabilities so unsupported actions are unavailable in both the browser and backend. Do not advertise recording adapters or unimplemented features as working native control.
- Isolated test fixtures may use recording adapters and temporary test storage. Keep them separate from normal operation, loopback-only, and clearly identified in evidence.
- Use the pinned toolchain where supported. Any dependency/toolchain adjustment needs a documented reason, compatible lockfiles, license review, and relevant advisory checks. Keep downloads, generated binaries, settings, credentials, logs, and personal machine information out of Git.
- Run meaningful checks for the change. Native input checks must use explicitly created disposable targets and abort if target focus is lost. Do not inject into the user's active work. Real shutdown, restart, suspend, uninstall, and trust changes require appropriate separate task authorization and disposable targets; recording tests cannot establish their physical execution.
- Never claim CI proves native input, physical iPhone use, installed-app behavior, or LAN trust. Record exact environments and commands, with sensitive details redacted, and retain historical evidence.
- Update architecture/build/security instructions in both agent files when affected. Keep public release declarations and links aligned with the actual published release. Development work must not invent new download links or claim a new release is complete.
- Update this document's progress table and append dated evidence to its execution log. Include changed files, checks and results, remaining gaps, decisions, and what the next phase needs. End the phase with a concise handoff.

## Progress

Use `Not started`, `In progress`, or `Implemented` for implementation. Record automated and native/LAN results independently; include `Pending` or `Unavailable` when appropriate.

| Phase | Implementation | Automated checks | Native / physical-phone / LAN acceptance |
| --- | --- | --- | --- |
| 1. Portable backend | Implemented | macOS ARM64 and Ubuntu 24.04 ARM64 container: 254 portable checks; Release builds; macOS strict TLS and browser checks pass. CI matrix configured, not executed. | Windows runtime/DPAPI/native regression execution pending; phone/LAN acceptance pending |
| 2. Shared desktop shell | Implemented | macOS ARM64 / Ubuntu 24.04 ARM64 container: 257 portable and 51 desktop checks; 13 macOS browser checks; strict TLS/audits and Release/cross-builds pass. CI configured, not executed. | Native macOS setup window/menu/Settings/Quit verified; Linux X11 startup under Xvfb verified. Native control, GNOME tray, Windows execution, installed-app and phone/LAN acceptance pending |
| 3. macOS MVP | Implemented | macOS ARM64: 260 portable / 60 desktop / 36 adapter-Keychain checks; 13 browser checks; real Keychain HTTPS/WSS and strict TLS pass; ARM64/x64 development candidates built | Apple Silicon guarded Cocoa input/shortcuts and interruption cleanup passed (41 adapter checks); physical-phone/LAN, lock/suspend/permission-loss, Intel hardware and installed/signed acceptance pending |
| 4. Linux MVP | In progress — source candidate wired; real GNOME gates pending | macOS/Ubuntu ARM64: 260 shared / 60 desktop / 47 Linux faults; isolated X11 US/French 30 each; 14 private-portal protocol; 24 real keyring/instance/strict-TLS checks; CI configured, not executed | Ubuntu 24.04 x64/GNOME X11 and Wayland text/cleanup, lock/suspend/tray/roaming and physical-phone/LAN acceptance pending |
| 5. Feature completion and release readiness | Implemented supported feature adapters, final packaging/CI and readiness handoff; released as experimental macOS/Ubuntu archives in v0.3.0; v0.3.1 keeps macOS LAN approval across Space/display/readiness changes | Local Mac/shared/Linux isolated checks plus observed hosted Windows/macOS/Ubuntu x64 CI recorded below | Manual native/player/audio/login/power/installed/phone/LAN acceptance pending |

## Phase 1 — portable backend and test foundation

**Outcome:** shared server logic and tests build without WPF or native Windows dependencies, and the Windows companion continues using its existing adapters and protected data.

**Copy this prompt:**

```text
Implement Phase 1 of MULTIPLATFORM_PLAN.md: portable backend and test foundation.

Read MULTIPLATFORM_PLAN.md in full, AGENTS.md, CLAUDE.md, README.md,
docs/protocol.md, and .github/SECURITY.md. Follow the roadmap's shared rules.
Inspect the current repository, Git state, pinned tools, and available platforms.
Implement only Phase 1 and preserve unrelated work.

Extract CompanionServer, its endpoint partials, and reusable lifecycle code from
the WPF app into a portable PcController.Host project (or a documented equivalent).
Remove direct construction of Windows services from the shared host. Introduce
explicit contracts/factories for native input, volume/launcher/power actions,
protected storage, certificates, trusted network discovery, and lifecycle needs.
Move genuinely portable preferences out of the Windows assembly while retaining
OS-specific launcher validation and discovery behind adapters.

Keep the WPF app wired to Windows implementations. Preserve the existing DPAPI
data format, trusted root, leaf-renewal behavior, and remembered devices. Refactor
certificate generation separately from OS key storage and filesystem protections.
Do not introduce a weaker production storage fallback.

Audit Windows assumptions in Core: modifier names, shortcut/media mappings,
scroll units, executable paths, and error messages. Establish logical platform
semantics and a capability model without changing existing Windows behavior.
If a protocol extension is needed, document its version/compatibility strategy
and keep the bundled Windows frontend compatible throughout the migration.

Separate portable protocol, pairing, ownership, transport, and security checks
from WPF/native Windows checks. Add portable loopback fixtures with recording
adapters and temporary test storage, never used by normal production startup.
Make build/test entry points support macOS and Linux as well as Windows; remove
unconditional dotnet.exe/npm.cmd/Windows tool paths from portable entry points.
Keep pinned versions unless an evidenced compatibility issue requires a change.
Extend CI to build/test shared code on Windows, macOS, and Linux while retaining
the Windows-specific regression job. Add strict TLS positive and negative checks.

Acceptance: shared projects build on available non-Windows systems; portable
pairing/ownership/security/cleanup checks pass; the Windows app remains wired to
real Windows adapters; protected-data migration tests preserve root/device data;
Windows build and regression checks pass where a Windows runner is available.
Record unavailable platform checks as pending, not as successes. Configuration
of a CI job alone is not evidence that it has run successfully.

Update relevant docs and both agent files for actual architecture/build changes.
Update Phase 1's progress and append dated results and next-phase prerequisites
to MULTIPLATFORM_PLAN.md. Do not bump the public release version or publish assets.
Finish with changed files, validation results, remaining gaps, and a handoff.
Do not start Phase 2 automatically.
```

## Phase 2 — shared desktop shell and platform-aware remote

**Outcome:** the Avalonia companion opens on macOS/Linux and exposes the existing setup/control workflow, with accurate readiness and capability reporting.

**Copy this prompt:**

```text
Implement Phase 2 of MULTIPLATFORM_PLAN.md: shared desktop shell and
platform-aware remote.

Read MULTIPLATFORM_PLAN.md in full, AGENTS.md, CLAUDE.md, README.md,
docs/protocol.md, .github/SECURITY.md, and docs/ui-design.md. Follow the roadmap's
shared rules. Verify Phase 1's implementation and log. Resolve prerequisite code
defects needed for this phase; retain pending native/manual evidence honestly.
Implement only Phase 2 and preserve unrelated work.

Create a PcController.Desktop Avalonia app for macOS/Linux, selecting a supported
stable version compatible with the toolchain and intended OS baseline. Pin it,
record the choice, and update lockfiles and third-party notices. Preserve the
existing app's visual language and use accessible controls and platform conventions.

Connect the shell to the portable host and implement connection address/manual
fallback, pairing QR, local approval/deny dialogs, pending-request expiry,
controller status, remembered-device revocation, preferences, Pause/Resume,
Release input, and graceful Exit. Implement macOS menu-bar and Linux tray behavior
with an accessible window fallback where a tray is unavailable. Preserve approval
denial on dismissal, timeout, failed storage, and shutdown. Keep the Windows WPF
companion functioning; Windows UI migration is outside this phase.

Implement trusted-network setup and permission/readiness reporting. LAN listeners
must require explicit network approval and must stop or require renewed approval
when network identity changes. Do not equate every RFC1918 address with trust.
Keep displayed addresses, QR links, certificate SANs, and listener bindings aligned.

Extend authenticated status/capability reporting and the browser remote so labels,
modifier names, shortcuts, and available controls reflect the connected platform.
Capabilities are informational: backend authorization and availability checks
remain mandatory. Preserve compatibility with the existing Windows behavior and
test the protocol strategy chosen in Phase 1.

Before native adapters are complete, show clear unavailable/setup-required states.
Exercise pairing and shell workflows with the isolated portable test fixture.
Never substitute recording input for a production adapter or report native input
readiness when native control is unavailable.

Acceptance: the shell builds and opens on available macOS/Linux environments;
approval/deny/expiry/revoke/Pause/Exit workflows pass appropriate UI and host
checks; browser capability/label checks pass; unavailable controls stay disabled
and backend requests fail safely; Windows/browser regressions pass where runnable.
Record real shell startup separately from fixture UI checks and native control.

Update affected setup/UI/protocol/build documentation and both agent files when
their instructions change. Update Phase 2's progress and append dated evidence
and a Phase 3 handoff to MULTIPLATFORM_PLAN.md. Do not bump the public release
version or publish assets. Do not start Phase 3 automatically.
```

## Phase 3 — usable macOS MVP

**Outcome:** a real macOS companion can pair with the existing phone browser and safely control pointer and keyboard input in the signed-in user's session.

**Copy this prompt:**

```text
Implement Phase 3 of MULTIPLATFORM_PLAN.md: usable macOS MVP.

Read MULTIPLATFORM_PLAN.md in full, AGENTS.md, CLAUDE.md, README.md,
docs/protocol.md, .github/SECURITY.md, docs/certificates.md, and prior phase logs.
Follow the roadmap's shared rules. Verify the shared host and desktop shell are
ready; resolve prerequisite code defects necessary for macOS. Implement only
Phase 3, preserving unrelated changes and working Windows behavior.

Implement PcController.MacOS native adapters using supported macOS APIs and the
shared contracts. Provide relative pointer movement, left/right/double click,
scrolling, drag, held-button release, Unicode text, navigation keys, held modifiers,
bounded key repeat, and common shortcuts. Translate logical copy/paste/select-all/
undo/switch-app actions into Mac semantics. Verify keyboard-layout behavior and
record target-app/text limitations; do not assume Windows key codes or wheel units.

Add explicit Accessibility permission detection/onboarding and clear recovery
instructions. Control must remain unavailable until permission exists, and must
pause safely when permission is revoked or native insertion/cleanup fails.
Track the native events this adapter can leave held and make cleanup idempotent.
Do not request unrelated permissions or run the companion as root.

Implement protected certificate/device storage backed by macOS Keychain and
restricted per-user data files where needed. Verify persistence, revocation,
restart, locked/unavailable storage, and certificate renewal. Keep the per-install
trusted root stable when addresses change or server leaves renew. Validate strict
chain/name trust; the phone receives only public trust material.

Wire the real adapters into production macOS startup. Handle single instance,
graceful exit, session lock/suspend/resume, network changes, and permission loss.
Complete trusted-network setup and actionable error states. Keep unfinished
volume/media/launcher/power features unavailable until Phase 5 implements them.
Produce a runnable local .app candidate; distinguish unsigned development startup
from signed/notarized distribution and record any onboarding limitations.

Run shared/security/browser checks and guarded native smoke checks against
explicitly created disposable targets on an available Mac. Cover actual pointer
and key insertion, Unicode, shortcuts, drag/modifier cleanup, pause, revoke,
disconnect, takeover, watchdog expiry, and host restart. Abort native tests on
target focus loss. Do not change trust or sleep/reboot the active machine without
the required separate task authorization. Test Apple Silicon first and track
Intel builds/native acceptance separately.

Acceptance: on an available real Mac, the actual phone/browser can establish
strictly trusted HTTPS/WSS, receive desktop approval, remember/revoke pairing,
move/click/scroll/drag, send text/navigation/shortcuts, and release input safely.
If physical-phone, LAN, Intel hardware, or other native checks are unavailable,
complete independent implementation/checks and record precise pending steps.
Do not claim physical acceptance from recording adapters or browser emulation.

Update macOS setup, certificates, limitations, protocol, third-party notices, and
agent instructions when affected. Update Phase 3's progress and append dated
results and remaining gaps to MULTIPLATFORM_PLAN.md. Keep public release claims
accurate; do not publish or replace released assets. Finish with runnable commands
and a Linux handoff. Do not start Phase 4 automatically.
```

## Phase 4 — usable Linux MVP on X11 and Wayland

**Outcome:** the same browser workflow controls a documented Linux desktop baseline, with separate native acceptance for X11 and Wayland.

**Copy this prompt:**

```text
Implement Phase 4 of MULTIPLATFORM_PLAN.md: usable Linux MVP on X11 and Wayland.

Read MULTIPLATFORM_PLAN.md in full, AGENTS.md, CLAUDE.md, README.md,
docs/protocol.md, .github/SECURITY.md, docs/certificates.md, and prior phase logs.
Follow the roadmap's shared rules. Verify the portable host/shell and reuse the
Mac MVP's shared improvements without copying Mac-specific input or storage.
Implement only Phase 4 and preserve unrelated work and Windows/macOS behavior.

Implement PcController.Linux adapters for the proposed Ubuntu 24.04 LTS x64/GNOME
baseline, rechecking dependency/OS support. Implement X11 input first, then
Wayland through the desktop Remote Desktop portal, using the supported portal
input transport and explicit user consent. Detect the actual session/backend
and validate required capabilities. XWayland window rendering does not establish
permission to control arbitrary Wayland applications.

Support pointer movement/clicks/scroll/drag, held-button release, navigation,
modifiers, bounded repeats, shortcuts, and tested text insertion on each backend.
Validate Unicode and keyboard-layout behavior in real disposable apps. Document
backend limitations and expose accurate capabilities instead of pretending that
unimplemented text or input works. The Wayland gate includes text and cleanup,
not only permission-dialog creation or D-Bus connectivity.

Handle portal denial, revocation/session closure, unsupported portal providers,
desktop lock/suspend/resume, display-server disconnects, and application exit.
Release tracked input or pause safely; never silently enable a privileged input
fallback. Normal operation must not require root or a privileged background daemon.

Implement protected storage via an available, supported Secret Service/keyring
provider with restricted per-user files as needed. Review actual provider storage
properties; do not assume the API alone guarantees DPAPI-equivalent protection.
Handle locked/missing keyrings explicitly. Preserve roots and remembered devices
across restart, address changes, and leaf renewal; validate TLS chain/name trust.
Complete Linux network approval, single-instance/lifecycle integration, tray/window
fallback, setup prerequisites, and production wiring of real input adapters.

Run shared/security/browser tests and guarded disposable-target native checks on
available Linux environments. Test X11 and GNOME Wayland separately: real input,
Unicode, shortcuts, key/drag interruption, permission loss, pause/revoke/takeover,
watchdog cleanup, and remembered reconnect. Headless/fake tests and a successful
build cannot establish native desktop or physical-phone/LAN acceptance.

Acceptance: the documented Linux baseline pairs over strictly trusted HTTPS/WSS
and supports the MVP's pointer, keyboard, and cleanup behavior on both X11 and
Wayland, with evidence separated by backend. Mark unavailable checks pending and
keep unsupported environments/features clearly identified. Add CI coverage that
matches what the runners can actually test; do not claim CI proves native Wayland.

Update Linux prerequisites/setup, certificates, limitations, protocol, notices,
and both agent files when affected. Update Phase 4's progress and append dated
results, support matrix, gaps, and a Phase 5 handoff to MULTIPLATFORM_PLAN.md.
Do not publish assets or start Phase 5 automatically.
```

## Phase 5 — feature completion and release readiness

**Outcome:** supported macOS/Linux desktop features, reproducible provisional packages, and an evidence-backed checklist for a separately authorized preview release.

**Copy this prompt:**

```text
Implement Phase 5 of MULTIPLATFORM_PLAN.md: feature completion and release
readiness. This prompt authorizes development and provisional packaging, not
public release publication, tagging, replacement of released assets, or real
power/uninstall actions on an active machine.

Read MULTIPLATFORM_PLAN.md in full, AGENTS.md, CLAUDE.md, README.md,
docs/protocol.md, .github/SECURITY.md, docs/pc-features.md, docs/releases.md,
docs/release-checklist.md, docs/third-party.md, docs/release-evidence.md, and prior
phase logs. Follow the roadmap's shared rules. Verify prerequisites, resolve
feature-blocking defects, and preserve unrelated work and all working platforms.

Implement supported system volume/read-back, General and applicable player media
actions, configured local app launching, and optional start-at-login on macOS and
the documented Linux baseline. Verify player shortcuts per OS and focus behavior.
Use platform-native audio/media integrations where appropriate. Launcher requests
must continue carrying configured IDs rather than remote paths or command lines;
adapt .app/Linux app selection without exposing arbitrary shell execution.

Implement sleep/restart/shutdown only where the OS/desktop permission model and
native adapter support them. Retain the existing session-bound confirmation,
cancellable countdown, final authorization recheck, and cleanup semantics.
Enforce unsupported capabilities in the backend as well as the UI. Test power
flows with recording adapters unless separate authorization and disposable
machines are available for physical execution; retain those acceptance gaps.

Create version-aware packaging for macOS .app archives (and a DMG if justified)
and an initial Ubuntu-compatible package/archive, bundling the remote and the
required .NET runtime. Document remaining native OS dependencies. Retain Windows
packaging. Verify pinned inputs, package contents, absence of debug/private files,
licenses, SHA-256 checksums, startup behavior, and artifact secret scans. Record
signing/notarization status accurately; do not invent signing credentials.

Finish cross-platform build/test/packaging CI where supported. Run appropriate
Release, shared, security, pairing, lifecycle, feature, browser, strict TLS, and
dependency/runtime advisory checks. Record Windows/macOS/Linux results separately.
Perform installed/native/phone/LAN checks only with available authorized targets;
carry unavailable acceptance forward visibly and document exact manual steps.

Prepare release documentation and process changes for multi-platform downloads,
shared version declarations, source revision, assets, checksums, and support matrix.
Keep current published version/download declarations truthful until a new release
is authorized. Test packaging with provisional output; do not label an unreleased
binary as a completed new version or add nonexistent public download links.
When publication is subsequently authorized, follow every AGENTS.md synchronization
step: choose/update the version everywhere, update README/both agent files/releases/
dated release evidence/affected .github files, run the required checks, commit all
source/docs before final packaging, build from that exact commit, create a new
annotated tag, publish accurate preview assets, and verify public URLs/hashes.

Acceptance for this phase is implemented supported features, verified provisional
packages, completed automated checks where available, accurate manual gaps, and a
concrete release-readiness handoff. Distinguish implementation complete, candidate
validated, manual acceptance pending, and public release not published.
Do not claim full platform parity when an action/backend/architecture is unverified.

Update affected docs and both agent files consistently, append new dated actual
evidence without relabeling historical results, and update Phase 5's progress and
execution log in MULTIPLATFORM_PLAN.md. Finish with package paths/hashes, test
results, supported environments, unresolved acceptance gaps, and remaining release
steps. Do not publish without the required authorization. Android/Android TV and
Windows UI migration remain outside this plan.
```

## Execution log

Append a new entry for each execution; do not overwrite earlier results. Use the user's current date when recording new evidence.

### 2026-10-13 — roadmap created

- Planned five sequential phases and copyable prompts for macOS/Linux work.
- Baseline findings: Core targets `net10.0`; App/tests currently depend on WPF and `net10.0-windows`; CompanionServer constructs Windows services directly; build/toolchain scripts and regression CI currently assume Windows.
- Implementation status: no cross-platform code implemented as part of this planning task.
- Validation status: no new native, physical-phone, LAN, or release-package acceptance recorded.

### Template for a future execution

- Date / phase / status:
- Implemented behavior and changed files:
- Environment/backend/architecture (redact personal details):
- Commands/checks and actual results:
- Native input evidence:
- Physical-phone / LAN / installed-app evidence:
- Pending or unavailable checks and exact follow-up steps:
- Dependency/protocol/security decisions or deviations:
- Prerequisites and handoff for the next phase:

### 2026-10-13 — Phase 1 portable backend and test foundation implemented

- Scope: Phase 1 only. Public app/version/downloads remain **0.2.1 preview**; no release assets, tag, commit/push or Phase 2 shell were created.
- Git/platform inspection: clean working tree at start; macOS 27.0 Apple Silicon available, no Windows runner. Docker supplied Ubuntu 24.04.5 LTS ARM64 (container, no GNOME/native input). The preinstalled Node 24.17.0 did not match the pin; downloaded the pinned native SDK 10.0.401 and Node 24.21.0/npm 11.19.0 into ignored `.tools/`, verifying official manifest digests. No pinned version changed.
- Architecture/files: extracted `CompanionServer`, both endpoint partials and `ServerLifecycle` to new `src/PcController.Host` (`net10.0`, Core + ASP.NET Core only). `HostServices` requires explicit input/actions, protected devices/certificates, preferences, approved network snapshot, capability model and web assets. `WindowsHostFactory` wires the WPF app to real Windows adapters, retaining one native input tracker across replacements. Updated both solutions/project references and the app's lockfile for the project graph.
- Storage/certificates: split shared issuance/renewal/device JSON from Windows DPAPI/ACL protection and Schannel key loading. Retained filenames, DPAPI payload/flags, root, renewal thresholds, AKI/SKI and device formats. Windows tests write historical DPAPI device JSON, renew a legacy leaf, and assert remembered authentication and untouched protected root/device bytes. These tests cross-compile but **Windows execution remains pending**. No generic production plaintext store or weaker fallback was introduced.
- Preferences/semantics: pointer, media and launcher JSON live in Host. Windows launcher path validation/comparison/discovery remain behind `ILauncherPlatform`. Core now allows logical shortcut/media actions; native chords live in Windows mapping tables. Defined v1 Meta alias and normalized 120-unit detents without changing Windows injection. Added authenticated status capabilities and backend action checks, preserving protocol v1 and the bundled Windows frontend.
- Portable tests/scripts: new `tests/PcController.PortableTests` contains temporary test storage, recording input/power, a disposable launcher policy and loopback-only fixtures; production has no path to them. Shared test suites moved out of WPF and Windows-native checks remain in `PcController.Tests`. `scripts/portable.py` supplies build/test/TLS/browser/audit commands on all OSes; PowerShell build/test auto-select shared projects off Windows. Removed unconditional Windows tool selection from shared entry points. Playwright uses Edge on Windows and its pinned Chromium elsewhere.
- Actual macOS checks: `python3 scripts/portable.py build` passed Release with zero warnings/errors and the pinned TypeScript/Vite frontend build. `python3 scripts/portable.py test` initially passed 253 checks; final PowerShell 7.6.6 `scripts/test.ps1 -Portable` passed **254 checks** after adding the empty-media-capability case, including pairing/expiry/deny/revoke, storage-failure denial, ownership/takeover, actual HTTPS/WSS diagnostics and authenticated control, malformed/replayed frames, HTTP budgets/safety capacity, delayed-body reauthorization, cleanup/watchdog, lifecycle replacement/pause, preferences, capability rejection and root-preserving leaf renewal. `python3 scripts/portable.py audit` found no npm/NuGet advisories in current feeds. A full-solution NuGet audit also passed with `EnableWindowsTargeting=true` for cross-target restore (the initial audit without that property could not restore WPF projects on macOS). Python 3.13.16 / OpenSSL 3.5.9 `scripts/portable.py tls` passed strict trusted-chain/name validation, wrong-host rejection and untrusted-root rejection. `scripts/portable.py browser` passed **6 Playwright checks** using the existing explicit loopback UI TLS bypass; this is separate from strict TLS acceptance.
- Actual Linux checks: isolated `mcr.microsoft.com/dotnet/sdk:10.0.401` ARM64 container (image digest `sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317`), Ubuntu 24.04.5 LTS, ran `dotnet build PcController.Portable.slnx -c Release` with zero warnings/errors and `dotnet run --project tests/PcController.PortableTests -c Release --no-build`: **254 checks passed**. This is shared-code/container evidence, not native Linux desktop support. Source was copied into an isolated container checkout; host build outputs were not reused.
- Actual Windows compilation: from macOS, `.tools/dotnet/dotnet build PcController.slnx -c Release -p:EnableWindowsTargeting=true` compiled App, Windows and both runners with zero warnings/errors. No Windows execution, DPAPI/Schannel handshake, WPF startup, native input, installed-app or physical power test was performed.
- Tooling compatibility findings: macOS .NET rejects `EphemeralKeySet`; the **test-only** key loader uses temporary OS key loading there, user containers on Windows and ephemeral keys on Linux. Generation/storage remain distinct; Windows production flags are unchanged. Auxiliary Python 3.13.16 and PowerShell 7.6.6 validation runtimes were downloaded to ignored `.tools/` and digest-verified; they are not shipped dependencies.
- CI: extended `.github/workflows/security-checks.yml` with Windows/macOS/Linux shared build/test/strict-TLS/audit jobs, retaining the Windows regression/browser job. Jobs have **not** been submitted or observed running for this work; configuration alone is not validation evidence.
- Documentation: updated README, AGENTS, CLAUDE, protocol/security/certificate/limitations/third-party docs and added `docs/portable-host.md`; historical release evidence remains intact. PowerShell 7.6.6 `scripts/build.ps1 -Configuration Release` also passed on macOS with automatic portable selection; final `scripts/test.ps1 -Portable` passed 254 checks. Release-documentation synchronization for v0.2.1, all PowerShell parser checks, `git diff --check`, Python syntax checks and a targeted source credential/private-key pattern scan passed (no matches or new credential/binary files in Git status).
- Pending checks: execute the Windows Release build/default/security/pairing/lifecycle/PC-feature/browser/TLS suites on a real Windows runner, including the new DPAPI root/device migration assertions; observe all three CI matrix runs; validate Ubuntu x64 shared tests separately from the ARM64 container; retain all native/macOS Intel/Linux GNOME X11/Wayland/physical-phone/LAN/installed-app gaps. No recording result establishes native input or physical power execution.
- Phase 2 prerequisites/handoff: read `docs/portable-host.md`; consume the explicit host contracts, add Avalonia only under the next authorized phase, implement capability-driven browser labels/controls and trusted-network identity/approval, and supply unavailable capabilities until real native/storage adapters exist. Future Mac/Linux storage requires reviewed Keychain/keyring implementations and must fail when unavailable. Preserve Windows runtime validation gaps and the public release. **Do not start Phase 2 automatically.**

### 2026-10-13 — Phase 2 shared shell and platform-aware remote implemented

- Scope/prerequisite verification: Phase 2 only, preserving the existing uncommitted Phase 1 extraction and its historical evidence. Reviewed the complete roadmap, both agent files, README, protocol, security, UI design and portable-host documentation; inspected actual host/Windows wiring and reproduced the Phase 1 baseline of **254 portable checks** before extensions. Public Windows app/downloads remain **0.2.1 preview**. No commit, push, tag, published assets or Phase 3 implementation was performed.
- Dependency decision: pinned **Avalonia.Desktop / Themes.Fluent / test-only Headless 12.1.4**, stable 2026-10-09, with Desktop/DesktopTests NuGet lockfiles and third-party notices. Official desktop requirements support .NET 8+, compatible with unchanged SDK **10.0.401** and `net10.0`. The upstream table lists macOS 26 ARM64/x64 Tier 1, macOS 14/15 Tier 2 and Ubuntu 24.x x64/ARM64 Tier 2; local macOS 27 startup does not expand that support claim. Linux shell rendering uses X11 (XWayland on Wayland), without native input permission. Node **24.21.0** / npm **11.19.0** remain pinned. Build scripts/CI now set `AVALONIA_TELEMETRY_OPTOUT=1`; initial dependency-selection builds used the vendor's default build-service behavior. No runtime telemetry was added. Sources/license decisions are in `docs/desktop-shell.md` and `docs/third-party.md`.
- Shell/files: added `src/PcController.Desktop` to both solutions. Native chrome, graphite/mint styling, accessible controls and responsive connection/device cards preserve the remote's visual language. The explicit controller/factory contract drives connection/manual fallback, short-lived QR/countdown, fingerprint/public profile save picker, local Allow/Deny dialogs, expiry, controller status, remembered-device revocation, Host pointer/media preferences, Pause/Resume, Release input and graceful Exit. Dismissal/Escape, expiry, replacement, failed protected storage and shutdown deny authorization. Failed cleanup retains pause; failed revocation pauses/cancels ownership. macOS has a branded native Open/Settings menu and tray menu; native Hide/Quit remain. Application menu is initialized before Avalonia's exporter, avoiding the vendor's default About menu. Tray actions disable without a host. Default close exits; keep-running-on-close is opt-in, and Linux retains a minimized taskbar window so a missing tray host cannot strand it.
- Production readiness: normal Mac/Linux composition creates **no host, listener, certificate, remembered credential, recording input or temporary/plaintext store**. It visibly reports missing Keychain/Accessibility or Secret Service/X11/Wayland services and disables unavailable actions. This deliberately uses a stricter no-listener gate than the roadmap's permitted loopback exposure until a reviewed secure store exists. Only `tests/PcController.DesktopTests --show-fixture` composes temporary loopback storage/recording input, with an explicit fixture window title and unavailable native capabilities. It shares the real shell composition and never auto-approves requests.
- Network/security: `TrustedNetworks` requires a currently observable, explicitly selected interface plus local confirmation. No RFC1918 trust inference; session-only approval hashes interface, gateways, addresses and observable Wi-Fi identity. Unobservable/redacted Wi-Fi is omitted, without requesting Location permission. Connectivity events invalidate approval even at the same IP; refresh detects identity changes. Changes pause ownership, deny prompts and stop old listeners before replacement. The lifecycle queues refreshes, rechecks trust before/after bind and rejects factories that ignore the approved snapshot. Inspection failure stops existing exposure. Certificates must have SANs covering localhost and each exact bound address; connection address and QR links use that same snapshot. Interface/connectivity identity is conservative, not cryptographic authentication: actual roaming/same-subnet wired changes and OS notification coverage remain production prerequisites.
- Prerequisite defects resolved: shared shutdown closes pairing synchronously before asynchronous listener stop, preventing late approvals, and avoids UI-context disposal deadlocks. Session claim reauthenticates after delayed body reads under the ownership lock (new revoke-race regression). Revocation failures cancel ownership. Capability-bearing pong now uses consistent camel-case serialization and bounded **4096-byte server replies**; receivers assemble fragments. The **1024-byte/depth-four client-command limit**, exact v1 command fields, units and original live-session checks remain intact. These changes preserve Windows adapter composition, DPAPI/root/device formats and backend authorization.
- Browser/protocol: added authenticated platform labels/readiness and independent pointer, keyboard, text, shortcut, profile/media, volume, launcher and power gating. macOS uses Command/Option/Mac; Linux Super/Computer; Windows keeps Win/Ctrl/Alt/PC. Historical `Windows` wire key remains logical Meta. An old v1 companion without capabilities gets the legacy Windows contract; malformed present capabilities disable controls. Unsupported protocol status is rejected before session claim. Capabilities remain informational; backend checks reject unsupported requests even if UI controls are modified.
- Actual macOS automated results (27.0 ARM64): `scripts/portable.py build` passed frontend TypeScript/Vite and Release builds, zero warnings/errors. Final portable runner passed **257 checks**; `scripts/portable.py desktop-test` passed **51 checks**, including real shell Allow/Deny/dismiss/expiry, actual QR/manual renewal, Pause/Resume/cleanup/revoke/preferences/Exit buttons, protected-storage failure, late shutdown approval and network replacement. These use Avalonia headless controls and isolated HTTPS/WSS fixtures, not native input. `scripts/portable.py browser-capabilities` passed **7 Playwright checks** (old v1/malformed/unsupported protocol plus macOS/Linux/unavailable at phone/desktop widths); existing `scripts/portable.py browser` passed **6**, including gestures, keyboard/media/PC, layouts and connection recovery: **13 browser checks total**. Browser fixtures use the existing explicit UI TLS bypass only; separately Python 3.13.16/OpenSSL 3.5.9 strict TLS passed trusted-chain/name validation and wrong-host/untrusted-root negatives. npm/NuGet audit reported no current-feed advisories, including Avalonia transitives.
- Actual native macOS shell startup: real Avalonia native backend `DesktopTests --native-startup-check` opened the normal setup-required window and exited successfully, separately from headless tests. After the user unlocked the Mac, a temporary unsigned self-contained `.app` built from this shell was inspected through native accessibility/screenshot: two-card setup, charcoal disclosure panels, disabled host/native controls, branded **Open PC Remote / Settings…** menu, Settings opening Preferences, native Quit and process exit verified. Temporary QA processes were closed. This was provisional startup/menu evidence, not signed/notarized/installed distribution, native input, permission grant, LAN listener or physical-phone acceptance. Menu-bar icon interaction/hide/reopen still needs explicit native manual acceptance.
- Actual Linux results: isolated Ubuntu **24.04.5 LTS ARM64** container using SDK image `mcr.microsoft.com/dotnet/sdk:10.0.401` (same Phase 1 image digest), with an isolated source checkout and pinned local NuGet feed. Locked restore and portable solution Release build passed; **257 portable / 51 desktop checks** passed. Installing X11 libraries/fonts and Xvfb in this disposable container allowed `xvfb-run -a dotnet run --project tests/PcController.DesktopTests -c Release --no-build -- --native-startup-check` to open the actual X11 shell backend and exit successfully. No host binaries were substituted for Linux outputs. This is virtual-display ARM64 startup, not Ubuntu x64/GNOME, physical X11/Wayland input or real tray acceptance. The final recopy initially included macOS AppleDouble files; excluding/removing those transfer artifacts restored a clean passing Linux build, without a source workaround.
- Windows/build/process checks: macOS cross-built the **full Windows solution** with `EnableWindowsTargeting=true`, zero warnings/errors; Windows execution/WPF/DPAPI/Schannel checks remain pending. Portable CI matrix now includes desktop headless/browser capability checks and build telemetry opt-out, retaining full Windows regression. CI configuration has not been submitted or observed running. Final standard locked restore was normalized after provisional RID-specific publish changed the local lockfile; full-solution `restore --locked-mode -p:EnableWindowsTargeting=true` then passed. No RID-specific provisional package is committed. Final v0.2.1 release-documentation synchronization, all PowerShell parser checks, Python syntax and `git diff --check` passed. A targeted scan of tracked/untracked source found zero private-key/credential-pattern matches and zero private/generated binary files. The disposable Linux container and temporary QA processes were stopped after checks.
- Documentation: updated README, both agent files, setup, UI design, protocol, security, portable-host, limitations, third-party notices and build/test entry points; added `docs/desktop-shell.md`. Historical Phase 1/release evidence is retained. No public version/download declarations changed.
- Pending/exact follow-up: run Windows Release/default/security/pairing/lifecycle/PC/browser/TLS suites including DPAPI migration on a real Windows runner; observe CI; validate macOS Intel and supported OS baselines; run Ubuntu 24.04 x64/GNOME X11 and Wayland shell/tray/window fallback separately; manually exercise native approval focus/Escape/dialog expiry and public-profile save picker using only the isolated fixture; validate menu-bar icon/hide/reopen; test real network roaming/route/same-subnet replacement and exact SAN/display alignment only after protected production storage exists. Native input, physical power, phone/LAN, installed/signed/notarized acceptance remain **pending**, with no recording result treated as native evidence.
- **Phase 3 handoff:** read `docs/desktop-shell.md`, `docs/portable-host.md`, the additive v1 protocol and both dated logs. Implement reviewed macOS Keychain certificate/device protection, real Accessibility input and permission/readiness reporting, OS lock/suspend/cleanup, single-instance/lifecycle and stronger validated network identity as needed before wiring an explicit real factory. Preserve installation roots/device authorization on renewal/restart; never use fixture services or advertise native readiness early. Test Command/Option/Meta, Unicode/layouts, held-input cleanup and permission loss in guarded disposable apps; then independently establish strict phone/LAN and installed-app evidence. Linux secure storage/input stays Phase 4. Keep Windows/manual gaps and public 0.2.1 intact. **Do not start Phase 3 automatically.**


### 2026-10-13 — Phase 3 macOS MVP implementation and independent checks

- Scope/baseline: Phase 3 only. Read the complete roadmap, both agent files, README, protocol, security, certificates and dated Phase 1/2 logs; inspected actual shared host/shell services and a clean starting Git tree. Preserved Windows WPF/native/DPAPI composition and public **0.2.1 preview** declarations. No Phase 4/5 implementation, version bump, commit/push/tag, public release or replacement asset was performed.
- Environment: available macOS **27.0 ARM64**, Xcode Command Line Tools, pinned SDK **10.0.401**, Node **24.21.0**, npm **11.19.0** and auxiliary Python **3.13.16 / OpenSSL 3.5.9**. No Windows or Intel hardware runner and no physical phone/LAN acceptance session. No new shipped dependency version was chosen; existing Avalonia **12.1.4** remains pinned.
- Native implementation: added `src/PcController.MacOS` with an authored Objective-C public-framework bridge and managed native-event tracker. Quartz relative coordinates/display clamping, left/right click with double-click count, pixel scrolling, left/right drag, tracked button release, Unicode scalars including surrogate pairs, Return/Tab normalization, native navigation/modifiers and Core-bounded repeat. Logical Meta/Option/Command and copy/paste/select-all/undo/switch-app use Mac semantics. C/V/A/Z resolve the current Unicode Command-layout table instead of copying Windows key codes. Source references and target-app/IME/Home/End/scroll limits are documented. `CGEventPost` is void: successful return/ack reports submission, not target receipt.
- Permissions/safety: explicit Accessibility onboarding opens the correct privacy pane, no unrelated permission/root request. Permission/session/Secure Input and focused-application checks run before native submission. Tracking is conservative before down and retained on failed up, including Unicode packets; all cleanup categories are attempted and successful ups removed idempotently. Native lifecycle/configuration callbacks synchronously release/pause/invalidate trust, with a generation check on every new input and an inspected-generation Resume arm. Polling also detects TCC/storage loss. Failed cleanup blocks Resume and keeps graceful Exit open; forced termination cannot prove cleanup.
- Protected storage: per-user default **file-based Keychain** generic-password installation key (non-syncing; explicit default scope, no System/plaintext fallback/implicit unlock), AES-256-GCM protected certificate/device envelopes with path AAD, private atomic writes/flush, 0700 directories / 0600 files / umask 077 and symlink refusal. Missing key/ciphertext/root and authenticated corruption fail without automatic CA reset. Host certificate orchestration retains root/device records on leaf SAN/AKI-SKI/expiry renewal; temporary macOS .NET key loading uses supported default import, not unsupported EphemeralKeySet. Keychain ACL/TCC identity after rebuild and signed-distribution migration remain manual checks.
- Production composition/lifecycle: Desktop supplies real `MacHostFactory` plus `MacDesktopEnvironment`, a per-user kernel advisory instance lock and AppKit workspace/SystemConfiguration observers. Starts/replacements are paused; available secure storage permits loopback before Accessibility. LAN requires explicit interface/connectivity-session confirmation, exact bound SANs and session-only approval. Native/.NET network changes pause, deny pending prompts, stop old listeners and require approval/Resume again. Redacted Wi-Fi identity uses explicit interface/session approval without Location or claims of SSID/router authentication. Actual same-subnet roaming/cable/route and lock/suspend notification coverage remains pending; use loopback until accepted in the target environment. Media/volume/launcher/power/start-at-login remain unavailable for Phase 5, with backend capability denial.
- Shared prerequisite defects fixed: lifecycle no longer clears a platform factory's startup pause; host creation/Keychain work runs off the UI context; controller awaits avoid UI-context deadlocks; synchronous socket input failures explicitly pause even when immediate cleanup succeeds; bounded failure close preserves WSS shutdown; failed certificate construction disposes imported keys; readiness/storage transitions stop listening and recover paused. New recording/headless regressions distinguish these semantics from native input.
- Automated results on this Mac: frontend TypeScript/Vite and Release builds passed with zero warnings/errors. Final shared runner **260 checks** (including three new real-WSS native-failure/pause regressions); Desktop runner **60 checks** (environment callback/permission/storage recovery/startup pause/failed Exit/Resume generation race). Mac runner **36 checks** with real Keychain storage: fault-injection tracker/repeat/Unicode cleanup, read-only actual US/French AZERTY/current Command-layout resolution, encrypted file modes/tamper/missing-key/missing-root refusal, separate process restart, remembered authorization/revocation, disposable Keychain creation/lock/denial, root-preserving leaf renewal, actual loopback HTTPS and certificate-validated WSS diagnostics, host restart/safety pause. These storage/layout-table results do not prove actual input receipt.
- TLS/browser/audit: `scripts/portable.py mac-tls` ran the real Keychain-backed Mac loopback factory, always paused/no approval callback, and Python 3.13 strict chain/name acceptance plus wrong-host/untrusted-root rejection passed. Separate portable `tls` also passed. `browser` passed **6** and `browser-capabilities` passed **7** Playwright checks; browser UI uses the existing explicit fixture-only TLS bypass, separate from strict TLS evidence. npm/NuGet feed audit reported no advisories. HTTP suites were rerun sequentially on 8443 after an initial overlapping diagnostic attempt; final results are from the sequential run.
- Native smoke availability: `scripts/portable.py mac-onboard` / `mac-smoke` build a dedicated test bundle and a test-only disposable Cocoa target, checks target PID before every input-producing post, aborts actions on focus loss and permits only tracked ups for cleanup. Its private scratch clipboard avoids altering the general clipboard. It covers actual Unicode/navigation/pointer/scroll/drag/modifier receipt and Mac host pause/revoke/disconnect/takeover/watchdog/restart when permitted. The attempted run reported **UNAVAILABLE** because the runner lacked Accessibility (and/or an interactive permitted session); **no native events were sent and no native-input success is claimed**. The user subsequently explicitly authorized the agent to enable the test permission and run it. A dedicated self-contained **PC Remote Smoke.app** was added to the OS Accessibility gate (macOS 27 labels it Device Control and Data Access); unrelated entries were preserved. Early guarded attempts aborted before native assertions because the target was not frontmost. The target now launches as a Cocoa app via LaunchServices. A subsequent attempt reported UNAVAILABLE, and computer-use inspection confirmed the Mac was locked; the user was asked to unlock locally. No completed input-receipt result has yet been obtained. Switch-app requires a second disposable app; supervised permission revocation/physical lock/suspend remain pending. No active-work input, keyboard-layout change, sleep/reboot or trust installation was performed.
- Candidates/architecture: `scripts/package-macos.py` creates local self-contained `.app`/ZIP/checksum outputs under ignored `artifacts/macos-development-osx-arm64` and `osx-x64`. Bundles the remote/native bridge/runtime/licenses, removes symbols, rejects private/debug package suffixes and checks native executable/bridge/runtime/Avalonia architectures. Initial Intel package inspection caught an ARM64 bridge copied beside an x64 executable; architecture propagation and independent native intermediate paths were fixed and candidates rebuilt. No Developer ID signature/notarization; linker/SDK ad-hoc signatures are not distribution signing. Intel compilation/package inspection is not Intel hardware acceptance. Final candidate hashes/startup results are appended below when verified.
- Windows/CI: full solution Release cross-build with `EnableWindowsTargeting=true` passed, zero warnings/errors. Windows execution/WPF/native/DPAPI/Schannel/installed acceptance remains pending. CI adds adapter fault checks to its existing three-OS shared matrix and retains Windows regressions; no CI run was submitted/observed. Linux code is still setup-required; no Linux adapter or Phase 4 acceptance is invented.
- Documentation/files: updated README, both agent instructions, setup, protocol, certificates, security, portable-host/desktop-shell, limitations and third-party notices; added `docs/macos-setup.md`, pinned Avalonia license text, packaging entry point, Mac project/tests and project/lockfile graph changes. Historical evidence remains intact. Public release declarations/assets remain unchanged.
- Pending exact acceptance: grant the exact candidate/test-runner Accessibility in an unlocked user session, close companions and run `mac-smoke` only against its disposable target; record text, native receipt/cleanup and focus-loss abort. Test Command shortcuts/layouts in real Cocoa and other disposable apps, with two targets for switch-app; restore/revoke permission under a supervised hold and retry cleanup; separately authorize/observe physical lock/suspend/resume. Validate Wi-Fi/Ethernet roaming/route/same-subnet notification coverage before relying on LAN. On an actual phone install only the exported public root after fingerprint verification, establish strict HTTPS/WSS without warnings, approve locally, remember/revoke/restart and test every input/interruption in Safari/Home Screen. Verify Intel on real Intel hardware, supported macOS baselines, menu-bar hide/reopen, Keychain ACL after rebuild and clean-account/Gatekeeper/signed-notarized startup. Preserve all prior Windows/CI/native gaps.
- **Linux handoff:** read `docs/macos-setup.md`, `docs/desktop-shell.md`, `docs/portable-host.md`, protocol and all dated logs. Reuse optional environment readiness/synchronous callbacks, generation-safe Resume, startup pause, shutdown/revocation/ownership fixes and capability-aware browser. Phase 4 must independently implement real X11/Wayland portal input/cleanup, Secret Service storage, network identity/lifecycle and per-user single instance; do not copy Keychain or Quartz assumptions or enable a fixture in production. Keep Mac/Windows behavior and pending acceptance intact. **Do not start Phase 4 automatically.**


### 2026-10-13 — Phase 3 native acceptance follow-up

- The user explicitly authorized Accessibility onboarding for the dedicated smoke bundle, then unlocked the Mac locally. The final guarded `python3 scripts/portable.py mac-smoke` run passed **41 macOS adapter checks**: 18 fault/layout checks plus 23 actual native assertions on this Apple Silicon macOS 27 machine. This supersedes the earlier unavailable/aborted smoke attempts above; it does not relabel those attempts as successes.
- Actual disposable Cocoa target receipt: Unicode `é😀 native`, navigation/backspace, Command select-all/replacement, copy/paste using a private scratch clipboard, undo, relative pointer displacement, right/double click, drag, scrolling and held-button/modifier cleanup. Each interruption first verified the target received held left-button and Shift, then verified release on host pause, device revocation, disconnect ownership end, takeover, watchdog expiry and host restart. Restart retained the real Keychain-backed CA and started paused. Command+Tab switched between two explicitly created disposable apps. Tests aborted actions on focus loss and confined pointer insertion to the disposable window; only tracked cleanup ups are allowed after focus loss.
- Native findings fixed and verified: use the live system-wide Accessibility focused PID rather than cached NSWorkspace frontmost state in a CLI process; maintain one private Quartz event source; synchronize relative pointer coordinates before posting so immediately following clicks use the new position. The disposable text target validates its private clipboard actions independently of the user's general clipboard. No user clipboard, trust installation, keyboard-layout setting, sleep/reboot or active-work target was changed. The test permission remains enabled for PC Remote Smoke; production candidate onboarding is separate.
- Keychain locked/unavailable, corruption, persistence, renewal and revocation checks passed as recorded above. A later isolated storage rerun passed **32 checks** (host HTTP checks omitted to reserve the port) after tightening default-Keychain ownership validation. Fault-injection failures are evidence of fail-closed tracking; they do not substitute for a supervised real permission-revocation test.
- Remaining native acceptance: supervise Accessibility revocation during a disposable hold and recovery; separately authorize physical lock/suspend/resume; test alternate active keyboard layouts/IMEs and additional disposable target apps, scaled/multiple monitors, native menu-bar hide/reopen, candidate rebuild/Keychain ACL migration and supported macOS baselines. Validate network roaming/route/cable changes, then actual phone Safari/Home Screen strict HTTPS/WSS, desktop approval, remembered pairing/revocation/restart and native cleanup on a selected LAN. Intel candidate compilation is separate from Intel hardware acceptance. Windows runtime/DPAPI/Schannel and observed CI runs remain pending. Public Windows release remains 0.2.1; Linux Phase 4 has not started.

- Final production ARM64 `.app` native startup was observed through the macOS accessibility UI: real Keychain-backed `https://localhost:8443`, certificate fingerprint/short-lived pairing QR, no remembered devices, LAN blocked and control paused with actionable Accessibility instructions. No production permission grant or LAN approval was made. Native Exit closed the app. The duplicate-instance check found .NET FileStream's implicit shared Unix flock conflicted with the explicit exclusive lock; native `open` with NOFOLLOW/CLOEXEC and a safely owned handle now lets the second instance report already running rather than throw. Final duplicate-launch verification and package digests follow below.

- Final osx-arm64 development candidate: `artifacts/macos-development-osx-arm64/PCRemote-0.2.1-osx-arm64-development.zip`, 58101181 bytes; SHA-256 `a5eb5a32916d780244aff0f9191c5884f7d32181e34e9dfa42063506894743e4`. Packaging verified bundled browser/native bridge, every native architecture, runtime/dependency license inventory and absence of private/debug file suffixes. No Developer ID signing/notarization or release publication.

- Final osx-x64 development candidate: `artifacts/macos-development-osx-x64/PCRemote-0.2.1-osx-x64-development.zip`, 60775306 bytes; SHA-256 `1810002ba95222e39114fff2f309259125e993a0ed4e894b34c3d8b92ebcbe17`. Packaging verified bundled browser/native bridge, every native architecture, runtime/dependency license inventory and absence of private/debug file suffixes. No Developer ID signing/notarization or release publication.

- Final rebuilt ARM64 production candidate restart retained the same displayed certificate fingerprint, loopback HTTPS and startup pause. A concurrent second executable invocation exited successfully with `PC Remote is already running. Open its menu-bar window.` Native Exit was observed again, and the loopback listener was closed. Final source private-key/credential-pattern scan found zero matches; Python syntax and `git diff --check` passed. Locked restore/release-documentation checks are recorded in ignored local logs. No additional native or physical acceptance is claimed.


### 2026-10-13 — Phase 4 Linux source candidate and independent verification

- Scope/status: **Phase 4 In progress**. Implemented the Linux source adapters, production wiring and independent checks; the real GNOME Wayland text/cleanup gate is **not accepted**. Read the complete roadmap/prior logs, AGENTS/CLAUDE, README, protocol, security, certificates, portable-host, desktop-shell and Mac handoff. Starting Git status contained uncommitted Phase 3 work; it was preserved. Public Windows app/download/version remains **0.2.1 preview**. No commit/push/tag, assets, Linux package, release publication or Phase 5 implementation.
- Environment/dependencies: macOS 27 ARM64 plus an isolated **Ubuntu 24.04.5 LTS ARM64** SDK 10.0.401 container, no native Ubuntu x64/GNOME desktop or phone. Rechecked Ubuntu 24.04/GNOME 46 lifecycle, Avalonia 12.1.4 OS support and current portal/Secret Service documentation. SDK 10.0.401, Node 24.21.0, npm 11.19.0 and Avalonia 12.1.4 remain pinned; no new NuGet dependency. Ubuntu system libraries actually exercised: GNOME Keyring **46.1-2ubuntu0.2**, libsecret **0.21.4-1build3**, libxcb-xtest **1.15-1ubuntu2**, libxkbcommon-x11 **1.6.0-1build1**, GTK **3.24.41-4ubuntu1.3**. An auxiliary Linux Python **3.13.16** standalone archive was verified against upstream SHA-256 `397ca401f416c3c99330a6be16d735d0cbeaf53e82070b80395b2fb4a8891f34`; it is ignored validation tooling, not shipped. A Python 3.9 download attempt failed certificate validation; the successful download used system curl with HTTPS verification and digest validation, with no verification bypass.
- Files/architecture: added `src/PcController.Linux` (managed input tracker, XCB/GDBus/libsecret C bridge, storage/key loader, host factory/environment and instance lock), `tests/PcController.LinuxTests` (faults, disposable GTK target, private portal protocol fixture, real encrypted keyring/TLS/instance checks), `scripts/linux-checks.py`, portable runner actions and both solution entries. Desktop wires real Linux services and has consent, GTK opt-in, local recovery and graceful POSIX signal handling. Only the Ubuntu 24.04 x64/GNOME factory is enabled in production; other OS/architectures stay setup-required. Test-only session/focus seams compile into a separate test bridge; `nm -D` verified no `pc_linux_test*` exports in production. No test input/store enters production composition.
- X11 implementation: checked XCB/XTEST and live XKB group/symbol lookup, relative movement/click/recognized double click, signed discrete detents, drag and tracked wheel/button/modifier release. Navigation downs/repeats are complete pairs while Core retains logical held state, preventing independent server auto-repeat without changing global configuration. Stable modifier-up tokens survive layout-resolution changes; Core bounds/lease/watchdog remain intact. A 40 ms interval separates double-click pairs for application recognition. XCB disconnect errors fail safely without Xlib's fatal I/O handler; display loss/recovery still needs real GNOME acceptance.
- Wayland implementation: actual logind backend/session checks select portal rather than XWayland. Explicit CreateSession → SelectDevices(keyboard+pointer) → Start consent, device/property/provider checks, subscribe-before-request Response handling, cancellation/denial/partial-grant/timeout and Closed/owner-loss disarming. Uses the official supported **RemoteDesktop Notify D-Bus transport**; EIS is recommended upstream but not required by that contract, and is neither opened nor mixed with Notify. No uinput/root/privileged input daemon, silent X11 fallback, screen capture, clipboard sharing, persisted consent or restore token. Exit cancels a pending consent request instead of holding the input gate for the permission timeout. GNOME provider presence and local session checks do not establish target receipt; actual Wayland Unicode, layouts, key/drag interruption and virtual-device cleanup remain pending.
- Text decision/limit: generic universal Linux Unicode insertion is **not implemented**. A local, session-only **GTK Unicode mode** (Ctrl+Shift+U scalar hex, normalized Return/Tab, preflighted and layout-resolved digits) is implemented for explicitly verified GTK targets on each transport. Text capability defaults false and becomes true only with local opt-in plus native readiness; unsupported apps/IMEs may execute chords or reject text. No clipboard mutation/keymap remapping/input-source changes. Partial compose remains paused and needs local target cancellation plus explicit recovery. Unknown ended-backend holds remain tracked until locally verified recovery; old ups are never silently sent into a replacement session. These manual recoveries are not automatic cleanup passes.
- Storage review/implementation: libsecret with the same-user reviewed GNOME daemon, exact persistent unlocked **login** collection and restricted encrypted binary login-keyring format. Plaintext/blank-password/session/default/unreviewed/locked/missing providers fail. GNOME's legacy password-derived AES-128-CBC format and modest KDF are **not DPAPI-equivalent**; an unlocked keyring/same-user compromise is outside the claimed boundary. Certificate/device files independently use Linux `PCL1` AES-256-GCM envelopes, path AAD, private atomic writes, 0700/0600, umask 077, owned regular-file/directory and symlink checks. Missing keys/roots or authentication failures never reset trust. Shared root/SAN/AKI-SKI renewal and device schema are retained; Windows DPAPI and Mac Keychain formats/production native adapters are unchanged.
- Lifecycle/network: local actual logind type/GNOME session, Active/Remote/LockedHint, GNOME ScreenSaver, sleep preparation, session/system buses, NetworkManager and Secret Service changes disarm generation and trigger release/pause/trust invalidation. Required observer/provider loss stops listeners; storage I/O/permission failures fail closed. Resume rechecks/arms the inspected generation. Network approval stays explicit/session-only; exact bound SANs, interface/gateway/addresses/observable SSID-BSSID plus conservative NetworkManager/.NET notifications are retained. Native GNOME lock/suspend/resume, provider loss, same-subnet roaming/cable/route and tray/window/signal behavior remain unaccepted. The existing taskbar fallback is preserved when GNOME has no tray host.
- Shared verification actually run: pinned frontend TypeScript/Vite and portable Release build passed; **260 portable / 60 desktop checks** passed on macOS and Ubuntu ARM64, including pairing, request budgets/reserved safety actions, delayed-body original-session authorization, ownership, WSS/native-failure pause, watchdog/lifecycle and headless shell workflows. **47 Linux adapter fault/semantic checks** pass (also on macOS without native input). Existing Mac **18 adapter/layout checks** pass, with no new physical Mac input test or permission/trust change. Browser **6 interaction + 7 capability checks** pass on macOS using the existing explicitly isolated UI TLS bypass; that is separate from strict TLS. Portable Python 3.13 strict chain/name plus incorrect-host/untrusted-root negatives pass. npm/NuGet audits reported no advisories in current feeds.
- X11 receipt evidence: `scripts/portable.py linux-x11` passed **30 checks per US/French layout** on private Xvfb and its own disposable GTK process. Actual GTK received accents/non-BMP Unicode, normalized Return/Tab, Home/ArrowRight/Backspace/End, Ctrl select-all/copy/paste, pointer displacement, right/recognized double click, scroll, drag and held modifiers. A navigation hold produced one pulse with no server auto-repeat. Core ownership pause/revoke/disconnect/takeover/replacement/exit hooks and watchdog cleared actual server-held state; focus loss blocked new events and permitted tracked ups. Target-event counters and server key/button state are checked separately. This is XTEST/GTK virtual-display evidence, **not native Ubuntu x64/GNOME desktop acceptance**, a full host replacement on that desktop, Alt+Tab/undo acceptance, phone or LAN evidence.
- Portal protocol evidence: `scripts/portable.py linux-portal` passed **14 private-bus fixture checks**: denial, partial grants, Create/Select/Start, supported Notify arguments, recorded keyboard/GTK compose, closure/generation invalidation, new-input refusal, retained unreachable holds/local recovery and pending-consent Exit cancellation. It has **no Wayland compositor, real consent dialog, target receipt or native cleanup result**. No successful D-Bus call is relabeled native Wayland acceptance.
- Linux keyring/TLS/instance evidence: Python 3.13 `scripts/linux-checks.py linux-storage` passed **24 checks** (17 initial + 3 fresh-process + four single checks), including actual kernel duplicate/restart lock, real encrypted provider/envelopes, modes/tamper/path/symlink refusal, root-preserving SAN/address renewal, real private-loopback chain/name-validated HTTPS and WSS diagnostics, strict positive/negative Python TLS, remembered fresh-process reconnect, persisted revocation, locked refusal, real provider restart with unchanged key, missing-key refusal and plaintext-format refusal. The loopback host is test-project-owned, paused, with unavailable input and no automatic pairing approval; it tests real Linux storage, not production GNOME desktop input. Keyring operations touched only an isolated HOME/bus/provider. Earlier test precondition/provider-ownership/unlock/context-menu assertions were corrected; final results are passing runs, not relabeled failed attempts.
- Build/CI/process: full Windows solution Release cross-build with `EnableWindowsTargeting=true` passed with zero warnings/errors; **Windows runtime/WPF/DPAPI/Schannel/native execution remains pending**. Removed the Mac-only architecture property before traversal into shared Host references to prevent duplicate shared-project output races. A later overlapping final build/test invocation hit a shared output race and was rerun sequentially; final evidence uses successful checks. CI pins Ubuntu **24.04** and installs native build libraries; a separate x64 isolated job covers US/French Xvfb, private portal protocol and encrypted disposable keyring/strict TLS. The full Windows job remains. CI was configured but **not submitted/observed**, and never claims native Wayland. Release-documentation synchronization remains v0.2.1; Python syntax and `git diff --check` pass. Source/private-file and production-export checks are recorded separately; generated libraries/tooling/credentials stay ignored.
- Documentation: added `docs/linux-setup.md`; updated README, Linux prerequisites/setup, certificates, limitations, protocol, security, desktop/portable architecture, third-party notices, both consistent agent files and dated development evidence. Historical Phase 1–3 logs/public release evidence are retained. No new public support/download declaration.

#### Phase 4 support and evidence matrix (2026-10-13)

| Environment / backend | Source implementation | Evidence actually obtained | Remaining gate |
| --- | --- | --- | --- |
| Ubuntu 24.04 x64/GNOME X11 | Production candidate; real XCB/XTEST + reviewed keyring | Native libraries compile on ARM64; Xvfb/GTK US/French checks and real keyring/TLS | **Pending:** real x64/GNOME input, layout/undo/Alt+Tab, lock/suspend/display loss, tray/network and phone/LAN |
| Ubuntu 24.04 x64/GNOME Wayland | Production candidate; explicit GNOME RemoteDesktop Notify consent, GTK text opt-in | Private protocol fixture only; real Linux keyring/TLS separately | **Pending:** actual consent/denial/revocation, GTK Unicode/layouts, arbitrary native Wayland target control, keys/drag/text/cleanup, lock/suspend/provider loss, reconnect/phone/LAN |
| Ubuntu 24.04 ARM64 container / Xvfb | Test-only native seam; production factory refuses this architecture | **Passed:** shared/headless/fault/XTEST-GTK US/French, real disposable GNOME keyring/strict TLS | Not a supported native desktop or x64 acceptance result |
| KDE, other distributions/providers/architectures, SSH/headless production | Setup-required / unsupported | No acceptance evidence | Separate review and implementations/evidence required |
| Windows public 0.2.1 / macOS Phase 3 | Existing adapters/storage preserved | Release cross-build + shared/Mac regressions | Prior Windows/Mac native/installed/phone gaps remain as logged |

- **Remaining Phase 4 acceptance:** use real Ubuntu 24.04 x64/GNOME X11 and a separate GNOME Wayland login/isolated VM with disposable GTK/native targets. Validate current/alternate layouts, Unicode/Return/Tab, pointer and repeat/shortcuts including two-app Alt+Tab/undo; abort target input on focus loss. Exercise permission denial/revocation/session closure, key/drag/compose interruption, pause/revoke/takeover/watchdog, display/provider loss, restart/remembered pairing, root retention and graceful Exit. Independently authorize guarded lock/suspend/resume on disposable targets; verify observer coverage/roaming/tray. Establish actual phone Safari/Home Screen root-fingerprint trust and strictly validated HTTPS/WSS without warnings on the explicitly approved LAN. Observe x64 CI runs. Keep unsupported targets and every unavailable result visible.
- **Phase 5 handoff:** read the Linux guide and this matrix; resolve the real X11/Wayland text/cleanup/native-network acceptance gates before calling Phase 4 complete. Universal text, other providers/desktops and distribution/signing require their own decisions/evidence. Only after separate authorization proceed to media/volume/launcher/power/start-at-login and Linux packaging/release readiness. Preserve both platform stores, no-root input, explicit network/portal consent, capability gating, cleanup retention and current public Windows version. **Phase 5 has not started; do not start or publish automatically.**


### 2026-10-13 — Phase 5 supported features and provisional release readiness

- **Status/scope:** supported feature implementation and provisional packaging/process handoff implemented; candidate verification is separate from manual acceptance and public publication. Read the complete roadmap/prior logs, both agent files, README, protocol/security, PC features, releases/checklist/notices/evidence and platform guides. Preserved the starting uncommitted Phase 3/4 work. Phase 4 remains **In progress** for real GNOME gates. Public Windows **0.2.1 preview**/links/assets remain unchanged; no version bump, commit/push, tag, new release, installed-login change, actual power/uninstall or trust installation was performed.
- **Environment/pins:** macOS 27 ARM64 with CLT, SDK 10.0.401, Node 24.21.0/npm 11.19.0, Avalonia 12.1.4 and runtime 10.0.12 unchanged. Ubuntu 24.04.5 x64 ran under Docker/Rosetta, explicitly emulated and not hardware/GNOME acceptance. Native ARM64 Ubuntu container separately exercised reviewed GNOME keyring/TLS. An initial x64 SDK invocation stalled; the successful emulated checks use validation-only `DOTNET_EnableWriteXorExecute=0`, `DOTNET_PROCESSOR_COUNT=2` and serialized builds. This emulator workaround is not production configuration or a relaxation of certificate/storage/input checks. See [upstream emulation issue](https://github.com/dotnet/runtime/issues/102226). Exact image/tool digests are appended with final results.
- **Mac source:** Core Audio writable default-output level/mute/read-back (master or complete channel controls; read-only HDMI unavailable); General native system media-key pairs with retained failure cleanup and system audio steps; current-layout player chords. VLC Mac has its own Command/Option mappings, verified against VideoLAN defaults instead of Windows key codes. Native AX frontmost bundle checks require the matching VLC/Stremio/browser; browser tab/player focus remains local. Local `.app` selection uses NSBundle validation and LaunchServices, no remote paths/arguments/URLs; launch completion is bounded and foreground is conservatively unclaimed. SMAppService main-app login registration is opt-in and needs code-signing eligibility/local Login Items approval. IOKit sleep is console-user/readiness gated; restart/shutdown have no reviewed adapter and stay backend-denied. No unrelated permissions/root/Automation/shell fallback.
- **Linux source:** native libpulse against PulseAudio/PipeWire-Pulse default output; bounded read/set/toggle/read-back, preserving channel balance and capping 100%. General uses uniquely selected controllable MPRIS (prefer the sole playing player); ambiguity and unsupported active-player actions fail instead of selecting a paused peer. VLC/YouTube/Stremio shortcuts resolve layout symbols through X11/consented Notify input; Wayland global focus cannot be inspected, so actual player focus is a manual gate. Local restricted `.desktop` GIO launches require ELF application, reject terminal/interpreter/wrapper/D-Bus activation/arbitrary arguments, allow only file/URI placeholders and VLC's benign flag. Snap/Flatpak/script/browser wrappers outside that policy are unavailable. Own XDG login registration uses a stable exact executable, private atomic file, ownership/symlink/conflict checks and matching-entry-only removal. logind Suspend/Reboot/PowerOff advertise only `Can* == yes`, recheck at dispatch with `interactive=false`, no inhibitor override/privileged fallback. No real power action was sent to system logind.
- **Shared shell/host fixes:** local app name/profile/add/remove, output level/actual read-back, login preference and confirmed/cancellable local power controls. Unsupported local controls disable independently. Readiness changes force capability snapshot replacement even with unchanged loopback bindings, stop old ownership/listeners and retain startup pause. Media refuses held drag before potentially blocking native service calls; tracked cleanup remains available. Native Linux feature calls serialize with backend teardown so asynchronous power/media cannot use freed D-Bus pointers. Existing v1 allowlists/fields, cookies, Origin/Host/SAN checks, separate budgets/reserved cancel capacity, post-body original-session reauthorization and session-bound single-use power confirmation/countdown/final recheck are retained. Windows native/DPAPI/Schannel/WPF composition and packaging remain intact.
- **Keychain prerequisite defect:** native candidate startup after ad-hoc sealing exposed legacy SecItem/ACL reads waiting despite `kSecUseAuthenticationUIFail`. Added `SecKeychainSetUserInteractionAllowed(false)` to the existing file-based provider path; never prompts/unlocks/resets trust. Disposable storage/regression/strict-TLS pass. The actual sealed candidate now shows denied storage (`OSStatus -25293`), with no listener or enabled remote controls, and native Exit completed. Current-account old-key ACL/signing identity acceptance remains pending local review; no approval/key/root reset was performed. Read-only default Core Audio control availability was true; no active-machine volume write/playback was tested.
- **PC request prerequisite defect:** inspection found the shared PC routes captured a supplied session only after asynchronous body reads. They now capture the original connected session before reading and require the body/session/authorization to match under the ownership lock. Five real HTTPS chunked-body regressions prevent volume/launch/prepare/confirm/cancel from adopting a new session ID or its power ticket; final shared result is **265 checks** on both available platforms. Existing v1 fields and reserved cancel budgets are unchanged.
- **Automated evidence:** shared/security/pairing/ownership/transport/lifecycle/features **260 checks** on Mac and emulated Ubuntu x64; Mac adapter/source-layout/media-focus/failure tests and disposable real Keychain tests; headless shell workflows and stale-capability denial; browser **6 interactions + 7 capabilities**, with the existing isolated UI TLS bypass explicitly separate from trust validation. Strict portable/Mac-Keychain TLS trusted-chain/name plus wrong-host/untrusted-root negatives pass; emulated Linux shared TLS and native ARM64 GNOME storage TLS pass. Final adapter/headless/native protocol counts follow after the final sequential rerun. Current npm/NuGet feeds and seven GitHub bundled-runtime package advisory queries returned no matching advisories; this is feed evidence, not a claim about unknown vulnerabilities. Initial Python 3.9 certificate-store failure was retried using verified Python 3.13 HTTPS, without TLS bypass.
- **Linux independent evidence:** x64 private Xvfb/GTK US/French each **41 checks**, including actual receipt/held cleanup plus private PulseAudio null-sink exact level/mute/read-back, GIO disposable no-op launch and shell/argument refusal. Player chords reached GTK/released modifiers; this is not installed-player playback or GNOME desktop acceptance. Private fake portal/MPRIS/logind tests cover permission denial, partial grants, Notify transport/closure, recipient ambiguity/action properties, `Can*` no/na/challenge denial, native power arguments to fake logind only, fresh backend and in-flight feature teardown. Native ARM64 real encrypted disposable GNOME keyring/instance/strict TLS **24 checks**. The x64 storage attempt correctly failed provider review because emulation exposes Rosetta in `/proc`; the production provider check was not weakened. An ARM64 fixture initially lacked copied web assets and was rerun successfully after correcting only that test prerequisite. Earlier failed runs are not relabeled as passes.
- **Native/manual limits:** the fresh Mac guarded smoke bundle's LaunchServices invocation failed `-10810`; read-only diagnostics had Secure Input active. No new native input-receipt success is claimed, and the earlier Phase 3 physical evidence is retained unchanged. Mac Intel hardware, actual player/media/audio writes/login/sign-in/Keychain ACL after signing, supported macOS baselines and clean-account/Gatekeeper distribution remain pending. Ubuntu x64/GNOME X11/Wayland input/text/cleanup, real keyring/session/backend/lock/roaming/tray and installed/phone/LAN gates remain pending. Windows cross-compilation succeeds; Windows runtime/WPF/DPAPI/Schannel/native/installed regression requires a real runner. Physical power/uninstall requires separate authorization and disposable machines.
- **Packaging/process:** shared `PcRemoteVersion` in Directory.Build.props, Desktop inheritance, explicit Windows version agreement/build guard and release-doc guard; source revision embedded and checked. New timestamped candidate directories avoid replacing Phase 3 outputs. Mac ARM64/x64 `.app` ZIP and Ubuntu x64 TAR.GZ bundle remote/.NET/ASP.NET/native bridge/licenses, verify native architecture/inventory/archive integrity/private-debug absence/pattern scan/checksums, and record base revision, dirty-source digest and per-file manifest. Mac gets a verified local ad-hoc bundle seal; no Developer ID/notarization/Linux distribution signature or public release. Archive-first distribution avoids unaccepted DMG/DEB installer/uninstall behavior. Read-only `--package-info` verifies bundled runtime/version/revision without listeners/input; architecture/hardware and GUI acceptance stay separate. Gitleaks 8.30.1 scans source and candidate trees, complementing raw byte-pattern checks. A .NET locale-table false positive was corrected by token boundaries, not by excluding runtime binaries.
- **CI/docs:** extended existing three-OS Release/shared/browser/strict-TLS/audit matrix, Linux isolated native/protocol/storage jobs and preserved full Windows regressions. Added Mac architecture/Ubuntu provisional packaging, content/hash/read-only startup/manifest workflow artifacts; Windows provisional installer/ZIP/content/revision checks stay non-installing. Jobs are configured, not submitted/observed. Updated both agent files consistently; README/release guide/checklist/PR template, protocol/security/platform setup/limitations/architecture/notices and this log. [Concrete release readiness and exact manual steps](docs/release-readiness.md) distinguish implemented supported features, candidate verification, pending manual acceptance and **no new public release**.
- **Remaining release steps:** obtain/record appropriate native/installed/phone/LAN targets and observed CI; review signing/login/storage identity/dependencies and source/artifact scans. On subsequent explicit publication authorization choose/synchronize a new version everywhere, update actual preview support/downloads/docs/agent/GitHub files, complete required checks, commit all source/docs to main before final clean packaging, verify informational revision/licenses/combined asset checksums, annotate a new exact-commit tag and publish accurate assets, then verify all public URLs/sizes/digests. Do not publish a dirty candidate, move a published tag, replace existing assets or claim full parity. Android/TV and Windows UI migration remain outside scope.


#### Final automated verification — 2026-10-13

| Environment actually exercised | Final result | Scope / boundary |
| --- | --- | --- |
| macOS 27 Apple Silicon | Release builds zero warnings/errors; shared **265**, desktop **61**, Mac adapter/layout/media **57**, disposable real Keychain/storage **75** checks passed | Shared suite covers security, pairing, lifecycle, features and recording power; no fresh physical player/power/phone acceptance |
| Ubuntu 24.04.5 x64 under Docker/Rosetta | Release build zero warnings/errors; shared **265**, headless desktop **61**, Linux fault/features **88**, private portal/MPRIS/logind **29** checks passed | Emulation/private buses are not native GNOME/Wayland acceptance; x64 keyring correctly refused the unreviewed emulated provider |
| Ubuntu 24.04 native ARM64 container | Disposable encrypted GNOME keyring/instance/storage **24** checks and strict TLS probes passed | Separate provider evidence; production Linux baseline remains x64/GNOME |
| Windows | Full Release solution cross-build zero warnings/errors | Windows runtime/WPF/DPAPI/Schannel/installed tests unavailable here; CI is configured, not observed |

Browser fixture checks: **6 interaction + 7 compatibility/capability** pass, with no unexpected application errors. Strict Python 3.13 trust/name positive and wrong-host/untrusted-root negatives pass for portable HTTPS, real Mac Keychain, Linux shared and native ARM64 storage. npm/NuGet feed audits, all 12-project resolved NuGet graphs and seven queried bundled .NET/ASP.NET/Windows runtime 10.0.12 advisory inputs show no matching current advisories. Source and package Gitleaks 8.30.1 scans plus artifact byte/private/debug checks found no leaks; exact final assets and hashes follow below. Documentation guard passes existing public version **0.2.1**; both agent shared sections agree. Python syntax and whitespace checks pass.

Reproducibility inputs for disposable validation: x64 container local image ID `sha256:4df5db956df7e072bd410b3f1b69b60194ab4cdd163e8d0f44bdb0d57446371d` (amd64; local image has no RepoDigest), ARM64 image ID `sha256:96a7bfb50bba4da24edbf495822ea5f1a58616b7e096f986c856350917195c80`, official SDK image index `sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317`. Official Node 24.21.0 x64 archive SHA-256 `6e1db87ef58b8819e5d5402eff1536491b18edd8eb7bee5ef7897876e88dc5ff`; separately verified Python 3.13.16 x64 tool archive `24801ea31c7d406d559a17e2226325c723d50c56900b9c0da7b62adaae2affc9`, ARM64 tool archive `397ca401f416c3c99330a6be16d735d0cbeaf53e82070b80395b2fb4a8891f34`. Verified Gitleaks tool archive `b40ab0ae55c505963e365f271a8d3846efbc170aa17f2607f13df610a9aeb6a5`. These are tooling/container checks, not an accepted installed environment.

Ignored actual logs are `.tools/phase5-*-final.log` and the separate earlier native/provider/TLS/audit logs. Failed attempts remain available separately; none are relabeled as passes. Packages embed base revision `22e37bc1edfefde60c90e2af4809a458a8eb320b` and **dirty source=true**, with source-tree/per-file SHA-256 manifests. Runtime metadata startup passes on Apple Silicon and emulated Linux x64 with global .NET lookup removed; Intel inventory/architecture passes but hardware startup remains unavailable. macOS bundles have verified local ad-hoc seals only. No Developer ID/notarization, Linux distribution signature, Windows new asset, public tag or release is claimed.


#### Final provisional packages and handoff — 2026-10-13

| Candidate | Local archive path | Bytes | SHA-256 |
| --- | --- | ---: | --- |
| osx-arm64 | `artifacts/candidate-0.2.1-osx-arm64-20261013-191413-440846/PCRemote-0.2.1-osx-arm64-candidate.zip` | 60,421,660 | `e5885ff642047c4626e7e061e9e0385b036ed1187edbe1c2131ed3a71ff40dee` |
| osx-x64 | `artifacts/candidate-0.2.1-osx-x64-20261013-191852-554753/PCRemote-0.2.1-osx-x64-candidate.zip` | 63,197,730 | `c001e4ab85fb84cdb42148c1c8edfefbad77c8ac8181b156365c377343ee8d4a` |
| linux-x64 | `artifacts/candidate-0.2.1-linux-x64-20261013-191430-080138/PCRemote-0.2.1-linux-x64-candidate.tar.gz` | 60,461,538 | `d8b608e5249a50ec6bcc0f73111b60488860ee27860a027e0338f46a9d2a4895` |

Each adjacent `SHA256SUMS.txt` verifies its archive and `candidate-manifest.json`; combined local sums are `artifacts/phase5-SHA256SUMS.txt`. Mac bundles contain **464** inventoried files plus **371** separately inventoried public code-signature metadata entries; Linux contains **463** files. All actual archive files compare byte-for-byte with their inspected trees. Both Mac archives were extracted to private temporary directories and passed `codesign --verify --deep --strict`. Their flat .NET assemblies require public code-signature extended attributes: a test that stripped every attribute broke the extracted seal. Packaging now uses Apple's public `copyfile` PACK callback to retain only newly generated `com.apple.cs.*` signature attributes, omitting local provenance/Finder/ACL/quarantine data. ZIP metadata has its own manifest hashes and raw secret-pattern check. An unexpected AppleDouble file is otherwise rejected. Ubuntu TAR ownership headers are normalized (no build-user name/UID); normalized valid ZIP/TAR inventories and refusal of extra files/local ownership were checked. No private signing credential is included or claimed.

The final staging command `.tools/phase5-refresh-guides.py` refreshed only reviewable guides in the already-built provisional trees, resealed the Mac bundles, regenerated candidate manifests/archives/checksums and verified all actual archive bytes. Compiled application/native/remote files were unchanged since the recorded final builds/tests. Source snapshot hashes remain in each manifest; this final hash/evidence appendix and publishing guidance were appended afterward. These are **dirty-source development snapshots**, not clean exact-commit release assets. Source/package Gitleaks and raw byte/private/debug checks pass; standalone self-contained metadata startup passes on ARM Mac and emulated x64 Linux, and Intel remains inventory/signature-only. Python syntax, release-doc synchronization at 0.2.1 and whitespace checks pass. Final isolated Xvfb/GTK US/French rerun passes **41 each**; final browser capability rerun passes **7**.

**Handoff status:** supported feature implementation complete; provisional candidates validated to the recorded automated/container/architecture/signature scope; manual native/installed/player/audio/login/phone/LAN/power acceptance pending; hosted CI configured but not observed; **public release not published**. Windows 0.2.1 preview assets and unrelated pre-existing work remain preserved. Mac source minimum is 14+, tested here on 27 ARM64 only; Intel/baseline hardware remains pending. Linux production candidate baseline is Ubuntu 24.04 x64/GNOME X11 or consented GNOME Wayland, with Phase 4 native gates still open. Other Linux desktops/providers/architectures are unsupported. Mac restart/shutdown stay backend-denied; Linux power requires native permission. Exact manual and subsequent separately authorized publication steps are in [release readiness](docs/release-readiness.md). No actual power/uninstall or login-registration action was performed; disposable test containers were removed after collecting artifacts.


### 2026-10-13 — Release preparation: Keychain update defect, build race and final packaging mode

- **Scope:** separately authorized release preparation for a new experimental preview. This entry covers source fixes made before the version bump; public Windows **0.2.1 preview** assets and tags are unchanged. Phase 4 remains **In progress**; no next roadmap phase was started.
- **Keychain investigation:** read-only inspection of the current account's existing `local.pc-remote.storage.v1` item (no secret read, ACL listing only) showed its decrypt access and partition list name a single earlier development build by `cdhash`. Every ad-hoc-sealed rebuild or release has a different code identity, so non-interactive reads are refused (the Phase 5 candidate reported `OSStatus -25293`). Fresh installations create their own item and are unaffected; every **update** of an existing installation would otherwise stay locked with no in-app recovery. The item, key, root and remembered devices were not modified.
- **Fix:** startup and background reads still never prompt. A new explicit local **Allow Keychain access after update…** action (with confirmation) temporarily permits macOS's own Keychain dialog for the existing item only; the user approves with the login Keychain password (**Always Allow**). The bridge never edits access lists, unlocks a locked Keychain, creates or replaces keys, and returns no key bytes from that call. A process-wide read/write lock makes concurrent background reads fail closed while the dialog is pending, so Exit and readiness polling never wait for it. Disposable test storage refuses the dialog path.
- **Build race:** the Desktop project's MacOS reference always passed `MacBridgeArchitecture`, creating a second parallel MacOS project instance in non-RID solution builds; it intermittently failed writing `PcController.MacOS.deps.json`. The property is now passed only for RID-specific builds.
- **Packaging/CI:** `package-macos.py`/`package-linux.py`/`check-package.py --final` produce `PCRemote-<version>-<rid>` archives and manifests only from a clean committed tree, verifying the exact revision; candidate mode is unchanged. CI gains a `final` `workflow_dispatch` input (workflow artifacts only, no tag/release), longer job timeouts for first observed runs and an authenticated read-only runtime-advisory query.
- **Actual local results (macOS 27 Apple Silicon):** Release build zero warnings/errors; shared **265**, desktop **65** (four new consent checks), Mac adapter **57**, real disposable Keychain **78** (three new: an item owned by another code identity fails promptly without a dialog or new key; test storage never opens the dialog) checks pass. Browser **6 + 7**, portable and Keychain-backed strict TLS positives/negatives, npm/NuGet/runtime advisory feeds, full Windows Release cross-build, locked restore, Python syntax, whitespace and a Gitleaks 8.30.1 scan of all 245 source files to be committed pass. A candidate ARM64 package still builds and passes inventory/checksum/self-contained metadata startup.
- **Pending:** observing hosted Windows/macOS/Linux CI for this source, the real macOS dialog approval after an update (needs the user to enter their own password), and every earlier native/installed/phone/LAN/power gate.
- **First observed hosted CI (run 38459644695, commit `d828db9`):** Windows, macOS and Ubuntu 24.04 x64 portable jobs each passed the Release build, shared (265 on Windows), Mac adapter, Linux adapter (81 on Windows), desktop (65), browser capability (7) and strict TLS suites; each failed only the bundled-runtime advisory query. The Windows `validation` job passed Release build, dependency audit, default **147**, security **181**, pairing **187**, lifecycle **154**, PC feature **176** (including the DPAPI legacy-device and leaf-renewal migration assertions), six browser and strict TLS checks, then failed packaging because `.tools/` did not exist for the Inno Setup download. `linux-isolated` on native x64 passed private Xvfb/GTK US **41** and French **41** and private portal/MPRIS/logind **29**; its keyring/TLS step failed because that job never built the bundled web remote. All three candidate packaging jobs passed. These are hosted-runner automated results, not installed, GNOME desktop, phone/LAN or Windows physical acceptance.
- **Security update:** Microsoft's 2026-10-13 .NET security release (runtime 10.0.13, SDK 10.0.402) fixes CVE-2026-58280 (GHSA-mc4q-93x5-f6hj, High) and other CVEs affecting the bundled runtime 10.0.12. The SDK pin moves to **10.0.402**; the local macOS ARM64 archive was verified against Microsoft's published SHA-512 and the Windows tool installer pin was updated likewise. Runtime advisory queries for 10.0.13 return no matches. With SDK 10.0.402 the full local suite passed again (shared 265, desktop 65, Mac adapter 57, Keychain 78, browser 6 + 7, both strict TLS probes, audits, Windows cross-build, locked restore). Lockfiles are unchanged; runtime packs are not locked. CI fixes: create `.tools/` before the verified Inno download and build the remote in `linux-isolated`.
- **Second observed hosted CI (run 38460878703, commit `826e46c`, runtime 10.0.13): all jobs passed** on Windows, macOS ARM64 and Ubuntu 24.04 x64, including Windows validation (default 147, security 181, pairing 187, lifecycle 154, PC feature 176, browser 6, strict TLS, candidate installer/ZIP checks), shared 265 on each OS, native-x64 isolated Xvfb/GTK US/French 41 each, portal/MPRIS/logind 29 and disposable encrypted keyring/strict TLS 24, plus all three candidate packages with native metadata startup on ARM64 macOS and x64 Ubuntu. Details are in the [0.3.0 release evidence](docs/release-evidence.md).
- **Version 0.3.0:** shared/Windows versions, packaging default and installer fallback, README, both agent files, release/download/readiness/checklist/platform guides, `.github/SECURITY.md` and the pull request template now describe a Windows preview plus experimental macOS ARM64/Intel and Ubuntu x64 archives. Final assets are to be built by the `final` workflow dispatch from the exact release commit, verified locally, tagged and published; final digests, the release commit's CI and public verification follow in a documentation-only entry. Phase 4 remains **In progress**; all native/manual gaps above remain open.
- **Release-commit CI:** push run 38462165237 on `88ad0e5` passed every job. The `final` dispatch 38462165384 on the same commit passed validation and all final packaging but exposed two test races: the desktop headless check read `controller.Server` while an un-awaited refresh replaced the host, and the Xvfb check sampled a held button/modifier once after 300 ms. The tests now await the refresh and poll the private target for up to 5 s; strictness of the asserted states is unchanged. Final assets will be rebuilt from the fixed commit; the `88ad0e5` dispatch artifacts are discarded.
- **Published 2026-10-13:** [v0.3.0 pre-release](https://github.com/maamouribilel/windows-pc-mobile-controller/releases/tag/v0.3.0), annotated tag at `6bf908c`. Push run 38463186338, `final` dispatch 38463184359 and tag documentation run 38464949711 passed. Eight assets plus combined `SHA256SUMS.txt` were independently verified before upload and re-downloaded publicly with matching sizes/digests; final digests are in the [0.3.0 publication evidence](docs/release-evidence.md#version-030-publication-and-public-verification-2026-10-13). The final ARM64 app was observed failing closed on this account's earlier-build Keychain item (no dialog, no listener, data unchanged). Phase 4 remains **In progress**; all native/manual gaps remain open. No next roadmap phase was started.

### 2026-10-13 — macOS LAN approval kept across pause-only transitions (0.3.1 preparation)

- **User-observed defect on installed 0.3.0:** the Mac app kept showing `https://localhost:8443`. Inspection: Keychain approval had succeeded and `en0` was offered; a 90 s read-only observation of the native bridge and .NET network events on this Mac recorded no network events. Code review found every native unsafe callback (including `NSWorkspaceActiveSpaceDidChangeNotification`, display sleep/wake and any `State:/Network/*` key), every readiness flip (e.g. Secure Input from any password field once Accessibility is granted) and a Resume attempt after such a change all revoked the LAN approval and stopped the LAN listener. Taking a player full-screen therefore sent the companion back to loopback. The documented design only required those events to pause control.
- **Fix:** the native bridge keeps a separate network-trust generation advanced only by SystemConfiguration network keys, system sleep/wake, session resign/become-active and power-off. Space/full-screen and display sleep/wake advance only the input generation (pause, release, re-arm required). The shell revokes LAN approval synchronously only for network-trust events (and storage failure); readiness changes still replace the host paused on the same approved bindings. Linux and other environments keep revoking on every unsafe event. The loopback-only address now explains how to approve a network. Per-event console/Secure Input/login-window/armed-generation input checks are unchanged.
- **Local results (macOS 27 ARM64):** Release build zero warnings/errors; shared 265, desktop **74** (eight new trust/scope checks), Mac adapter **59** (two native generation checks), real Keychain/storage **82** (including `MacEnvironment` pause-only vs network-trust reporting), Linux faults 81. Real Space/full-screen/display/Secure Input transitions on the installed app and phone/LAN use remain manual checks.
- **Hosted CI on `62644b2`:** run 38471786573 passed every job on Windows, macOS and Ubuntu x64 (desktop 74 on all three, Mac adapter 59 on macOS). **Version 0.3.1** synchronizes shared/Windows versions, packaging/installer defaults, README, both agent files, release/download/readiness guides, platform guides and `.github/SECURITY.md`; final assets follow the same `final` dispatch, verification, tag and publication procedure as 0.3.0.
