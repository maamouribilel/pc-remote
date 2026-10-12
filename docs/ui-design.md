# Historical UI design — version 0.1.2

> Publication note: `192.0.2.10` is a documentation-only replacement for the original LAN address. Use the live address shown by your companion.

This record describes the approved version 0.1.2 design. Version 0.2.0 adds the PC tab and Windows volume controls; see [current controls](pc-features.md).

Implement the user's approved four-screen concept with the existing vanilla TypeScript and WPF applications. Keep all network/authentication/input logic and real Windows controls. Do not add playback metadata, streaming, sliders, cloud accounts or live typing.

## Visual specification

- Graphite background `#0B0F14`, charcoal surfaces `#171D25`, white `#F4F6F8`, slate secondary text `#8A95A5`, mint primary/selected controls `#5EE8BE`. Amber represents held input; red represents actionable errors.
- System fonts, 24px bold brand/page headings, 16px control text, 13–14px supporting text. Thin slate borders, subtle tactile surface shading, 16px control corners, generous 12px gaps. Consistent local SVG icons; no external font/icon requests.
- Compact brand, connection status and settings gear. Four fixed icon/label tabs, quiet selected mint icon/text, shared fixed Release input button. Connection RTT and setup instructions belong in connection settings.
- Touchpad: quick playback/volume, large dotted gesture surface, bottom gesture hint, left/right click row, double-click/drag row. No visible Touchpad heading in the accepted image.
- Keyboard: compose area, mint Send text, Enter/Backspace/Esc/Tab, four modifier keycaps, Ctrl+A/Ctrl+Z. Preserve Copy/Paste/Alt+Tab in expandable More shortcuts.
- Navigation: cross-shaped directional pad with central Enter, then Home/End/Page up/Page down.
- Media: profile selector, player focus hint, prominent combined Play / pause, adjacent seek controls, three volume controls, Fullscreen and Next episode. Profile-specific unsupported actions stay hidden. No invented playback-state display.
- Windows: same colors and tactile controls; two-column pairing QR/address and controller/paired-device dashboard. Certificate/network onboarding and pointer/media/startup preferences remain reachable in expandable sections. Actual device data replaces illustrative concept rows.

## Required responsive/state adaptations

- All control targets at least 44×44 CSS pixels. Keep safe areas, visual viewport sizing, landscape and normal content scrolling while the phone keyboard is open.
- Reduce the pad height on short screens; retain the fixed release/tab dock. At very short heights reduce chrome so the composer remains reachable.
- Opening connection settings releases held input; closing restores the selected tab. Unpaired/busy states expose their real pairing/takeover actions.
- Icon labels must survive dynamic drag/profile updates. Release controls, tab changes, page hiding, reconnect and PC pause continue resetting held state with no replay.
- Preserve quick keyboard access with a small keyboard button beside the touchpad's quick media controls. This is a functional addition to the picture required by the original release scope.

## References and QA

Approved phone concept: `assets/design/approved-mobile-concept.png`.

Coordinated Windows concept: `assets/design/approved-windows-concept.png`.

The Browser plugin/skill is not installed. Use the repository's pinned Edge/Playwright network integration fixture for phone UI QA. Its explicit loopback certificate bypass is UI evidence only. Production and .NET LAN tests continue validating TLS. Re-test the new layout on the user's actual iPhone; previous layout acceptance does not validate the redesign.

## Implementation and fidelity checks

Implemented the four phone panels and the coordinated WPF dashboard. Captured Edge screenshots at 363×874 (the concept's individual screen dimensions), 390×844, 390×700, 844×390, 1280×800 and a 390×320 reduced keyboard viewport. The 363px screenshots are `%TEMP%/pc-ui-{touchpad,keyboard,navigation,media}-363x874.png`; short-viewport evidence is `%TEMP%/pc-keyboard-compact.png`. These are actual rendered pages using real HTTPS/WSS and test-only recording input. Production uses SendInput.

`view_image` comparison against the approved phone concept:

| Area | Reference / render check | Fix or intentional adaptation |
| --- | --- | --- |
| Palette | Graphite background, quiet charcoal keycaps, mint primary/selected states | Matched colors and subtle tactile shading; previous all-green controls removed. |
| Header and copy | Compact PC Remote/status/gear; setup does not occupy main controls | Connection and RTT moved into a real dialog; matching headings and labels. |
| Touchpad | Large dotted surface, bottom hint, two click rows | Matches native screen balance; quick keyboard retained as required by release scope. |
| Keyboard | Composer, Send, 2×2 special keys, four modifiers, A/Z shortcuts | Reduced control weight to match reference. Extra C/V/Alt+Tab retained under More shortcuts. Live input feedback, focus outline and platform emoji remain real state. |
| Navigation | Cross pad, central Enter, two navigation rows | Corrected overly tall up/down keys so all four secondary keys fit at the native viewport. |
| Media | Profile/focus hint, combined Play / pause, adjacent seek, volume row, fullscreen/next | Stremio's unsupported Previous remains hidden. Real success/error feedback can add text beyond the idle concept; no invented playback metadata. |
| Dock and icons | Shared release and four icon/label tabs | Local SVGs remain present after drag/profile updates. Screenshot transitions fast-forwarded to capture settled selected states. |
| Responsive | Safe areas, scrolling and fixed tabs/release | Small/landscape screens scroll controls; 390×320 Send, status, release and tabs remain reachable. Actual iPhone keyboard and safe areas still need physical confirmation for this redesign. |

Above-the-fold copy review: matched brand, status, tab names, page headings, core button labels, focus hint and gesture hint. Intentional differences are actual live connection/device states, the required quick-keyboard icon, expandable additional shortcuts, transient feedback, and platform-native font/emoji rendering. No unrelated claims, marketing copy or new capabilities were added.

Verification so far: Release solution zero warnings/errors, strict TS/Vite build, 120 default checks, 155 TLS-validated pairing integration checks, 130 actual native mouse checks, 135 actual native keyboard checks, and six Edge browser tests. The browser flow verifies remembered approval, gestures/order, drag/cancel, settings-open release, shared modifier release, icon retention, text composition/one-time acknowledgement, media profile persistence/allowlists, tab release and offline recovery. Additional shortcut disclosure and final package/Windows checks continue below.

Fixed regression: adding the page's `data-panel` attribute initially matched the broad tab selector, so bubbled control clicks released input. Restricted tab registration to `button[data-panel]`; the complete real-WSS interaction flow passes after the fix.

Windows Computer Use initially returned an invalid cached installed-app identity for the development executable's window ("window id no longer belongs…", despite identical owner text). One re-selection retry failed. Selecting the installed executable's fresh window succeeded. The first occluded capture showed an unrelated surface; activating the returned PC Remote window produced the correct native dashboard. Never use the unrelated capture as UI evidence.

Native screenshot comparison found white primary-button text inherited from an implicit TextBlock style, double-applied TextBox padding, and a bright default scrollbar. Removed the conflicting inherited text defaults, corrected padding, added a dark scrollbar, and reduced excess pairing/status height. Preferences now open through the header's Settings button, leaving the initial dashboard/certificate row visible as in the concept. Added an explicit accessible Settings button name. Native system window controls are retained, with a dark title bar on Windows 11.

One CDP touch attempt missed its synthetic movement event; an instrumented rerun passed. The browser harness now waits for one rendering frame between touch samples to exercise the production per-frame batching reliably. All six browser tests, including the additional shortcut disclosure, pass with this harness adjustment. Production gesture code was not changed for this sampling issue.

At the end of independent testing physical redesign acceptance was pending; the final user follow-up below supplies fresh actual-device evidence. Earlier iPhone results remain separately recorded.

## Final installed verification

The accepted design was faithfully verified against rendered implementations at the native 363×874 phone viewport and the actual installed Windows dashboard. Palette, typography, icons, control spacing, container balance, visible copy and responsive behavior were compared with `view_image`; no material fixable visual mismatch remains. The additional quick-keyboard control/shortcut disclosure, live feedback/device expiry and native Windows chrome/system fonts are deliberate functional/platform adaptations listed above.

Final native screenshot: `%TEMP%/pc-ui-windows-final.jpg` (1006×843), captured through Windows Computer Use and retained for the requested design handoff. The QR in that snapshot is expired; use the app's live pairing action. Windows Settings opens/collapses preferences, and its four-item media dropdown uses the same dark palette. Fixed expander-header foreground/accessibility and footer contrast before this final capture.

Original sources were preserved in `%TEMP%/pc-remote-ui-backup-20261012-160211`; previous release directories remain available.

Final package: `artifacts/release-0.1.2-20261012-164259/`, containing installer, ZIP, self-contained app and hashes. Current-account installation succeeded (exit 0); log `%TEMP%/pc-remote-0.1.2-ui-approved-install.log`. Final installed desktop tests: 134 pass; verified Private-LAN transport: 135 pass. Evidence logs: `%TEMP%/pc-ui-installed-desktop-final.log` and `%TEMP%/pc-ui-installed-lan-final.log`. All 636 published files match installed files, both artifact hashes validate, no private certificate/profile/credential files are packaged, and the pre-update public CA hash is unchanged. App version 0.1.2.0 runs as the normal user at `https://192.0.2.10:8443/`.

The user received exact asynchronous instructions to refresh both Safari/Home Screen, rotate, open the actual keyboard and reach Send/status/release/tabs, then verify real mouse/text/Shift/dialog/tab release and Stremio actions. On 2026-10-12 they confirmed “Both Safari and Home Screen work” and “All controls and release/reset work.” These are fresh physical iPhone 15 Pro results against the installed 0.1.2 redesign, including Notepad Unicode text sent once, gear-open modifier reset and tab/explicit drag release. Clean-account/VM, final physical revocation, reboot/sign-in and manual uninstall/removal gates are unchanged and remain unverified.
