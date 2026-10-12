# Physical iPhone acceptance — Milestone 0

> Publication note: `192.0.2.10` is a documentation-only replacement for the original LAN address. Use the live address shown by your companion.
Status: **passed by actual user test**. User confirms iPhone 15 Pro / iOS 27 Safari and Home Screen full-trust/no-warning HTTPS/WSS. Initial Safari RTT was 11 ms. The checklist below preserves original feasibility steps. Desktop emulation alone does not pass this gate; RTT is not input latency. See release-evidence.md for current status.

Latest user-observed result (2026-10-11): actual iPhone Safari shows **Connected securely, 11 ms**. The PC is now Private, its scoped firewall rule is installed, and the Release app listens at `192.0.2.10:8443`. Remaining gate checks: confirm certificate full trust/no warning bypass and test the Home Screen page; record phone/iOS details and lock/unlock behavior.

## Prepare the PC

1. On this trusted home LAN, set Wi-Fi 3 to Private using Windows Settings or administrator PowerShell:

   ```powershell
   Set-NetConnectionProfile -InterfaceAlias "Wi-Fi 3" -NetworkCategory Private
   Set-Location "C:\path\to\windows-pc-mobile-controller"
   ./scripts/firewall.ps1 -Action Install
   ```

2. Close the PC Remote window and start it normally:

   ```powershell
   ./scripts/run.ps1 -Configuration Release
   ```

   Confirm the window says **Private LAN detected** and shows an IP address rather than localhost. Expected with the initially observed IP: `https://192.0.2.10:8443`. Use the current displayed address if different.

3. Click **Save iPhone certificate profile…**. Send that public file to yourself and open the attachment on the iPhone in Apple Mail. Review/install and enable full trust following [certificates.md](certificates.md). Never send `.protected` files.

## Test on the phone

1. Connect the iPhone to the same home LAN. A different Wi-Fi SSID is acceptable only if the router bridges the devices. Avoid guest Wi-Fi/client isolation. VPNs can affect reachability; record if enabled.
2. Open the displayed HTTPS address in Safari. It must load without a certificate warning after installing and trusting the matching root.
3. Confirm **Connected securely** and **HTTPS + WSS**. Tap **Test connection** five times, at least one second apart. Record five `Last round trip` values. These measure application ping/pong RTT, not cursor/input latency.
4. Add the page to the Home Screen from Safari Share → Add to Home Screen. Enable **Open as Web App** if that toggle is shown, then tap Add. Launch from its icon and repeat the connection test without a certificate warning. See [Apple's current instructions](https://support.apple.com/guide/iphone/open-as-web-app-iphea86e5236/ios).
5. Lock/unlock once and switch away/back once. Confirm a fresh successful connection. This is a connection prototype check; held-input release is tested in later milestones.
6. Send the results in this chat using the template below. A report of success establishes user-observed physical-device evidence, clearly identified as such.

```text
iPhone model:
iOS version:
PC displayed address:
Windows profile: Private / other
Private firewall rule installed: yes / no
Profile installation and fingerprint comparison:
Full root trust enabled:
Safari: loads without warning? Connected securely / HTTPS + WSS?
Five RTT readings (ms):
Home Screen: launches without warning? ping/pong works?
Lock/unlock and return from another app:
Any exact failure message:
```

## If it fails

- **No page / timeout:** keep the companion running, verify both devices' IP network, Private profile, TCP 8443 firewall rule, VPN routing and router Wi-Fi client/guest isolation. Test `https://IP:8443/health`; expected public diagnostic data are `ready: true`, `protocol: 1`, `stage: connection-check`, `inputEnabled: false`.
- **Certificate warning:** check the exact address, installed profile's unique CA name/fingerprint, full trust toggle and clocks on both devices. Restart after an IP change. Do not bypass the warning and report the gate as passed.
- **Page loads, WSS fails:** report the displayed message and Safari vs Home Screen difference. Origin must exactly match the HTTPS address. Do not try an HTTP URL.
- **Address is localhost:** network classification is Public/unknown, or there is no usable Private IPv4 interface. Resolve that and restart. The app intentionally does not expose a LAN listener on Public Wi-Fi.
- **Phone URL contains localhost or 127.0.0.1:** those addresses refer to the iPhone itself. Use the PC's displayed LAN IP, currently expected to be `https://192.0.2.10:8443`, after enabling the Private listener. Do not copy the desktop-only loopback URL to the phone.

Milestone 1 starts once the physical encrypted transport gate passes. If onboarding proves unworkable, document evidence and decide the transport tradeoff before proceeding.
