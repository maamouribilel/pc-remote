<div align="center">

<img src="src/remote/public/pc-remote-v2-128.png" alt="PC Remote logo: a phone controlling a desktop computer" width="96" height="96">

# PC Remote

### Your Windows PC. Controlled from your iPhone.

A touchpad, keyboard, media remote, and app launcher in Safari.<br>
Pair on your PC, connect over your home network, and keep control within reach.

[![Download](https://img.shields.io/badge/Download-v0.2.1_preview-5EE8BE?style=flat-square)](https://github.com/maamouribilel/windows-pc-mobile-controller/releases/tag/v0.2.1)
[![Windows](https://img.shields.io/badge/Windows-x64-0078D4?style=flat-square)](#download)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square)](#tech-stack)
[![TypeScript](https://img.shields.io/badge/TypeScript-7-3178C6?style=flat-square)](#tech-stack)
[![Vite](https://img.shields.io/badge/Vite-8-646CFF?style=flat-square)](#tech-stack)
[![Transport](https://img.shields.io/badge/Transport-HTTPS_%2B_WSS-5EE8BE?style=flat-square)](docs/protocol.md)
[![License](https://img.shields.io/badge/License-MIT-5EE8BE?style=flat-square)](LICENSE)

**[Download for Windows](https://github.com/maamouribilel/windows-pc-mobile-controller/releases/download/v0.2.1/PCRemote-0.2.1-win-x64-setup.exe)** · **[All versions](https://github.com/maamouribilel/windows-pc-mobile-controller/releases)** · **[Watch the walkthrough](#walkthrough)** · **[Build from source](#getting-started)**

<img src="docs/assets/desktop-controls.jpg" alt="The actual Windows companion showing power, pointer, and media preferences" width="900">

**A native Windows companion. A browser remote. One trusted local connection.**

</div>

---

## Made for the screen across the room

Watch your PC screen while your phone handles the controls. Move the pointer, send text, navigate menus, switch media profiles, open configured apps, and adjust Windows volume from five focused tabs.

PC Remote runs as a Windows notification-tray companion and serves its own browser interface. All application assets are bundled locally. Normal packaged operation needs no cloud account, subscription, Node.js installation, or separate .NET runtime.

## Download

**[Download PC Remote v0.2.1 for Windows x64 →](https://github.com/maamouribilel/windows-pc-mobile-controller/releases/download/v0.2.1/PCRemote-0.2.1-win-x64-setup.exe)**

| Download | Choose this for |
| :--- | :--- |
| **[Windows installer](https://github.com/maamouribilel/windows-pc-mobile-controller/releases/download/v0.2.1/PCRemote-0.2.1-win-x64-setup.exe)** | Recommended: Start menu shortcuts and an uninstall entry |
| **[Portable ZIP](https://github.com/maamouribilel/windows-pc-mobile-controller/releases/download/v0.2.1/PCRemote-0.2.1-win-x64.zip)** | Extract the complete archive, then run `PCRemote-win-x64/PcController.App.exe` |
| **[SHA-256 checksums](https://github.com/maamouribilel/windows-pc-mobile-controller/releases/download/v0.2.1/SHA256SUMS.txt)** | Verify the installer or portable archive |

**[Release notes for v0.2.1](https://github.com/maamouribilel/windows-pc-mobile-controller/releases/tag/v0.2.1)** · **[Browse every published version](https://github.com/maamouribilel/windows-pc-mobile-controller/releases)**

On GitHub, open **Releases**, select the version you want, and expand **Assets**. Choose the installer or portable ZIP; GitHub's automatic **Source code** archives are for developers. See [download and version instructions](docs/releases.md).

Version 0.2.1 is a public preview. It targets Windows 10 1809 or later, x64; Windows 11 is the tested desktop OS. The app and installer are unsigned, and the remaining manual checks are documented in the [release checklist](docs/release-checklist.md). After installation, follow the [iPhone setup guide](docs/setup.md).

**Security update:** v0.2.1 isolates paired users from unpaired HTTP request floods, reserves capacity for power cancellation, prevents late media settings changes, and fixes strict TLS certificate compatibility while retaining the trusted root. Users of v0.2.0 should update.

## Live demo

**[Install PC Remote, then connect on your own network →](#download)**

This application is hosted by your Windows PC on a trusted Private LAN. There is **no public hosted demo URL** in this project. Start the companion, open **the HTTPS address it displays** in iPhone Safari, complete certificate setup, and approve the pairing request on the PC. Follow the [complete onboarding guide](docs/setup.md).

The [walkthrough below](#walkthrough) previews the real interface before installation. Public cloud hosting of the frontend alone would not provide access to the Windows input backend.

## A closer look

### Five tabs. The controls you need.

<table>
  <tr>
    <td align="center" width="33%"><strong>Touchpad</strong><br><sub>Move, click, scroll, and drag.</sub><br><br><img src="docs/assets/mobile-touchpad.png" alt="Actual PC Remote touchpad in a mobile browser viewport" width="270"></td>
    <td align="center" width="33%"><strong>Keyboard</strong><br><sub>Compose Unicode text and send it deliberately.</sub><br><br><img src="docs/assets/mobile-keyboard.png" alt="Actual PC Remote keyboard and shortcut controls" width="270"></td>
    <td align="center" width="33%"><strong>Navigation</strong><br><sub>Large arrows and familiar navigation keys.</sub><br><br><img src="docs/assets/mobile-navigation.png" alt="Actual PC Remote navigation tab" width="270"></td>
  </tr>
  <tr>
    <td align="center"><strong>Media</strong><br><sub>Playback profiles and Windows volume.</sub><br><br><img src="docs/assets/mobile-media.png" alt="Actual media tab with playback and volume controls" width="270"></td>
    <td align="center"><strong>PC</strong><br><sub>Configured apps and confirmed power actions.</sub><br><br><img src="docs/assets/mobile-pc.png" alt="Actual PC tab showing the fixture's test app and power controls" width="270"></td>
    <td align="center"><strong>One connection</strong><br><br>🔒 PC-approved pairing<br><br>📶 Local HTTPS + WSS<br><br>🖥️ Native Windows input<br><br>📱 Safari / Home Screen<br><br>🛟 Visible release control</td>
  </tr>
</table>

<details>
<summary><strong>See the remote in a desktop browser</strong></summary>

<br>
<img src="docs/assets/desktop-remote.png" alt="Actual browser remote at a desktop viewport" width="1000">

</details>

Screenshots show the actual WPF companion and browser app, not generated mockups. Browser captures use the project's isolated loopback fixture with recording input/power adapters; the launcher entry is a test app. Mobile images are browser viewport captures, not photographs of a physical iPhone. See [capture details](docs/assets/README.md).

## Features

| | What you can do |
| :--- | :--- |
| 🖱️ **Touchpad** | One-finger pointer movement, tap to click, two-finger scrolling, right/double click, and explicit start/end dragging. Adjust sensitivity and natural scrolling on Windows. |
| ⌨️ **Keyboard** | Send composed text, including accents and emoji. Use held modifiers, common shortcuts, and **Release input**. |
| 🧭 **Navigation** | Use arrows, Enter, Home, End, and Page keys in the application focused on your PC. |
| 🎬 **Media profiles** | Choose General, YouTube, VLC, or Stremio. Controls follow each profile's supported shortcuts; Stremio includes **Next episode**. |
| 🔊 **Windows volume** | Set the current Windows output to an exact 0–100 percentage and read back its actual level. |
| 🚀 **App launcher** | Open or bring forward PC-configured executables. Manage up to 24 entries in the Windows companion; the phone sends configured IDs. |
| ⏻ **Power controls** | Request Sleep, Restart, or Shut down after confirmation and a cancellable 30-second countdown. Phone requests cancel on controller disconnect. |
| 🔐 **Approved devices** | Scan a short-lived pairing QR, approve on the PC, remember a browser, revoke devices, and explicitly take over another controller. |
| 🛟 **Input cleanup** | Release held input when switching tabs, pausing, disconnecting, or suspending the phone page. A watchdog clears interrupted holds. |
| 🖥️ **Tray companion** | Close the window to keep the server running. Open, pause, resume, or exit from the tray; optionally start at sign-in. |

<details>
<summary><strong>Compatibility and current limits</strong></summary>

- Targets **Windows 10 1809 or later, x64**; Windows 11 is the tested desktop OS.
- Safari and Home Screen operation have prior physical iPhone 15 Pro evidence. Other devices and AZERTY layouts remain unverified.
- Requires a trusted **Private IPv4 LAN** and a signed-in interactive Windows session. Elevated applications, the lock screen, and secure desktop are unsupported.
- Watch the PC screen: screen streaming, file/clipboard transfer, internet routing, Wake-on-LAN, and simultaneous controllers are not implemented.
- Media shortcuts require the matching player, its focus, and appropriate shortcut settings. Opening an app does not select a video or begin playback.
- iOS may suspend a backgrounded page; reopening reconnects without replaying stale input. Numerical input latency has not been measured.
- The app and installer are unsigned. Clean-account installation, reboot, manual uninstall, and physical execution of the newest power controls still require separate checks.

Read the [full limitations](docs/limitations.md) and [release checklist](docs/release-checklist.md).

</details>

## Tech stack

| Layer | Implementation |
| :--- | :--- |
| **Phone frontend** | Vanilla TypeScript 7.0.2, HTML, CSS, Vite 8.3.4, local SVG icons, Home Screen web manifest |
| **Windows desktop** | C# / .NET 10, WPF, Windows Forms notification-tray integration |
| **Backend** | Portable `PcController.Host`, ASP.NET Core / Kestrel, HTTPS endpoints, authenticated WebSockets |
| **Native controls** | Windows SendInput, Core Audio volume, configured executable launching, Windows power APIs |
| **Storage / database** | Local DPAPI-protected certificate/device files and JSON preferences; no database service |
| **Authentication** | PC-approved single-use pairing, an HttpOnly secure device cookie, one active controller |
| **Hosting** | Your Windows PC on TCP 8443, bound to loopback and selected Private IPv4 interfaces |
| **Testing** | Portable .NET fixture runner, Windows regressions/native checks, Playwright 1.64.0 (Edge on Windows, Chromium elsewhere) |
| **Packaging** | Self-contained `win-x64` publish, Inno Setup 7.1.0 installer, portable ZIP, SHA-256 hashes |

## Getting started

### Prerequisites

- An x64 Windows development machine with PowerShell and Git.
- Internet access for the initial toolchain/dependency downloads.
- For phone control: an iPhone with Safari and a trusted network marked **Private** on Windows.

The scripts prefer a verified portable toolchain in `.tools/`: **.NET SDK 10.0.401**, **Node.js 24.21.0**, and **npm 11.19.0**. Packaged releases bundle .NET runtime 10.0.12 and do not need development tools.

### Install, build, and run

```powershell
git clone https://github.com/maamouribilel/windows-pc-mobile-controller.git
Set-Location windows-pc-mobile-controller

./scripts/install-tools.ps1
./scripts/build.ps1 -Configuration Release
./scripts/run.ps1 -Configuration Release
```

`build.ps1` runs `npm ci`, TypeScript checking, a production Vite build, and a .NET solution build. It copies the built web app into the companion's `wwwroot/`. `run.ps1` selects the portable runtime when available.

### Environment configuration

**No `.env` file, API key, cloud credential, or database connection string is required.** Certificates, device authorization, and preferences are created locally. Private keys and device credentials use Windows user-bound DPAPI under `%LOCALAPPDATA%\PcController\certificates`, outside this repository.

Use the live address displayed by the companion on port **8443**. The optional firewall helper requests administrator access for a program-specific rule limited to **Private profiles / LocalSubnet**. Run the companion itself as a normal user.

<details>
<summary><strong>Pair your iPhone in four steps</strong></summary>

1. In Windows **Certificate & network setup**, export the public iPhone certificate profile. Compare its fingerprint with the PC before trusting it.
2. Install the profile on the iPhone and enable full trust under **Settings → General → About → Certificate Trust Settings**. Follow [certificate setup and removal](docs/certificates.md).
3. Create a pairing link, scan the QR in Camera, open Safari, name the device, and request approval. Approve on the PC. The link expires after two minutes and is single use.
4. Optionally use Safari **Share → Add to Home Screen**. The Home Screen browser may need separate approval.

Remembered authentication expires after 30 days and can be revoked immediately on Windows. Address changes preserve root/device records but require opening the new address.

</details>

### Everyday development

```powershell
./scripts/stop.ps1 -Configuration Release
./scripts/build.ps1 -Configuration Release
./scripts/run.ps1 -Configuration Release
```

Exit any installed copy through its tray menu before starting a development companion or isolated tests. Both use port 8443 and enforce one instance per Windows user.

## Walkthrough

<div align="center">
<img src="docs/assets/walkthrough.gif" alt="Actual PC Remote journey: touchpad, sending text, navigation, media profiles, and PC controls" width="390">
</div>

The recording follows the real browser interface through a secure connection, touchpad gestures, composed text, navigation, media selection, and the PC tab. Its isolated fixture records commands without injecting input into another app or executing power actions. [Capture notes](docs/assets/README.md) describe the environment.

## Project structure

```text
windows-pc-mobile-controller/
├── src/
│   ├── PcController.App/       # WPF companion and Windows production composition
│   ├── PcController.Desktop/   # Avalonia development shell (native adapters pending)
│   ├── PcController.Host/      # Portable HTTPS/WSS, endpoints, lifecycle, preferences
│   ├── PcController.Core/      # Protocol, pairing, ownership, media/power logic
│   ├── PcController.Windows/   # Native adapters, DPAPI/ACLs, key storage, discovery
│   └── remote/                # TypeScript browser UI and shipped branding
├── tests/
│   ├── PcController.PortableTests/ # Portable protocol/security/recording fixtures
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

The browser sends bounded, allowlisted commands over WSS. The companion authenticates devices, checks session ownership, and dispatches commands through the Windows adapters. Launcher requests carry configured app IDs; executable paths stay on the PC. Read the [protocol and security design](docs/protocol.md).

### Shared backend development on macOS/Linux

Phases 1–2 provide the portable backend, Avalonia desktop shell, and platform-aware browser. The published companion remains Windows-only. Normal Mac/Linux shell startup shows setup-required and starts no listener until secure storage and native adapters are implemented. See [desktop setup and fixture workflow](docs/desktop-shell.md). Use the same pinned SDK/Node/npm versions with native tool archives on PATH or in `.tools/`, plus Python 3.9+ (3.13+ for strict TLS):

```sh
python3 scripts/portable.py build
python3 scripts/portable.py test
python3 scripts/portable.py desktop-test
python3 scripts/portable.py browser-capabilities
python3 scripts/portable.py desktop
python3 scripts/portable.py tls
python3 scripts/portable.py audit
```

PowerShell 7 `build.ps1`/`test.ps1` select portable projects automatically on macOS/Linux; pass `-Portable` on Windows. `install-tools.ps1` remains a Windows archive installer. See [portable architecture, adapters and fixture rules](docs/portable-host.md) and [dated Phase 1 evidence](MULTIPLATFORM_PLAN.md#execution-log).

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

See the [initial publication validation results](docs/publication-checks.md) for build, browser, transport, desktop, packaging, documentation, and secret-scan evidence.

The [v0.2.1 release evidence](docs/release-evidence.md#version-021--security-hardening-2026-10-12) records the security fixes and current validation. GitHub's **Security and regression checks** workflow defines portable build/security/TLS checks on Windows, macOS and Linux, plus Windows regressions and browser tests. Configuring those jobs does not prove they have run.

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

Native tests abort on lost target focus. The browser fixture uses an explicit loopback-only TLS bypass for UI checks; this does not establish iPhone certificate trust. .NET transport tests validate the installation root, chain, server EKU, and hostname/IP SAN. Production has no plaintext or TLS-bypass mode.

</details>

## Contributing and releasing

Coding agents must read [AGENTS.md](AGENTS.md); Claude also reads [CLAUDE.md](CLAUDE.md). Every new version must update the README, both agent files, download documentation, validation evidence, and affected `.github/` files before publishing matching source, tag, and release assets. Follow the [release publishing procedure](docs/releases.md#publishing-the-next-version) and [acceptance checklist](docs/release-checklist.md).

Run `./scripts/check-release-docs.ps1` before packaging. The GitHub **Release documentation** check also catches version/link mismatches and missing release documentation updates. Runtime checks, secret scans, and public asset verification remain part of the release procedure.

Read the [security policy](.github/SECURITY.md) for private vulnerability reporting and trust boundaries. Approve only your own trusted browser: keyboard control grants broad access to your signed-in Windows session. Keep the app on a trusted Private LAN and verify certificate trust.

## Build and deploy

Deploy the companion to the Windows PC you want to control. Its web app is bundled with it rather than hosted separately in the cloud.

```powershell
./scripts/stop.ps1 -Configuration Release
./scripts/package.ps1 -Version 0.2.1
Get-Content ./artifacts/latest-release.txt
```

Packaging obtains the verified local Inno Setup compiler if needed, rebuilds Release, and publishes a self-contained x64 app into a new timestamped directory:

| Output | Use |
| :--- | :--- |
| `PCRemote-0.2.1-win-x64-setup.exe` | Per-user installation, Start menu shortcut, uninstall entry |
| `PCRemote-0.2.1-win-x64.zip` | Portable distribution; keep the executable and adjacent files together |
| `PCRemote-win-x64/` | App, bundled runtime, web assets, documentation, license notices |
| `SHA256SUMS.txt` | Installer and archive hashes |

The installer targets `%LOCALAPPDATA%\Programs\PCRemote`. Review the [release checklist](docs/release-checklist.md) before distributing binaries. Pinned tools and lockfiles support repeatable inputs; byte-identical installer output is not claimed.

Generated releases, dependencies, SDKs, environment files, certificate exports, credentials, logs, and temporary files are excluded from Git. **Never distribute `.protected` files or private keys.** Uninstall retains local data for reinstall; permanent retirement also requires removing certificate trust. See [setup](docs/setup.md), [troubleshooting](docs/troubleshooting.md), and [third-party notices](docs/third-party.md).

## License

PC Remote source is available under the **[MIT License](LICENSE)**. Dependencies retain their own licenses; packaging includes the .NET and QRCoder notices. See [third-party components and build-tool terms](docs/third-party.md), including Inno Setup's separate compiler terms.

---

<div align="center">

**Control stays on your PC. Convenience stays in your hand.**

[Download](#download) · [All versions](https://github.com/maamouribilel/windows-pc-mobile-controller/releases) · [Setup](docs/setup.md) · [Protocol](docs/protocol.md) · [Limitations](docs/limitations.md)

</div>
