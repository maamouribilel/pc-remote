# Milestone 1 verification and device checklist

Implemented: locally generated QR/manual fallback, two-minute single-use pairing, actual WPF approve/deny window, DPAPI device records, 30-day HttpOnly remembered authentication, device revocation, pause/resume, one controller, explicit takeover, validated authenticated WSS ping and backoff reconnect. Input is not enabled yet.

With the PC companion closed, run `./scripts/test.ps1 -Pairing`. The fixture starts a real Kestrel HTTPS/WSS server with a temporary CA and protected store, verifies its chain/EKU/hostname, and cleans up after disposal. Its controlled approval callback tests pending/approved states; it is only in the test executable. Production has no auto-approval flag or endpoint. Core tests use a controllable clock to exercise expiry without sleeping. They do not claim physical iPhone or SendInput evidence.

Initial results on 2026-10-11: Release build passes, zero warnings/errors; 64 protocol/certificate/pairing/ownership/real-transport checks pass. Live Private LAN regression adds real HTTPS/WSS certificate IP verification. UI QA uses installed Edge, actual server traffic and an explicit test-context TLS bypass because optional Windows root-store import fails to persist here; these UI results are not certificate evidence. Browser plugin not available. Physical iPhone Safari full-trust/no-warning diagnostics were separately confirmed by the user in Milestone 0.

Original physical checklist (approved pairing/refresh/Home Screen subsequently passed; final packaged revoke remains separate in release-evidence.md):

1. On the PC click Create new pairing link. Scan the QR using the iPhone Camera and open Safari. The phone should show Secure transport · unpaired.
2. Enter a name, tap Request PC approval. Before approving, it must show Waiting for PC approval. The PC must display the name and IP in an approval window.
3. Click Approve device on the PC. The phone should show Connected securely. Refresh Safari: it should reconnect without another approval.
4. Pause on the PC: the phone should show Paused on PC and reconnect after Resume. Revoke the device: its socket must close, and reconnect must return the pairing form.
5. Deny a fresh request; verify no connection is granted. Wait beyond two minutes before submitting another link; verify it expires.
6. For a second browser, generate a new QR and approve it. It should show Another controller is active. Only Take over control may replace the first.
7. Add to Home Screen, launch and pair that browser if needed. Confirm no certificate warning. Home Screen trust and lock/unlock are still actual-device checks, not inferred from Edge.

The PC currently needs a restart after a network/address change. Tray integration, actual mouse/keyboard/media and packaging remain later milestones. Do not record any input latency from the ping RTT.

Actual-device follow-up: user confirms pairing through the actual PC approval window, Safari refresh reconnect without another approval, and Home Screen connection without certificate warning. Pause/revoke/takeover and repeated lock/unlock remain separate physical checks.
