# Claude instructions for PC Remote

Read and follow [AGENTS.md](AGENTS.md) before editing this repository. It is the shared source of project, validation, security, and publishing instructions. Also read [README.md](README.md), [release publishing](docs/releases.md), and the [release checklist](docs/release-checklist.md) when preparing a release.

Current app version: `0.2.1`.
Current public release: [v0.2.1 preview](https://github.com/maamouribilel/windows-pc-mobile-controller/releases/tag/v0.2.1).

## Required for every new version

- Update the app/packaging/installer versions together.
- Update `README.md`, **both `AGENTS.md` and `CLAUDE.md`**, `docs/releases.md`, and dated `docs/release-evidence.md` results in the same release work. Refresh behavior, screenshots, setup, and limitations where affected.
- Review `.github/` workflows and templates; update affected files and complete the pull request release checklist.
- Run `./scripts/check-release-docs.ps1`, `./scripts/test.ps1 -Security`, `./scripts/test-certificate.ps1` (Python 3.13+), `./scripts/audit-dependencies.ps1`, and the other release checks required by `AGENTS.md`. Preserve HTTP budget isolation, reserved safety actions, atomic authorization after async body reads, and strict certificate validation with leaf-only migration. Review `.github/SECURITY.md`, verify bundled runtime advisories, package, verify hashes/licenses, and scan source and packages for secrets before publication.
- Commit source/documentation before final packaging so DLL informational revisions match the source tag. Push that commit and its annotated version tag, publish installer/ZIP/checksum assets with accurate notes and status, then verify their public URLs and digests.
- Never call a version finished while its GitHub files, README, agent instructions, or release assets are stale. Never overwrite a published version's tag or assets with a different build.

For documentation-only work, keep the existing version and release assets. If a release cannot be completed, state exactly what remains; do not claim publication or manual testing that did not happen.
