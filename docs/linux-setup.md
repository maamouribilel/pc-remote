# Ubuntu companion — experimental preview

Since version **0.3.0**, releases publish an **experimental** Ubuntu 24.04 x64 archive; Windows remains the primary platform. Linux has source adapters and production composition; **Phase 4 remains in progress** until real Ubuntu 24.04 x64/GNOME X11 and Wayland text/input/cleanup acceptance. The archive is published for testers with those gaps stated.

## Install the preview

1. On Ubuntu 24.04 x64 with GNOME, install the runtime packages from [Phase 5 feature/archive notes](#phase-5-featurearchive-candidate) (`libglib2.0-0t64 libsecret-1-0 libxcb1 libxcb-xtest0 libxkbcommon0 libxkbcommon-x11-0 libpulse0 libx11-6 libice6 libsm6 libfontconfig1 libgl1` plus the GNOME keyring, portal and NetworkManager services that a standard Ubuntu desktop provides).
2. Download `PCRemote-0.4.1-linux-x64.tar.gz` and `SHA256SUMS.txt` from the [v0.4.1 release](releases.md) and compare `sha256sum PCRemote-0.4.1-linux-x64.tar.gz`.
3. Extract with `tar -xzf PCRemote-0.4.1-linux-x64.tar.gz` into a stable folder you own and run `./PCRemote-linux-x64/PcController.Desktop` as your desktop user, never with `sudo`.
4. It starts paused with loopback HTTPS. On Wayland, request Remote Desktop consent; enable GTK Unicode text only for a verified GTK target. Then approve your trusted network for the session and pair the phone.

This archive has no distribution signature; verify the checksum. It was built and its metadata startup checked on a hosted Ubuntu 24.04 x64 runner, not on a GNOME desktop. Do not treat a build, portal dialog, XWayland shell, private-bus fixture or Xvfb check as that acceptance. [Dated evidence and support matrix](../MULTIPLATFORM_PLAN.md#execution-log) separate each backend.

## Baseline and prerequisites

Candidate baseline: **Ubuntu 24.04 LTS x64, GNOME 46**, local signed-in session, NetworkManager, logind, GNOME ScreenSaver interface and password-encrypted persistent GNOME **login** keyring. Ubuntu lists GNOME 46 in its [24.04 release notes](https://documentation.ubuntu.com/release-notes/24.04/); 24.04 standard security maintenance continues to 2029 in [Ubuntu's release cycle](https://ubuntu.com/about/release-cycle). Avalonia **12.1.4** remains pinned, with Ubuntu 24.x x64/ARM64 Tier 2; SDK **10.0.402** (runtime 10.0.13, updated for the 2026-10-13 .NET security release), Node **24.21.0** and npm **11.19.0** are pinned. The production factory explicitly limits startup to Ubuntu 24.04 x64/GNOME; ARM64 container execution uses test-only entry points and is development evidence, not production desktop support. KDE, other providers/distributions, headless/SSH sessions and privileged input daemons are unsupported.

Development packages (one-time administrator package installation, not privileged app operation):

```sh
sudo apt-get update
sudo apt-get install build-essential pkg-config libsecret-1-dev libxcb-xtest0-dev libxkbcommon-x11-dev libpulse-dev \
  gnome-keyring network-manager dbus-x11 libx11-6 libice6 libsm6 libfontconfig1 libgl1 xwayland \
  xdg-desktop-portal xdg-desktop-portal-gnome
# Isolated checks additionally need:
sudo apt-get install xvfb libgtk-3-dev x11-xkb-utils python3-gi
python3 scripts/portable.py build
python3 scripts/portable.py desktop
```

The authored bridge links system GLib/GIO, libsecret, XCB/XTEST and libxkbcommon-x11. Runtime packages normally arrive with the GNOME desktop; minimum corresponding libraries are `libglib2.0-0t64 libsecret-1-0 libxcb1 libxcb-xtest0 libxkbcommon0 libxkbcommon-x11-0 libpulse0`. Use Ubuntu-maintained security updates; system versions are not redistributed or separately pinned by the app. No new NuGet dependency was added. `cc`/`pkg-config` are build prerequisites on Linux. Build a Linux bridge on Linux; a Mac cross-build does not supply it. The original Phase 4 scope excluded feature/distribution work; the separately authorized Phase 5 additions are below.

Run as the desktop user, **never sudo/root**. The per-user 0600 advisory instance lock prevents concurrent production companions and is released by the kernel on termination. The accessible setup window remains the default control surface; GNOME may lack a tray host. Keep-running-on-close retains a minimized taskbar window, so an invisible tray cannot strand the companion. Exit/window close and SIGTERM/SIGINT/SIGHUP request graceful cleanup. SIGKILL/crashes cannot guarantee it. Native GNOME tray/window/signal acceptance is pending.

## Backend and permission

Detection requires explicit, consistent session variables and rechecks actual **logind session type**, Active/Remote/LockedHint and GNOME ScreenSaver state. Conflicting, missing or remote session information fails closed. A `DISPLAY` in Wayland selects the portal, never arbitrary-application XTEST control. Missing session-bus/logind/NetworkManager observation prevents production startup.

**X11:** connects as the user to the actual X11 server, checks XTEST/XKB, uses checked XCB requests and resolves logical symbols using the current XKB group. A disconnected server fails without Xlib's process-fatal I/O behavior. There is no uinput, root, ydotool daemon or silent fallback. X11 itself gives same-session clients broad access and lacks Wayland's per-client permission boundary.

**Wayland:** choose **Request Wayland Remote Desktop consent** under **Permissions & platform setup** (or **Request Remote Desktop consent** in the readiness banner) locally. The app creates a portal session, selects keyboard and pointer, starts it and requires both devices in the explicit consent response. Cancellation, denial, partial grants, unsupported providers or timeout leave input disabled. It requests neither screen capture nor clipboard nor persistent permission/restore tokens. The reviewed provider is `xdg-desktop-portal-gnome`; a successful response alone does not establish target receipt.

Transport is the documented **RemoteDesktop D-Bus `Notify*` API**, available from interface version 1 and still supported in version 2. It uses `NotifyPointerMotion`, evdev-coded buttons, discrete axes and `NotifyKeyboardKeysym`. [The portal documentation](https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.RemoteDesktop.html) also recommends EIS; this candidate deliberately does not call `ConnectToEIS`, mix transports, or substitute XWayland. Provider/transport errors stop input. D-Bus confirmation is submission, not proof that Mutter or a target accepted a keysym.

All startup/replacement, permission and lifecycle transitions remain paused. Review the target, **Release input**, and deliberately Resume. logind sleep/session changes, GNOME lock signals, NetworkManager properties/owner changes and .NET address/availability events disarm the native generation before ownership cancellation. Each new event rechecks the session and inspected Resume generation. Wake never automatically resumes. Storage loss stops listeners. Lock/suspend/roaming/provider-loss coverage must be verified on a real baseline desktop.

## Input and text limits

Pointer supports relative movement, left/right click/double-click, scroll, drag and held release. v1 scroll remains **120 units per detent**; fractions accumulate, with Core's existing natural-scroll conversion. X11 emits tracked wheel button pairs, Wayland emits discrete axes. GNOME accelerations, scaled/multiple displays and smooth-scroll expectations need separate acceptance. Navigation/modifiers and logical Ctrl+C/V/A/Z / Alt+Tab use Linux symbols; `Windows` wire spelling means Super. X11 releases retain original key codes across layout changes. Navigation downs/repeats emit complete key pairs while Core owns logical held state, so X11/Mutter cannot add independent auto-repeat. Modifiers remain physically tracked. Core retains repeat limits and the 1200 ms watchdog; no global auto-repeat configuration is changed.

**Text is disabled by default.** Enable **Unicode text for a verified GTK target only** locally. Text uses GNOME/GTK's [documented Ctrl+Shift+U Unicode hexadecimal entry](https://help.gnome.org/gnome-help/tips-specialchars.html). It preserves Unicode scalars, normalizes CRLF/Return and Tab, preflights the required symbol alphabet, and resolves shifted hex digits on French X11 layouts. It does not modify the clipboard, global keymap or selected input source. This is target-specific input: Qt/Electron/browser editors/games/password fields/custom IMEs may execute these chords or reject them. Verify a disposable GTK editor before enabling; universal Unicode insertion is **not** implemented. Capability `text` stays false until opt-in and real input readiness; the host rejects it independently of browser presentation. A text acknowledgement reports submission; do not automatically replay it.

The tracker conservatively retains possible down events on transport failure, retries all held categories, and survives listener replacement. Failed cleanup blocks Resume and graceful Exit. An interrupted GTK compose sequence stays blocked instead of sending Escape into an unrelated new target. Cancel composition **locally in the original target**, use Release input, then **Recover interrupted GTK text…** and explicitly confirm recovery. This is local recovery, not proof of automatic cleanup.

A closed/revoked portal or dead X11 connection may prevent even ups. The app never sends old tracked ups into a newly granted backend or silently clears tracking. Inspect/release all keys/buttons and cancel composition locally; only then use **Recover ended input session…**. That confirmation retires unreachable records only when the original transport is ended, remains paused and invalidates network approval. Wayland requires fresh consent; X11 requires companion restart. Do not count this recovery as successful automatic cleanup. Real GNOME revocation/virtual-device-disposal behavior remains a mandatory acceptance gap.

## Keyring, files and trust

Data lives under `~/.local/share/pc-remote/`, 0700 directories / 0600 files, umask 077. A random 256-bit installation key resides in **GNOME Keyring's persistent login collection**, via libsecret's Secret Service session. The bridge verifies the service process is the same user's `/usr/bin/gnome-keyring-daemon`, the exact login collection is unlocked, and the restricted, non-symlink `login.keyring` has the reviewed encrypted binary format. Missing/locked/provider-denied/session/default/blank-password plaintext keyrings fail. Unlock your own keyring through local GNOME tools; the app does not prompt-unlock or create a keyring. Alternate encrypted formats/providers need separate review, not a permissive fallback.

Secret Service **does not promise DPAPI-equivalent security**. GNOME's [collection implementation](https://github.com/GNOME/gnome-keyring/blob/master/pkcs11/secret-store/gkm-secret-collection.c) writes textual plaintext when the master password is empty; its [binary format](https://github.com/GNOME/gnome-keyring/blob/master/pkcs11/secret-store/gkm-secret-binary.c) uses legacy AES-128-CBC with password-derived keys and a modest iteration count. The format check rejects plaintext but does not strengthen that provider KDF. An unlocked login keyring can be accessed by same-user processes; offline protection depends on the password and disk security. Same-user compromise/admin access is outside this boundary. No hardware binding, per-application ACL equivalence or cryptographic network identity is claimed.

Certificate/device envelopes independently use AES-256-GCM, random 96-bit nonce, 128-bit tag and relative-path authenticated data (`PCL1` format). Private atomic writes flush before rename; symlink paths/subdirectories are refused. Loaded clear keys/PFX/device buffers are zeroed. The path-derived Secret Service attribute prevents accidental cross-installation reuse; it is not secret metadata or a service ACL. Existing ciphertext/public root without its key and missing installation roots fail rather than resetting trust. Restore matching files/keyring item. Deliberate retirement requires stopping the app and removing this exact root from every trusting device before deleting matching data/keyring item.

The shared certificate issuer preserves roots and remembered devices across restart, changed SANs and leaf renewal; Linux key imports are ephemeral. Strict chain/name verification and AKI/SKI stay mandatory. The shell exports only public root/profile material. Pair on loopback for development; LAN requires explicit interface approval, exact listener SANs and local device approval. NetworkManager association/route/device/configuration and address events invalidate trust even at the same IP; unobservable Wi-Fi is omitted. Same-subnet roaming/cable changes and real phone Safari/Home Screen HTTPS/WSS acceptance remain pending. Follow [certificate trust and fingerprint verification](certificates.md); never bypass a warning.

## Independent checks and completion gate

```sh
python3 scripts/portable.py test             # shared pairing/HTTP budgets/ownership/security/WSS
python3 scripts/portable.py desktop-test
python3 scripts/portable.py linux-test       # adapter faults; no native input
python3 scripts/portable.py linux-x11        # private Xvfb + disposable GTK, US/French; not GNOME
python3 scripts/portable.py linux-portal     # private fake portal bus; no compositor/consent/input
python3 scripts/portable.py linux-storage    # real disposable encrypted keyring/files + trusted loopback HTTPS/WSS
python3 scripts/portable.py browser
python3 scripts/portable.py browser-capabilities
python3 scripts/portable.py audit
# Python 3.13+: strict chain/name plus wrong-host/untrusted-root negatives
python3.13 scripts/portable.py tls
python3.13 scripts/portable.py linux-storage
```

The Linux script creates a **separate bus, HOME, keyring/runtime directory and Xvfb display**, never the user's bus/display/keyring. GTK checks verify focus and pointer bounds before every producing event; focus loss aborts input but permits tracked ups. The test-only native seam exists only in the test runner's separately compiled bridge; production does not export it. Clipboard tests operate only on the disposable display. X11 receipt, Core ownership interruption, watchdog and focus-loss checks run with US/French layouts on that private display. The portal fixture tests wire arguments/denial/partial grants/closure; it cannot prove Wayland Unicode or cleanup. Linux storage checks also run a fresh process, persist revoke/reconnect, test locked keyring, and use real protected PFX data for trusted HTTPS/WSS. With Python 3.13, they add strict TLS positives/negatives while the loopback host is up. Run HTTP suites sequentially on 8443.

Before Phase 4 is accepted, on **real Ubuntu x64/GNOME X11 and separately GNOME Wayland**, create disposable native GTK targets and record actual movement/click/scroll/drag, Unicode accents/non-BMP/Return/Tab and layout changes, Ctrl shortcuts and Alt+Tab between two disposable apps. Abort on target-focus loss (Wayland needs a dedicated isolated desktop/VM or supervised target guard, not XWayland focus introspection). Verify held repeats, interrupted keys/drag/text, denial/revocation/session closure, provider/display loss, pause/revoke/takeover/watchdog, real lock/suspend/resume, restart/remembered reconnect/root retention, tray/window fallback, roaming and strict phone HTTPS/WSS trust. Native lock/suspend tests require separate authorization/disposable machines. This candidate does not ship a safe general-purpose Wayland input smoke command; supervised isolated desktop acceptance remains required.

CI defines Ubuntu 24.04 x64 builds, faults, isolated Xvfb/GTK, private portal protocol and disposable keyring/TLS checks. It does not provide an interactive GNOME Wayland compositor or prove native/phone/LAN acceptance. Observe actual CI runs separately from configuring jobs. Phase 5 source features/provisional packaging are separately authorized; they do not close Phase 4 real GNOME gates or authorize publication.


## Phase 5 feature/archive candidate

[Release readiness](release-readiness.md) gives the exact feature/support matrix and native manual steps. Audio needs `libpulse0` plus the user's PulseAudio or PipeWire-Pulse server; development also needs `libpulse-dev`. General playback uses a unique eligible MPRIS player (prefer one playing); ambiguity/missing permission fails. VLC/YouTube/Stremio use documented focused shortcuts through the selected native input backend; Wayland cannot introspect global focus, so review the player locally. These are implementation/source-table checks until actual player acceptance is recorded.

Preferences selects restricted `.desktop` entries, display name and media profile; GIO launches only validated ELF applications. Remote requests still carry opaque configured IDs. Terminal, shell/interpreter/wrapper, D-Bus activation and arbitrary arguments are refused; only file/URI placeholders and VLC's `--started-from-file` flag are permitted. Many Snap/Flatpak/Chrome wrappers are therefore unsupported. No shell fallback is allowed. Activation/foreground success is not guaranteed by GIO or Wayland; select locally if prompted. Optional GNOME login start registers only this extracted `PcController.Desktop` path in a private own XDG autostart entry, refuses symlinks/conflicts and removes only the exact own registration. Moving/replacing/removing a candidate needs local autostart review. Sign-in starts paused, with fresh portal consent/LAN approval.

Power uses logind Suspend/Reboot/PowerOff only when `Can*` is `yes`, rechecked at execution with `interactive=false`. Challenge/no/na, inhibitors, inactive/session loss and failed cleanup refuse. The shared session-bound single-use confirmation/countdown/cancellation remains. logind is not an app-specific save-dialog protocol: save work first. Only recording/private-bus requests are used in automated tests; physical sleep/reboot/shutdown are unaccepted and separately authorized.

On a Linux x64 build host, `python3 scripts/package-linux.py` creates `artifacts/candidate-<version>-linux-x64-<timestamp>/PCRemote-<version>-linux-x64-candidate.tar.gz` (or, with `--final` from a clean commit, `artifacts/release-<version>-linux-x64-<timestamp>/PCRemote-<version>-linux-x64.tar.gz`), checksums and a source/file manifest. Extract the complete `PCRemote-linux-x64` directory to a stable local location and run `./PcController.Desktop` as the desktop user. .NET/ASP.NET and web/native bridge/managed notices are bundled; GNOME, GLib/GIO, libsecret, XCB/XTEST, xkbcommon, libpulse, fonts/graphics, keyring, logind, NetworkManager and portal services remain OS dependencies. Minimum runtime apt packages include `libglib2.0-0t64 libsecret-1-0 libxcb1 libxcb-xtest0 libxkbcommon0 libxkbcommon-x11-0 libpulse0 libx11-6 libice6 libsm6 libfontconfig1 libgl1`, plus the earlier reviewed desktop services. Keep Ubuntu security updates current. No DEB/AppImage installer or distribution signature/public download is claimed.

`python3 scripts/check-package.py --rid linux-x64` verifies inventory/checksums and self-contained read-only metadata startup on x64; it does not grant input, create trust or run power. Isolated tests additionally use a private PulseAudio null sink and private fake MPRIS/logind services. They touch neither user audio nor actual system power. Real Ubuntu x64/GNOME X11 and Wayland feature/cleanup/lock/roaming/tray/phone/installed acceptance stays pending, including the Phase 4 text/cleanup gate.
