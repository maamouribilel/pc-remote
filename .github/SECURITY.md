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
