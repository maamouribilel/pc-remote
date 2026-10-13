# Security policy

## Supported version

Security fixes are provided in the latest published preview, currently **v0.2.1**. Update from older previews using the [installer or portable ZIP](https://github.com/maamouribilel/windows-pc-mobile-controller/releases/tag/v0.2.1). The app has no automatic updater. Preview status and outstanding physical/manual acceptance checks are documented in release notes and `docs/release-checklist.md`.

## Report an issue privately

Use [GitHub's private vulnerability report form](https://github.com/maamouribilel/windows-pc-mobile-controller/security/advisories/new) or the repository's **Security → Report a vulnerability** button. Include the affected version, a minimal reproduction, expected/observed behavior, and relevant operating system/browser details. Use test data; omit credentials, private keys, pairing links, cookies, personal paths, and network addresses. Keep exploitable details out of public issues until a fix is available.

## Trust boundaries

- Pair only your own trusted browser and approve only a request you initiated. An approved browser can control applications in your signed-in Windows session; keyboard access is broad access, even though individual API commands are allowlisted.
- Use a trusted Private LAN, the companion's displayed HTTPS address, certificate fingerprint verification, and the scoped Private/LocalSubnet firewall rule. Do not expose TCP 8443 to the internet or bypass certificate warnings.
- Device credentials and private certificate keys use Windows-user DPAPI and restricted local storage. This protects them from other normal Windows users; it cannot protect a Windows account already compromised by malware or an administrator.
- Revoke lost browsers from the PC and use tray **Pause control** or **Exit** to stop control. Remembered browser authorization lasts up to 30 days. Remove certificate trust when permanently retiring an installation.
- Releases are unsigned. Obtain assets from this repository's GitHub Releases and compare their SHA-256 hashes; source archives are not installed applications.

## Verification

`scripts/test.ps1 -Security` exercises real certificate-validated HTTPS against an isolated loopback server with recording input/power adapters. It checks unauthorized mutations, browser-origin restrictions, private-file isolation, request limits, safety-action capacity, and ownership changes during delayed media requests. Other suites cover pairing, WebSocket validation/replay/cleanup, lifecycle, and browser interaction.

`scripts/audit-dependencies.ps1` checks npm and NuGet feeds. GitHub's **Security and regression checks** workflow runs the automated suites and dependency audits on code pushes and pull requests. Release preparation also scans secrets and verifies bundled runtime advisories, package contents, licenses, and public asset hashes. Passing checks reduce known risks; they do not establish that the app has no bugs or replace physical-device/manual acceptance.

`scripts/test-certificate.ps1` uses Python 3.13+ with strict chain/name verification, including negative trust tests. Existing server leaves missing authority/subject key identifiers are renewed on upgrade while keeping the trusted root. No production certificate-verification bypass is introduced.

## Portable foundation (development)

The shared host now requires explicit platform services; production WPF startup still uses Windows DPAPI, restricted ACLs, real Windows input/actions and Private-network discovery. There is no production plaintext protection fallback. Shared certificate issuance is independent of OS storage/key loading, retaining Windows protected-file formats and leaf-only renewal.

`tests/PcController.PortableTests` contains loopback recording adapters and temporary test storage, with no normal startup path. `python3 scripts/portable.py test` checks pairing/ownership/transport/security/cleanup; `tls` requires Python 3.13+ and verifies strict chain/name acceptance and rejection. CI defines shared checks on Windows/macOS/Linux and retains Windows-specific regressions. Actual new platform results belong in `MULTIPLATFORM_PLAN.md`; job configuration and cross-compilation do not prove Windows DPAPI/native behavior or phone/LAN acceptance. Future Mac/Linux production stores and explicit network approval remain prerequisites.

Phase 2 adds an Avalonia development shell and capability-aware browser. Normal Mac/Linux startup has no host listener until reviewed platform secure storage is implemented; no production recording/plaintext fallback exists. Network approval is session-only and invalidated on detected connectivity/identity changes, with old listeners stopped. Platform-native network identity/notification coverage and LAN acceptance remain prerequisites for production startup. Backend capabilities reject unavailable actions independently of browser presentation. Session claims reauthorize after body reads; shutdown denies late pairing approvals; failed revocation retains pause. See `docs/desktop-shell.md` for fixture boundaries and native/manual gaps.
