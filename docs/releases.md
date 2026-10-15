# Download a specific PC Remote version

Open [GitHub Releases](https://github.com/maamouribilel/pc-remote/releases), choose a version, and expand its **Assets** section.

For the latest public preview, [PC Remote v0.4.1](https://github.com/maamouribilel/pc-remote/releases/tag/v0.4.1):

- [Windows x64 installer](https://github.com/maamouribilel/pc-remote/releases/download/v0.4.1/PCRemote-0.4.1-win-x64-setup.exe): installs for the current user, adds a Start menu shortcut and an uninstall entry.
- [Windows x64 portable ZIP](https://github.com/maamouribilel/pc-remote/releases/download/v0.4.1/PCRemote-0.4.1-win-x64.zip): extract the whole archive and run `PCRemote-win-x64/PcController.App.exe`. Keep its adjacent runtime and web files.
- [macOS Apple Silicon ZIP](https://github.com/maamouribilel/pc-remote/releases/download/v0.4.1/PCRemote-0.4.1-osx-arm64.zip) — **experimental**: contains `PC Remote.app` for macOS 14 or later.
- [macOS Intel ZIP](https://github.com/maamouribilel/pc-remote/releases/download/v0.4.1/PCRemote-0.4.1-osx-x64.zip) — **experimental**: built for Intel Macs, not yet run on Intel hardware.
- [Ubuntu 24.04 x64 / GNOME archive](https://github.com/maamouribilel/pc-remote/releases/download/v0.4.1/PCRemote-0.4.1-linux-x64.tar.gz) — **experimental**: contains `PCRemote-linux-x64/`.
- `PCRemote-0.4.1-<platform>-manifest.json`: per-file hashes and the exact source revision for each macOS/Ubuntu archive.
- [SHA256SUMS.txt](https://github.com/maamouribilel/pc-remote/releases/download/v0.4.1/SHA256SUMS.txt): expected hashes for every download listed above.

GitHub's generated **Source code (zip/tar.gz)** files contain the project source, not an installed app. Normal use of the packaged builds needs no SDK, Node.js, or separate .NET installation; macOS and Ubuntu still need their native OS services.

## Install and verify on Windows

1. Download the installer or portable ZIP and the checksum file from the same version.
2. In PowerShell, run `Get-FileHash -LiteralPath .\PCRemote-0.4.1-win-x64-setup.exe -Algorithm SHA256` (or use the ZIP filename). Compare the returned hash with the matching line in `SHA256SUMS.txt`.
3. Run the installer as your normal Windows user, or extract the portable ZIP. The optional Private LAN firewall helper alone requests administrator access.
4. Open PC Remote and follow the [certificate and iPhone pairing setup](setup.md).

This preview is unsigned. Windows may show publisher/SmartScreen prompts. Versions 0.3.0 and later bundle .NET 10.0.13 with Microsoft's 2026-10-13 security fixes; the 10.0.12 runtime in 0.2.1 is affected, so updating is recommended. The 0.4.1 Windows build passed automated suites on a hosted Windows runner but has not yet been installed or used with a physical phone; its Air pointer's axis fix was confirmed on an iPhone against the macOS companion in the flat grip, and the rest of the phone acceptance table is pending. Requirements and remaining manual checks are described in the version's release notes, [limitations](limitations.md), and [release checklist](release-checklist.md).

For an installer update, download the new version's installer. It requests a graceful exit before replacing the existing application files; exit through the tray menu if requested. Protected local certificate/device data is outside the installation directory and is retained. Keep each portable version in its own extracted directory and run one companion at a time.

## Install and verify the experimental macOS and Ubuntu builds

1. Check the archive with `shasum -a 256 <file>` (macOS) or `sha256sum <file>` (Ubuntu) and compare it with `SHA256SUMS.txt`.
2. **macOS:** extract the ZIP (Finder or `ditto -x -k`), move `PC Remote.app` to a stable folder such as `/Applications` and open it. The app has only a local ad-hoc seal, so Gatekeeper treats it as unidentified: confirm the source and checksum, then allow it under **System Settings → Privacy & Security → Open Anyway**. Grant Accessibility only to this app. When updating an existing installation, choose **Permissions & platform setup → Allow Keychain access after update…** and approve the macOS Keychain dialog to keep the certificate root and remembered devices. To keep the Accessibility entry across updates (the Keychain dialog stays once per build), a source checkout can re-sign the installed app with a local identity (`python3 scripts/sign-macos-local.py`, see [the macOS guide](macos-setup.md#keep-one-code-identity-across-updates)). Read the [macOS guide](macos-setup.md).
3. **Ubuntu 24.04 x64 / GNOME:** install the runtime packages listed in the [Linux guide](linux-setup.md), extract with `tar -xzf`, keep the whole `PCRemote-linux-x64/` directory in a stable location and run `./PcController.Desktop` as your desktop user (never `sudo`). Wayland requires explicit Remote Desktop consent; text needs the local GTK opt-in.
4. Both start paused and serve loopback HTTPS only. Approve your trusted network for the session, save and verify the public root profile, then pair from the phone as on Windows.

These builds are experimental and are not signed for distribution. Their [release readiness](release-readiness.md) lists implemented features, pending native/phone/LAN acceptance and exact manual checks.

## Publishing the next version

Each published release uses its own Git tag and versioned asset filenames. Do not replace a published version's files with a different build.

Read [AGENTS.md](../AGENTS.md), [CLAUDE.md](../CLAUDE.md), and the [release checklist](release-checklist.md). A version is complete only when its source, README, both agent files, GitHub files, and public assets are synchronized.

1. Update shared `PcRemoteVersion` in `Directory.Build.props` (Desktop inherits it), the matching product version in `src/PcController.App/PcController.App.csproj`, the packaging default in `scripts/package.ps1`, and the installer fallback in `installer/PCRemote.iss`. Push source fixes before the version bump so `main` never advertises an unpublished version while CI is still being repaired.
2. Update README version/status/download links, commands, outputs, and affected features/media. Update both `AGENTS.md` and `CLAUDE.md` version/release links and changed instructions. Update this download guide and add dated actual results/remaining gaps in `docs/release-evidence.md`; review affected setup, limitations, and acceptance docs. Preserve historical evidence. Review `.github/` workflows/templates/configuration and update affected files.
3. Run `./scripts/check-release-docs.ps1` and the checks required by `AGENTS.md` and the [README](../README.md#validation). Provisional package tests may run before committing; do not publish those provisional artifacts.
4. Commit the corresponding source and all release documentation and push `main`. The **Security and regression checks** workflow no longer runs on every push; it runs on pull requests and manual dispatch. Build the final packages from that commit so each informational source revision matches the future tag: run the workflow manually on `main` with **final** checked (`gh workflow run security-checks.yml --ref main -f final=true`). That single run is the release gate. It runs every suite on Windows, macOS and Ubuntu and uploads `release-win-x64-<sha>`, `release-osx-arm64-<sha>`, `release-osx-x64-<sha>` and `release-linux-x64-<sha>` workflow artifacts without tagging or publishing. Locally, the equivalent commands are:

   ```powershell
   ./scripts/package.ps1 -Version 0.4.2
   Get-Content ./artifacts/latest-release.txt
   ```

   ```sh
   python3 scripts/package-macos.py --rid osx-arm64 --final
   python3 scripts/package-linux.py --final
   ```

   Download the artifacts into a new empty directory. Verify every archive hash, inventory, license notice, native architecture, extracted macOS seal and informational source revision, inspect packages for private data and debug symbols, and scan source and packages for secrets. Merge all asset digests into one `SHA256SUMS.txt` (LF line endings, two spaces). Create an annotated tag for that exact commit:

   ```powershell
   git tag -a v0.4.2 -m "PC Remote v0.4.2"
   git push origin v0.4.2
   ```

5. Create the GitHub release for that tag (for example `gh release create v0.4.2 --verify-tag --prerelease --notes-file <notes> <assets>`), with version-specific notes covering actual validation, platform status and known limits, and attach the Windows installer and ZIP, the macOS/Ubuntu archives and manifests, and the combined `SHA256SUMS.txt`. Keep **pre-release** for preview builds. Publish within the user's authorization.
6. Verify the public tag points to the intended source commit, every asset downloads publicly, and asset sizes/SHA-256 digests match the local files. Check the README/download links and rendered release notes, then report the release URL and any remaining limits. Record final digests and public verification in a documentation-only follow-up commit.

The `0.4.2` commands above illustrate a future release; they do not claim that version is already available. Build artifacts remain under ignored `artifacts/` locally and are uploaded as release assets, rather than committed to `main`.

The GitHub **Release documentation** workflow checks version/link agreement on pull requests, `main` pushes, and version tag pushes. For a version bump it also requires changes to README, both agent files, this guide, and release evidence. Run `./scripts/check-release-docs.ps1 -BaseRef <previous-commit>` locally to perform that comparison. This check does not prove runtime behavior, absence of secrets, or availability of draft assets; complete those checks separately.

Documentation-only maintenance keeps the existing app version, tag, and published binaries. Update and push `main` without replacing release assets.


## Multi-platform builds

Version 0.3.0 was the first release with experimental macOS ARM64/Intel ZIPs and an Ubuntu x64 TAR.GZ; 0.3.1 fixes macOS LAN approval being dropped on Space, display and Secure Input changes. 0.3.2 redesigns the macOS/Linux window to match the Windows dashboard and fixes host, phone-remote, shell and Linux bridge defects. 0.3.3 shows the phone address before network approval and fixes stale setup states in that window. 0.3.4 fixes the macOS server never starting once Accessibility is granted. 0.3.5 keeps macOS LAN approval across Wi-Fi signal-quality updates. 0.3.6 keeps it across Wi-Fi record bursts, keeps the host and pending phone approvals across pause-only transitions, names the pause reason and adds a local signing script that keeps the Accessibility entry across updates. 0.4.0 adds the phone remote's **Air pointer** (hold the pad and turn the phone to move the pointer) with no companion, protocol or storage change. 0.4.1 corrects the Air pointer's gyroscope axis order (0.4.0 turned a tilt into a sideways move on real phones), seeds the gravity sign per platform and reduces rest drift; the companion is unchanged. [Release readiness](release-readiness.md) contains the support matrix, packaging commands, signing status and exact remaining manual steps. Candidate scripts label local outputs as candidates with a dirty-source manifest; `--final` (packaging and `check-package.py`, or the CI `final` dispatch input) produces release names only from a clean committed tree. Do not publish a candidate or rename it into a release asset.

For every release, synchronize the shared/Windows version and all required documentation, commit all source/docs, then build all platforms from that exact clean commit. Review each platform's runtime, native architecture, notices, secrets, startup, informational revision and pending acceptance. Merge every Windows installer/ZIP, macOS architecture archive, Ubuntu archive and manifest digest into one `SHA256SUMS.txt`. Do not publish a platform/backend/architecture without its required automated gates and explicit preview limits. Signing/notarization may stay unavailable only if truthfully disclosed. The annotated tag, assets, support matrix and public checks must agree. Published releases are never replaced.
