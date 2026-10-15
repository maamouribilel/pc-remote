# PC Remote — update notifications and in-app updates plan

Created: 2026-10-15. Baseline at creation: app version **0.4.2**, commit `b1bd7b8`, public [v0.4.2 preview](https://github.com/maamouribilel/pc-remote/releases/tag/v0.4.2).

This plan has four phases. Phase 1 (update notices) and Phase 2 (Windows **Restart to update**) ship in **0.4.3** (prepared on 2026-10-15; publication pending authorization). Phase 2 has passed hosted Windows checks but has not run on a physical PC. Phases 3–4 are still only a plan. Record actual results in the execution log at the end. A check that has not been run on real hardware stays **Pending**.

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
| D9 | **Only a Windows installer copy installs updates itself (Phase 2), and only on request.** Elsewhere the notice opens the release page, which lists downloads, checksums and per-platform update steps (Keychain access on Mac). | Each platform needs its own install path; see Phases 2–3. |

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

## Release process additions (since 0.4.3)

After the final assets are downloaded, checked and merged into `SHA256SUMS.txt`, and before `gh release create`:

```sh
python3 scripts/update-manifest.py create --version 0.4.3 --assets <release-dir> --summary "…" [--security]
python3 scripts/update-manifest.py sign --manifest <release-dir>/update.json
python3 scripts/update-manifest.py verify --manifest <release-dir>/update.json
```

Attach `update.json` and `update.json.sig` to the release along with the other assets. After publishing, download them again from the public URLs and run `verify` on the downloaded copies. The signing key stays local (`~/.config/pc-remote-release/update-signing-key.pem` by default). The script refuses `CI=true`, refuses key paths inside the repository and never prints the private key. `python3 scripts/update-manifest.py protect` adds a passphrase to the key file without changing the public key.

**Key custody:** back up the private key offline (for example an encrypted USB drive or password manager). If the key is lost, installed companions can't verify later releases. Users would then need one manual update to a release that embeds a new key, announced in the release notes. Until a release ships with a given public key, that key can be replaced freely.

## Phases

### Phase 1 — signed update notices (Windows, macOS, Ubuntu) — in 0.4.3

- Shared Host: `UpdateChecker` (feed, redirects, limits, signature, manifest validation) and `UpdateService` (preferences, one-time consent, 45-second startup delay then every 24 hours, Check now, Skip this version, Later).
- Windows WPF: an update banner under the header (consent, then notices), an **Updates** section in Preferences, an **Update available…** tray menu item and one tray balloon per version while the companion runs (a cached notice is announced again after a restart until it is skipped or installed).
- macOS/Ubuntu Avalonia: the same banner and Preferences group (automation IDs `update-banner`, `update-consent-on`, `update-consent-off`, `update-view`, `update-skip`, `update-later`, `update-automatic`, `update-check-now`, `update-status`), plus a tray/menu-bar item.
- Tooling: `scripts/update-manifest.py` (`keygen`, `create`, `sign`, `verify`, `protect`, `selftest`).
- Checks: portable update checks (signature, tampering, wrong key, version/tag mismatch, redirects to other hosts, size limits, drafts and malformed tags, preference persistence and corruption) and headless desktop UI checks. All use in-memory handlers, never the network.
- **Pending acceptance:** a real installed build of version N seeing a real signed release N+1 on Windows, macOS and Ubuntu; the tray balloon on Windows; the menu-bar item on macOS; behavior behind a proxy.

### Phase 2 — "Restart to update" on Windows (installer installs only) — in 0.4.3, not yet run on a physical PC

- **Download update** (only on that click) re-reads and re-verifies the release's signed `update.json`, takes its `win-x64`/`installer` entry, and streams that file from the release download URL (GitHub hosts only) into `%LOCALAPPDATA%\PcController\updates\<name>.partial`. It checks the exact signed size and SHA-256 while streaming, fails if no data arrives for 60 seconds, then renames the file and reopens it read-only with read-only sharing. It hashes again through that handle and keeps the handle open until the installer starts, so the checked file can't be changed, renamed or deleted in between. Cancel and every failure remove the partial file.
- **Restart to update** is offered only when no phone is in control, no power countdown is pending and no pairing approval is open. It first pauses control and releases held input, and stops if that fails. It then runs `<installer> /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /relaunch=1 /LOG="%LOCALAPPDATA%\PcController\updates\install.log"`. The installer's `PrepareToInstall` closes the companion with `--exit`. The firewall helper doesn't run on silent installs (`Check: not WizardSilent`).
- `installer/PCRemote.iss` has an `UpdateRelaunch` check: a `[Run]` entry reopens PC Remote only for silent installs started with `/relaunch=1`. This works from the first release that contains it, because each update runs the *new* version's installer.
- If PC Remote is still running when the installer ends, the update failed. Exit code 7 (PrepareToInstall could not close it) and other codes are reported in the banner, and **Restart to update** can be retried. Control stays paused until the person resumes it.
- Installed copy detection: the per-user Inno uninstall entry (`<AppId>_is1`) must name this folder as `InstallLocation`, with its `UninstallString` uninstaller inside it. Portable ZIP copies keep Phase 1 notices only.
- On startup, the companion deletes only release-named files and `.partial` downloads in the updates folder. A file still in use (the installer that just reopened it) is left for the next start.
- Acceptance (pending): install N on a real Windows PC, publish a test N+1 (or use a disposable test repository and key), update, and confirm that certificates, remembered devices, sign-in startup and the firewall rule survive and PC Remote reopens. Record Defender/SmartScreen behavior for the unsigned installer run from `%LOCALAPPDATA%`.

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

### 2026-10-15 — Phase 2 source implementation (Windows "Restart to update")

- Phase 1 was committed to `main` as `e43b188` (not pushed). Phase 2 started in the same session at the maintainer's request; no version bump or release is authorized.
- Implemented:
  - Host: `UpdateChecker.DownloadAsync` and `VerifiedDownload`, plus `IUpdateInstaller` and the `Idle → Downloading → Ready → Installing` stages, cancel, exit-code reporting and startup cleanup in `UpdateService`.
  - Phase 1 fix found while doing this: response bodies had no time limit once headers arrived. Small fetches are now bounded by the 30-second request timeout and downloads by a 60-second stall timeout, and cancellation filters catch `OperationCanceledException`.
  - Windows: `WindowsUpdateRules` (AppId, installed-copy rule, installer arguments; also compiled into the portable tests) and `WindowsUpdateInstaller` (registry read, process start).
  - WPF: **Download update**, **Cancel download** and **Restart to update** in the banner, the blockers, revised consent and Preferences text.
  - Installer: the `UpdateRelaunch` check and `[Run]` entry in `installer/PCRemote.iss`.
- Automated results on the development Mac, 2026-10-15:
  - `python3 scripts/portable.py test`: 353 checks passed (34 new). They cover a verified download and its progress; refusal with no file left for a changed hash, oversized, truncated or wrong Content-Length body, untrusted or other-version manifest, foreign redirect and missing platform; stall timeout and cancellation; cleanup name rules; install stages with a recording installer (exit code 7 and others, start failure, retry, download failure and cancel); installed-copy rules; installer arguments; and the `.iss` AppId, `--exit` and relaunch entries. The Windows-only "file cannot be changed or deleted while held" check runs only in the Windows suite.
  - `python3 scripts/portable.py desktop-test`: 100 passed (shell unchanged). `update-manifest.py selftest` and `check-release-docs.ps1` passed.
  - WPF app and `tests/PcController.Tests` compiled with `-p:EnableWindowsTargeting=true`; neither has run on Windows.
- Reviewed for the relaunch: nothing in the companion creates inheritable handles (the single-instance `Mutex` and named pipe, Kestrel sockets and the verified file use .NET's non-inheritable defaults), so the installer started with `UseShellExecute = false` should not keep the old instance's mutex or port 8443 alive for the reopened companion. This is unverified until Windows runs it. `scripts/check-windows-package.py` doesn't inspect installer entries; `scripts/package.ps1` compiles the script.
- While an update is downloading or ready, the Windows banner stays visible without **Later**, because it holds the only **Cancel download** and **Restart to update** buttons.
- **Pending:**
  - The Inno Pascal changes are compiled only when the Windows CI job builds the provisional installer; that job hasn't run for this change.
  - The WPF banner, the Windows-only lock check, a real silent install with `--exit` and relaunch, and Defender/SmartScreen behavior for an unsigned installer run from `%LOCALAPPDATA%` have not run on Windows.
  - A real installed N → N+1 update needs an installed build that contains Phase 2 and a later published release with a signed `update.json`. Correction (2026-10-15): this does not need two Phase 2 releases. A test build of the Phase 2 source (it reports 0.4.2) installed on a PC can update to the first Phase 2 release, 0.4.3, once that is published. Ordinary 0.4.2 users still have to install 0.4.3 by hand.

### 2026-10-15 — first hosted CI run for Phases 1–2

- [Run 38801590541](https://github.com/maamouribilel/pc-remote/actions/runs/38801590541) (manual dispatch, not `final`) on `61398aa`: all 8 jobs succeeded.
- Windows (`validation`): every Windows test-suite invocation ran the update checks. They include the first Windows pass of "the verified installer cannot be changed or deleted while it is held", the installed-copy rules and the `PCRemote.iss` checks.
- The provisional installer compiled with Inno Setup 7.1.0. `package.ps1` checks the compiler's exit code and then hashed the generated `setup.exe`, so the new `UpdateRelaunch` Pascal code compiles. The Windows package content checks passed.
- `portable (windows-latest)`: 351 checks. This is 353 minus the three Unix-only file-mode and symlink checks, plus the Windows-only lock check. macOS and Ubuntu ran 353, and the manifest tool self-test passed on both.
- Still pending, because CI does not install or run the app: a real silent install with `--exit` and relaunch, the WPF banner on screen, Defender/SmartScreen behavior, and a real installed N → N+1 update.

### 2026-10-15 — 0.4.3 release preparation

- At the maintainer's request (no tag or publication without separate approval), the version is set to 0.4.3 (shared `Directory.Build.props`, Windows project, packaging default, installer fallback). README, both agent files, download guide, security policy, limitations, readiness, setup, troubleshooting, media notes, the implementation/multiplatform plans and a dated release-evidence entry now describe 0.4.3.
- 0.4.3 is the first release with update checks, so it attaches its own signed `update.json`/`update.json.sig` (created and signed locally after the final assets are verified). Installed 0.4.2 copies can't see it; a test build of the Phase 2 source can, which is the planned way to observe the full Windows update path.
- Local and hosted check results for the release source are recorded in `docs/release-evidence.md`.
