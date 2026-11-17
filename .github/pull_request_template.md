## Change

Describe the user-visible result and why it is needed.

## Validation

List the checks actually run, their results, and remaining limitations.

## Release synchronization

For a new app version, complete every item before publication. For other changes, mark this section as not applicable.

- [ ] Shared product/Windows app/Desktop inheritance, packaging and installer versions agree.
- [ ] README version, status, download links, commands, features, and media are current.
- [ ] Both AGENTS.md and CLAUDE.md have the new version and current instructions.
- [ ] docs/releases.md and dated docs/release-evidence.md results are updated; affected setup, limitations, and acceptance docs are current.
- [ ] .github workflows/templates/release configuration have been reviewed and affected files updated.
- [ ] Release documentation check, security/regression suites, dependency/runtime advisory checks, package/license/hash checks, and source/package secret scans pass.
- [ ] The exact release commit's hosted CI run passed on Windows, macOS and Linux, and final assets came from that commit (`final` workflow dispatch or `--final` packaging), never from renamed candidates.
- [ ] Bundled runtime advisories were rechecked on the release day (Patch Tuesday releases can affect the pinned SDK/runtime).
- [ ] The release plan includes the matching main commit, annotated tag, actual per-platform installer/archives and manifests, combined checksum file, the locally signed `update.json`/`update.json.sig` (from the first release that ships update checks), accurate notes/status, and public download/digest/signature verification.

For multi-platform candidates, link the support matrix and distinguish implementation, provisional package verification, native/manual pending acceptance and public release status. Do not claim CI, recording power, emulation, Xvfb or private portal protocols prove native GNOME/phone/installed acceptance. See `docs/release-readiness.md`.
