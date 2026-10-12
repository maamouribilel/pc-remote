# Milestone 2 — mouse verification

Implemented real SendInput movement, left/right/double click, scrolling and drag; bounds, fractional sensitivity, natural/standard scroll settings, ordered frame aggregation, backpressure, cancellation and held-button cleanup. Keyboard and media remain unimplemented. Final four-tab usability is Milestone 5.

Release build passes with zero warnings/errors. `scripts/test.ps1 -MouseSmoke` passes 70 checks. Its disposable WPF window owns the foreground and cursor before each injection and receives actual Windows mouse events. GetAsyncKeyState confirms drag hold, explicit release and actual timer watchdog release. It closes the window and restores the cursor; no arbitrary focused app or typed text is used.

`scripts/test.ps1 -Pairing` passes 85 checks including real certificate-validated HTTPS/WSS and revocation while a button is held. The network fixture uses a recording input adapter. `scripts/test-browser-mouse.ps1` passes the complete pairing/reload/move/scroll/right/double/drag-order/cancellation/network-loss flow over actual WSS. This loopback-only test executable uses controlled approval and recording input; production has no auto-approval endpoint or flag. It supplements the separate real-input smoke test.

Edge screenshots at 390×700, 844×390 and 1280×800 show no horizontal overflow; gesture interactions pass without runtime errors. Browser plugin not available; Playwright/Edge used. Browser TLS validation is explicitly bypassed only in the UI fixture; certificate-validated .NET transport and trusted iPhone tests are separate evidence. Initial QA failed from stale touch coordinates after scrolling; reacquiring the pad position fixed the check. Enabling WPF in the test harness required explicit IO/HTTP usings; that failure was fixed before the smoke test.

Actual-device results (2026-10-11): user reports iPhone 15 Pro / iOS 27, all real PC mouse actions work and movement is smooth. Follow-up explicitly confirms cross-monitor dragging and cleanup after both phone lock and Wi-Fi loss. Milestone 2's physical acceptance passes. Numeric latency remains unmeasured. Safari pairing, remembered refresh and trusted Home Screen connection already passed.

Detailed physical check: use a disposable PC window. Move with one finger; tap, Double click, Right click and two-finger scroll; Start drag, move/reposition a finger, End drag. During drag, lock the phone or disable Wi-Fi before End drag; verify release after roughly two seconds and reset after reconnect. Also test Pause, revoke and app exit while dragging. Drag across both monitors.

Numeric responsiveness is unverified. Ping RTT is transport only. For measurement, record a slow-motion video containing finger and PC cursor, count frames between motion and cursor response, and divide by frame rate. Until then report only the user's subjective “smooth” result.

```powershell
./scripts/build.ps1 -Configuration Release
./scripts/run.ps1 -Configuration Release
```

Output: `src/PcController.App/bin/Release/net10.0-windows/PcController.App.exe`; bundle: `src/remote/dist/` and app `wwwroot/`. Development launch uses the ignored portable runtime. Self-contained build/installer remain Milestone 6. Close the companion before fixtures using port 8443. Browser fixture expires after five minutes and cleans temporary files.

Milestone 2 historical limits: Milestone 5 adds automatic address refresh. Normal privileges cannot inject into elevated apps, lock screen or secure desktop. No screen streaming, clipboard transfer or arbitrary commands. Final user result confirms multi-monitor drag and BOTH lock/Wi-Fi-loss cleanup; tray pause/release passes in Milestone 5. Numeric latency and final packaged physical revoke remain unverified. See release-evidence.md for current keyboard/media acceptance.
