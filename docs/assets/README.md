# README media

These assets were captured from PC Remote 0.2.0 on 2026-10-12.

| Asset | Source |
| --- | --- |
| `desktop-controls.jpg` | Actual installed Windows WPF companion, showing preferences without the pairing QR, LAN address, device names, or executable paths |
| `mobile-*.png` | Actual bundled browser UI in Microsoft Edge, at a 390 × 844 CSS-pixel portrait viewport and 2× pixel density |
| `desktop-remote.png` | The same browser application at a 1280 × 860 CSS-pixel viewport and 2× pixel density |
| `walkthrough.gif` | Actual browser screenshots assembled into a looping GIF, at 390 × 844 pixels |

The browser media uses the existing `MouseBrowserFixture` in `tests/PcController.Tests`, with isolated temporary storage and real loopback HTTPS/WSS on port 8443. That fixture approves its own demo browser and uses recording input/power adapters. The displayed **Test player** launcher entry and **42%** browser volume come from that fixture, not a claimed physical player/audio session. No production browser cookies, credentials, certificate keys, or active pairing links are included.

The walkthrough exercises actual touch gestures, drag/release, Unicode text submission, navigation, VLC profile selection, fixture launcher selection, and dismissing a power confirmation. Commands were checked against the server's recording adapters, and the dismissed confirmation produced no power command. Its screens were not reconstructed in HTML or altered into mockups. Browser console/page errors and horizontal overflow were checked during capture.

Browser plugin was not available; captures used the project's pinned Playwright/Edge tooling. Only the isolated loopback browser context ignores the fixture certificate warning, matching the existing browser tests. This is UI evidence; production certificate validation and iPhone trust are unchanged. The separate .NET integration/LAN checks validate certificate chains and names.

For a fresh capture, close all companions, run the project's isolated fixture, pair an isolated browser with its temporary challenge, navigate the real tabs, and save screenshots without exposing the pairing form or connection details. Shut down the fixture and remove its unique temporary storage when finished. Do not publish private fixture metadata or reuse screenshots containing live pairing links.

## Version note

These real captures were made from version 0.2.0. Version 0.2.1 changes server request limits and authorization; the pictured controls and layout are unchanged. They are visual evidence, not proof of the newer security checks.
