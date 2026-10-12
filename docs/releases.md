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

1. Update the product version in `src/PcController.App/PcController.App.csproj`, the packaging/installer defaults, and the current README download links. Describe the resulting changes and known limitations in the release notes.
2. Run the relevant checks in the [README](../README.md#validation), then package the chosen version:

   ```powershell
   ./scripts/package.ps1 -Version 0.2.1
   Get-Content ./artifacts/latest-release.txt
   ```

3. Verify the installer/ZIP hashes, inspect the package for private data and debug symbols, and commit the corresponding source. Create and push an annotated tag for that exact commit:

   ```powershell
   git tag -a v0.2.1 -m "PC Remote v0.2.1"
   git push origin main
   git push origin v0.2.1
   ```

4. On GitHub **Releases → Draft a new release**, select that tag, add version-specific notes, and attach the generated installer, portable ZIP, and `SHA256SUMS.txt`. Check **pre-release** for preview builds. Publish after reviewing the completed draft and its assets.

The `0.2.1` commands above illustrate a future release; they do not claim that version is already available. Build artifacts remain under ignored `artifacts/` locally and are uploaded as release assets, rather than committed to `main`.
