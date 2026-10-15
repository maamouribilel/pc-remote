# Development guide

Build the companion for the computer being controlled. User installation and pairing are covered in [setup](setup.md); [the README](../README.md) describes current downloads and features.

## Prerequisites

- **Windows:** an x64 machine with PowerShell 7 and Git. **macOS:** Xcode Command Line Tools and Python 3. **Ubuntu:** `cc`, `pkg-config` and the native development packages listed in the [Linux guide](linux-setup.md).
- Internet access for the initial toolchain/dependency downloads.
- For control: a phone, tablet or computer with a modern browser on the same trusted network. iPhone Safari is the verified path; on Windows that network must be marked **Private**, on macOS and Ubuntu you approve the interface in the companion.

The scripts prefer a verified portable toolchain in `.tools/`: **.NET SDK 10.0.402**, **Node.js 24.21.0**, and **npm 11.19.0**. Packaged releases bundle .NET runtime 10.0.13 and do not need development tools.

## Install, build, and run

Windows (PowerShell 7):

```powershell
git clone https://github.com/maamouribilel/pc-remote.git
Set-Location pc-remote

./scripts/install-tools.ps1
./scripts/build.ps1 -Configuration Release
./scripts/run.ps1 -Configuration Release
```

macOS or Ubuntu:

```sh
git clone https://github.com/maamouribilel/pc-remote.git
cd pc-remote

python3 scripts/portable.py build
python3 scripts/portable.py desktop
```

`build.ps1` runs `npm ci`, TypeScript checking, a production Vite build, and a .NET solution build, and copies the built web app into the companion's `wwwroot/`; `run.ps1` selects the portable runtime when available. `portable.py build` does the same for the shared companion, and `desktop` starts it; packaging commands for each platform are under [Shared backend development](#shared-backend-development-on-macoslinux) and [Build and deploy](#build-and-deploy).

## Environment configuration

**No `.env` file, API key, cloud credential, or database connection string is required.** Certificates, device authorization, and preferences are created locally. Private keys and device credentials stay outside this repository: Windows user-bound DPAPI under `%LOCALAPPDATA%\PcController\certificates`, a Keychain-backed key with AES-GCM files under `~/Library/Application Support/PC Remote` on macOS, and a GNOME-keyring-backed key on Ubuntu.

Use the live address displayed by the companion on port **8443**. On Windows, the optional firewall helper requests administrator access for a program-specific rule limited to **Private profiles / LocalSubnet**; on macOS and Ubuntu you approve the interface under **Trusted network** for the session instead. Run the companion itself as a normal user.

<details>
<summary><strong>Pair your phone in four steps</strong></summary>

1. In the companion's certificate setup, export the public trust profile. Compare its fingerprint with the one the companion shows before trusting it.
2. Trust the companion's root on the phone. iPhone: install the profile and enable full trust under **Settings → General → About → Certificate Trust Settings** (verified path). Other devices: import the public root certificate file, which the project has not yet verified. Follow [certificate setup and removal](certificates.md).
3. Create a pairing link, scan the QR with the phone's camera, open the link in its browser, name the device, and request approval. Approve on the computer. The link expires after two minutes and is single use.
4. Optionally add the page to the Home Screen (Safari **Share → Add to Home Screen**; other browsers offer their own install option). A Home Screen app may need its own approval.

Remembered authentication expires after 30 days and can be revoked immediately in the companion. Address changes preserve root/device records but require opening the new address.

</details>

## Everyday development

```powershell
./scripts/stop.ps1 -Configuration Release
./scripts/build.ps1 -Configuration Release
./scripts/run.ps1 -Configuration Release
```

Exit any installed copy through its tray menu before starting a development companion or isolated tests. Both use port 8443 and enforce one instance per user.

## Project structure

```text
pc-remote/
├── src/
│   ├── PcController.App/       # WPF companion and Windows production composition
│   ├── PcController.Desktop/   # Shared Avalonia shell, real macOS development composition
│   ├── PcController.Linux/     # XCB/XTEST, consented portal, GNOME login keyring candidate
│   ├── PcController.MacOS/     # Quartz/Accessibility, Keychain storage and lifecycle
│   ├── PcController.Host/      # Portable HTTPS/WSS, endpoints, lifecycle, preferences
│   ├── PcController.Core/      # Protocol, pairing, ownership, media/power logic
│   ├── PcController.Windows/   # Native adapters, DPAPI/ACLs, key storage, discovery
│   └── remote/                # TypeScript browser UI and shipped branding
├── tests/
│   ├── PcController.PortableTests/ # Portable protocol/security/recording fixtures
│   ├── PcController.DesktopTests/  # Shared shell host and headless UI workflows
│   ├── PcController.MacOSTests/    # macOS adapter, Keychain and guarded native checks
│   ├── PcController.LinuxTests/    # Linux adapter faults, Xvfb, portal and keyring checks
│   ├── PcController.Tests/    # Windows DPAPI, integration and native smoke checks
│   └── remote/                # Playwright interactions and responsive checks
├── scripts/                   # Tools, build, run, tests, packaging, setup
├── installer/                 # Inno Setup definition
├── assets/                    # Original branding and historical design references
├── docs/                      # Guides, protocol, limitations, evidence, README media
├── PcController.slnx          # Full Windows solution
├── PcController.Portable.slnx # Core, Host, Desktop, and isolated tests
├── global.json                # Pinned .NET SDK
└── Directory.Build.props      # Shared compiler settings
```

The browser sends bounded, allowlisted commands over WSS. The companion authenticates devices, checks session ownership, and dispatches commands through the platform adapters (Windows, macOS or Linux). Launcher requests carry configured app IDs; executable paths stay on the PC. Read the [protocol and security design](protocol.md).

### Shared backend development on macOS/Linux

Phases 1–5 provide the portable backend, Avalonia shell, platform-aware browser, a macOS companion with real Quartz/Accessibility input and Keychain storage, and an Ubuntu companion with X11/consented Wayland portal input and encrypted GNOME keyring storage. Since version 0.3.0, releases include these as **experimental** archives; Windows remains the primary platform. **Phase 4 remains in progress**: real Ubuntu x64/GNOME X11 and Wayland text/cleanup plus phone/LAN acceptance are pending, and Linux text is an optional GTK Unicode mode. See [Linux prerequisites, commands, recovery and gates](linux-setup.md). macOS starts paused with loopback HTTPS after secure storage setup; LAN needs explicit session approval and native input needs Accessibility. See [macOS build, permissions, Keychain updates and limits](macos-setup.md) and [desktop setup and fixture workflow](desktop-shell.md). Use the same pinned SDK/Node/npm versions with native tool archives on PATH or in `.tools/`, plus Python 3.9+ (3.13+ for strict TLS):

```sh
python3 scripts/portable.py build
python3 scripts/portable.py test
python3 scripts/portable.py desktop-test
python3 scripts/portable.py remote-unit
python3 scripts/portable.py browser-capabilities
python3 scripts/portable.py desktop
# Local Apple Silicon candidate on a Mac with Xcode Command Line Tools:
python3 scripts/package-macos.py
# Open the .app path printed by packaging; candidates use fresh timestamped directories.
python3 scripts/check-package.py --rid osx-arm64
# On a Linux x64 build host:
python3 scripts/package-linux.py
python3 scripts/check-package.py --rid linux-x64
# Release names only from a clean committed tree (CI: workflow_dispatch input "final"):
python3 scripts/package-macos.py --rid osx-arm64 --final
python3 scripts/check-package.py --rid osx-arm64 --final
python3 scripts/portable.py mac-test
python3 scripts/portable.py tls
python3 scripts/portable.py audit
```

Phase 5 adds native supported audio/media/launcher/login/power adapters and version-aware archives. Mac power supports sleep only; Linux actions require logind permission without prompting. See the [feature matrix, package checks and remaining acceptance](release-readiness.md).

PowerShell 7 `build.ps1`/`test.ps1` select portable projects automatically on macOS/Linux; pass `-Portable` on Windows. `install-tools.ps1` remains a Windows archive installer. See [portable architecture, adapters and fixture rules](portable-host.md) and [dated Phase 1 evidence](../MULTIPLATFORM_PLAN.md#execution-log).

## Validation

With all companions closed:

```powershell
./scripts/test.ps1
./scripts/test.ps1 -Security
./scripts/test.ps1 -Pairing
./scripts/test.ps1 -Lifecycle
./scripts/test.ps1 -PcFeatures
./scripts/test-browser-mouse.ps1
./scripts/audit-dependencies.ps1
```

The browser fixture script needs Python 3.9+ on Windows too; strict TLS needs Python 3.13+. The browser script starts the portable temporary loopback fixture and cleans it up afterward. Checks cover pairing, gestures, Unicode text, navigation, media, launcher/profile selection, volume read-back, cancelled power actions, reconnects, and portrait/landscape/desktop viewports.

See the [initial publication validation results](publication-checks.md) for build, browser, transport, desktop, packaging, documentation, and secret-scan evidence.

The [release evidence](release-evidence.md) records per-version local and hosted results. Security and regression checks run on pull requests and manual workflow dispatch; the final release dispatch runs all OS suites and builds assets once. CI configuration and recording checks do not establish physical-phone, installed-app or native desktop acceptance.

Update checks ([AUTO_UPDATE_PLAN.md](../AUTO_UPDATE_PLAN.md)) are covered by the portable/Windows suites (in-memory GitHub, signature, tampering, redirects, limits, preferences) and the headless desktop UI checks; none contacts GitHub. `python3 scripts/update-manifest.py selftest` checks the manifest tool with a throwaway key on macOS/Linux.

For strict certificate interoperability, run `./scripts/test-certificate.ps1` with **Python 3.13+** available. It uses only Python's standard library and checks valid trust, incorrect-host rejection, and untrusted-certificate rejection. CI includes this check; Python is a validation tool and is not needed to run the packaged app.

<details>
<summary><strong>Native and installed-app checks</strong></summary>

```powershell
# Actual Windows input in guarded disposable targets.
./scripts/test.ps1 -MouseSmoke
./scripts/test.ps1 -KeyboardSmoke

# With the companion running: certificate-validated HTTPS/WSS.
./scripts/test.ps1 -Integration
./scripts/test.ps1 -Lan

# Exercises the actual process and exits it at the end.
./scripts/test-desktop.ps1 -Executable "$env:LOCALAPPDATA\Programs\PCRemote\PcController.App.exe"
```

Native tests abort on lost target focus. The browser fixture uses an explicit loopback-only TLS bypass for UI checks; this does not establish phone certificate trust. .NET transport tests validate the installation root, chain, server EKU, and hostname/IP SAN. Production has no plaintext or TLS-bypass mode.

</details>

## Contributing and releasing

Coding agents must read [AGENTS.md](../AGENTS.md); Claude also reads [CLAUDE.md](../CLAUDE.md). Every new version must update the README, both agent files, download documentation, validation evidence, and affected `.github/` files before publishing matching source, tag, and release assets. Follow the [release publishing procedure](releases.md#publishing-the-next-version) and [acceptance checklist](release-checklist.md).

Run `./scripts/check-release-docs.ps1` before packaging. The GitHub **Release documentation** check also catches version/link mismatches and missing release documentation updates. Runtime checks, secret scans, and public asset verification remain part of the release procedure.

Read the [security policy](../.github/SECURITY.md) for private vulnerability reporting and trust boundaries. Approve only your own trusted browser: keyboard control grants broad access to your signed-in desktop session. Keep the app on a trusted Private LAN and verify certificate trust.

## Build and deploy

Deploy the companion to the computer you want to control. Its web app is bundled with it rather than hosted separately in the cloud. Windows packaging follows; the macOS and Ubuntu packaging commands are under [Shared backend development](#shared-backend-development-on-macoslinux).

```powershell
./scripts/stop.ps1 -Configuration Release
./scripts/package.ps1 -Version 0.4.2
Get-Content ./artifacts/latest-release.txt
```

Packaging obtains the verified local Inno Setup compiler if needed, rebuilds Release, and publishes a self-contained x64 app into a new timestamped directory:

| Output | Use |
| :--- | :--- |
| `PCRemote-0.4.2-win-x64-setup.exe` | Per-user installation, Start menu shortcut, uninstall entry |
| `PCRemote-0.4.2-win-x64.zip` | Portable distribution; keep the executable and adjacent files together |
| `PCRemote-win-x64/` | App, bundled runtime, web assets, documentation, license notices |
| `SHA256SUMS.txt` | Installer and archive hashes |

The installer targets `%LOCALAPPDATA%\Programs\PCRemote`. Review the [release checklist](release-checklist.md) before distributing binaries. Pinned tools and lockfiles support repeatable inputs; byte-identical installer output is not claimed.

Generated releases, dependencies, SDKs, environment files, certificate exports, credentials, logs, and temporary files are excluded from Git. **Never distribute `.protected` files or private keys.** Uninstall retains local data for reinstall; permanent retirement also requires removing certificate trust. See [setup](setup.md), [troubleshooting](troubleshooting.md), and [third-party notices](third-party.md).
