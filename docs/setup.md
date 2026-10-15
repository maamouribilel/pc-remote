# Setup and everyday use

PC Remote runs a companion on the computer you control and serves a browser remote to your device. This guide describes the current **0.4.2 preview**. Windows is primary; macOS and Ubuntu are experimental.

| Computer | Start here |
| :--- | :--- |
| Windows 10 1809+ x64 (Windows 11 tested) | [Windows companion](#windows-companion) below |
| macOS 14+ · Apple Silicon or Intel | [macOS installation, Accessibility, Keychain and network approval](macos-setup.md) |
| Ubuntu 24.04 x64 · GNOME X11/Wayland | [Ubuntu dependencies, keyring, consent and text opt-in](linux-setup.md) |

The verified browser onboarding path is **iPhone Safari**, including the Home Screen app. Chromium/Edge has automated viewport coverage. Android, iPad and Firefox remain untested. [Compatibility](../README.md#compatibility) · [Limitations](limitations.md).

## Windows companion

Run the [Windows installer](releases.md) as your normal signed-in user. It installs to `%LOCALAPPDATA%\Programs\PCRemote`, adds Start menu shortcuts and an uninstall entry. For portable use, extract the complete ZIP and run `PCRemote-win-x64\PcController.App.exe`; keep all adjacent files. Packaged releases need no SDK, Node.js or separate .NET runtime.

<img src="assets/desktop-controls.jpg" alt="Windows companion preferences, captured from 0.2.0" width="900">

*Historical native capture from 0.2.0; the Windows dashboard is retained. The current browser captures and their provenance are in [media notes](assets/README.md).*

Use a trusted network marked **Private** in Windows Settings → Network & internet. **Configure Private LAN firewall** requests elevation for a program-specific TCP 8443 rule limited to Private profiles and LocalSubnet. The companion itself stays unelevated and does not change your network profile or router.

Expand **Certificate & network setup** for the public trust export, fingerprint, QR/manual pairing link and firewall setup. Open **Settings** for pointer sensitivity, natural scrolling, media preferences, launcher apps, volume, power and sign-in startup.

## Pair a browser

1. Start the companion and read its current HTTPS address. On macOS/Ubuntu, complete platform setup, approve the trusted interface and deliberately resume control. `localhost` is for the computer itself; use the approved LAN address from your other device.
2. Export the installation's **public** trust profile/root and transfer it to your device. Compare its SHA-256 fingerprint with the companion. Never transfer private keys or `.protected` files.
3. On iPhone, install the profile under **Settings → General → VPN & Device Management**, then enable full trust under **Settings → General → About → Certificate Trust Settings**. Follow [certificate setup](certificates.md); other platforms' import paths are unverified. Resolve any certificate warning before pairing.
4. Create a fresh pairing link, scan the QR and open it in your browser. Name the device, tap **Request PC approval**, and approve it on the computer. The link lasts two minutes and is single use.
5. Wait for **Connected securely**. The gear opens connection status, round-trip time and the connection test. On iPhone, optionally use Safari **Share → Add to Home Screen**, with **Open as Web App** if shown; approve that browser separately if asked.

Remembered authentication expires after 30 days. **Revoke** on the companion removes it immediately. One browser owns input; **Take over control** explicitly replaces the current controller.

## Touchpad

Use **Touch** for one-finger movement, tap to click, double tap, two-finger scrolling and three-finger cancellation. Dedicated controls provide right/double click. **Start drag** holds the left button; move using the pad, then choose **End drag** or **Release input**.

Adjust pointer sensitivity and natural scrolling in the companion. The software keyboard and landscape view can reduce usable height; scroll the controls normally.

## Air pointer

Air turns your phone into a relative pointer using its gyroscope. It was introduced in 0.4.0; 0.4.1 corrected its motion axes, gravity handling and filtering. Use 0.4.1 or later.

1. Open **Touchpad → Air**.
2. If **Enable motion** appears, tap it and allow motion access. On iOS, the request must come from that tap.
3. When the pad says **Point the phone at the screen**, rest your thumb on it and turn your wrist. The pad changes to **Pointing**.
4. Lift your thumb to stop. Re-grip and hold again when you want to move; pointing never restarts by itself.
5. Tap the pad to click. Use **Start drag**, hold to move, then **End drag** to release.

The button next to the Touch/Air switch opens **Air pointer settings**: speed **0.5–2.5×**, **Invert horizontal**, and **Invert vertical**. These preferences stay in this browser on the phone; companion sensitivity also applies. Raw motion readings stay on the phone; only rounded pointer deltas are sent.

If motion access was declined, reload the remote, select Air and tap **Enable motion** again, or choose **Use Touch**. A device without usable sensor samples shows **No motion sensor on this device** and offers Touch. Air needs the visible Touchpad tab and active control. Switching away, backgrounding, losing control or interrupted samples stops pointing; hold again after returning.

Air is relative movement, so lift to re-grip rather than expecting absolute aiming at a spot on the screen. Flat-grip directions were confirmed on an iPhone against the Mac companion. Other grips, measured precision/drift, the complete Safari/Home Screen permission/lifecycle checks and Android remain pending. [Detailed acceptance](../AIR_POINTER_PLAN.md).

## Keyboard and navigation

Focus a disposable text field on your computer. Type into **Keyboard**, then tap **Send text**; typing alone sends nothing. Each submission is limited to 256 characters. Accents and emoji are supported where the companion advertises text capability; Linux text requires explicit local GTK opt-in and has target limits.

Lit modifiers stay held until tapped again or released. **More shortcuts** includes copy/paste and app switching; paste uses the computer's existing clipboard, with no clipboard synchronization. **Navigation** has large arrows, Enter, Home/End and Page keys. Labels/shortcuts adapt to the companion.

## Media, apps and power

**Media** offers General, YouTube, VLC and Stremio profiles. Focus the matching player on the computer; shortcuts follow its defaults. Stremio's Next means next episode, and Previous is unavailable. Seek distances depend on the player.

The system volume slider controls the current output, separately from profile-specific volume buttons. In **PC**, configured apps open or request foreground activation, then switch the remote to their media profile. Focus may still need a local click.

Power actions require confirmation and a cancellable 30-second countdown. Availability differs by platform. For learning the controls, dismiss with **Keep PC running**; this schedules nothing. [Configure apps, volume and power](pc-features.md).

## Pause, reconnect and exit

**Release input** stays above the five tabs. Switching tabs, opening connection settings, pausing or disconnecting releases held input. Locking/backgrounding the phone suspends control; reopen it to reconnect without replaying commands.

Closing the companion window keeps it running. Use the Windows tray, macOS menu bar or supported Ubuntu tray/window controls to open, pause, resume or exit. Windows sign-in startup is opt-in; macOS/Ubuntu startup requires local OS approval and starts paused with fresh network approval (and Wayland consent).

If the computer's address changes, reopen the address shown by the companion; old bookmarks cannot discover it. The root and remembered devices persist. Mac/Ubuntu require fresh approval after a real network change, sleep or user switch. Version 0.4.2 ignores the Mac peer-to-peer Wi-Fi record churn that previously revoked that approval. On the developer's installed build, approval survived ten bursts over twelve minutes with a paired iPhone; the user reported continued control and reconnection after auto-lock. Longer sessions and other Macs remain unverified. [Recorded check](release-evidence.md#version-042-publication-and-public-verification-2026-10-15).

## Update or remove an installation

Download the current platform archive/installer from [Releases](releases.md). Keep a stable installation path. On macOS, follow [Keychain and Accessibility update recovery](macos-setup.md); existing roots/devices must be retained.

On Windows, uninstall through **Settings → Apps → PC Remote**. It exits the companion and removes its own startup entry, shortcuts, files and scoped firewall rule; rule removal may need elevation. Cancelling cleanup leaves the app installed for retry. Protected data under `%LOCALAPPDATA%\PcController` remains for reinstall. For permanent retirement, remove phone/computer certificate trust before deleting that data. Older developer firewall rules are intentionally separate.

The application/installer is unsigned. Windows warnings, clean-account/reboot/uninstall and platform-specific acceptance remain documented in [limitations](limitations.md) and [release readiness](release-readiness.md). For a short guided tour, use the [two-minute demo](demo.md).
