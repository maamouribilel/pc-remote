# Local transport certificates

The app generates a unique per-Windows-user installation CA (RSA 3072, SHA-256, five-year validity) and server leaf (RSA 2048, SHA-256, 90-day validity, server-auth EKU, SANs for localhost, IPv4 loopback and current Private IPv4 addresses). The CA signs only the app's leaf certificates. No public CA or internet access is required during normal operation.

Files under `%LOCALAPPDATA%\PcController\certificates\`:

- `root.protected` / `server.protected`: PFX bytes protected with user-bound Windows DPAPI. The directory ACL permits the signed-in user and SYSTEM. Plain PFX buffers are zeroed after use. Windows Schannel uses temporary user key containers; .NET removes these on certificate disposal. A compromised process running as the same Windows user can access these keys, as it can other user-protected data.
- `pc-remote-root.cer`: public root only.
- `pc-remote.mobileconfig`: unsigned iPhone configuration profile containing that same public root. No private key, device credential or pairing secret is exported.
- `addresses.txt`: non-sensitive SAN address list used to decide when to regenerate the leaf.

Trusting a private root allows your device to accept certificates it signs. Install only your own PC's public root, verify it against the PC's SHA-256 fingerprint, and remove it when no longer using this app. Do not import profiles from unknown PCs.

## iPhone onboarding

1. Click **Save iPhone certificate profile…** in the PC setup window. Transfer the exported public `.mobileconfig` to your iPhone, for example as an attachment you send yourself and open in Apple Mail. Sending it is a manual user action; the app does not send email or upload it.
2. Open the attachment, then open Settings → General → VPN & Device Management → downloaded PC Remote profile. Review its certificate details and compare the SHA-256 fingerprint with the PC window. Install the profile. It is unsigned because no external profile-signing service is used; this is distinct from TLS trust.
3. Open Settings → General → About → Certificate Trust Settings. Enable full trust for the **PC Remote Local CA** whose unique suffix matches your PC's profile.
4. In Safari, open the exact `https://IP:8443` address shown by the app. A security warning is a failed test; do not treat clicking through it as onboarding success.
5. Confirm **Connected securely**, **HTTPS + WSS**, and a measured round trip. Safari's application-level pong must arrive before the page shows success.
6. Use Safari Share → Add to Home Screen, enable **Open as Web App** if shown, and tap Add. Launch the saved page and repeat the test. See [Apple's Home Screen instructions](https://support.apple.com/guide/iphone/open-as-web-app-iphea86e5236/ios). Manifest/icons are bundled; this browser may need separate PC approval.

These steps follow [Apple's manual certificate trust instructions](https://support.apple.com/en-us/102390). The user confirms actual iPhone 15 Pro Safari and Home Screen full trust/connectivity without warning bypass. New installations still require this explicit trust flow. There is no insecure fallback.

## Address changes and renewal

The running app inspects network changes every two seconds and on Windows network/resume events. It releases input, replaces listeners and reissues a leaf if SANs changed, using the same protected root. Existing iPhone root trust can remain; open the new address and update the Home Screen shortcut if needed. DHCP reservation on your own router may stabilize the IP.

The leaf is also regenerated at startup or automatic inspection when fewer than 14 days of validity remain. If a root has fewer than 100 days left, certificate setup fails and requires deliberate renewal. Replacing the CA requires removing old iPhone/Windows trust and reinstalling the new public profile; it is not done silently.

There is no online revocation service for this local CA. Device access revocation in Milestone 1 is separate from transport trust. If its signing key is compromised, remove the CA from every trusting device and regenerate the installation before further use.

## Remove trust

- iPhone: Settings → General → VPN & Device Management → PC Remote profile → Remove Profile. Confirm its root disappears from Certificate Trust Settings.
- Windows, if you used the optional helper: `./scripts/trust-windows.ps1 -Action Remove`, as the same user. It targets this installation's thumbprint only; it never removes unrelated roots.
- Keep `.cer` available until Windows trust cleanup is done. Removing application files does not remove iPhone trust. Uninstall removes application-owned startup/firewall entries but retains protected user data for reinstall.
- Without the source helper, open `certmgr.msc` → Current User → Trusted Root Certification Authorities → Certificates. Compare the exact installation certificate thumbprint/fingerprint before removing only that root. The app never automatically installs Windows trust.
- After trust is removed and the app has exited, retiring the installation may include deleting `%LOCALAPPDATA%\PcController`. This resets all its paired devices/settings and CA, so a future install requires new iPhone trust. Do not delete unrelated user directories.

No Windows certificate store trust is automatically added by the companion. Verification used a temporary CurrentUser root entry, then removed it. Later re-imports on this host appeared successful in memory but were absent after reopening the store, including a native certutil attempt. The helper verifies persistence and reports failure. Earlier Edge UI runs passed with verification enabled; final UI-only QA used an explicit loopback test-context bypass, as documented in the README. .NET integration tests use a custom root store and verify the peer hostname without disabling verification. Windows root-store retention is an environmental issue; actual iPhone trust passes separately by user report.

## Windows TLS finding

The initial server used ephemeral imported private keys: its process listened, but the TLS handshake closed. Loading into a temporary user key container fixed the live handshake. This is consistent with [Microsoft's Schannel/SslStream troubleshooting documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/sslstream-troubleshooting). DPAPI remains the persistent file protection mechanism.
