# Download a specific PC Remote version

Open [GitHub Releases](https://github.com/maamouribilel/windows-pc-mobile-controller/releases), choose a version, and expand its **Assets** section.

For the first public preview, [PC Remote v0.2.0](https://github.com/maamouribilel/windows-pc-mobile-controller/releases/tag/v0.2.0):

- [Windows x64 installer](https://github.com/maamouribilel/windows-pc-mobile-controller/releases/download/v0.2.0/PCRemote-0.2.0-win-x64-setup.exe): installs for the current user, adds a Start menu shortcut and an uninstall entry.
- [Windows x64 portable ZIP](https://github.com/maamouribilel/windows-pc-mobile-controller/releases/download/v0.2.0/PCRemote-0.2.0-win-x64.zip): extract the whole archive and run `PCRemote-win-x64/PcController.App.exe`. Keep its adjacent runtime and web files.
- [SHA256SUMS.txt](https://github.com/maamouribilel/windows-pc-mobile-controller/releases/download/v0.2.0/SHA256SUMS.txt): expected hashes for both downloads.

GitHub's generated **Source code (zip/tar.gz)** files contain the project source, not an installed Windows app. Normal use of the installer/portable build needs no SDK, Node.js, or separate .NET installation.

## Install and verify

1. Download the installer or portable ZIP and its checksum file from the same version.
2. In PowerShell, run `Get-FileHash -LiteralPath .\PCRemote-0.2.0-win-x64-setup.exe -Algorithm SHA256` (or use the ZIP filename). Compare the returned hash with the matching line in `SHA256SUMS.txt`.
3. Run the installer as your normal Windows user, or extract the portable ZIP. The optional Private LAN firewall helper alone requests administrator access.
4. Open PC Remote and follow the [certificate and iPhone pairing setup](setup.md).

This preview is unsigned. Windows may show publisher/SmartScreen prompts. Requirements and remaining manual checks are described in the version's release notes, [limitations](limitations.md), and [release checklist](release-checklist.md).

For an installer update, download the new version's installer. It requests a graceful exit before replacing the existing application files; exit through the tray menu if requested. Protected local certificate/device data is outside the installation directory and is retained. Keep each portable version in its own extracted directory and run one companion at a time.

## Publishing the next version

Each published release uses its own Git tag and versioned asset filenames. Do not replace a published version's files with a different build.

Read [AGENTS.md](../AGENTS.md), [CLAUDE.md](../CLAUDE.md), and the [release checklist](release-checklist.md). A version is complete only when its source, README, both agent files, GitHub files, and public assets are synchronized.

1. Update the product version in `src/PcController.App/PcController.App.csproj`, the packaging default in `scripts/package.ps1`, and the installer fallback in `installer/PCRemote.iss`.
2. Update README version/status/download links, commands, outputs, and affected features/media. Update both `AGENTS.md` and `CLAUDE.md` version/release links and changed instructions. Update this download guide and add dated actual results/remaining gaps in `docs/release-evidence.md`; review affected setup, limitations, and acceptance docs. Preserve historical evidence. Review `.github/` workflows/templates/configuration and update affected files.
3. Run `./scripts/check-release-docs.ps1` and the checks required by `AGENTS.md` and the [README](../README.md#validation), then package the chosen version. Packaging also checks documentation synchronization before building:

   ```powershell
   ./scripts/package.ps1 -Version 0.2.1
   Get-Content ./artifacts/latest-release.txt
   ```

4. Verify the installer/ZIP hashes and license notices, inspect the package for private data and debug symbols, and scan source and packages for secrets. Commit the corresponding source and all release documentation. Create and push an annotated tag for that exact commit:

   ```powershell
   git tag -a v0.2.1 -m "PC Remote v0.2.1"
   git push origin main
   git push origin v0.2.1
   ```

5. On GitHub **Releases → Draft a new release**, select that tag, add version-specific notes with actual validation and known limits, and attach the generated installer, portable ZIP, and `SHA256SUMS.txt`. Check **pre-release** for preview builds. Publish after reviewing the completed draft and its assets within the user's authorization.
6. Verify the public tag points to the intended source commit, all three assets download publicly, and asset sizes/SHA-256 digests match the local files. Check the README/download links and rendered release notes, then report the release URL and any remaining limits.

The `0.2.1` commands above illustrate a future release; they do not claim that version is already available. Build artifacts remain under ignored `artifacts/` locally and are uploaded as release assets, rather than committed to `main`.

The GitHub **Release documentation** workflow checks version/link agreement on pull requests, `main` pushes, and version tag pushes. For a version bump it also requires changes to README, both agent files, this guide, and release evidence. Run `./scripts/check-release-docs.ps1 -BaseRef <previous-commit>` locally to perform that comparison. This check does not prove runtime behavior, absence of secrets, or availability of draft assets; complete those checks separately.

Documentation-only maintenance keeps the existing app version, tag, and published binaries. Update and push `main` without replacing release assets.
