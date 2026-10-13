# Portable host foundation (Phase 1)

Development architecture as of 2026-10-13. The public app remains the Windows **0.2.1 preview**. An Avalonia development shell now exists; macOS and Linux now have explicit real platform source composition with independent acceptance gaps; the recording fixture is a test executable, not native control.

## Boundaries and composition

- `PcController.Core` (`net10.0`) owns protocol validation, logical actions, pairing, ownership, input tracking/watchdog, and the power queue. It contains no native calls, executable discovery, or platform key chords.
- `PcController.Host` (`net10.0`, ASP.NET Core framework reference) owns `CompanionServer`, pairing/PC endpoint partials, `ServerLifecycle`, JSON preferences, protected-device serialization, certificate issuance/renewal, and public certificate/profile export. It has no Windows assembly or WPF reference.
- `PcController.Windows` owns SendInput, media/shortcut mappings, audio/launcher/power actions, DPAPI, directory ACLs, Schannel key loading, Private-network discovery, same-user IPC, firewall and startup integration.
- `PcController.App` remains the WPF composition/UI. It calls `WindowsHostFactory.Create` with one real `WindowsInput` retained across server replacements. Approval still uses the actual local dialog; there is no production auto-approval or recording fallback.

`CompanionServer` requires `HostServices`: approved network snapshot, certificate provider, device repository, preferences, launcher catalog, native input/actions, capabilities and bundled web root. There are no optional Windows defaults. `ServerLifecycle` requires both a server factory and a network-read function; UI/OS notifications trigger refresh, session cancellation or pause. Replacement stops old listeners and releases input before creating the next server, retaining pause when cleanup fails.

| Contract | Platform responsibility |
| --- | --- |
| `IMouseInput`, `IKeyboardInput`, `IMediaInput` | Native insertion and conservative, idempotent cleanup; logical shortcuts/media map in the adapter |
| `IVolumeActions`, `ILauncherActions`, `IPowerActions` / `IPcActions` | Platform actions without remote executable strings or shell commands |
| `ILauncherPlatform` | Path normalization/comparison, validation, availability and discovery; shared launcher JSON keeps the existing schema |
| `IProtectedStorage` | Establish filesystem protections before access, read protected bytes, atomically write protected data, and fail explicitly if protection is unavailable |
| `ICertificateKeyLoader`, `ILocalCertificates` | Platform key-loading semantics and certificate lifetime; shared issuer does not choose OS key-storage flags |
| `ITrustedNetworkDiscovery` / `NetworkSnapshot` | Return only policy-approved bindings; `TrustIdentity` allows a trust change to trigger replacement even if IP addresses stay unchanged |

A future Mac/Linux factory must supply real adapters and reviewed Keychain/keyring protection. A private IP alone is not network approval. Unapproved addresses belong only in `AllAddresses`, with an empty approved list (loopback binding). No generic production plaintext secret store was added.

## Windows data compatibility

The per-user directory, filenames, PFX export format, DPAPI call flags/entropy, device JSON fields, credential hashes and preference JSON remain unchanged. The Windows storage adapter retains the signed-in-user/SYSTEM directory ACL and atomic `.tmp` replacement. Windows key loading retains `UserKeySet | Exportable` without `PersistKeySet`.

Certificate issuance is shared, but storage and key loading remain explicit adapters. Root validity, server-auth EKU, SANs, AKI/SKI and renewal thresholds are unchanged. Missing AKI/SKI or changed addresses renew only the server leaf; they do not overwrite the protected root or device file. Windows regression tests write the historical DPAPI device format directly, renew a legacy leaf, and verify authentication plus unchanged protected root/device bytes. Their **execution on Windows is pending**; cross-compilation does not prove DPAPI or Schannel behavior.

## Logical input and capabilities

| Previous Windows assumption | Phase 1 semantics |
| --- | --- |
| `Windows` modifier name | Frozen v1 wire spelling for logical Meta; the Windows adapter still uses the left Windows key. A future adapter maps Meta to its native equivalent. `Control`, `Alt`, `Shift` retain explicit meanings. |
| Ctrl+C/V/A/Z, Alt+Tab | Core allows logical copy/paste/selectAll/undo/switchApp actions; chords now live in `WindowsShortcutMappings`. |
| Media virtual keys/player chords | Core allows profile/action pairs; `IMediaInput` maps them in `WindowsMediaMappings`, preserving all previous Windows chords. |
| Windows wheel units | v1 normalized units remain 120 per detent, bounded to ±1200. Adapter scroll receives right/up positive after the existing natural-scroll conversion; native adapters convert units to their OS API. Windows passes them through unchanged. |
| `.exe` paths/discovery | Core carries an opaque configured launcher record; `WindowsLauncherPlatform` retains local absolute `.exe` validation, case-insensitive comparison and existing discovery paths. Paths never enter phone responses. |
| Windows error text in shared code | Shared errors describe input/OS/network availability; SendInput and WPF retain Windows-specific guidance. |

`PlatformCapabilities` advertises pointer, keyboard, text, shortcuts, media profile/actions, volume, launcher and allowed power actions. The host rejects unavailable actions before invoking native adapters; cleanup and ping stay available. `/health` and status derive input readiness from this model. Authenticated `/api/status` adds a capability object; the existing Windows browser ignores it and retains its current behavior because all existing Windows actions remain enabled. Phase 2 must add capability-driven labels/controls before any non-Windows production startup. See [protocol compatibility](protocol.md#portable-host-capability-extension-development).

## Portable builds and checks

Use the unchanged **.NET SDK 10.0.401**, **Node 24.21.0**, **npm 11.19.0**. Install native tool archives for your OS/architecture, either on PATH or under `.tools/dotnet/` and `.tools/node-v24.21.0-darwin-arm64/` (or the corresponding OS/architecture directory). The Windows installer script intentionally downloads Windows archives only. `global.json` rejects an SDK mismatch; the Python frontend entry point also checks exact Node/npm versions.

With Python 3.9+ and the pinned tools, from the repository root:

```sh
python3 scripts/portable.py build
python3 scripts/portable.py test
python3 scripts/portable.py audit
# Python 3.13+ with strict X.509 validation support:
python3 scripts/portable.py tls
# UI fixture only; first install the pinned Playwright Chromium browser:
cd src/remote
npx playwright install chromium
cd ../..
python3 scripts/portable.py browser
```

Windows can use `python` for the same commands. PowerShell 7 on macOS/Linux also supports `./scripts/build.ps1` and `./scripts/test.ps1`; these select the portable solution/runner automatically. On Windows, use `-Portable` to select shared checks explicitly. The Windows solution and default Windows regression runner remain available. Windows-only run/package/firewall/trust/native scripts are not macOS/Linux deployment entry points.

`PcController.Portable.slnx` excludes WPF/Windows projects. The portable runner exercises protocol validation, pairing/expiry/deny/revoke, ownership/takeover, real HTTPS/WSS transport, malformed/replayed frames, HTTP limits, delayed-body authorization, cleanup/watchdog, replacement/pause, preferences, recording PC actions, capability rejection, certificate renewal and storage-failure denial. It starts fresh sequential loopback servers on port 8443; close other companions before running.

`TestProtectedStorage`, recording input/power, test launcher policy and test key loaders are internal to the test executable. They use unique temporary directories and contain no production composition path. Test storage writes clear test data and must never be copied into production. macOS cannot load `EphemeralKeySet`, so its fixture uses temporary OS key loading; Windows uses temporary user containers for Schannel and Linux uses ephemeral keys. No persistent trust installation is performed by these fixtures.

Browser QA alone uses the existing explicit loopback TLS bypass. The separate Python strict TLS fixture checks trusted localhost plus wrong-host and untrusted-root rejection without bypass. Browser selection retains Edge on Windows and uses Playwright Chromium elsewhere.

CI now defines shared Release/build, full portable checks, strict TLS and audits on Windows/macOS/Linux, alongside the existing Windows regression/browser job. A configured job is not evidence of a successful run. Actual results and remaining Windows/native/LAN gaps are in [the roadmap execution log](../MULTIPLATFORM_PLAN.md#execution-log). The shared Desktop shell and test runner are included in both solutions. Mac/Linux native adapters and secure stores now exist, while native platform acceptance remains pending; see [Phase 2 shell composition, network trust and tests](desktop-shell.md).


## Phase 3 adapter/handoff

`PcController.MacOS` supplies production Quartz input, public Accessibility/session checks, a Keychain-backed envelope store, temporary macOS certificate-key loading, supported Phase 5 feature adapters, a per-user instance lock and native lifecycle/network notifications. Desktop's optional `IDesktopEnvironment` drives permission/storage gating and synchronous safety notifications. Linux now supplies its own Phase 4 source factory; see the current Linux section below. Shared lifecycle preserves an adapter's startup pause and builds platform hosts off the UI context; synchronous socket input failure pauses ownership. Certificate construction disposes loaded keys if later storage/export fails. No Windows format/adapter or v1 command changes are introduced. [Mac setup](macos-setup.md) specifies persistence, recovery, commands and unaccepted native/LAN cases for the Phase 4 handoff.

## Phase 4 Linux candidate

`PcController.Linux` supplies XCB/XTEST and explicit RemoteDesktop Notify transport, a reviewed GNOME login keyring-backed AES-GCM store, ephemeral PFX loading, instance lock and generation-gated logind/GNOME lock/NetworkManager monitoring. Desktop wires only real platform services. Text capability is optional GTK Unicode mode, default false; supported Phase 5 actions and their independent permission gates are described below. Production builds exclude the native test seam; LinuxTests compiles a separate bridge for guarded Xvfb/private-portal checks. Native Ubuntu x64/GNOME X11 and Wayland text/cleanup acceptance remains pending; Phase 4 is In progress. [Linux prerequisites and limits](linux-setup.md) define the remaining gates. Mac bridge builds remove their architecture-only property before traversing shared Host references, preventing duplicate shared-project output races without changing native behavior.


## Phase 5 source candidates (2026-10-13)

Supported native feature adapters and provisional packages are described in [the release-readiness handoff](release-readiness.md). Public support/downloads remain Windows 0.2.1. Mac Core Audio/native system media/player shortcuts/LaunchServices/login registration/sleep and Linux libpulse/MPRIS/player shortcuts/restricted GIO/XDG/logind use real platform services with backend denial and final native readiness/permission checks. Mac restart/shutdown stay unavailable; Linux power requires `Can* == yes`, `interactive=false`, no inhibitor override. Launcher wire input is still configured IDs only; no remote path/arguments/shell. Native dependencies/signing/login eligibility/actual player and physical power acceptance remain separate from recording/source-table/protocol/package checks.

Readiness transitions now force replacement of the capability snapshot even on unchanged loopback bindings; old ownership/listeners stop and replacement starts paused. v1 fields, budgets/reserved cancellation, original-session authentication checks, device-bound confirmation/countdown/cancellation and failed cleanup retention remain. Optional login start never preserves LAN approval/Wayland consent or resumes control. Provisional manifests record version/base revision/dirty source/file hashes; no tag/public release is created. Windows adapters, DPAPI and packaging remain intact. Real GNOME/phone/LAN/installed/Intel/CI gates stay visible.
