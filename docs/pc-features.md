# Apps, volume and power

Current preview: **0.4.2**. Reload the remote or reopen the Home Screen app after updating. The browser adapts controls to the companion's capabilities; local permissions and service availability can disable individual actions. Windows is primary; macOS and Ubuntu are experimental. For Touch/Air, text and navigation, use [setup and everyday use](setup.md); for a guided tour, use [the demo](demo.md).

| Feature | Windows | macOS (experimental) | Ubuntu (experimental) |
| :--- | :--- | :--- | :--- |
| System volume | Core Audio current output | Writable Core Audio current output | PulseAudio/PipeWire-Pulse current sink |
| General media | System media keys | Native system media keys | Unambiguous controllable MPRIS player |
| Launcher configuration | Local `.exe` | Local `.app` | Restricted ELF-backed `.desktop` |
| Power | Sleep, restart, shut down | Sleep only | Actions logind permits without a prompt |
| Sign-in startup | Per-user opt-in | OS approval/signing eligibility | Local GNOME XDG opt-in |

The walkthrough below describes Windows controls; the final section explains Mac/Ubuntu differences. Implementation is separate from native acceptance: see [release readiness](release-readiness.md).

## App launcher

In **PC**, tap an app to open it or bring an existing instance forward. The app's configured media profile is selected and the phone opens Media. Stremio, VLC, Chrome and Edge are discovered when present. The Windows companion's **Settings → App launcher** lets you add a local `.exe`, choose its displayed name/media profile, or remove an entry. There are up to 24 apps. Removed entries stay removed after restart.

The phone can send only an opaque configured app ID. It cannot supply a path, arguments, script or shell command. Paths remain in the PC's local `launcher-settings.json`, alongside the protected device data. Missing apps are shown as unavailable. Windows can deny foreground activation; in that case the app flashes and the phone tells you to select it on the PC taskbar. Opening an app does not select a video or begin playback.

## Windows volume

The **Media → Windows volume** slider sets the current Windows output to an exact 0–100 percentage. It commits when you release the slider or use its keyboard controls, then displays the actual returned level. Moving it unmutes Windows. The current level/mute state is synchronized on authenticated heartbeats. App-specific volume buttons retain their existing profile behavior. No audio endpoint is shown when Windows has no available output. The companion offers the same slider in Settings. Output-device switching is not included.

## Power controls

In **PC**, choose **Sleep**, **Restart** or **Shut down**. A confirmation explains the action. Confirming starts a 30-second countdown; **Cancel power action** is available on the phone and in Windows Settings. Choosing an action or dismissing its confirmation does not schedule it. Confirmation tickets expire after one minute and are single use.

Keep the phone app open during its countdown. Phone requests cancel if their active controller disconnects, is paused/revoked/replaced, or the server shuts down. Requests started locally from Windows Settings use the same countdown and can be cancelled from an active paired phone. Restart/shutdown use Windows' normal unsaved-work handling, without forced termination. Account permissions or sleep support can prevent execution; check the message on the PC. Once the countdown finishes and Windows receives the request, cancellation of Windows prompts is local. Sleep disconnects the remote; wake the PC locally and reopen it to reconnect. Wake-on-LAN is not included.

## Validation

The core/HTTPS tests cover allowlists, current-session ownership, origin checking, integer volume bounds, configured-ID launching, confirmation expiry/replay, cancellation, and a real countdown delivered once to a recording adapter. Browser tests exercise launcher/profile selection, slider read-back, dismissed confirmation and cancelled shutdown, plus the existing mouse/keyboard/media/reconnect tests in five portrait/landscape/desktop viewports. Browser plugin is not available; the pinned Playwright/Edge workflow uses loopback-only TLS bypass for visual QA, while .NET HTTPS/WSS tests validate the fixture root and hostname. Power tests use a recording adapter and never sleep, restart or shut down the development PC.

Run with the companion closed:

```powershell
./scripts/test.ps1 -PcFeatures
./scripts/test-browser-mouse.ps1
```

Actual iPhone operation and actual sleep/restart/shutdown remain device checks. Existing clean-account, reboot and uninstall gates also remain separately tracked.

Native API references: [Core Audio volume](https://learn.microsoft.com/en-us/windows/win32/api/endpointvolume/nn-endpointvolume-iaudioendpointvolume), [foreground activation](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow), [sleep](https://learn.microsoft.com/en-us/windows/win32/api/powrprof/nf-powrprof-setsuspendstate), and [normal Windows shutdown/restart](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-exitwindowsex).


## macOS and Ubuntu behavior

Since version 0.3.0, releases publish the macOS/Ubuntu adapters as experimental archives. [The feature/support matrix and exact manual checks](release-readiness.md) describe implemented candidates separately from native acceptance. Mac uses writable Core Audio default outputs; Linux uses PulseAudio/PipeWire-Pulse via libpulse with actual read-back and channel-balance preservation. Missing/read-only outputs fail. General playback is native system media keys on Mac, unambiguous controllable MPRIS on Linux. Player shortcuts are resolved in the current layout; Mac VLC uses Command+Right/Left, Command+Up/Down, Command+Option+Down mute, Command+Option+Left/Right seek and Command+F fullscreen. Linux VLC retains N/P, Ctrl+Up/Down, M, Shift+Left/Right and F. YouTube/Stremio retain documented player chords; browser tab/player focus and installed defaults need manual acceptance. Mac rejects mismatched frontmost app bundles. Linux Wayland has no global focus introspection: explicitly focus the matching player locally.

The shared shell Preferences selects a local `.app` or restricted ELF-backed `.desktop`, display name/profile, and removal. Native LaunchServices/GIO request activation; compositor/OS denial is reported conservatively as requiring local selection. No remote paths, arguments or arbitrary shell are added. Linux shell/interpreter/wrapper/terminal/D-Bus-activated entries and arbitrary Exec arguments are unsupported, including many Snap/Flatpak/browser wrappers. Missing apps stay unavailable. Optional login start uses Mac SMAppService approval/signing eligibility or an exact-executable private GNOME XDG entry; own conflicting/symlink entries are refused. Use a stable installed/extracted path. Rebuild/move/removal needs local registration review. Every login start is paused with fresh LAN approval and Wayland consent.

Mac sleep uses IOKit as the console user; restart/shutdown stay backend-denied. Linux power advertises only logind `Can* == yes` and rechecks at dispatch with `interactive=false`; authentication challenges/inhibitors/permission changes can deny. The existing device/session ticket, expiry, single use, 30-second countdown, cancellation, final authorization and cleanup semantics are preserved. Local shell power also requires confirmation/countdown. Once dispatched the OS owns completion; Linux logind does not promise each app's unsaved-work dialog, so save work first. Never claim recording/private-bus delivery proves sleep/restart/shutdown execution.
