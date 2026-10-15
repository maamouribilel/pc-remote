# PC Remote — update notifications and in-app updates plan

Created: 2026-10-15. Baseline at creation: app version **0.4.2**, commit `b1bd7b8`, public [v0.4.2 preview](https://github.com/maamouribilel/pc-remote/releases/tag/v0.4.2).

This plan has four phases. Phase 1 (update notices) is implemented in source but **not yet released**. Phases 2–4 are still only a plan. Record actual results in the execution log at the end. A check that has not been run on real hardware stays **Pending**.

## The problem

No published version (0.4.2 and earlier) checks for updates. When a fix or security release ships, people who already installed PC Remote don't find out unless they visit GitHub. These versions can't be fixed retroactively: the first version with an update check must be installed by hand. Its release notes and the README should say so clearly. Until then, people can use GitHub's **Watch → Custom → Releases** to get an email for each release.

## Why the design starts with signatures

Every download is unsigned: Windows shows SmartScreen/Unknown publisher, the Mac app has only an ad-hoc seal, and Ubuntu archives have no distribution signature. PC Remote also injects keyboard and mouse input. If an updater simply trusted "the newest file on GitHub", anyone who took over the GitHub account, a token or the CI could put code on every installed PC. Files that an app downloads itself also carry no download-quarantine mark, so SmartScreen and Gatekeeper never check them. The updater's own check would be the only safeguard.

So every update decision depends on a **release manifest signed with an offline key**:

- One ECDSA P-256 key pair. The private key lives only on the maintainer's Mac, outside the repository, and never in CI or GitHub secrets (same rule as `scripts/sign-macos-local.py`). ECDSA P-256 is built into .NET on Windows, macOS and Linux, so no dependency is added.
- The public key(s) are committed in `src/PcController.Host/update-signing-keys.pub` and embedded in the Host assembly.
- Each release gets `update.json` and `update.json.sig` as extra assets. The companion accepts a release only if the signature verifies against an embedded key, the manifest's version and tag match the GitHub release, and the version is newer than its own.

GitHub only delivers the files. Trust comes from the signature, not from TLS or the GitHub account.

## Key decisions

| # | Decision | Why |
| --- | --- | --- |
| D1 | **Phase 1 only checks and notifies.** It downloads the release list and the signed `update.json`, never an installer or archive, and runs nothing. | It solves the stated problem on every platform, including portable ZIPs, with the smallest attack surface. |
| D2 | **Automatic checks are opt-in, asked once.** A non-modal banner in the companion window asks once. "Check automatically" or "Don't check" is saved and the banner never returns. A switch and **Check now** stay in Preferences. | The product has made no internet connection so far, and its docs say internet access is out of scope. Pressing **Check now** is an explicit one-time choice. |
| D3 | **Releases are found through the GitHub API list** (`api.github.com/repos/maamouribilel/pc-remote/releases?per_page=20`), not `releases/latest`. | Every release is a pre-release, and `releases/latest` skips pre-releases, so it would never see them. |
| D4 | **Hosts are fixed and HTTPS only**: `api.github.com`, `github.com`, and GitHub's asset hosts `release-assets.githubusercontent.com` and `objects.githubusercontent.com`. Redirects are followed manually (at most 5) and only between these hosts. Default certificate validation, size caps (list 4 MiB, manifest 64 KiB, signature 1 KiB), and a 30-second timeout apply. | GitHub redirects release downloads to its asset host. As of 2026-10-15 a v0.4.2 asset redirected to `release-assets.githubusercontent.com`. |
| D5 | **Nothing identifying is sent.** The only header besides the standard ones is `User-Agent: PC-Remote-update-check` (GitHub requires a User-Agent). No version, install ID, device, pairing or setting is sent. GitHub still sees the computer's IP address, and the consent text says so. | Privacy. |
| D6 | **Companion only.** No v1 message, field, capability, endpoint or Host setting changes. The phone can't see, trigger or approve an update. | Same rule as the Air pointer: protocol and trust boundaries stay unchanged. The phone remote is served by the companion, so updating the companion also updates the phone page. |
| D7 | **Preferences live in a plain `update-settings.json`** next to the other non-secret settings: Windows `%LOCALAPPDATA%\PcController`, macOS `~/Library/Application Support/PC Remote`, Ubuntu `~/.local/share/pc-remote`. It's written atomically. On Unix the directory is 0700 and the file 0600, and a symlinked file is refused. | Update checks must keep working when certificate storage is locked or setup failed. That's when an update matters most, so this file must not depend on DPAPI or the Keychain. A corrupted file falls back to "not asked yet"; trust data is never touched. |
| D8 | **Several trusted keys are supported** (any `PUBLIC KEY` block in the `.pub` file). | Key rotation: release N+1 ships the new key alongside the old one, and is still signed with the old key. Later releases switch to the new key. |
| D9 | **Installing stays manual until Phase 2.** The notice opens the release page, which lists downloads, checksums and per-platform update steps (Keychain access on Mac). | Each platform needs its own install path; see Phases 2–3. |

## Manifest

`update.json` (UTF-8 JSON; the signature covers the exact file bytes):

```json
{
  "schema": 1,
  "product": "pc-remote",
  "version": "0.4.3",
  "tag": "v0.4.3",
  "releaseUrl": "https://github.com/maamouribilel/pc-remote/releases/tag/v0.4.3",
  "published": "2026-10-20T12:00:00Z",
  "security": false,
  "summary": "Short plain-text note shown in the update banner (at most 300 characters).",
  "assets": [
    { "platform": "win-x64", "kind": "installer", "name": "PCRemote-0.4.3-win-x64-setup.exe", "size": 123456, "sha256": "<64 lowercase hex>" }
  ]
}
```

`update.json.sig` is one line: the Base64 of the DER-encoded ECDSA P-256 / SHA-256 signature over `update.json`.

The companion rejects a manifest if the schema isn't 1, the product is wrong, the version doesn't match the tag or the GitHub release, the release URL isn't exactly the expected one, the summary is too long or contains control characters, or any asset entry is malformed. Phase 1 doesn't use the assets list. It's signed now so that Phase 2 can verify downloads against it.

Candidates are checked in descending version order. A release without `update.json` is skipped. If a newer release fails verification and nothing newer verifies, the companion reports "could not be verified" and offers nothing. A verified older-but-still-newer release is still offered.

## Release process additions (from the first release that ships Phase 1)

After the final assets are downloaded, checked and merged into `SHA256SUMS.txt`, and before `gh release create`:

```sh
python3 scripts/update-manifest.py create --version 0.4.3 --assets <release-dir> --summary "…" [--security]
python3 scripts/update-manifest.py sign --manifest <release-dir>/update.json
python3 scripts/update-manifest.py verify --manifest <release-dir>/update.json
```

Attach `update.json` and `update.json.sig` to the release along with the other assets. After publishing, download them again from the public URLs and run `verify` on the downloaded copies. The signing key stays local (`~/.config/pc-remote-release/update-signing-key.pem` by default). The script refuses `CI=true`, refuses key paths inside the repository and never prints the private key. `python3 scripts/update-manifest.py protect` adds a passphrase to the key file without changing the public key.

**Key custody:** back up the private key offline (for example an encrypted USB drive or password manager). If the key is lost, installed companions can't verify later releases. Users would then need one manual update to a release that embeds a new key, announced in the release notes. Until a release ships with a given public key, that key can be replaced freely.

## Phases

### Phase 1 — signed update notices (Windows, macOS, Ubuntu) — implemented in source, unreleased

- Shared Host: `UpdateChecker` (feed, redirects, limits, signature, manifest validation) and `UpdateService` (preferences, one-time consent, 45-second startup delay then every 24 hours, Check now, Skip this version, Later).
- Windows WPF: an update banner under the header (consent, then notices), an **Updates** section in Preferences, an **Update available…** tray menu item and one tray balloon per version while the companion runs (a cached notice is announced again after a restart until it is skipped or installed).
- macOS/Ubuntu Avalonia: the same banner and Preferences group (automation IDs `update-banner`, `update-consent-on`, `update-consent-off`, `update-view`, `update-skip`, `update-later`, `update-automatic`, `update-check-now`, `update-status`), plus a tray/menu-bar item.
- Tooling: `scripts/update-manifest.py` (`keygen`, `create`, `sign`, `verify`, `protect`, `selftest`).
- Checks: portable update checks (signature, tampering, wrong key, version/tag mismatch, redirects to other hosts, size limits, drafts and malformed tags, preference persistence and corruption) and headless desktop UI checks. All use in-memory handlers, never the network.
- **Pending acceptance:** a real installed build of version N seeing a real signed release N+1 on Windows, macOS and Ubuntu; the tray balloon on Windows; the menu-bar item on macOS; behavior behind a proxy.

### Phase 2 — "Restart to update" on Windows (installer installs only)

- Download the `win-x64` installer named in the verified manifest into `%LOCALAPPDATA%\PcController\updates`, streaming its SHA-256 and enforcing the signed size. Keep the file open with read-only sharing from verification until the installer process starts, so it can't be swapped.
- Offer **Restart to update** only when no phone is in control and no power countdown is pending. Run `PCRemote-x.y.z-win-x64-setup.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART`. The installer's `PrepareToInstall` already calls `PcController.App.exe --exit` for a graceful exit, and failed input cleanup still blocks that exit. The firewall helper already doesn't run on silent installs (`Check: not WizardSilent`).
- `installer/PCRemote.iss` needs a relaunch step for silent update installs. The existing "Open PC Remote setup" step is `skipifsilent`, so a silent install would otherwise finish without reopening the app.
- Portable ZIP copies (no installer uninstall entry for this path) keep Phase 1 notices only.
- Acceptance: install N on a real Windows PC, publish a test N+1 (or use a disposable test repository and key), update, and confirm that certificates, remembered devices, sign-in startup and the firewall rule survive.

### Phase 3 — macOS and Ubuntu

- **macOS:** replacing the `.app` is technically simple: verify, `ditto -x -k` into a staging folder on the same volume, `codesign --verify --deep --strict`, swap with a helper after exit, then relaunch. But every unsigned build has a new code identity, so after any update the person still has to approve **Allow Keychain access after update…** and grant Accessibility again. No updater can avoid that. Keep notices only until Phase 4.
- **Ubuntu:** a user-writable extracted folder could be swapped with a sibling folder after exit. AppImage, `.deb` or Flatpak packaging is the more natural route. Keep notices only until packaging is decided.

### Phase 4 — distribution signing

- macOS Developer ID plus notarization gives a stable identity, so Accessibility and Keychain approval survive updates. Then an in-place "Restart to update" (Sparkle-style) is worth building.
- Windows Authenticode signing (for example Azure Trusted Signing) removes SmartScreen prompts and adds a second check on downloaded installers.
- The offline manifest key stays the primary trust anchor either way.

## Not doing

- No silent background installs, no automatic restarts, no phone-triggered updates.
- No telemetry, install IDs or usage data.
- No off-the-shelf updater for now. Velopack would replace the Inno installer and the inventoried archive pipeline, NetSparkle adds dependencies, and neither solves the unsigned Mac identity problem.

## Execution log

### 2026-10-15 — plan and Phase 1 source implementation

- Plan created from baseline `b1bd7b8` (0.4.2).
- Checked live on 2026-10-15: `https://github.com/maamouribilel/pc-remote/releases/download/v0.4.2/SHA256SUMS.txt` answered `302` to `release-assets.githubusercontent.com`, and the releases API list answered `200` without authentication.
- Phase 1 implemented in source (no version bump, release or commit authorized by this work):
  - Host: `UpdateCheck.cs` (`UpdateChecker`) and `UpdateService.cs`; trusted key file `update-signing-keys.pub` embedded as a resource; internal test constructors exposed only to the test assemblies (`AssemblyInfo.cs`).
  - Windows WPF (`App.xaml.cs`, `MainWindow.xaml(.cs)`) and macOS/Ubuntu Avalonia (`DesktopApp.cs`, `MainWindow.cs`) UI; `MacHostFactory.DefaultDirectory` shared with the update preferences path.
  - `scripts/update-manifest.py`; packaging now also refuses a stray `update-settings.json`; CI runs the tool's self-test on macOS/Ubuntu.
- Signing key: key 1 generated on the maintainer's Mac with `update-manifest.py keygen` at `~/.config/pc-remote-release/update-signing-key.pem` (0600 in a 0700 folder, no passphrase yet; `protect` adds one without changing the public key). Its public key is the only entry in `update-signing-keys.pub`. **Not yet backed up offline.** No release embeds it yet, so it can still be replaced freely.
- Automated results on the development Mac (macOS 27, ARM64), 2026-10-15:
  - `python3 scripts/update-manifest.py selftest`: round trip plus 6 rejections passed.
  - `python3 scripts/portable.py test`: 319 checks passed, including 47 update checks: the manifest signed by the Python/openssl tool verifies in .NET and is rejected by the production key, and tampering, wrong key, version/tag mismatch, foreign release page, control characters, unexpected download, unknown schema, foreign or plain-HTTP redirects, foreign asset URL, listed and streamed size limits, rate limit, offline and malformed list are all refused or reported. The tests also cover consent, Check now, Later/Skip persistence, damaged or edited settings, 0700/0600 modes, symlink refusal and cancellation on exit.
  - `python3 scripts/portable.py desktop-test`: 100 checks passed, including 8 update UI checks (no banner without a service, question before any request, Don't check, Preferences state, Check now with a security notice, status text, Skip, Check automatically).
  - `mac-test` 70 and `linux-test` 81 checks passed (unchanged adapters; `MacHostFactory` default path refactor).
  - Windows WPF app and `tests/PcController.Tests` **compiled only** (`-p:EnableWindowsTargeting=true` on macOS). They have not been run on Windows.
  - `scripts/check-release-docs.ps1` (PowerShell from `.tools`) passed for v0.4.2.
  - Offscreen Skia renders of the Avalonia window (scratch only, not committed), each inspected: the question at 1020 px and 600 px, the notice at 1020 px, the security notice at 600 px, and the six-section Preferences grid. None was clipped.
- Live read-only check, 2026-10-15: the production `UpdateChecker` read the real GitHub release list without authentication and reported **up to date** for both 0.4.2 and 0.1.0, as expected, because no published release carries `update.json` yet.
- **Pending:** a Windows run of the WPF banner, tray item and balloon; an installed N → N+1 check on Windows, macOS and Ubuntu against a real signed release; behavior behind a proxy; offline backup of the signing key.
