# Troubleshooting

| Symptom | Recovery |
| --- | --- |
| The phone's browser cannot connect | Keep the companion running. Use its live HTTPS address, not localhost on the phone. Check same LAN, the trusted network (Private profile and firewall rule on Windows; an approved interface on macOS/Ubuntu), VPN/security software and guest/client isolation. No router forwarding is needed. |
| Only localhost in setup | Windows: no suitable Private IPv4 address exists; mark only your trusted network Private in Windows Settings and setup refreshes automatically. macOS/Ubuntu: approve your interface under Trusted network for this session. |
| Certificate warning | Stop onboarding. Check exact address, matching fingerprint, installed public profile and full trust. A changed root needs new trust. See CERTIFICATES.md; warning bypass is not acceptance. |
| Home Screen or another browser asks to pair | Each browser, including a Home Screen app, keeps its own cookies. Create a fresh pairing link and approve that browser. |
| Pairing expired/denied | Generate a fresh two-minute single-use link. Closing/denying/timing out approval never grants control. |
| Phone approval denied at once (macOS) | Before 0.3.6, any Space/full-screen, display or Secure Input change denied the pending request and replaced the host, and the approval dialog activating PC Remote from a full-screen app switched Spaces, so requests were denied as they appeared. Now only network changes, storage failures, a new link or Exit deny them. Create a fresh link and approve the dialog on the Mac. |
| Address drops back to localhost, phone shows "TypeError: Load failed" (macOS) | Before 0.3.6, every Wi-Fi record update (scan flags, auto-join timestamp, AirPlay/AWDL state, every few minutes) revoked the approved network. Now only a different network name, address, route, configuration, sleep or user switch revokes it. Re-approve the interface under Trusted network and reopen the phone page. |
| Accessibility alert after every macOS update | Each ad-hoc build has a new code identity, so the enabled entry no longer matches. Remove and re-add it once, or sign installed builds with `scripts/sign-macos-local.py` so one entry survives updates (see the macOS guide). The Keychain dialog after an update still appears once per build. |
| "Control is paused: …" with Accessibility granted | The text names the one reason macOS reports: leave the password field, prompt or lock screen (Secure Input), wake or unlock the Mac, sign in, switch back to this user or click a window on this desktop, then choose Resume. If it says the Mac is asleep while it is awake, quit and reopen PC Remote. The host, pairing link and pending approvals are kept. |
| Another controller owns input | Background the other browser or explicitly Take over. Only one controller injects input. |
| Paused/input failed | Check target is an unelevated app on the interactive desktop, then resume on PC. Cleanup failure keeps control paused. Do not elevate the companion. |
| Text delivery uncertain | Inspect PC before resending; the first send may have arrived before acknowledgement was lost. Nothing is automatically retried. Test in a new unsaved Notepad tab. |
| Wrong media action | Select matching profile, focus its built-in player and check default hotkeys/playlist. General uses Windows media/system volume. |
| Phone lock disconnects | The phone suspends the page. Held input releases; reopen to reconnect. No background control is promised. |
| IP changed | Open setup's new address and update Home Screen if needed. Root/devices persist and leaf SANs refresh; an old IP bookmark cannot discover the new IP. |
| Server fails to start | Exit other copies, check port 8443, certificate validity and user data access. Setup retries without falling back to HTTP. |
| Uninstall firewall prompt canceled | Files remain so removal can be retried. Approve its own rule removal; unrelated rules are preserved. User data/phone trust require separate retirement. |
| Unsigned-app prompt (SmartScreen, Gatekeeper) | All builds are unsigned. Compare SHA256SUMS.txt/source. No warning-free installation is claimed. |

Report the failed check, OS/device version and setup network notice. Do not share pairing links, cookies or `.protected` files. No typed text or credentials are logged.
