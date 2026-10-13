# Instructions for coding agents

These instructions apply to the whole repository. Read this file before changing the project. Claude must also read `CLAUDE.md`.

Current app version: `0.2.1`.
Current public release: [v0.2.1 preview](https://github.com/maamouribilel/windows-pc-mobile-controller/releases/tag/v0.2.1).

## Project and development

PC Remote is a Windows .NET/WPF tray companion serving a bundled TypeScript browser remote over HTTPS/WSS on a trusted Private LAN. Use the pinned toolchain and existing scripts described in `README.md`. There is no cloud backend, database, or required `.env` file.

- Preserve PC-approved pairing, Windows user-bound credential protection, allowlisted commands, and input cleanup. Read `docs/protocol.md` before changing those paths.
- Document actual behavior and evidence. Keep historical evidence intact; add a dated entry for new work. Never claim recording-adapter tests prove physical iPhone use or execution of power actions.
- Keep dependencies, generated binaries, local settings, certificate exports, private keys, credentials, logs, and personal machine information out of Git. Release binaries belong in GitHub Release assets.
- Run checks appropriate to the change. Do not perform shutdown, restart, uninstall, or physical input tests against a user's active work without the required task authorization and disposable targets.
- Preserve separate HTTP budgets for unpaired peers and approved devices, plus reserved power-cancel/logout capacity. Recheck authentication and the original live session under the ownership lock after asynchronous request-body reads. Read `.github/SECURITY.md` for trust boundaries and private reporting.
- Certificates must pass strict chain/name validation and carry server AKI/SKI extensions. Renew legacy server leaves without replacing the trusted root or remembered devices. Do not resolve compatibility failures by disabling certificate verification.


## Portable backend and shared shell development (Phases 1–2)

- The distributed app remains Windows 0.2.1. `PcController.Host` and `PcController.Core` target `net10.0`; WPF/UI and native Windows integration stay in App/Windows. Read `docs/portable-host.md` and the dated `MULTIPLATFORM_PLAN.md` execution log before continuing the migration.
- Shared host startup requires explicit `HostServices`; use `WindowsHostFactory` for the WPF companion. Keep DPAPI formats, ACLs, Schannel key loading, trusted roots and remembered devices intact. Certificate issuance/renewal is separate from OS key storage. Never substitute temporary test storage or recording adapters in production.
- Pointer/media/launcher preferences live in Host; Windows paths, discovery, shortcut/media key mappings and native APIs remain adapters. Preserve v1 wire semantics and the additive authenticated capability contract. The browser now consumes additive authenticated v1 capabilities/readiness; preserve independent action gating and Windows legacy compatibility.
- Use the unchanged SDK 10.0.401, Node 24.21.0 and npm 11.19.0. `python3 scripts/portable.py build`, `test`, `audit`, and `tls` support native macOS/Linux tools; strict TLS needs Python 3.13+. PowerShell 7 build/test automatically select portable projects off Windows; use `-Portable` on Windows. Keep the full Windows solution/regression job.
- Portable fixtures are test-project-only, loopback-only and temporary. Recording tests, container checks, cross-compilation and configured CI jobs cannot establish native input, DPAPI execution on Windows, physical-phone/LAN or installed-app acceptance. Record actual platform results and pending checks separately; do not start the next roadmap phase automatically.
- `PcController.Desktop` pins Avalonia 12.1.4 and retains the Windows WPF companion. Read `docs/desktop-shell.md`. Normal Mac/Linux startup is setup-required with no listener until reviewed Keychain/keyring and real native services exist. No test adapter/store may enter production composition.
- Network approval is explicit and session-only; invalidate it on connectivity/identity changes, pause/release ownership and stop old LAN listeners before replacement. Validate platform identity/notification coverage before production LAN startup; certificate SANs must cover every exact binding. Do not infer trust from private addresses.
- Use `python3 scripts/portable.py desktop`, `desktop-test`, `desktop-fixture` (test-only), and `browser-capabilities`; PowerShell supports `test.ps1 -Desktop`. Run loopback HTTP suites sequentially on port 8443. Record native shell startup separately from headless/recording checks and native control. Phase 3/4 must provide secure stores, permissions, lifecycle/single-instance and real adapters; do not start them automatically.


## Mandatory release synchronization

**A new app version is not complete until its source, documentation, agent instructions, and GitHub release agree. Do not publish a binary-only update or report a release as complete while these updates are missing.** Follow `docs/releases.md` and `docs/release-checklist.md` for every release.

1. Choose a new three-component version. Update `src/PcController.App/PcController.App.csproj`, the default in `scripts/package.ps1`, and the fallback in `installer/PCRemote.iss`. The private frontend package's `0.0.0` is not the distributed app version.
2. Update `README.md`: current version, preview/stable status, download badge, installer/ZIP/checksum links, release notes link, package commands and output names. Refresh features, requirements, setup, limitations, screenshots, and walkthrough when their behavior changes. Use real captures and preserve media provenance.
3. Update **both `AGENTS.md` and `CLAUDE.md`**: current version/release links and any changed build, test, architecture, security, or publishing instructions. Keep their shared instructions consistent.
4. Update `docs/releases.md` for the new downloads and publishing procedure. Add dated results and known gaps in `docs/release-evidence.md`. Review `docs/release-checklist.md`, setup, limitations, protocol, third-party notices, and implementation status; change them when affected. Do not relabel old evidence as new validation.
5. Review **`.github/` files** (workflows, pull request template, and any future release configuration) for the new version and process. Update affected GitHub files; do not leave stale version assumptions or broken automation. The pull request release checklist must be completed for version bumps.
6. Run `./scripts/check-release-docs.ps1`, Release build, default checks, `./scripts/test.ps1 -Security`, pairing, lifecycle, PC feature, browser checks, and `./scripts/test-certificate.ps1` (Python 3.13+ for strict TLS validation; provided in CI). Run `./scripts/audit-dependencies.ps1` and check advisories for the bundled runtime versions. Run applicable installed/native/LAN checks and record manual acceptance gaps accurately. Packaging calls the documentation check automatically. Verify installer/ZIP hashes, license notices, absence of debug symbols, and a secret scan of the source and distributable package.
7. Commit all corresponding source and documentation to `main` before final packaging and tagging. Build final assets from that commit so the DLL's informational source revision agrees with the release tag; provisional package tests may run earlier. Create an annotated `vX.Y.Z` tag pointing to that exact commit, and push `main` and the tag. Never move a published tag or replace its assets with a different build; use a new version.
8. Prepare the GitHub Release for that tag with accurate changes, requirements, checks, known limits, and installer, portable ZIP, and `SHA256SUMS.txt` assets. Keep incomplete manual acceptance visible and use preview status when appropriate. Publish within the user's authorization; do not treat this file as standing permission for an otherwise unauthorized release.
9. After publication, verify the public tag/commit, release status, all download links, asset sizes, and SHA-256 digests against the local files. Check the README and release notes render correctly. Report the release URL, version, validation, and remaining limits only after verification.

The GitHub `Release documentation` workflow catches inconsistent version declarations and download links. It also checks that README, both agent files, download documentation, and release evidence changed when the app version changes. It does not replace runtime testing, secret scanning, or public asset verification.

For documentation-only maintenance, keep the existing release version and tag. Commit and push the documentation without rebuilding or replacing published assets.
