# Instructions for coding agents

These instructions apply to the whole repository. Read this file before changing the project. Claude must also read `CLAUDE.md`.

Current app version: `0.2.0`.
Current public release: [v0.2.0 preview](https://github.com/maamouribilel/windows-pc-mobile-controller/releases/tag/v0.2.0).

## Project and development

PC Remote is a Windows .NET/WPF tray companion serving a bundled TypeScript browser remote over HTTPS/WSS on a trusted Private LAN. Use the pinned toolchain and existing scripts described in `README.md`. There is no cloud backend, database, or required `.env` file.

- Preserve PC-approved pairing, Windows user-bound credential protection, allowlisted commands, and input cleanup. Read `docs/protocol.md` before changing those paths.
- Document actual behavior and evidence. Keep historical evidence intact; add a dated entry for new work. Never claim recording-adapter tests prove physical iPhone use or execution of power actions.
- Keep dependencies, generated binaries, local settings, certificate exports, private keys, credentials, logs, and personal machine information out of Git. Release binaries belong in GitHub Release assets.
- Run checks appropriate to the change. Do not perform shutdown, restart, uninstall, or physical input tests against a user's active work without the required task authorization and disposable targets.

## Mandatory release synchronization

**A new app version is not complete until its source, documentation, agent instructions, and GitHub release agree. Do not publish a binary-only update or report a release as complete while these updates are missing.** Follow `docs/releases.md` and `docs/release-checklist.md` for every release.

1. Choose a new three-component version. Update `src/PcController.App/PcController.App.csproj`, the default in `scripts/package.ps1`, and the fallback in `installer/PCRemote.iss`. The private frontend package's `0.0.0` is not the distributed app version.
2. Update `README.md`: current version, preview/stable status, download badge, installer/ZIP/checksum links, release notes link, package commands and output names. Refresh features, requirements, setup, limitations, screenshots, and walkthrough when their behavior changes. Use real captures and preserve media provenance.
3. Update **both `AGENTS.md` and `CLAUDE.md`**: current version/release links and any changed build, test, architecture, security, or publishing instructions. Keep their shared instructions consistent.
4. Update `docs/releases.md` for the new downloads and publishing procedure. Add dated results and known gaps in `docs/release-evidence.md`. Review `docs/release-checklist.md`, setup, limitations, protocol, third-party notices, and implementation status; change them when affected. Do not relabel old evidence as new validation.
5. Review **`.github/` files** (workflows, pull request template, and any future release configuration) for the new version and process. Update affected GitHub files; do not leave stale version assumptions or broken automation. The pull request release checklist must be completed for version bumps.
6. Run `./scripts/check-release-docs.ps1`, Release build, default checks, pairing, lifecycle, PC feature, and browser checks. Run applicable installed/native/LAN checks and record manual acceptance gaps accurately. Packaging calls the documentation check automatically. Verify installer/ZIP hashes, license notices, absence of debug symbols, and a secret scan of the source and distributable package.
7. Commit all corresponding source and documentation to `main` before tagging. Create an annotated `vX.Y.Z` tag pointing to the exact source used to build the assets, and push `main` and the tag. Never move a published tag or replace its assets with a different build; use a new version.
8. Prepare the GitHub Release for that tag with accurate changes, requirements, checks, known limits, and installer, portable ZIP, and `SHA256SUMS.txt` assets. Keep incomplete manual acceptance visible and use preview status when appropriate. Publish within the user's authorization; do not treat this file as standing permission for an otherwise unauthorized release.
9. After publication, verify the public tag/commit, release status, all download links, asset sizes, and SHA-256 digests against the local files. Check the README and release notes render correctly. Report the release URL, version, validation, and remaining limits only after verification.

The GitHub `Release documentation` workflow catches inconsistent version declarations and download links. It also checks that README, both agent files, download documentation, and release evidence changed when the app version changes. It does not replace runtime testing, secret scanning, or public asset verification.

For documentation-only maintenance, keep the existing release version and tag. Commit and push the documentation without rebuilding or replacing published assets.
