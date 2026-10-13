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
| 1. Portable backend | Not started | Not run | Windows regression evidence pending |
| 2. Shared desktop shell | Not started | Not run | macOS/Linux shell acceptance pending |
| 3. macOS MVP | Not started | Not run | macOS native input and phone/LAN checks pending |
| 4. Linux MVP | Not started | Not run | X11 and Wayland native input and phone/LAN checks pending |
| 5. Feature completion and release readiness | Not started | Not run | Packaged/native/manual acceptance pending |

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
