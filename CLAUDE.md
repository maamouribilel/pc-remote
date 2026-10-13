# Claude instructions for PC Remote

Read and follow [AGENTS.md](AGENTS.md) before editing this repository. It is the shared source of project, validation, security, and publishing instructions. Also read [README.md](README.md), [release publishing](docs/releases.md), and the [release checklist](docs/release-checklist.md) when preparing a release.

Current app version: `0.2.1`.
Current public release: [v0.2.1 preview](https://github.com/maamouribilel/windows-pc-mobile-controller/releases/tag/v0.2.1).


## Portable backend and shared shell development (Phases 1–2)

- The distributed app remains Windows 0.2.1. `PcController.Host` and `PcController.Core` target `net10.0`; WPF/UI and native Windows integration stay in App/Windows. Read `docs/portable-host.md` and the dated `MULTIPLATFORM_PLAN.md` execution log before continuing the migration.
- Shared host startup requires explicit `HostServices`; use `WindowsHostFactory` for the WPF companion. Keep DPAPI formats, ACLs, Schannel key loading, trusted roots and remembered devices intact. Certificate issuance/renewal is separate from OS key storage. Never substitute temporary test storage or recording adapters in production.
- Pointer/media/launcher preferences live in Host; Windows paths, discovery, shortcut/media key mappings and native APIs remain adapters. Preserve v1 wire semantics and the additive authenticated capability contract. The browser now consumes additive authenticated v1 capabilities/readiness; preserve independent action gating and Windows legacy compatibility.
- Use the unchanged SDK 10.0.401, Node 24.21.0 and npm 11.19.0. `python3 scripts/portable.py build`, `test`, `audit`, and `tls` support native macOS/Linux tools; strict TLS needs Python 3.13+. PowerShell 7 build/test automatically select portable projects off Windows; use `-Portable` on Windows. Keep the full Windows solution/regression job.
- Portable fixtures are test-project-only, loopback-only and temporary. Recording tests, container checks, cross-compilation and configured CI jobs cannot establish native input, DPAPI execution on Windows, physical-phone/LAN or installed-app acceptance. Record actual platform results and pending checks separately; do not start the next roadmap phase automatically.
- `PcController.Desktop` pins Avalonia 12.1.4 and retains the Windows WPF companion. Read `docs/desktop-shell.md`. Normal Mac/Linux startup is setup-required with no listener until reviewed Keychain/keyring and real native services exist. No test adapter/store may enter production composition.
- Network approval is explicit and session-only; invalidate it on connectivity/identity changes, pause/release ownership and stop old LAN listeners before replacement. Validate platform identity/notification coverage before production LAN startup; certificate SANs must cover every exact binding. Do not infer trust from private addresses.
- Use `python3 scripts/portable.py desktop`, `desktop-test`, `desktop-fixture` (test-only), and `browser-capabilities`; PowerShell supports `test.ps1 -Desktop`. Run loopback HTTP suites sequentially on port 8443. Record native shell startup separately from headless/recording checks and native control. Phase 3/4 must provide secure stores, permissions, lifecycle/single-instance and real adapters; do not start them automatically.


## Required for every new version

- Update the app/packaging/installer versions together.
- Update `README.md`, **both `AGENTS.md` and `CLAUDE.md`**, `docs/releases.md`, and dated `docs/release-evidence.md` results in the same release work. Refresh behavior, screenshots, setup, and limitations where affected.
- Review `.github/` workflows and templates; update affected files and complete the pull request release checklist.
- Run `./scripts/check-release-docs.ps1`, `./scripts/test.ps1 -Security`, `./scripts/test-certificate.ps1` (Python 3.13+), `./scripts/audit-dependencies.ps1`, and the other release checks required by `AGENTS.md`. Preserve HTTP budget isolation, reserved safety actions, atomic authorization after async body reads, and strict certificate validation with leaf-only migration. Review `.github/SECURITY.md`, verify bundled runtime advisories, package, verify hashes/licenses, and scan source and packages for secrets before publication.
- Commit source/documentation before final packaging so DLL informational revisions match the source tag. Push that commit and its annotated version tag, publish installer/ZIP/checksum assets with accurate notes and status, then verify their public URLs and digests.
- Never call a version finished while its GitHub files, README, agent instructions, or release assets are stale. Never overwrite a published version's tag or assets with a different build.

For documentation-only work, keep the existing version and release assets. If a release cannot be completed, state exactly what remains; do not claim publication or manual testing that did not happen.
